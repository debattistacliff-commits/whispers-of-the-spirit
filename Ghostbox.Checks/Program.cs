using Ghostbox.Core;
using System.Net;
using System.Net.Sockets;

void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
Check(ScanSettings.ParseFrequency("87.5MHz") == 87500000, "frequency suffix");
Check(ScanSettings.ParseFrequency("100k") == 100000, "step suffix");
foreach (string bad in new[] { "NaN", "0", "-5", "0.5Hz", "999999999999999999999999999999999999999" })
{
    bool rejected = false;
    try { ScanSettings.ParseFrequency(bad); } catch (ArgumentException) { rejected = true; }
    Check(rejected, "reject " + bad);
}
foreach (var pattern in Enum.GetValues<ScanPattern>())
{
    var settings = new ScanSettings(MinimumHz: 100, MaximumHz: 145, StepHz: 10, Pattern: pattern);
    var sequence = new ScanSequence(settings, new Random(7));
    var values = Enumerable.Range(0, 100).Select(_ => sequence.Next()).ToArray();
    Check(values.All(v => v >= 100 && v <= 140 && v % 10 == 0), pattern + " grid and boundaries");
    if (pattern == ScanPattern.Bounce) Check(values.Zip(values.Skip(1)).All(v => Math.Abs(v.First - v.Second) == 10), "bounce reflects without duplicate endpoints");
    if (pattern == ScanPattern.Forward) Check(values.Zip(values.Skip(1)).All(v => v.Second == (v.First == 140 ? 100 : v.First + 10)), "forward wrap");
    if (pattern == ScanPattern.Backward) Check(values.Zip(values.Skip(1)).All(v => v.Second == (v.First == 100 ? 140 : v.First - 10)), "backward wrap");
    var single = new ScanSequence(settings with { MaximumHz = 100 }, new Random(4));
    Check(Enumerable.Range(0, 10).All(_ => single.Next() == 100), pattern + " single point");
}
var starts = new HashSet<long>();
var directions = new HashSet<long>();
for (int seed = 0; seed < 100; seed++)
{
    var s = new ScanSequence(new ScanSettings(MinimumHz: 100, MaximumHz: 1000, StepHz: 10, Pattern: ScanPattern.Bounce), new Random(seed));
    long a = s.Next(), b = s.Next(); starts.Add(a); directions.Add(b - a);
}
Check(starts.Count > 20 && directions.SetEquals([-10, 10]), "random starts and bounce directions");

