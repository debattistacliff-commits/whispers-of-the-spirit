using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Ghostbox.Linux;

/// <summary>
/// 48 kHz, 16-bit mono audio through the PulseAudio "simple" API. Works with PulseAudio itself,
/// with PipeWire (pipewire-pulse) on current desktops and with WSLg on Windows.
/// </summary>
static class PulseAudio
{
    const string Simple = "libpulse-simple.so.0", Pulse = "libpulse.so.0";
    const int SampleS16LE = 3, Playback = 1, Record = 2;

    [StructLayout(LayoutKind.Sequential)] struct SampleSpec { public int Format; public uint Rate; public byte Channels; }
    [StructLayout(LayoutKind.Sequential)] struct BufferAttr { public uint MaxLength, TLength, PreBuf, MinReq, FragSize; }

    [DllImport(Simple)] static extern IntPtr pa_simple_new(string? server, string name, int dir, string? dev, string streamName, ref SampleSpec ss, IntPtr map, ref BufferAttr attr, out int error);
    [DllImport(Simple)] static extern int pa_simple_write(IntPtr s, byte[] data, UIntPtr bytes, out int error);
    [DllImport(Simple)] static extern int pa_simple_read(IntPtr s, byte[] data, UIntPtr bytes, out int error);
    [DllImport(Simple)] static extern int pa_simple_flush(IntPtr s, out int error);
    [DllImport(Simple)] static extern void pa_simple_free(IntPtr s);
    [DllImport(Pulse)] static extern IntPtr pa_strerror(int error);

    static string Error(int code) => Marshal.PtrToStringAnsi(pa_strerror(code)) ?? ("error " + code);

    static IntPtr Open(int direction, string stream, uint tlength, uint fragsize)
    {
        var spec = new SampleSpec { Format = SampleS16LE, Rate = 48000, Channels = 1 };
        var attr = new BufferAttr { MaxLength = uint.MaxValue, TLength = tlength, PreBuf = uint.MaxValue, MinReq = uint.MaxValue, FragSize = fragsize };
        IntPtr handle;
        int error;
        try { handle = pa_simple_new(null, "Whispers of the Spirit", direction, null, stream, ref spec, IntPtr.Zero, ref attr, out error); }
        catch (DllNotFoundException)
        { throw new IOException("PulseAudio client library (libpulse-simple.so.0) is missing. Install libpulse0 (it is on almost every Linux desktop)."); }
        if (handle == IntPtr.Zero)
            throw new IOException((direction == Playback ? "Audio output" : "Microphone") + " could not be opened: " + Error(error) + ". Check that PulseAudio or PipeWire is running.");
        return handle;
    }

    /// <summary>
    /// Speaker output with non-blocking writes and about 80 ms of server buffering, like the Windows one.
    /// Audio waits in a small queue while the sound server wakes an idle device, so the start of a
    /// review clip is not lost; only if playback falls more than two seconds behind is the backlog skipped.
    /// </summary>
    public sealed class Output : IDisposable
    {
        const int MaxQueuedBytes = 96000 * 2;
        readonly IntPtr handle = Open(Playback, "Monitor", 96000 * 80 / 1000, uint.MaxValue);
        readonly ConcurrentQueue<byte[]> queue = new();
        readonly SemaphoreSlim available = new(0);
        readonly Thread thread;
        volatile bool stopping;
        int queued;

        public Output()
        {
            thread = new Thread(Pump) { IsBackground = true, Name = "Whispers audio output" };
            thread.Start();
            if (LinuxHost.Trace) Console.Error.WriteLine($"[whispers] {DateTime.Now:HH:mm:ss.fff} audio output opened");
        }

        public void Write(byte[] data)
        {
            if (data.Length == 0 || stopping) return;
            if (Volatile.Read(ref queued) > MaxQueuedBytes)
            {
                int dropped = 0;
                while (queue.TryDequeue(out var old)) { Interlocked.Add(ref queued, -old.Length); dropped += old.Length; }
                if (LinuxHost.Trace) Console.Error.WriteLine($"[whispers] audio output behind: skipped {dropped / 96.0:F0} ms");
            }
            queue.Enqueue((byte[])data.Clone());
            Interlocked.Add(ref queued, data.Length);
            available.Release();
        }

        void Pump()
        {
            while (!stopping)
            {
                available.Wait(200);
                while (!stopping && queue.TryDequeue(out var chunk))
                {
                    Interlocked.Add(ref queued, -chunk.Length);
                    if (pa_simple_write(handle, chunk, (UIntPtr)chunk.Length, out int error) < 0)
                    { Console.Error.WriteLine("[whispers] audio output stopped: " + Error(error)); stopping = true; break; }
                }
            }
        }

        public void Dispose()
        {
            if (LinuxHost.Trace) Console.Error.WriteLine($"[whispers] {DateTime.Now:HH:mm:ss.fff} audio output closed");
            stopping = true; available.Release();
            thread.Join(1000);
            pa_simple_flush(handle, out _);
            pa_simple_free(handle);
            available.Dispose();
        }
    }

    /// <summary>Default microphone (the input chosen in the desktop's sound settings).</summary>
    public sealed class Input : IDisposable
    {
        readonly IntPtr handle = Open(Record, "Question", uint.MaxValue, 1920);
        public void Read(byte[] buffer)
        {
            if (pa_simple_read(handle, buffer, (UIntPtr)buffer.Length, out int error) < 0) throw new IOException("Microphone read failed: " + Error(error));
        }
        public void Dispose() => pa_simple_free(handle);
    }
}
