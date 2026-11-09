using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ghostbox.Core;

public sealed record ProcessingControl(bool Enabled, double Value);
public sealed record InvestigationMarker(double Seconds, string Note, string ProcessingNotes);
public sealed record ProcessingRecipe
{
    public Dictionary<string, ProcessingControl> Controls { get; init; } = [];
    public bool PreservePitch { get; init; } = true;
    public bool Reverse { get; init; }
    public int Channels { get; init; } = 1;
    public double? NoiseStart { get; init; }
    public double? NoiseEnd { get; init; }
    public string? NoiseSource { get; init; }
    public double Get(string key, double fallback = 0) => Controls.TryGetValue(key, out var v) && v.Enabled ? v.Value : fallback;
    public bool On(string key) => Controls.TryGetValue(key, out var v) && v.Enabled;
}
public sealed record InvestigationSession
{
    public int Version { get; init; } = 1;
    public string Name { get; init; } = "Untitled investigation";
    public string OriginalFile { get; init; } = "";
    public string OriginalSha256 { get; init; } = "";
    public double Start { get; init; }
    public double End { get; init; }
    public ProcessingRecipe Recipe { get; init; } = new();
    public List<InvestigationMarker> Markers { get; init; } = [];
}

/// <summary>Local, nondestructive FFmpeg processing. File paths are separate process arguments.</summary>
public static class Investigation
{
    static string N(double v) => v.ToString("0.######", CultureInfo.InvariantCulture);
    public static string EnginePath { get; set; } = "ffmpeg";
    public static async Task<string> HashAsync(string path)
    { using var f = File.OpenRead(path); return Convert.ToHexString(await SHA256.HashDataAsync(f)); }
    public static async Task RunAsync(IEnumerable<string> arguments, CancellationToken token)
    {
        var info = new ProcessStartInfo(EnginePath) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        foreach (var arg in arguments) info.ArgumentList.Add(arg);
        using var process = Process.Start(info) ?? throw new IOException("FFmpeg could not start.");
        var errors = process.StandardError.ReadToEndAsync(token); var output = process.StandardOutput.ReadToEndAsync(token);
        try { await process.WaitForExitAsync(token); }
        catch { if (!process.HasExited) process.Kill(entireProcessTree: true); throw; }
        string log = await errors; await output;
        if (process.ExitCode != 0) throw new IOException("Audio engine: " + log[^Math.Min(log.Length, 1600)..]);
    }
    public static async Task DecodeAsync(string input, string output, CancellationToken token) =>
        await RunAsync(["-hide_banner", "-loglevel", "error", "-nostdin", "-n", "-i", input, "-map", "0:a:0", "-vn", "-ac", "1", "-ar", "48000", "-c:a", "pcm_s16le", output], token);

