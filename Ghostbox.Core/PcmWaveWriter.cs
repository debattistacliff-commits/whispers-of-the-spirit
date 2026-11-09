using System.Text;

namespace Ghostbox.Core;

public sealed class PcmWaveWriter : IDisposable
{
    readonly FileStream stream;
    readonly int sampleRate;
    public long DataBytes { get; private set; }
    bool disposed;
    public PcmWaveWriter(string path, int sampleRate = 48000)
    {
        this.sampleRate = sampleRate;
        stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
        WriteHeader();
    }
    public void Write(byte[] pcm, int length = -1)
    {
        if (disposed) throw new ObjectDisposedException(nameof(PcmWaveWriter));
        if (length < 0) length = pcm.Length;
        if (length > pcm.Length || length % 2 != 0) throw new ArgumentException("PCM must contain whole 16-bit mono samples.");
        if (DataBytes + length > uint.MaxValue - 36) throw new IOException("WAV file size limit reached.");
        stream.Write(pcm, 0, length); DataBytes += length;
    }
    void WriteHeader()
    {
        stream.Position = 0;
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write((uint)(36 + DataBytes));
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(sampleRate); writer.Write(sampleRate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write((uint)DataBytes);
    }
    public void Dispose()
    {
        if (disposed) return;
        try { WriteHeader(); } finally { disposed = true; stream.Dispose(); }
    }
}

public sealed class RecordingTap
{
    readonly object gate = new();
    PcmWaveWriter? raw, processed;
    public long DataBytes { get { lock (gate) return raw?.DataBytes ?? 0; } }
    public void Begin(string rawPath, string processedPath, bool writeProcessed = true)
    {
        lock (gate)
        {
            if (raw is not null) throw new InvalidOperationException("Already recording.");
            var newRaw = new PcmWaveWriter(rawPath);
            try { processed = writeProcessed ? new PcmWaveWriter(processedPath) : null; raw = newRaw; }
            catch { newRaw.Dispose(); throw; }
        }
    }
    public long End()
    {
        lock (gate)
        {
            long bytes = raw?.DataBytes ?? 0;
            try { raw?.Dispose(); } finally { try { processed?.Dispose(); } finally { raw = null; processed = null; } }
            return bytes;
        }
    }
    public byte[] Process(byte[] pcm, AudioEffects effects)
    {
        lock (gate)
        {
            raw?.Write(pcm);
            var result = effects.Process(pcm);
            processed?.Write(result);
            return result;
        }
    }
}
