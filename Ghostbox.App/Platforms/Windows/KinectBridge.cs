using System.Diagnostics;

namespace Ghostbox.App;

/// <summary>
/// Windows only: runs the small Kinect helper (kinect\WhispersKinect.exe, .NET Framework, Kinect for
/// Windows Runtime 1.8) while the SLS camera uses the Kinect, and keeps its latest picture and skeleton
/// for the console page (DesktopWebHost answers /kinect/…). The helper stops when it is told to, or
/// by itself when the app closes.
/// </summary>
static class KinectBridge
{
    static readonly object Gate = new();
    static Process? helper;
    static byte[]? picture;
    static string skeleton = "{\"people\":[]}";
    static long frame;
    static string status = "";

    static string HelperPath => Path.Combine(AppContext.BaseDirectory, "kinect", "WhispersKinect.exe");
    public static bool Available => File.Exists(HelperPath);

    /// <summary>Starts the helper (or switches its picture). Returns "ready", "no-kinect" or an error text.</summary>
    public static string Start(string mode)
    {
        if (!Available) return "no-helper";
        mode = mode == "depth" ? "depth" : "ir";
        lock (Gate)
        {
            if (helper is { HasExited: false } && status == "ready")
            {
                try { helper.StandardInput.WriteLine(mode); helper.StandardInput.Flush(); } catch { }
                return "ready";
            }
            Stop();
            status = ""; picture = null; frame = 0;
            var p = new Process
            {
                StartInfo = new ProcessStartInfo(HelperPath, mode)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                },
            };
            try { p.Start(); }
            catch (Exception e) { return "error: " + e.Message; }
            helper = p;
            new Thread(() => Read(p)) { IsBackground = true, Name = "Kinect helper reader" }.Start();
        }
        // Wait for the helper to say whether the Kinect started (it needs a few seconds the first time).
        for (int i = 0; i < 150; i++)
        {
            string now;
            lock (Gate) now = status;
            if (now != "") return now;
            Thread.Sleep(50);
        }
        return "error: the Kinect did not answer";
    }

    public static void Stop()
    {
        lock (Gate)
        {
            var p = helper; helper = null;
            if (p is null) return;
            try { p.StandardInput.Close(); } catch { }
            if (!p.WaitForExit(2000)) try { p.Kill(); } catch { }
            p.Dispose();
            status = "";
        }
    }

    /// <summary>The newest picture after frame number <paramref name="after"/>, with its skeleton JSON, or null.</summary>
    public static (byte[] Jpeg, long Frame, string Skeleton)? Next(long after)
    {
        lock (Gate) return picture is not null && frame > after ? (picture, frame, skeleton) : null;
    }

    static void Read(Process p)
    {
        try
        {
            var input = p.StandardOutput.BaseStream;
            var head = new byte[5];
            while (ReadExactly(input, head, 5))
            {
                int length = BitConverter.ToInt32(head, 1);
                if (length < 0 || length > 8_000_000) break;
                var body = new byte[length];
                if (!ReadExactly(input, body, length)) break;
                lock (Gate)
                {
                    switch ((char)head[0])
                    {
                        case 'I': picture = body; frame++; break;
                        case 'S': skeleton = System.Text.Encoding.UTF8.GetString(body); break;
                        case 'T': status = System.Text.Encoding.UTF8.GetString(body); break;
                    }
                }
            }
        }
        catch { }
        lock (Gate) if (status == "") status = "error: the Kinect helper stopped";
    }

    static bool ReadExactly(Stream s, byte[] buffer, int count)
    {
        int done = 0;
        while (done < count) { int n = s.Read(buffer, done, count - done); if (n <= 0) return false; done += n; }
        return true;
    }
}