    public static List<string> Filters(ProcessingRecipe p)
    {
        var f = new List<string>();
        void Add(string key, Func<double, string> expression) { if (p.On(key)) f.Add(expression(p.Get(key))); }
        double mains=p.Get("hum")<55?50:60;
        Add("hum", v => $"equalizer=f={N(mains)}:t=q:w=12:g=-{N(p.Get("humdepth",24))}");
        if (p.On("harmonics") && p.On("hum")) for (int i = 2; i <= 1+Math.Clamp((int)Math.Round(p.Get("harmonics")),0,4); i++) f.Add($"equalizer=f={N(mains * i)}:t=q:w=15:g=-{N(p.Get("humdepth",24)*.75)}");
        Add("highpass", v => $"highpass=f={N(v)}"); Add("lowpass", v => $"lowpass=f={N(v)}");
        Add("hiss", v => $"afftdn=nr={N(v)}:nf=-45:tn=1:gs=5");
        Add("clicks", v => v<=0?"anull":$"adeclick=t={N(12-v*.1)}");
        Add("voice", v => $"highpass=f=180,lowpass=f=4200,equalizer=f=1800:t=q:w=1:g={N(v)},acompressor=threshold=0.15:ratio=2:attack=15:release=180:makeup=1");
        Add("attenuate", v => $"equalizer=f=1500:t=o:w=3:g=-{N(v)}");
        Add("bass", v => $"bass=g={N(v)}"); Add("treble", v => $"treble=g={N(v)}");
        Add("compressor", v => $"acompressor=threshold=0.12:ratio={N(v)}:attack=20:release=200:makeup=1");
        Add("gain", v => $"volume={N(v)}dB");
        Add("normalize", v => $"loudnorm=I={N(v)}:TP=-1.5:LRA=11");
        Add("distortion", v => v<=0?"anull":$"asoftclip=type=tanh:threshold={N(1-v*.8)}");
        Add("chop",v=>$"tremolo=f={N(v)}:d=1");
        Add("sweep",v=>v<=0?"anull":$"aphaser=in_gain=1:out_gain=1:delay=3:decay={N(v*.4)}:speed=0.5");
        if(p.On("echo")&&p.Get("echo")>0)
        {
            double delay=p.Get("delay",250),feedback=p.Get("feedback",0),mix=p.Get("echo");int taps=feedback>0?4:1;
            f.Add($"aecho=1:1:{string.Join('|',Enumerable.Range(1,taps).Select(i=>N(delay*i)))}:{string.Join('|',Enumerable.Range(0,taps).Select(i=>N(mix*.6*Math.Pow(feedback,i))))}");
        }
        Add("reverb", v => v<=0?"anull":$"aecho=1:1:47|79|127:{N(v*.35)}|{N(v*.25)}|{N(v*.15)}");
        if (p.On("speed"))
        {
            double speed = p.Get("speed",1);
            if (p.PreservePitch) f.Add($"rubberband=tempo={N(speed)}:pitch=1:formant=preserved");
            else f.Add($"asetrate={N(48000*speed)},aresample=48000");
        }
        if (p.Reverse) f.Add("areverse");
        Add("limiter", v => $"alimiter=limit={N(Math.Pow(10,v/20))}:level=0");
        return f;
    }
    public static async Task RenderAsync(string input, string output, ProcessingRecipe p, double start, double end, bool creative, CancellationToken token)
    {
        if (!double.IsFinite(start) || !double.IsFinite(end) || start < 0 || end <= start) throw new ArgumentException("Choose a valid nonempty audio range.");
        if (Path.GetFullPath(input).Equals(Path.GetFullPath(output),StringComparison.OrdinalIgnoreCase)) throw new IOException("Original audio cannot be overwritten.");
        if(File.Exists(output))throw new IOException("Choose a new export filename; existing audio is never overwritten.");
        if (p.Channels is not (1 or 2)) throw new ArgumentException("Choose mono or stereo.");
        // Offline reverse buffers the chosen range internally. Bound it explicitly.
        if (p.Reverse && end-start > 600) throw new ArgumentException("Select at most ten minutes for reverse playback.");
        var args = new List<string> { "-hide_banner","-loglevel","error","-nostdin","-n","-i",input };
        string graph = $"[0:a]atrim=start={N(start)}:end={N(end)},asetpts=PTS-STARTPTS,aresample=48000,aformat=channel_layouts=mono[target];";
        string current="target";
        if (p.On("denoise"))
        {
            if (p.NoiseStart.HasValue && p.NoiseEnd > p.NoiseStart && p.NoiseSource is not null)
            {
                args.AddRange(["-i",p.NoiseSource]); double duration=p.NoiseEnd.Value-p.NoiseStart.Value;
                graph += $"[1:a]atrim=start={N(p.NoiseStart.Value)}:end={N(p.NoiseEnd.Value)},asetpts=PTS-STARTPTS,aresample=48000,aformat=channel_layouts=mono[noise];[noise][target]concat=n=2:v=0:a=1,asendcmd=c='0 afftdn sn start;{N(duration)} afftdn sn stop',afftdn=nr={N(p.Get("denoise"))}:nf=-50:gs=5,atrim=start={N(duration)},asetpts=PTS-STARTPTS[clean];";
            }
            else graph += $"[target]afftdn=nr={N(p.Get("denoise"))}:nf=-45:tn=1:gs=5[clean];";
            current="clean";
        }
        var filters=Filters(p);
        if (!creative && (p.On("white") || p.On("echo") || p.On("reverb") || p.On("distortion") || p.On("chop") || p.On("sweep")))
            throw new ArgumentException("Enable Include creative effects explicitly before processing white noise, echo, reverb or distortion.");
        graph += $"[{current}]" + (filters.Count==0?"anull":string.Join(',',filters)) + "[fx];";
        if (p.On("white"))
        {
            graph += $"anoisesrc=color=white:amplitude={N(p.Get("white")*.1)}:r=48000[synthetic];[fx][synthetic]amix=inputs=2:duration=first:normalize=0[out]";
        }
        else graph += "[fx]anull[out]";
        args.AddRange(["-filter_complex",graph,"-map","[out]","-ac",p.Channels.ToString(),"-ar","48000","-c:a","pcm_s16le",output]);
        try { await RunAsync(args,token); }
        catch { if (File.Exists(output)) File.Delete(output); throw; }
    }
}