async Task Serve(Func<StreamReader, StreamWriter, Task> handler, Func<int, Task> client)
{
    var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    int port = ((IPEndPoint)listener.LocalEndpoint).Port;
    var task = Task.Run(async () =>
    {
        using var tcp = await listener.AcceptTcpClientAsync();
        using var reader = new StreamReader(tcp.GetStream());
        using var writer = new StreamWriter(tcp.GetStream()) { AutoFlush = true, NewLine = "\n" };
        await handler(reader, writer);
    });
    try { await client(port); await task.WaitAsync(TimeSpan.FromSeconds(5)); }
    finally { listener.Stop(); }
}
if (!args.Contains("--dsp-only")) {
await Serve(async (r, w) =>
{
    Check(await r.ReadLineAsync() == "M WFM", "mode command");
    await w.WriteAsync("RPR"); await w.FlushAsync(); await Task.Delay(20); await w.WriteAsync("T 0\n"); await w.FlushAsync();
    Check(await r.ReadLineAsync() == "F 100000000", "frequency command");
    await w.WriteLineAsync("RPRT 0");
}, async port =>
{
    using var cancel = new CancellationTokenSource(5000);
    var progress = new CancelProgress(cancel);
    try { await Scanner.RunAsync(new ScanSettings(Port: port, MinimumHz: 100000000, MaximumHz: 100000000), progress, cancel.Token); }
    catch (OperationCanceledException) { }
    Check(progress.Frequency == 100000000, "acknowledged tune and cancellation");
});
await Serve(async (r, w) => { await r.ReadLineAsync(); await w.WriteLineAsync("RPRT 1"); }, async port =>
{
    await using var remote = new GqrxConnection(1000);
    await remote.ConnectAsync("127.0.0.1", port, default);
    bool rejected = false;
    try { await remote.CommandAsync("M WFM", default); } catch (IOException) { rejected = true; }
    Check(rejected, "rejected command stops");
});
await Serve(async (r, w) => { await r.ReadLineAsync(); await Task.Delay(400); }, async port =>
{
    await using var remote = new GqrxConnection(100);
    await remote.ConnectAsync("127.0.0.1", port, default);
    bool timedOut = false;
    try { await remote.CommandAsync("M WFM", default); } catch (TimeoutException) { timedOut = true; }
    Check(timedOut, "unresponsive server times out");
});
}
var dry = new AudioEffects { Settings = new(Volume: 1) };
var tone = new byte[48000 * 2];
for (int i = 0; i < 48000; i++) System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(tone.AsSpan(i * 2, 2), (short)(12000 * Math.Sin(2 * Math.PI * 1000 * i / 48000)));
var original = (byte[])tone.Clone();
dry.Process(tone);
Check(Enumerable.Range(0, 48000).All(i => Math.Abs(System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(tone.AsSpan(i * 2, 2)) - System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(original.AsSpan(i * 2, 2))) <= 1), "dry audio passes through");
var echoFx = new AudioEffects { Settings = new(Echo: 0.5, Volume: 1) };
var impulse = new byte[48000 * 2];
System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(impulse.AsSpan(0, 2), 16000);
echoFx.Process(impulse);
Check(System.Buffers.Binary.BinaryPrimitives.ReadInt16LittleEndian(impulse.AsSpan(12000 * 2, 2)) > 7000, "echo returns after 250 ms");
var muted = new AudioEffects { Settings = new(VoiceFilter: true, Echo: 0.8, ChopHz: 6, FilterSweep: true, Volume: 0) };
muted.Process(original);
Check(original.All(v => v == 0), "volume zero mutes all effects");
var analyzer = new AudioAnalyzer();
AudioAnalysis? measurement = null;
var testTone = new byte[2048 * 3 * 2];
for (int i = 0; i < testTone.Length / 2; i++) System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(testTone.AsSpan(i * 2, 2), (short)(16384 * Math.Sin(2 * Math.PI * 1000 * i / 48000)));
measurement = analyzer.Add(testTone);
Check(measurement is not null && Math.Abs(measurement.DominantHz - 1000) < 25 && Math.Abs(measurement.RmsDb - (-9.03)) < 0.2, "FFT finds 1 kHz and correct RMS");
Check(measurement!.ClippedPercent == 0 && measurement.Spectrum.Length == 32, "spectrum bands and no false clipping");
Check(measurement.Waveform.Length == 2048 && Math.Abs(measurement.Waveform.Max() - .5) < .001 && Math.Abs(measurement.Waveform.Min() + .5) < .001, "waveform preserves signed full-scale sample amplitudes");
var silence = new AudioAnalyzer().Add(new byte[2048 * 3 * 2]);
Check(silence is { RmsDb: -96, PeakDb: -96, DominantHz: 0 }, "silent input has finite floor and no dominant tone");
Check(silence!.Waveform.All(v => v == 0) && silence.Spectrum.All(v => v == 0), "silence produces an empty heat map and flat waveform");
string waveFolder = Path.Combine(Environment.GetEnvironmentVariable("GHOSTBOX_CHECK_DIR") ?? Path.GetTempPath(), "ghostbox-check-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(waveFolder);
string rawPath = Path.Combine(waveFolder, "raw.wav"), fxPath = Path.Combine(waveFolder, "processed.wav");
var tap = new RecordingTap();
tap.Begin(rawPath, fxPath);
byte[] knownPcm = [0, 64, 0, 32, 0, 0, 0, 128];
var before = (byte[])knownPcm.Clone();
tap.Process(knownPcm, new AudioEffects { Settings = new(Volume: 0) });
Check(tap.End() == 8, "recording counts received PCM bytes");
var wavBytes = File.ReadAllBytes(rawPath);
Check(wavBytes.Length == 52 && System.Text.Encoding.ASCII.GetString(wavBytes, 0, 4) == "RIFF" && BitConverter.ToInt32(wavBytes, 40) == 8 && wavBytes[44..].SequenceEqual(before), "WAV header finalized and original audio preserved");
Check(File.ReadAllBytes(fxPath)[44..].All(b => b == 0), "processed recording kept separate from original");
Check(tap.End() == 0, "recording stop is idempotent");
string joined = Path.Combine(waveFolder, "conversation.wav");
var segments = new[] { new ConversationSegment(rawPath, "MIC", DateTime.UtcNow), new ConversationSegment(fxPath, "RADIO", DateTime.UtcNow) };
ConversationFile.Save(joined, segments);
var joinedData = ConversationFile.Inspect(joined);
var joinedBytes = File.ReadAllBytes(joined);
Check(joinedData.Length == 16 && joinedBytes.AsSpan((int)joinedData.Offset, 16).SequenceEqual(before.Concat(new byte[8]).ToArray()), "conversation preserves source PCM in order and excludes metadata from playback");
Check(BitConverter.ToUInt32(joinedBytes, 4) == joinedBytes.Length - 8 && System.Text.Encoding.UTF8.GetString(joinedBytes).Contains("RADIO"), "conversation RIFF size and embedded source markers");
ConversationFile.Save(joined, segments.Take(1).ToArray());
Check(ConversationFile.Inspect(joined).Length == 8, "conversation rebuild replaces rather than duplicates audio");
var savedConversation = File.ReadAllBytes(joined);
try { ConversationFile.Save(joined, [new ConversationSegment(joined + ".missing", "BAD", DateTime.UtcNow)]); } catch (IOException) { }
Check(File.ReadAllBytes(joined).SequenceEqual(savedConversation) && File.ReadAllBytes(rawPath).SequenceEqual(wavBytes), "failed save preserves conversation and original clips");
foreach (var file in Directory.GetFiles(waveFolder)) File.Delete(file);
Directory.Delete(waveFolder);
var remoteTone = new byte[12000 * 2];
for (int i = 0; i < 12000; i++) System.Buffers.Binary.BinaryPrimitives.WriteInt16BigEndian(remoteTone.AsSpan(i*2, 2), (short)(16000 * Math.Sin(2 * Math.PI * 1000 * i / 12000)));
var upsampled = new LinearPcmResampler().ProcessBigEndian(remoteTone, 12000);
var upsampledAnalysis = new AudioAnalyzer().Add(upsampled);
Check(upsampled.Length == 96000 && upsampledAnalysis is not null && Math.Abs(upsampledAnalysis.DominantHz - 1000) < 25, "remote antenna PCM converts endian and resamples 12 kHz to 48 kHz");
var allEffects = new AudioEffects { Settings = new(Echo: 0.7, Reverb: 0.7, Drive: 0.8, Volume: 0) };
allEffects.Process(upsampled);
Check(upsampled.All(v => v == 0), "reverb and drive respect master mute");
foreach (double speed in new[] { 1.0, .5, .25 })
{
    var converter = new PlaybackRate(); var playback = new List<byte>();
    var inputTone = new byte[48000 * 2];
    for (int i = 0; i < 48000; i++) System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(inputTone.AsSpan(i * 2, 2), (short)(16000 * Math.Sin(2 * Math.PI * 1000 * i / 48000)));
    var unchanged = (byte[])inputTone.Clone();
    for (int offset = 0; offset < inputTone.Length; offset += 1920) playback.AddRange(converter.Process(inputTone[offset..Math.Min(inputTone.Length, offset + 1920)], speed));
    playback.AddRange(converter.Flush(speed));
    Check(Math.Abs(playback.Count - inputTone.Length / speed) <= 2, $"{speed}x playback has correct duration across chunk boundaries");
    Check(inputTone.SequenceEqual(unchanged), "slow motion leaves saved input unchanged");
    var measured = new AudioAnalyzer().Add(playback.ToArray());
    Check(measured is not null && Math.Abs(measured.DominantHz - 1000 * speed) < 25, $"{speed}x tape-style playback lowers frequency correctly");
    if (speed == 1) Check(playback.SequenceEqual(inputTone), "normal replay is sample exact including final sample");
}
double FilterRms(double hz, double amplitude, double strength)
{
    var filter = new NoiseReductionFilter(); double sum = 0;
    for (int i = 0; i < 48000; i++)
    {
        double output = filter.Process(amplitude * Math.Sin(2 * Math.PI * hz * i / 48000), strength);
        if (i >= 24000) sum += output * output;
    }
    return Math.Sqrt(sum / 24000);
}
double speechLevel = FilterRms(1000, .2, 1);
Check(speechLevel > .1, "noise filter retains a strong speech-band tone");
Check(FilterRms(12000, .2, 1) < speechLevel * .4 && FilterRms(40, .2, 1) < speechLevel * .25, "noise filter attenuates high hiss and low rumble bands");
Check(FilterRms(1000, .001, 1) < FilterRms(1000, .001, 0) * .01, "noise filter suppresses quiet background between louder sounds");
var bypassFilter = new NoiseReductionFilter();
Check(Enumerable.Range(0, 1000).All(i => { double input = Math.Sin(i * .07); return bypassFilter.Process(input, 0) == input; }), "noise filter off is exact bypass");
var mixFilter = new NoiseReductionFilter();
double rawEnergy = 0, filteredEnergy = 0, rawSin = 0, rawCos = 0, filteredSin = 0, filteredCos = 0;
for (int i = 0; i < 96000; i++)
{
    double phase = 2 * Math.PI * 1000 * i / 48000;
    double mixed = .15 * Math.Sin(phase) + .1 * Math.Sin(2 * Math.PI * 40 * i / 48000) + .1 * Math.Sin(2 * Math.PI * 12000 * i / 48000);
    double filtered = mixFilter.Process(mixed, 1);
    if (i < 48000) continue;
    rawEnergy += mixed * mixed; filteredEnergy += filtered * filtered;
    rawSin += mixed * Math.Sin(phase); rawCos += mixed * Math.Cos(phase);
    filteredSin += filtered * Math.Sin(phase); filteredCos += filtered * Math.Cos(phase);
}
double SignalNoiseRatio(double total, double sin, double cos)
{
    double signal = 2 * (sin * sin + cos * cos) / 48000;
    return 10 * Math.Log10(signal / Math.Max(1e-12, total - signal));
}
double improvement = SignalNoiseRatio(filteredEnergy, filteredSin, filteredCos) - SignalNoiseRatio(rawEnergy, rawSin, rawCos);
Check(improvement > 6, "mixed tone test improves speech-band signal versus rumble/hiss by more than 6 dB");
Console.WriteLine($"Measured mixed-tone improvement: {improvement:F1} dB (synthetic test, not a speech intelligibility claim).");
// Review deck analysis: 1 s quiet hiss, 1 s harmonic "voice" at 180 Hz, 1 s quiet hiss.
var noiseRandom = new Random(7);
var deckSamples = new short[48000 * 3];
for (int i = 0; i < deckSamples.Length; i++)
{
    double hiss = (noiseRandom.NextDouble() * 2 - 1) * .004;
    double voiced = i >= 48000 && i < 96000 ? Enumerable.Range(1, 8).Sum(h => .25 / h * Math.Sin(2 * Math.PI * 180 * h * i / 48000)) : 0;
    deckSamples[i] = (short)Math.Clamp((hiss + voiced) * 32767, short.MinValue, short.MaxValue);
}
double floorDb = ReviewAnalysis.NoiseFloorDb(deckSamples);
var lanes = ReviewAnalysis.Analyze(deckSamples, 0, 3, 300, floorDb);
Check(lanes.Columns == 300 && lanes.Heat.Length == 300 * ReviewAnalysis.Bins, "review lanes have one column per requested pixel");
double VoiceAvg(int a, int b) => lanes.Voice.Skip(a).Take(b - a).Average(v => (double)v);
Check(VoiceAvg(120, 180) > 150 && VoiceAvg(10, 80) < 40 && VoiceAvg(220, 290) < 40, "speech-like lane is high on voiced sound and low on hiss");
var pitched = lanes.PitchTenthsHz.Skip(120).Take(60).Where(p => p > 0).Select(p => p / 10.0).ToArray();
Check(pitched.Length > 50 && pitched.All(p => Math.Abs(p - 180) < 6), "pitch lane tracks a 180 Hz voice-like tone");
Check(lanes.PitchTenthsHz.Take(80).All(p => p == 0), "pitch lane stays empty on hiss");
int BinOf(double hz) => (int)(Math.Log(hz / ReviewAnalysis.MinHz) / Math.Log(ReviewAnalysis.MaxHz / ReviewAnalysis.MinHz) * ReviewAnalysis.Bins);
Check(lanes.Heat[150 * ReviewAnalysis.Bins + BinOf(180)] > lanes.Heat[150 * ReviewAnalysis.Bins + BinOf(5000)] + 60, "heat map is brightest at the voice fundamental");
var zoomed = ReviewAnalysis.Analyze(deckSamples, 1.2, 1.4, 200, floorDb);
Check(zoomed.Start == 1.2 && zoomed.End == 1.4 && zoomed.Voice.Average(v => (double)v) > 150, "zoomed view analyses only the requested range");
var tenth = new PlaybackRate().Process(new byte[960], .1);
Check(tenth.Length >= 9000, "0.1x tape-style playback stretches audio ten times");
// WAV import without FFmpeg: 44.1 kHz stereo 24-bit -> 48 kHz mono 16-bit, same length and pitch.
string wavDir = Path.Combine(Path.GetTempPath(), "whispers-wav-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(wavDir);
string stereo = Path.Combine(wavDir, "stereo24.wav"), canon = Path.Combine(wavDir, "canon.wav");
using (var fs = File.Create(stereo))
{
    var w = new BinaryWriter(fs); int n = 44100 * 2; w.Write("RIFF"u8); w.Write(36 + n * 6); w.Write("WAVEfmt "u8); w.Write(16); w.Write((short)1); w.Write((short)2); w.Write(44100); w.Write(44100 * 6); w.Write((short)6); w.Write((short)24); w.Write("data"u8); w.Write(n * 6);
    for (int i = 0; i < n; i++) { int v = (int)(Math.Sin(2 * Math.PI * 300 * i / 44100.0) * 4_000_000); for (int c = 0; c < 2; c++) { w.Write((byte)v); w.Write((byte)(v >> 8)); w.Write((byte)(v >> 16)); } }
}
Check(WavDecoder.LooksLikeWav(stereo) && !WavDecoder.LooksLikeWav(Path.Combine(AppContext.BaseDirectory, "Ghostbox.Core.dll")), "WAV detection");
WavDecoder.ToCanonical(stereo, canon);
var canonPcm = ReviewAnalysis.ReadPcm(canon);
Check(Math.Abs(canonPcm.Length - 96000) <= 2, "WAV import resamples 44.1 kHz stereo to 48 kHz mono with the same duration");
Check(Math.Abs(ReviewAnalysis.Pitch(canonPcm, 48000).hz - 300) < 3, "WAV import keeps the pitch");
Directory.Delete(wavDir, true);
Console.WriteLine("All checks passed.");

sealed class CancelProgress(CancellationTokenSource cancel) : IProgress<long>
{
    public long Frequency { get; private set; }
    public void Report(long value) { Frequency = value; cancel.Cancel(); }
}
