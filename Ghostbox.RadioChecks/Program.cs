using Ghostbox.Core;
using Ghostbox.App;

if (args.FirstOrDefault() == "--remote")
{
    var receivers = await RemoteReceivers.FindAsync(default);
    var receiver = args.Length > 1 ? receivers.First(r => r.Url.TrimEnd('/') == args[1].TrimEnd('/')) : receivers[0];
    using var done = new CancellationTokenSource(TimeSpan.FromSeconds(35));
    long total = 0; double energy = 0; int tunes = 0;
    try
    {
        await RemoteSdr.RunAsync(receiver, new ScanSettings(MinimumHz: 531000, MaximumHz: 576000, StepHz: 9000, DwellMs: 250), "AM", pcm =>
        {
            total += pcm.Length;
            for (int i = 0; i < pcm.Length; i += 2) { double sample = BitConverter.ToInt16(pcm, i) / 32768.0; energy += sample * sample; }
            if (total >= 192000 && tunes > 2) done.Cancel();
        }, new TuneProgress(() => tunes++), new ConsoleProgress(), done.Token);
    }
    catch (OperationCanceledException) when (done.IsCancellationRequested) { }
    if (total < 192000 || tunes < 3 || energy == 0) throw new Exception($"Remote RF test failed: bytes={total}, tunes={tunes}");
    Console.WriteLine($"PASS REMOTE RF: {total} bytes of antenna audio; {tunes} frequency commands; RMS {20*Math.Log10(Math.Sqrt(energy/(total/2.0))):F1} dBFS");
    return;
}

if (args.FirstOrDefault() == "--directory")
{
    foreach (string language in new[] { "mt", "it", "en" })
    {
        var stations = await InternetStations.FindAsync(language, default);
        if (stations.Length == 0) throw new Exception("No station results for " + language);
        Console.WriteLine($"PASS directory {language}: {stations.Length} real stream URLs; first: {stations[0].Name}");
    }
    return;
}

string url = args.FirstOrDefault() ?? "https://s4.radio.co/sf3aa4c25a/listen";
using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(40));
long bytes = 0; double squareSum = 0;
try
{
    await InternetRadioPlayer.RunAsync([new("Live network test", url, "", "")], ScanPattern.Forward, 3, false, pcm =>
    {
        bytes += pcm.Length;
        for (int i = 0; i < pcm.Length; i += 2) { double value = BitConverter.ToInt16(pcm, i) / 32768.0; squareSum += value * value; }
        if (bytes >= 192000) cancel.Cancel();
    }, new ConsoleProgress(), cancel.Token);
}
catch (OperationCanceledException) when (cancel.IsCancellationRequested) { }
if (bytes < 192000 || squareSum <= 0) throw new Exception("No non-silent decoded live audio received.");
Console.WriteLine($"PASS: {bytes} bytes of live 48 kHz mono PCM decoded; RMS {20 * Math.Log10(Math.Sqrt(squareSum / (bytes / 2.0))):F1} dBFS");

sealed class ConsoleProgress : IProgress<string> { public void Report(string value) => Console.WriteLine(value); }
sealed class TuneProgress(Action tick) : IProgress<long> { public void Report(long value) { tick(); Console.WriteLine("Tune Hz: " + value); } }
