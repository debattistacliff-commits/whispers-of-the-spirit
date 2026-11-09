using System.Net;
using System.Net.Sockets;
using Ghostbox.Core;

namespace Ghostbox.App;

public static class AudioStream
{
    public static async Task RunAsync(string host, int port, AudioEffects effects, IProgress<string> status, IProgress<AudioAnalysis> analysis, RecordingTap recording, Func<bool> muted, CancellationToken token)
    {
        var allowed = await Dns.GetHostAddressesAsync(host.Trim(), token);
        using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        udp.Client.ReceiveBufferSize = 192000;
        using var output = new AudioOutput();
        status.Report($"Listening for Gqrx audio on UDP {port}…");
        DateTime lastReport = DateTime.MinValue;
        var analyzer = new AudioAnalyzer();
        while (true)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(3000);
            UdpReceiveResult packet;
            try { packet = await udp.ReceiveAsync(timeout.Token); }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            { status.Report($"NO INCOMING SIGNAL on UDP {port}. Check Gqrx's UDP output and destination IP."); continue; }
            if (!allowed.Contains(packet.RemoteEndPoint.Address)) continue;
            if (packet.Buffer.Length == 0 || packet.Buffer.Length % 2 != 0) continue;
            if (analyzer.Add(packet.Buffer) is { } measurement) analysis.Report(measurement);
            var processed = recording.Process(packet.Buffer, effects); output.Write(muted() ? new byte[processed.Length] : processed);
            if ((DateTime.UtcNow - lastReport).TotalSeconds >= 1)
            { status.Report("LIVE GQRX AUDIO • received UDP packets • 48 kHz mono"); lastReport = DateTime.UtcNow; }
        }
    }

    public static async Task RunDemoAsync(AudioEffects effects, IProgress<AudioAnalysis> analysis, CancellationToken token)
    {
        using var output = new AudioOutput();
        var analyzer = new AudioAnalyzer();
        var random = new Random(42);
        long sampleIndex = 0;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var pcm = new byte[1920]; // 20 ms of synthesized test audio, no microphone.
            for (int i = 0; i < 960; i++, sampleIndex++)
            {
                double t = sampleIndex / 48000.0;
                double envelope = 0.4 + 0.3 * Math.Sin(2 * Math.PI * 3 * t);
                double sample = envelope * (0.12 * Math.Sin(2 * Math.PI * 440 * t) + 0.07 * Math.Sin(2 * Math.PI * 880 * t)) + (random.NextDouble() - 0.5) * 0.025;
                System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(i * 2, 2), (short)(sample * 32767));
            }
            if (analyzer.Add(pcm) is { } measurement) analysis.Report(measurement);
            output.Write(effects.Process(pcm));
            await Task.Delay(20, token);
        }
    }
}

sealed class AudioOutput : IDisposable
{
#if ANDROID
    readonly Android.Media.AudioTrack track;
    public AudioOutput()
    {
        int size = Math.Max(19200, Android.Media.AudioTrack.GetMinBufferSize(48000, Android.Media.ChannelOut.Mono, Android.Media.Encoding.Pcm16bit));
        track = new Android.Media.AudioTrack.Builder()
            .SetAudioAttributes(new Android.Media.AudioAttributes.Builder()!.SetUsage(Android.Media.AudioUsageKind.Media)!.SetContentType(Android.Media.AudioContentType.Music)!.Build()!)!
            .SetAudioFormat(new Android.Media.AudioFormat.Builder()!.SetSampleRate(48000)!.SetChannelMask(Android.Media.ChannelOut.Mono)!.SetEncoding(Android.Media.Encoding.Pcm16bit)!.Build()!)!
            .SetBufferSizeInBytes(size)!.SetTransferMode(Android.Media.AudioTrackMode.Stream)!.Build()!;
        if (track.State != Android.Media.AudioTrackState.Initialized) { track.Dispose(); throw new IOException("Could not initialize audio playback."); }
        track.Play();
    }
    public void Write(byte[] data)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            int written = track.Write(data, offset, data.Length - offset);
            if (written <= 0) throw new IOException("Android audio playback failed.");
            offset += written;
        }
    }
    public void Dispose() { track.Pause(); track.Flush(); track.Stop(); track.Release(); track.Dispose(); }
#elif WINDOWS
    readonly NAudio.Wave.BufferedWaveProvider buffer;
    readonly NAudio.Wave.WaveOutEvent output;
    public AudioOutput()
    {
        buffer = new(new NAudio.Wave.WaveFormat(48000, 16, 1)) { BufferDuration = TimeSpan.FromMilliseconds(250), DiscardOnBufferOverflow = true };
        output = new() { DesiredLatency = 80 };
        output.Init(buffer); output.Play();
    }
    public void Write(byte[] data)
    {
        if (buffer.BufferedDuration.TotalMilliseconds > 180) buffer.ClearBuffer();
        buffer.AddSamples(data, 0, data.Length);
    }
    public void Dispose() { output.Stop(); output.Dispose(); }
#elif LINUX
    // Linux build (Ghostbox.Linux): PulseAudio / PipeWire output with the same latency and overflow rules.
    readonly Ghostbox.Linux.PulseAudio.Output output = new();
    public void Write(byte[] data) => output.Write(data);
    public void Dispose() => output.Dispose();
#endif
}
