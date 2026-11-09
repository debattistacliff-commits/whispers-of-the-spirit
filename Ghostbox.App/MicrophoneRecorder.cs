using Ghostbox.Core;

namespace Ghostbox.App;

static class MicrophoneRecorder
{
    public static string[] Inputs()
    {
#if WINDOWS
        return new[] { "Windows default microphone" }.Concat(Enumerable.Range(0, NAudio.Wave.WaveInEvent.DeviceCount).Select(i => NAudio.Wave.WaveInEvent.GetCapabilities(i).ProductName)).ToArray();
#else
        return ["Device microphone"];
#endif
    }
    public static async Task RecordAsync(string path, int device, IProgress<double> level, CancellationToken token, SilenceStop? endpoint=null)
    {
        using var writer = new PcmWaveWriter(path);
        DateTime lastReport = DateTime.MinValue;
        void Report(byte[] bytes, int count)
        {
            if ((DateTime.UtcNow - lastReport).TotalMilliseconds < 100) return;
            lastReport = DateTime.UtcNow;
            double sum = 0;
            for (int i = 0; i + 1 < count; i += 2) { double sample = System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(i, 2)) / 32768.0; sum += sample * sample; }
            level.Report(20 * Math.Log10(Math.Max(1e-6, Math.Sqrt(sum / Math.Max(1, count / 2)))));
        }
#if ANDROID
        int bufferSize = Math.Max(9600, Android.Media.AudioRecord.GetMinBufferSize(48000, Android.Media.ChannelIn.Mono, Android.Media.Encoding.Pcm16bit));
        using var mic = new Android.Media.AudioRecord(Android.Media.AudioSource.Mic, 48000, Android.Media.ChannelIn.Mono, Android.Media.Encoding.Pcm16bit, bufferSize);
        if (mic.State != Android.Media.State.Initialized) throw new IOException("Microphone could not be opened.");
        mic.StartRecording();
        try
        {
            var bytes = new byte[1920];
            while (!token.IsCancellationRequested)
            {
                int count = mic.Read(bytes, 0, bytes.Length, (int)Android.Media.AudioRecordReadOptions.NonBlocking);
                if (count < 0) throw new IOException("Microphone read failed.");
                if (count > 0) { writer.Write(bytes, count - count % 2); Report(bytes, count); if(endpoint?.Add(bytes.AsSpan(0,count-count%2))==true)break; }
                else await Task.Delay(10, token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally { mic.Stop(); mic.Release(); }
#elif WINDOWS
        using var mic = new NAudio.Wave.WaveInEvent { DeviceNumber = device, WaveFormat = new NAudio.Wave.WaveFormat(48000, 16, 1), BufferMilliseconds = 40 };
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Exception? writeError = null;
        mic.DataAvailable += (_, e) => { try { writer.Write(e.Buffer, e.BytesRecorded); Report(e.Buffer, e.BytesRecorded); if(endpoint?.Add(e.Buffer.AsSpan(0,e.BytesRecorded))==true)mic.StopRecording(); } catch (Exception ex) { writeError = ex; mic.StopRecording(); } };
        mic.RecordingStopped += (_, e) => { if (e.Exception is not null) stopped.TrySetException(e.Exception); else if (writeError is not null) stopped.TrySetException(writeError); else stopped.TrySetResult(); };
        mic.StartRecording();
        using var registration = token.Register(() => mic.StopRecording());
        await stopped.Task;
#elif LINUX
        // Linux build (Ghostbox.Linux): the desktop's default microphone through PulseAudio / PipeWire.
        using var mic = new Ghostbox.Linux.PulseAudio.Input();
        var bytes = new byte[1920];
        while (!token.IsCancellationRequested)
        {
            mic.Read(bytes);
            writer.Write(bytes, bytes.Length); Report(bytes, bytes.Length);
            if (endpoint?.Add(bytes) == true) break;
        }
        await Task.CompletedTask;
#endif
    }
}
