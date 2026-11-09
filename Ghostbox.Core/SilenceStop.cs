namespace Ghostbox.Core;

/// <summary>Audio-level endpointing, not speech recognition. Uses sample time, never UI timer timing.</summary>
public sealed class SilenceStop(double thresholdDb = -38, double silenceSeconds = 1.5, double waitingSeconds = 20)
{
    double elapsed, voiced, silence;
    public bool HeardActivity { get; private set; }
    public bool TimedOut { get; private set; }
    public bool Add(ReadOnlySpan<byte> pcm)
    {
        int samples=pcm.Length/2;if(samples==0)return false;
        double sum=0;
        for(int i=0;i<samples;i++){double v=System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(pcm.Slice(i*2,2))/32768d;sum+=v*v;}
        double seconds=samples/48000d,db=20*Math.Log10(Math.Max(1e-6,Math.Sqrt(sum/samples)));
        elapsed+=seconds;
        if(db>=thresholdDb){voiced+=seconds;silence=0;if(voiced>=.2)HeardActivity=true;}
        else {silence+=seconds;if(!HeardActivity)voiced=0;}
        TimedOut=!HeardActivity&&elapsed>=waitingSeconds;
        return TimedOut||(HeardActivity&&silence>=silenceSeconds);
    }
}
