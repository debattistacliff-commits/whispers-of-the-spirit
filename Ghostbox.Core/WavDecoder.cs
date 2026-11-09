using System.Buffers.Binary;
using System.Text;

namespace Ghostbox.Core;

/// <summary>
/// Converts an uncompressed WAV (8/16/24/32-bit integer or 32-bit float, any channel count and rate)
/// to the app's review format: mono, 48 kHz, 16-bit. Works on every platform without FFmpeg.
/// </summary>
public static class WavDecoder
{
    public static bool LooksLikeWav(string path)
    {
        try
        {
            using var f = File.OpenRead(path); var head = new byte[12];
            return f.Read(head) == 12 && Encoding.ASCII.GetString(head, 0, 4) == "RIFF" && Encoding.ASCII.GetString(head, 8, 4) == "WAVE";
        }
        catch { return false; }
    }

    public static void ToCanonical(string input, string output)
    {
        if (Path.GetFullPath(input).Equals(Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase)) throw new IOException("Original audio cannot be overwritten.");
        using var file = File.OpenRead(input);
        using var reader = new BinaryReader(file, Encoding.ASCII);
        if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") throw new IOException("Not a WAV file.");
        reader.ReadUInt32();
        if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") throw new IOException("Not a WAV file.");
        int format = 0, channels = 0, rate = 0, bits = 0; long dataOffset = -1, dataLength = 0;
        while (file.Position + 8 <= file.Length)
        {
            string chunk = Encoding.ASCII.GetString(reader.ReadBytes(4)); long size = reader.ReadUInt32(); long next = file.Position + size + (size % 2);
            if (chunk == "fmt ")
            {
                format = reader.ReadUInt16(); channels = reader.ReadUInt16(); rate = reader.ReadInt32(); reader.ReadInt32(); reader.ReadUInt16(); bits = reader.ReadUInt16();
                if (format == 0xFFFE && size >= 40) { reader.ReadUInt16(); reader.ReadUInt16(); reader.ReadUInt32(); format = reader.ReadUInt16(); }
            }
            else if (chunk == "data") { dataOffset = file.Position; dataLength = Math.Min(size, file.Length - file.Position); }
            file.Position = Math.Min(next, file.Length);
        }
        bool pcm = format == 1 && bits is 8 or 16 or 24 or 32, floating = format == 3 && bits == 32;
        if (dataOffset < 0 || channels is < 1 or > 32 || rate is < 1000 or > 768000 || !(pcm || floating))
            throw new IOException("Unsupported WAV. Use uncompressed PCM or 32-bit float WAV (MP3 and FLAC need FFmpeg).");

        int bytesPerSample = bits / 8, frameBytes = bytesPerSample * channels;
        long frames = dataLength / frameBytes;
        file.Position = dataOffset;
        var mono = new float[frames];
        var buffer = new byte[frameBytes * 4096];
        for (long done = 0; done < frames;)
        {
            int want = (int)Math.Min(4096, frames - done), read = 0;
            while (read < want * frameBytes) { int n = file.Read(buffer, read, want * frameBytes - read); if (n == 0) break; read += n; }
            int got = read / frameBytes; if (got == 0) break;
            for (int f = 0; f < got; f++)
            {
                double sum = 0;
                for (int c = 0; c < channels; c++)
                {
                    var s = buffer.AsSpan(f * frameBytes + c * bytesPerSample, bytesPerSample);
                    sum += floating ? BinaryPrimitives.ReadSingleLittleEndian(s) : bits switch
                    {
                        8 => (s[0] - 128) / 128.0,
                        16 => BinaryPrimitives.ReadInt16LittleEndian(s) / 32768.0,
                        24 => ((s[0] | (s[1] << 8) | (s[2] << 16)) << 8 >> 8) / 8388608.0,
                        _ => BinaryPrimitives.ReadInt32LittleEndian(s) / 2147483648.0,
                    };
                }
                mono[done + f] = (float)(sum / channels);
            }
            done += got;
        }

        using var writer = new PcmWaveWriter(output);
        long outFrames = (long)Math.Floor(frames * 48000.0 / rate);
        var chunkOut = new byte[2 * 4800];
        for (long o = 0; o < outFrames;)
        {
            int n = (int)Math.Min(4800, outFrames - o);
            for (int i = 0; i < n; i++)
            {
                double pos = (o + i) * (double)rate / 48000; long a = (long)pos; double t = pos - a;
                double v = a + 1 < mono.Length ? mono[a] * (1 - t) + mono[a + 1] * t : mono[Math.Min(a, mono.Length - 1)];
                BinaryPrimitives.WriteInt16LittleEndian(chunkOut.AsSpan(i * 2, 2), (short)Math.Clamp(Math.Round(v * 32767), short.MinValue, short.MaxValue));
            }
            writer.Write(chunkOut, n * 2); o += n;
        }
    }
}
