using System.Buffers.Binary;

namespace Ghostbox.Core;

/// <summary>Streaming tape-style playback speed for 48 kHz mono PCM. Pitch changes with speed.</summary>
public sealed class PlaybackRate
{
    short? previous;
    double position;
    public byte[] Process(byte[] pcm, double speed)
    {
        if (!double.IsFinite(speed) || speed is < .1 or > 1) throw new ArgumentOutOfRangeException(nameof(speed));
        if (pcm.Length % 2 != 0) throw new ArgumentException("Whole PCM samples required.");
        int count = pcm.Length / 2;
        if (count == 0) return [];
        var samples = new short[count + (previous.HasValue ? 1 : 0)];
        int first = previous.HasValue ? 1 : 0;
        if (previous.HasValue) samples[0] = previous.Value;
        for (int i = 0; i < count; i++) samples[first + i] = BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i * 2, 2));
        var result = new byte[(int)Math.Ceiling(samples.Length / speed) * 2]; int written = 0;
        while (position < samples.Length - 1)
        {
            int low = (int)position; double fraction = position - low;
            short value = (short)Math.Clamp(Math.Round(samples[low] + fraction * (samples[low + 1] - samples[low])), short.MinValue, short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(result.AsSpan(written, 2), value); written += 2; position += speed;
        }
        position -= samples.Length - 1; previous = samples[^1];
        return result[..written];
    }
    public byte[] Flush(double speed)
    {
        if (!double.IsFinite(speed) || speed is < .1 or > 1) throw new ArgumentOutOfRangeException(nameof(speed));
        if (!previous.HasValue) return [];
        var result = new byte[(int)Math.Ceiling(1 / speed) * 2]; int written = 0;
        while (position < 1) { BinaryPrimitives.WriteInt16LittleEndian(result.AsSpan(written, 2), previous.Value); written += 2; position += speed; }
        previous = null; position = 0; return result[..written];
    }
}
