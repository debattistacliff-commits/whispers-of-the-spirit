using System.Buffers.Binary;
using System.Numerics;

namespace Ghostbox.Core;

public sealed record AudioAnalysis(double RmsDb, double PeakDb, double DominantHz, double ClippedPercent, float[] Spectrum)
{
    public float[] Waveform { get; init; } = [];
}

/// <summary>Incoming, pre-effect audio measurements; FFT size 2048 at 48 kHz.</summary>
public sealed class AudioAnalyzer
{
    const int Size = 2048;
    readonly double[] samples = new double[Size];
    int position, frames;
    public AudioAnalysis? Add(byte[] pcm)
    {
        AudioAnalysis? result = null;
        for (int i = 0; i + 1 < pcm.Length; i += 2)
        {
            samples[position++] = BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i, 2)) / 32768.0;
            if (position != Size) continue;
            position = 0;
            // About eight visual updates per second, independent of UDP packet size.
            if (++frames % 3 == 0) result = Analyze();
        }
        return result;
    }
    AudioAnalysis Analyze()
    {
        double sum = 0, peak = 0;
        int clipped = 0;
        var fft = new Complex[Size];
        for (int i = 0; i < Size; i++)
        {
            double value = samples[i]; sum += value * value; peak = Math.Max(peak, Math.Abs(value));
            if (Math.Abs(value) >= 0.999) clipped++;
            fft[i] = new Complex(value * (0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (Size - 1))), 0);
        }
        for (int i = 1, j = 0; i < Size; i++)
        {
            int bit = Size >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) (fft[i], fft[j]) = (fft[j], fft[i]);
        }
        for (int length = 2; length <= Size; length <<= 1)
        {
            var root = Complex.FromPolarCoordinates(1, -2 * Math.PI / length);
            for (int i = 0; i < Size; i += length)
            {
                var w = Complex.One;
                for (int j = 0; j < length / 2; j++)
                {
                    var a = fft[i + j]; var b = fft[i + j + length / 2] * w;
                    fft[i + j] = a + b; fft[i + j + length / 2] = a - b; w *= root;
                }
            }
        }
        int dominant = 0; double strongest = 1e-9;
        for (int i = 1; i < Size / 2; i++)
            if (fft[i].Magnitude > strongest) { strongest = fft[i].Magnitude; dominant = i; }
        var spectrum = new float[32];
        // Logarithmic bands, 50 Hz to 16 kHz. Normalize a Hann-windowed sinusoid.
        for (int band = 0; band < spectrum.Length; band++)
        {
            int low = Math.Max(1, (int)(50 * Math.Pow(320, band / 32.0) * Size / 48000));
            int high = Math.Min(Size / 2, Math.Max(low + 1, (int)(50 * Math.Pow(320, (band + 1) / 32.0) * Size / 48000)));
            double magnitude = 0;
            for (int i = low; i < high; i++) magnitude = Math.Max(magnitude, fft[i].Magnitude * 4 / Size);
            spectrum[band] = (float)Math.Clamp((Db(magnitude) + 80) / 80, 0, 1);
        }
        return new(Db(Math.Sqrt(sum / Size)), Db(peak), dominant * 48000.0 / Size, clipped * 100.0 / Size, spectrum) { Waveform = samples.Select(v => (float)v).ToArray() };
    }
    static double Db(double value) => Math.Max(-96, 20 * Math.Log10(Math.Max(value, 1e-12)));
}
