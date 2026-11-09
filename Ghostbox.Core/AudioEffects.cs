using System.Buffers.Binary;

namespace Ghostbox.Core;

public sealed record EffectSettings(bool VoiceFilter = false, double Echo = 0,
    double ChopHz = 0, bool FilterSweep = false, double Volume = 0.6, double Reverb = 0, double Drive = 0, double NoiseReduction = 0, double WhiteNoise = 0, double EchoDelayMs = 250, double EchoFeedback = .2, double SweepMix = 1, double VoiceBoostDb = 0);

// Stateful, allocation-free per sample; 48 kHz signed little-endian mono PCM.
public sealed class AudioEffects
{
    public const int SampleRate = 48000;
    readonly double[] delay = new double[SampleRate * 2];
    int delayIndex;
    readonly Random syntheticNoise = new();
    readonly double[][] room = [new double[1499], new double[2111], new double[2633]];
    readonly int[] roomIndices = new int[3];
    readonly NoiseReductionFilter noiseFilter = new();
    double previousInput, highpass, lowpass, sweepLowpass, gate = 1, phase, sweepPhase;
    public EffectSettings Settings { get; set; } = new();
    public byte[] Process(byte[] pcm)
    {
        var settings = Settings;
        double echo = Math.Clamp(settings.Echo, 0, 0.8);
        double volume = Math.Clamp(settings.Volume, 0, 1);
        double chop = Math.Clamp(settings.ChopHz, 0, 20);
        double reverb = Math.Clamp(settings.Reverb, 0, 0.8), drive = Math.Clamp(settings.Drive, 0, 1);
        const double highAlpha = 1 / (1 + 2 * Math.PI * 300 / SampleRate);
        double lowAlpha = 1 - Math.Exp(-2 * Math.PI * 3400 / SampleRate);
        for (int offset = 0; offset + 1 < pcm.Length; offset += 2)
        {
            double input = BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(offset, 2)) / 32768.0;
            input = noiseFilter.Process(input, settings.NoiseReduction);
            highpass = highAlpha * (highpass + input - previousInput); previousInput = input;
            lowpass += lowAlpha * (highpass - lowpass);
            double sample = settings.VoiceFilter ? lowpass*Math.Pow(10,Math.Clamp(settings.VoiceBoostDb,0,8)/20) : input;
            double cutoff = 500 + (1 + Math.Sin(sweepPhase)) * 1750;
            sweepPhase = (sweepPhase + 2 * Math.PI * 0.5 / SampleRate) % (2 * Math.PI);
            sweepLowpass += (1 - Math.Exp(-2 * Math.PI * cutoff / SampleRate)) * (sample - sweepLowpass);
            if (settings.FilterSweep) sample = sample*(1-Math.Clamp(settings.SweepMix,0,1))+sweepLowpass*Math.Clamp(settings.SweepMix,0,1);
            phase = (phase + chop / SampleRate) % 1;
            double target = chop == 0 || phase < 0.5 ? 1 : 0;
            gate += 0.02 * (target - gate); // Smooth edges to avoid hard clicks.
            sample *= gate;
            int delaySamples=(int)(Math.Clamp(settings.EchoDelayMs,40,1200)*SampleRate/1000);
            double delayed = delay[(delayIndex-delaySamples+delay.Length)%delay.Length];
            delay[delayIndex] = Math.Clamp(sample + delayed * Math.Clamp(settings.EchoFeedback,0,.7), -1, 1);
            delayIndex = (delayIndex + 1) % delay.Length;
            sample += delayed * echo;
            double roomMix = 0;
            for (int i = 0; i < room.Length; i++)
            {
                double tap = room[i][roomIndices[i]]; roomMix += tap / room.Length;
                room[i][roomIndices[i]] = Math.Clamp(sample * 0.4 + tap * 0.68, -1, 1);
                roomIndices[i] = (roomIndices[i] + 1) % room[i].Length;
            }
            sample += roomMix * reverb;
            if (drive > 0) sample = Math.Tanh(sample * (1 + drive * 8)) / Math.Tanh(1 + drive * 8);
            sample += (syntheticNoise.NextDouble() * 2 - 1) * Math.Clamp(settings.WhiteNoise, 0, .1);
            sample *= volume;
            short result = (short)Math.Clamp(Math.Round(sample * 32767), short.MinValue, short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(offset, 2), result);
        }
        return pcm;
    }
}
