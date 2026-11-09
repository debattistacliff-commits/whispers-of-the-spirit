namespace Ghostbox.Core;

/// <summary>Speech-band filter and soft downward expander. Does not generate or recognize speech.</summary>
public sealed class NoiseReductionFilter
{
    double previousInput, highpass, lowpass, envelope, gain = 1;
    public double Process(double input, double strength)
    {
        strength = double.IsFinite(strength) ? Math.Clamp(strength, 0, 1) : 0;
        const double highAlpha = 1 / (1 + 2 * Math.PI * 250 / 48000);
        highpass = highAlpha * (highpass + input - previousInput); previousInput = input;
        lowpass += (1 - Math.Exp(-2 * Math.PI * 3500 / 48000)) * (highpass - lowpass);
        double amplitude = Math.Abs(lowpass);
        envelope += (amplitude > envelope ? .008 : .0002) * (amplitude - envelope);
        double threshold = Math.Pow(10, (-60 + strength * 30) / 20);
        double target = Math.Clamp(envelope / threshold, 0, 1);
        target *= target;
        gain += (target > gain ? .008 : .0002) * (target - gain);
        return input * (1 - strength) + lowpass * gain * strength;
    }
}
