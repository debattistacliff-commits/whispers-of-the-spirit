using Ghostbox.Core;
using System.Globalization;
using System.Text.Json;

namespace Ghostbox.App;

public partial class MainPage : ContentPage
{
    readonly Entry host = Input("127.0.0.1", "Gqrx host / IP"), port = Input("7356", "TCP port", true);
    readonly Entry minimum = Input("87.5M", "Minimum frequency"), maximum = Input("108M", "Maximum frequency");
    readonly Entry step = Input("100k", "Step size"), dwell = Input("100", "Dwell milliseconds", true);
    readonly Entry passband = Input("", "Automatic"), timeout = Input("3000", "Timeout milliseconds", true);
    readonly Entry audioPort = Input("7355", "UDP audio port", true);
    readonly Picker pattern = new() { ItemsSource = Enum.GetNames<ScanPattern>().ToList(), SelectedIndex = 0 };
    readonly Picker mode = new() { ItemsSource = ScanSettings.Modes.ToList(), SelectedIndex = 0 };
    readonly Switch quiet = new(), audioEnabled = new(), voice = new(), sweep = new();
    readonly Switch demo = new();
    readonly SpectrumDrawing spectrumDrawing = new();
    readonly GraphicsView spectrumView = new() { HeightRequest = 155 };
    readonly ProgressBar levelMeter = new() { Progress = 0, ProgressColor = Color.FromArgb("8DCFE6") };
    readonly Label analysisText = new() { Text = "Waiting for audio", FontSize = 14 };
    readonly Slider echo = new() { Minimum = 0, Maximum = 0.8 }, chop = new() { Minimum = 0, Maximum = 12 }, volume = new() { Minimum = 0, Maximum = 1, Value = 0.6 };
    readonly Slider reverb = new() { Minimum = 0, Maximum = 0.8 }, drive = new() { Minimum = 0, Maximum = 1 };
    readonly Slider noiseReduction = new() { Minimum = 0, Maximum = 1 };
    readonly Switch reduceNoise = new();
    readonly Switch muteWhenSpeaking = new() { IsToggled = true }, autoListen = new() { IsToggled = true };
    volatile bool monitorMuted;
    readonly Label frequency = new() { Text = "— MHz", FontSize = 38, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
    readonly Label status = new() { Text = "No radio receiver connected", HorizontalTextAlignment = TextAlignment.Center };
    readonly Label detail = new() { Text = "Live tuning requires Gqrx with a supported SDR receiver", FontSize = 13, HorizontalTextAlignment = TextAlignment.Center };
    readonly Label audioStatus = new() { Text = "No incoming radio audio", FontSize = 13 };
    readonly Label error = new() { IsVisible = false, TextColor = Colors.IndianRed, FontSize = 15 };
    readonly Button start = new() { Text = "Connect & scan", BackgroundColor = Color.FromArgb("146C60"), TextColor = Colors.White, MinimumHeightRequest = 52 };
    readonly Button stop = new() { Text = "Stop", IsEnabled = false, MinimumHeightRequest = 52 };
    readonly VerticalStackLayout form = new() { Spacing = 18 };
    CancellationTokenSource? cancellation;
    AudioEffects effects = new();
    long hops;
    DateTime lastUpdate;
    AudioAnalysis? lastMeasurement;
    DateTime lastMeasurementAt;

    public MainPage()
    {
        Title = "Gqrx Ghostbox";
        BackgroundColor = Color.FromArgb("020508");
        LoadSettings();
        reduceNoise.IsToggled = Preferences.Default.Get("noise-enabled", noiseReduction.Value > 0);
        noiseReduction.Value = Math.Clamp(Preferences.Default.Get("noise-strength", noiseReduction.Value > 0 ? noiseReduction.Value : .35), 0, 1);
        noiseReduction.IsEnabled = reduceNoise.IsToggled;
        demo.Toggled += (_, _) => start.Text = demo.IsToggled ? "Play demo sound" : useRemoteSdr ? "Start RF sweep" : onlineEnabled.IsToggled ? "Play online radio" : "Connect & scan";
        spectrumView.Drawable = spectrumDrawing;
        SemanticProperties.SetDescription(spectrumView, "Incoming audio frequency spectrum from 50 Hz to 16 kHz");
        var preset = new Picker { Title = "Load a band preset", ItemsSource = new[] { "Broadcast FM", "Medium-wave AM", "Shortwave AM" } };
        preset.SelectedIndexChanged += (_, _) =>
        {
            switch (preset.SelectedIndex)
            {
                case 0: minimum.Text = "87.5M"; maximum.Text = "108M"; step.Text = "100k"; mode.SelectedIndex = 0; break;
                case 1: minimum.Text = "531k"; maximum.Text = "1602k"; step.Text = "9k"; mode.SelectedIndex = Array.IndexOf(ScanSettings.Modes, "AM"); break;
                case 2: minimum.Text = "5.9M"; maximum.Text = "6.2M"; step.Text = "5k"; mode.SelectedIndex = Array.IndexOf(ScanSettings.Modes, "AM"); break;
            }
        };
        var gqrxPanel = new VerticalStackLayout { Spacing = 18 };
        form.Add(BuildRadioSources(gqrxPanel));
        form.Add(Section("Test source only", Row(new Label { Text = "Demo sound • synthesized test tones" }, demo)));
        form.Add(gqrxPanel);
        gqrxPanel.Add(Section("Gqrx SDR connection", new Label { Text = "This optional mode needs Gqrx and SDR hardware. Online radio above does not.", FontSize = 13 }, Row(Field("Gqrx host / IP", host), Field("TCP port", port))));
        gqrxPanel.Add(Section("Scan range", preset, Row(Field("Minimum", minimum), Field("Maximum", maximum)),
            new Label { Text = "Use 87.5M for MHz, 100k for kHz, or whole Hz. Presets depend on your SDR's supported bands.", FontSize = 12 },
            Row(Field("Step", step), Field("Dwell (ms)", dwell))));
        gqrxPanel.Add(Section("Scan behavior", Row(Field("Pattern", pattern), Field("Demodulator", mode)),
            new Label { Text = "WFM = broadcast FM • FM = narrowband FM. Bounce also chooses a random direction.", FontSize = 12 }));
        gqrxPanel.Add(Section("Advanced", Row(Field("Passband (Hz or suffix)", passband), Field("Timeout (ms)", timeout)), Row(new Label { Text = "Quiet activity • errors only" }, quiet)));
        gqrxPanel.Add(Section("Audio connection", Row(new Label { Text = "Play streamed audio in this app" }, audioEnabled), Field("UDP listening port", audioPort),
            new Label { Text = "In Gqrx Audio → Network, send mono 48 kHz / 16-bit UDP audio to this device's LAN IP and this port, then enable UDP output. Audio effects need this stream.", FontSize = 12 }));
        var audioControls = BuildCreativePanel();
        foreach (var slider in new[] { echo, chop, volume, reverb, drive, noiseReduction }) slider.ValueChanged += (_, _) => UpdateEffects();
        reduceNoise.Toggled += (_, _) => { noiseReduction.IsEnabled = reduceNoise.IsToggled; Preferences.Default.Set("noise-enabled", reduceNoise.IsToggled); UpdateEffects(); };
        noiseReduction.ValueChanged += (_, _) => Preferences.Default.Set("noise-strength", noiseReduction.Value);
        muteWhenSpeaking.Toggled += (_, _) => monitorMuted = muteWhenSpeaking.IsToggled && microphoneCancellation is not null;
        voice.Toggled += (_, _) => UpdateEffects(); sweep.Toggled += (_, _) => UpdateEffects();
        UpdateEffects();
        var axis = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)] };
        axis.Add(new Label { Text = "50 Hz", FontSize = 12 }, 0, 0);
        axis.Add(new Label { Text = "1 kHz", FontSize = 12, HorizontalTextAlignment = TextAlignment.Center }, 1, 0);
        axis.Add(new Label { Text = "16 kHz", FontSize = 12, HorizontalTextAlignment = TextAlignment.End }, 2, 0);
        var analysisSection = Section("Incoming sound analysis", BuildHeatPanel(), levelMeter, analysisText, spectrumView, axis,
            new Label { Text = "Measures the incoming sound before effects: RMS loudness, peak level, strongest frequency and clipped samples. dBFS is relative to digital full scale. This does not transcribe speech or identify sound sources.", FontSize = 13 });
        // Initialize the controls used by the instrument console's audio and review commands.
        BuildReviewWorkspace(BuildRecordingsPanel());
        BuildCleanupPanel();
        BuildVoicePanel();
        Content = BuildWebConsole();
        start.Clicked += StartClicked; stop.Clicked += (_, _) => StopScanning();
        SemanticProperties.SetDescription(frequency, "Current tuned frequency");
    }
    List<string>? liveEffectLog;
    void UpdateEffects()
    {
        effects.Settings = new(EffectValue("voice") > 0, EffectValue("echo"), EffectValue("chop"), EffectValue("sweep") > 0, volume.Value, EffectValue("reverb"), EffectValue("distortion"), reduceNoise.IsToggled ? noiseReduction.Value : 0, EffectValue("white") * .1,EffectValue("delay",250),EffectValue("feedback"),EffectValue("sweep"),EffectValue("voice"));
        liveEffectLog?.Add(DateTime.UtcNow.ToString("O")+" "+JsonSerializer.Serialize(effects.Settings,SettingsJsonContext.Default.EffectSettings));
    }

    static Entry Input(string text, string name, bool numeric = false)
    {
        var entry = new Entry { Text = text, Placeholder = name, Keyboard = numeric ? Keyboard.Numeric : Keyboard.Default, MinimumHeightRequest = 48 };
        SemanticProperties.SetDescription(entry, name); return entry;
    }
    static VerticalStackLayout Field(string title, View input) => new() { Spacing = 3, Children = { new Label { Text = title, FontSize = 13, FontAttributes = FontAttributes.Bold }, input } };
    static View SliderField(string title, Slider slider, Func<double, string> format)
    {
        var value = new Label { Text = format(slider.Value), FontSize = 13 };
        slider.ValueChanged += (_, _) => value.Text = format(slider.Value);
        SemanticProperties.SetDescription(slider, title);
        return new VerticalStackLayout { Children = { Row(new Label { Text = title }, value), slider } };
    }
    static Grid Row(View left, View right)
    {
        var row = new Grid { ColumnSpacing = 14, ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)] };
        row.Add(left, 0, 0); row.Add(right, 1, 0); return row;
    }
    static VerticalStackLayout Section(string title, params View[] children)
    {
        var section = new VerticalStackLayout { Spacing = 10 };
        section.Add(new Label { Text = title.ToUpperInvariant(), FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("8DCFE6") });
        foreach (var child in children) section.Add(child); return section;
    }
    static int Integer(Entry entry, string name)
    {
        if (!int.TryParse(entry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)) throw new ArgumentException($"{name} must be a whole number.");
        return v;
    }
    ScanSettings ReadSettings() => new(Host: host.Text?.Trim() ?? "", Port: Integer(port, "Port"),
        MinimumHz: ScanSettings.ParseFrequency(minimum.Text ?? ""), MaximumHz: ScanSettings.ParseFrequency(maximum.Text ?? ""),
        StepHz: ScanSettings.ParseFrequency(step.Text ?? ""), DwellMs: Integer(dwell, "Dwell"), Mode: mode.SelectedItem as string ?? "WFM",
        Pattern: (ScanPattern)pattern.SelectedIndex, PassbandHz: string.IsNullOrWhiteSpace(passband.Text) ? null : checked((int)ScanSettings.ParseFrequency(passband.Text)), TimeoutMs: Integer(timeout, "Timeout"));

    async void StartClicked(object? sender, EventArgs e)
    {
        if (cancellation is not null) return;
        if (workPlaybackCancellation is not null) { ShowError("Stop review playback before starting live monitoring."); return; }
        if (stationLoading || receiverLoading) return;
        if (useRemoteSdr && !demo.IsToggled && remoteReceivers.Length == 0) { await LoadRemoteReceiversAsync(); if (remoteReceivers.Length == 0) return; }
        if (!useRemoteSdr && onlineEnabled.IsToggled && !demo.IsToggled && onlineStations.Length == 0 && string.IsNullOrWhiteSpace(customStream.Text))
        { await LoadOnlineStationsAsync(); if (onlineStations.Length == 0) return; }
        ScanSettings settings; int udpPort = 7355; error.IsVisible = false;
        InternetStation[] stationList = []; int stationSeconds = 5; bool autoHop = onlineAutoHop.IsToggled;
        try
        {
            settings = useRemoteSdr && !demo.IsToggled ? RemoteSettings().settings : demo.IsToggled || onlineEnabled.IsToggled ? new ScanSettings() : ReadSettings(); settings.Validate();
            if (onlineEnabled.IsToggled && !demo.IsToggled) { stationList = SelectedOnlineStations(); stationSeconds = Integer(onlineDwell, "Station time"); if (stationSeconds is < 1 or > 120) throw new ArgumentException("Station time must be 1–120 seconds."); }
            if (audioEnabled.IsToggled && !demo.IsToggled && !onlineEnabled.IsToggled && !useRemoteSdr) { udpPort = Integer(audioPort, "UDP port"); if (udpPort is < 1 or > 65535) throw new ArgumentException("UDP port must be 1–65535."); }
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException) { ShowError(ex is OverflowException ? "Passband is too large." : ex.Message); return; }
        if (!demo.IsToggled && !onlineEnabled.IsToggled && !useRemoteSdr) Preferences.Default.Set("scan-settings", JsonSerializer.Serialize(settings, SettingsJsonContext.Default.ScanSettings));
        Preferences.Default.Set("quiet", quiet.IsToggled);
        Preferences.Default.Set("audio-port", udpPort); Preferences.Default.Set("audio-enabled", audioEnabled.IsToggled);
        Preferences.Default.Set("effects", JsonSerializer.Serialize(effects.Settings, SettingsJsonContext.Default.EffectSettings));
        var active = cancellation = new CancellationTokenSource();
        sessionBegan = DateTime.UtcNow; lastAudioAt = DateTime.MinValue;
        effects = new AudioEffects { Settings = effects.Settings };
        analysisText.Text = "Waiting for audio"; levelMeter.Progress = 0; spectrumDrawing.Bands = new float[32]; spectrumView.Invalidate();
        heatDrawing.Reset(); heatView.Invalidate(); heatSource.Text = "Waiting for receiver audio";
        start.IsEnabled = false; stop.IsEnabled = true; form.IsEnabled = false;
        hops = 0; lastUpdate = DateTime.MinValue; status.Text = "Connecting…";
        detail.Text = $"{settings.Pattern} • {settings.Mode} • {settings.PointCount:N0} scan points";
        var progress = new Progress<long>(hz =>
        {
            if (cancellation != active || active.IsCancellationRequested) return;
            hops++;
            lastFrequencyHz = hz;
            captureTuneLog?.Add($"{DateTime.UtcNow:O},{hz}");
            if ((quiet.IsToggled && !useRemoteSdr) || (DateTime.UtcNow - lastUpdate).TotalMilliseconds < 100) return;
            lastUpdate = DateTime.UtcNow; frequency.Text = $"{hz / 1_000_000.0:F6} MHz";
            status.Text = "Sweeping receiver"; detail.Text = $"{settings.Pattern} • {(useRemoteSdr ? remoteMode.SelectedItem : settings.Mode)} • {hops:N0} hops";
        });
        if (quiet.IsToggled) { frequency.Text = "Quiet mode"; detail.Text = "Activity hidden • errors will still appear"; status.Text = "Scan active"; }
        Task? audioTask = null, scannerTask = null;
        IProgress<AudioAnalysis> analysisProgress = new Progress<AudioAnalysis>(measurement =>
        {
            if (cancellation != active || active.IsCancellationRequested) return;
            lastAudioAt = DateTime.UtcNow; lastMeasurement = measurement; lastMeasurementAt = DateTime.UtcNow;
            heatDrawing.Add(measurement); heatView.Invalidate();
            heatSource.Text = (demo.IsToggled ? "DEMO • synthesized test audio" : liveSource) + " • pre-effects audio";
            if (quiet.IsToggled && !onlineEnabled.IsToggled && !useRemoteSdr) return;
            levelMeter.Progress = Math.Clamp((measurement.RmsDb + 60) / 60, 0, 1);
            analysisText.Text = $"RMS {measurement.RmsDb:F1} dBFS • Peak {measurement.PeakDb:F1} dBFS\nStrongest {measurement.DominantHz:F0} Hz • Clipped {measurement.ClippedPercent:F1}%";
            spectrumDrawing.Bands = measurement.Spectrum; spectrumView.Invalidate(); centralWave.Samples = measurement.Waveform; centralWaveView.Invalidate();
        });
        try
        {
            if (useRemoteSdr && !demo.IsToggled)
            {
                var remote = RemoteSettings();
                frequency.Text = "Connecting…"; status.Text = "Opening remote antenna";
                detail.Text = $"{remote.mode} • {settings.Pattern} • {settings.DwellMs} ms";
                radioAudioRunning = true; liveSource = "Remote SDR antenna: " + remote.receiver.Url;
                IProgress<string> receiverProgress = new Progress<string>(message =>
                {
                    if (cancellation != active || active.IsCancellationRequested) return;
                    audioStatus.Text = message; captureSourceLog?.Add($"{DateTime.UtcNow:O} {message}");
                });
                audioTask = Task.Run(async () =>
                {
                    using var output = new AudioOutput(); var analyzer = new AudioAnalyzer();
                    var candidates = remoteReceivers.Where(r => r != remote.receiver && r.MinimumHz <= settings.MinimumHz && r.MaximumHz >= settings.MaximumHz)
                        .Prepend(remote.receiver).Take(4).ToArray();
                    for (int i = 0; i < candidates.Length; i++)
                    {
                        bool receivedAudio = false;
                        var candidate = candidates[i];
                        receiverProgress.Report($"Connecting to {candidate} ({i + 1}/{candidates.Length})");
                        try
                        {
                            await RemoteSdr.RunAsync(candidate, settings, remote.mode, pcm =>
                            {
                                receivedAudio = true;
                                if (analyzer.Add(pcm) is { } measurement) analysisProgress.Report(measurement);
                                var processed = recordingTap.Process(pcm, effects); output.Write(monitorMuted ? new byte[processed.Length] : processed);
                            }, progress, receiverProgress, active.Token);
                            break;
                        }
                        catch (Exception) when (!receivedAudio && !active.IsCancellationRequested && i + 1 < candidates.Length)
                        {
                            receiverProgress.Report($"{candidate} did not send audio. Trying the next receiver.");
                        }
                    }
                });
                await audioTask; return;
            }
            if (onlineEnabled.IsToggled && !demo.IsToggled)
            {
                frequency.Text = "Online radio"; status.Text = "Connecting to live stream…"; detail.Text = "Station streams • no AM/FM frequency measurement";
                var order = (ScanPattern)stationPattern.SelectedIndex;
                var onlineProgress = new Progress<string>(message =>
                {
                    if (cancellation != active || active.IsCancellationRequested) return;
                    audioStatus.Text = message;
                    liveSource = message; captureSourceLog?.Add($"{DateTime.UtcNow:O} {message}");
                    if (message.StartsWith("LIVE INTERNET RADIO: ")) { status.Text = "Receiving live station audio"; frequency.Text = message[21..]; }
                });
                radioAudioRunning = true; liveSource = "Internet radio • connecting";
                audioTask = Task.Run(async () =>
                {
                    using var output = new AudioOutput(); var analyzer = new AudioAnalyzer();
                    await InternetRadioPlayer.RunAsync(stationList, order, stationSeconds, autoHop, pcm =>
                    {
                        if (analyzer.Add(pcm) is { } measurement) analysisProgress.Report(measurement);
                        var processed = recordingTap.Process(pcm, effects); output.Write(monitorMuted ? new byte[processed.Length] : processed);
                    }, onlineProgress, active.Token);
                });
                await audioTask; return;
            }
            if (demo.IsToggled)
            {
                frequency.Text = "Demo audio"; status.Text = "Synthetic test sound"; detail.Text = "No radio connected • no real voices";
                audioStatus.Text = "Test tones and noise • try the Effects and Analyze tabs";
                audioTask = Task.Run(() => AudioStream.RunDemoAsync(effects, analysisProgress, active.Token));
                await audioTask;
                return;
            }
            if (audioEnabled.IsToggled)
            {
                liveSource = "Gqrx UDP: " + settings.Host;
                var audioProgress = new Progress<string>(message => { if (cancellation == active && !active.IsCancellationRequested && !quiet.IsToggled) audioStatus.Text = message; });
                radioAudioRunning = true;
                audioTask = Task.Run(() => AudioStream.RunAsync(settings.Host, udpPort, effects, audioProgress, analysisProgress, recordingTap, () => monitorMuted, active.Token));
            }
            scannerTask = Scanner.RunAsync(settings, progress, active.Token);
            if (audioTask is null) await scannerTask;
            else await await Task.WhenAny(scannerTask, audioTask);
        }
        catch (OperationCanceledException) when (active.IsCancellationRequested) { status.Text = "Stopped"; }
        catch (Exception ex) { frequency.Text = "— MHz"; status.Text = "Radio/audio session unavailable"; ShowError(ex.Message); }
        finally
        {
            active.Cancel();
            radioAudioRunning = false;
            if (scannerTask is not null)
                try { await scannerTask; } catch (OperationCanceledException) { } catch { /* Reported by the main scan await. */ }
            if (audioTask is not null)
                try { await audioTask; } catch (OperationCanceledException) { } catch (Exception ex) { ShowError("Audio: " + ex.Message); }
            cancellation = null; active.Dispose(); start.IsEnabled = true; stop.IsEnabled = false; form.IsEnabled = true;
            if (sessionBegan.HasValue) lastSessionDuration = DateTime.UtcNow - sessionBegan.Value;
            sessionBegan = null;
            Preferences.Default.Set("effects", JsonSerializer.Serialize(effects.Settings, SettingsJsonContext.Default.EffectSettings));
            audioStatus.Text = "Audio stopped";
            heatSource.Text = "STOPPED • last received audio retained for review";
        }
    }
    void ShowError(string text) { error.Text = text; error.IsVisible = true; showLivePanel?.Invoke(); SemanticScreenReader.Default.Announce(text); }
    public void StopScanning() { StopRecordingOperations(); if (cancellation is null) return; status.Text = "Stopping…"; stop.IsEnabled = false; cancellation.Cancel(); }
    void LoadSettings()
    {
        try
        {
            string saved = Preferences.Default.Get("scan-settings", "");
            if (string.IsNullOrEmpty(saved)) return;
            var s = JsonSerializer.Deserialize(saved, SettingsJsonContext.Default.ScanSettings)!; s.Validate();
            host.Text = s.Host; port.Text = s.Port.ToString(); minimum.Text = s.MinimumHz.ToString(); maximum.Text = s.MaximumHz.ToString();
            step.Text = s.StepHz.ToString(); dwell.Text = s.DwellMs.ToString(); mode.SelectedIndex = Array.IndexOf(ScanSettings.Modes, s.Mode); pattern.SelectedIndex = (int)s.Pattern;
            passband.Text = s.PassbandHz?.ToString() ?? ""; timeout.Text = s.TimeoutMs.ToString(); quiet.IsToggled = Preferences.Default.Get("quiet", false);
            audioPort.Text = Preferences.Default.Get("audio-port", 7355).ToString(); audioEnabled.IsToggled = Preferences.Default.Get("audio-enabled", false);
            var fx = JsonSerializer.Deserialize(Preferences.Default.Get("effects", "{}"), SettingsJsonContext.Default.EffectSettings)!;
            voice.IsToggled = fx.VoiceFilter; sweep.IsToggled = fx.FilterSweep; echo.Value = fx.Echo; chop.Value = fx.ChopHz; volume.Value = fx.Volume; reverb.Value = fx.Reverb; drive.Value = fx.Drive;
            noiseReduction.Value = fx.NoiseReduction;
        }
        catch { Preferences.Default.Remove("scan-settings"); }
    }
}


