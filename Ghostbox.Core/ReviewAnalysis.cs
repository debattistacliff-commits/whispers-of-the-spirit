using System.Buffers.Binary;

namespace Ghostbox.Core;

/// <summary>
/// Time-aligned analysis lanes for one view of a recording: waveform envelope, log-frequency
/// heat map (spectrogram), speech-like activity and pitch. Measurements only; nothing here
/// identifies a speaker or the origin of a sound.
/// </summary>
public sealed record ReviewLanes(double Start, double End, int Columns, int Bins, double MinHz, double MaxHz,
    sbyte[] WaveMin, sbyte[] WaveMax, byte[] Heat, byte[] Voice, ushort[] PitchTenthsHz, byte[] Level);

public static class ReviewAnalysis
{
    public const int Bins = 96, SampleRate = 48000;
    public const double MinHz = 50, MaxHz = 12000;
    const int FftSize = 2048;
    // Heat map maps -100 dBFS .. -10 dBFS onto 0..255 so colours stay comparable while zooming.
    const double HeatFloorDb = -100, HeatRangeDb = 90;
    static readonly double[] Hann = Enumerable.Range(0, FftSize).Select(i => 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (FftSize - 1))).ToArray();
    static readonly double WindowGain = Hann.Sum() / 2;

    public static short[] ReadPcm(string wavPath)
    {
        var wave = ConversationFile.Inspect(wavPath);
        using var input = File.OpenRead(wavPath);
        input.Position = wave.Offset;
        var bytes = new byte[wave.Length - wave.Length % 2];
        input.ReadExactly(bytes);
        var samples = new short[bytes.Length / 2];
        for (int i = 0; i < samples.Length; i++) samples[i] = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(i * 2, 2));
        return samples;
    }

    /// <summary>Voice-band level that 20% of the recording stays below; used as the speech-activity reference.</summary>
    public static double NoiseFloorDb(short[] samples)
    {
        if (samples.Length < FftSize) return -90;
        int frames = Math.Clamp(samples.Length / FftSize, 1, 600);
        var levels = new double[frames];
        var re = new double[FftSize]; var im = new double[FftSize];
        for (int f = 0; f < frames; f++)
        {
            long centre = (long)((f + .5) * samples.Length / frames);
            Spectrum(samples, centre, re, im);
            levels[f] = BandDb(re, im, 250, 3500);
        }
        Array.Sort(levels);
        return levels[(int)(levels.Length * .2)];
    }

    public static ReviewLanes Analyze(short[] samples, double start, double end, int columns, double noiseFloorDb, CancellationToken token = default)
    {
        double duration = samples.Length / (double)SampleRate;
        start = Math.Clamp(start, 0, duration); end = Math.Clamp(end, start, duration);
        columns = Math.Clamp(columns, 16, 4000);
        var waveMin = new sbyte[columns]; var waveMax = new sbyte[columns];
        var heat = new byte[columns * Bins]; var voice = new byte[columns]; var pitch = new ushort[columns]; var level = new byte[columns];
        if (end - start <= 0 || samples.Length == 0)
            return new(start, end, columns, Bins, MinHz, MaxHz, waveMin, waveMax, heat, voice, pitch, level);

        var edges = Enumerable.Range(0, Bins + 1).Select(k => MinHz * Math.Pow(MaxHz / MinHz, k / (double)Bins)).ToArray();
        var re = new double[FftSize]; var im = new double[FftSize]; var power = new double[FftSize / 2];
        double span = (end - start) / columns;
        for (int c = 0; c < columns; c++)
        {
            token.ThrowIfCancellationRequested();
            long first = (long)((start + c * span) * SampleRate), last = Math.Max(first + 1, (long)((start + (c + 1) * span) * SampleRate));
            first = Math.Clamp(first, 0, samples.Length - 1); last = Math.Clamp(last, first + 1, samples.Length);
            short lo = short.MaxValue, hi = short.MinValue; double sum = 0;
            int stride = (int)Math.Max(1, (last - first) / 20000);
            long counted = 0;
            for (long i = first; i < last; i += stride) { short v = samples[i]; if (v < lo) lo = v; if (v > hi) hi = v; sum += (double)v * v; counted++; }
            waveMin[c] = (sbyte)Math.Clamp(Math.Round(lo / 258.0), -127, 127);
            waveMax[c] = (sbyte)Math.Clamp(Math.Round(hi / 258.0), -127, 127);
            double rmsDb = 20 * Math.Log10(Math.Max(1e-9, Math.Sqrt(sum / Math.Max(1, counted)) / 32768));
            level[c] = (byte)Math.Clamp((rmsDb + 80) / 80 * 255, 0, 255);

            // Average up to three windows inside wide columns so short sounds are not skipped.
            int windows = last - first > FftSize * 2 ? 3 : 1;
            Array.Clear(power);
            for (int w = 0; w < windows; w++)
            {
                long centre = first + (long)((w + .5) * (last - first) / windows);
                Spectrum(samples, centre, re, im);
                for (int k = 0; k < power.Length; k++) power[k] += (re[k] * re[k] + im[k] * im[k]) / windows;
            }
            for (int b = 0; b < Bins; b++)
            {
                double lowBin = edges[b] * FftSize / SampleRate, highBin = edges[b + 1] * FftSize / SampleRate;
                double strongest;
                if (highBin - lowBin < 1)
                {   // Narrow low-frequency bins: interpolate between neighbouring FFT bins.
                    double centreBin = (lowBin + highBin) / 2; int k = (int)centreBin; double frac = centreBin - k;
                    strongest = power[Math.Min(k, power.Length - 1)] * (1 - frac) + power[Math.Min(k + 1, power.Length - 1)] * frac;
                }
                else
                {
                    strongest = 0;
                    for (int k = (int)Math.Ceiling(lowBin); k < Math.Min(power.Length, (int)Math.Ceiling(highBin)); k++) strongest = Math.Max(strongest, power[k]);
                }
                double db = 10 * Math.Log10(Math.Max(1e-20, strongest)) - 20 * Math.Log10(WindowGain);
                heat[c * Bins + b] = (byte)Math.Clamp((db - HeatFloorDb) / HeatRangeDb * 255, 0, 255);
            }
            double voiceDb = PowerBandDb(power, 250, 3500), allDb = PowerBandDb(power, 50, 12000);
            long middle = (first + last) / 2;
            var (hz, confidence) = Pitch(samples, middle);
            double loud = Math.Clamp((voiceDb - noiseFloorDb - 4) / 16, 0, 1);
            double focus = Math.Clamp(1 + (voiceDb - allDb) / 10, 0, 1); // most energy inside the speech band
            double score = loud * (0.45 * focus + 0.55 * confidence);
            voice[c] = (byte)Math.Clamp(score * 255, 0, 255);
            pitch[c] = loud > 0.15 && confidence > 0.55 ? (ushort)Math.Round(hz * 10) : (ushort)0;
        }
        return new(start, end, columns, Bins, MinHz, MaxHz, waveMin, waveMax, heat, voice, pitch, level);
    }

    static void Spectrum(short[] samples, long centre, double[] re, double[] im)
    {
        long offset = centre - FftSize / 2;
        for (int i = 0; i < FftSize; i++)
        {
            long s = offset + i;
            re[i] = s >= 0 && s < samples.Length ? samples[s] / 32768.0 * Hann[i] : 0;
            im[i] = 0;
        }
        Fft(re, im);
    }

    static double BandDb(double[] re, double[] im, double lowHz, double highHz)
    {
        double sum = 0;
        for (int k = (int)(lowHz * FftSize / SampleRate); k <= (int)(highHz * FftSize / SampleRate); k++) sum += re[k] * re[k] + im[k] * im[k];
        return 10 * Math.Log10(Math.Max(1e-20, sum)) - 20 * Math.Log10(WindowGain);
    }
    static double PowerBandDb(double[] power, double lowHz, double highHz)
    {
        double sum = 0;
        for (int k = (int)(lowHz * FftSize / SampleRate); k <= Math.Min(power.Length - 1, (int)(highHz * FftSize / SampleRate)); k++) sum += power[k];
        return 10 * Math.Log10(Math.Max(1e-20, sum)) - 20 * Math.Log10(WindowGain);
    }

    /// <summary>YIN pitch estimate (60–600 Hz) on a 40 ms window, decimated to 12 kHz.</summary>
    public static (double hz, double confidence) Pitch(short[] samples, long centre)
    {
        const int decimate = 4, rate = SampleRate / decimate, size = 480, minLag = rate / 600, maxLag = rate / 60;
        var x = new double[size + maxLag];
        long begin = centre - (long)x.Length * decimate / 2;
        double energy = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double acc = 0;
            for (int j = 0; j < decimate; j++) { long s = begin + (long)i * decimate + j; acc += s >= 0 && s < samples.Length ? samples[s] : 0; }
            x[i] = acc / decimate / 32768.0; energy += x[i] * x[i];
        }
        if (energy / x.Length < 1e-7) return (0, 0);
        var d = new double[maxLag + 1];
        for (int lag = 1; lag <= maxLag; lag++)
        {
            double sum = 0;
            for (int i = 0; i < size; i++) { double diff = x[i] - x[i + lag]; sum += diff * diff; }
            d[lag] = sum;
        }
        double running = 0; var cmnd = new double[maxLag + 1]; cmnd[0] = 1;
        for (int lag = 1; lag <= maxLag; lag++) { running += d[lag]; cmnd[lag] = running > 0 ? d[lag] * lag / running : 1; }
        int best = -1;
        for (int lag = minLag; lag <= maxLag; lag++)
            if (cmnd[lag] < 0.2) { while (lag + 1 <= maxLag && cmnd[lag + 1] < cmnd[lag]) lag++; best = lag; break; }
        if (best < 0) return (0, 0);
        double shift = 0;
        if (best > minLag && best < maxLag)
        {
            double a = cmnd[best - 1], b = cmnd[best], c = cmnd[best + 1], denom = a - 2 * b + c;
            if (Math.Abs(denom) > 1e-12) shift = Math.Clamp(0.5 * (a - c) / denom, -1, 1);
        }
        return (rate / (best + shift), Math.Clamp(1 - cmnd[best], 0, 1));
    }

    static void Fft(double[] re, double[] im)
    {
        int n = re.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i < j) { (re[i], re[j]) = (re[j], re[i]); (im[i], im[j]) = (im[j], im[i]); }
        }
        for (int length = 2; length <= n; length <<= 1)
        {
            double angle = -2 * Math.PI / length, wr = Math.Cos(angle), wi = Math.Sin(angle);
            for (int i = 0; i < n; i += length)
            {
                double cr = 1, ci = 0;
                for (int j = 0; j < length / 2; j++)
                {
                    int a = i + j, b = a + length / 2;
                    double tr = re[b] * cr - im[b] * ci, ti = re[b] * ci + im[b] * cr;
                    re[b] = re[a] - tr; im[b] = im[a] - ti; re[a] += tr; im[a] += ti;
                    double next = cr * wr - ci * wi; ci = cr * wi + ci * wr; cr = next;
                }
            }
        }
    }
}
