using Microsoft.Maui.Controls.Shapes;

namespace Ghostbox.App;

public partial class MainPage
{
    static readonly Color Void = Color.FromArgb("020508");
    static readonly Color Plate = Color.FromArgb("111822");
    static readonly Color Groove = Color.FromArgb("070B10");
    static readonly Color Rim = Color.FromArgb("263346");
    static readonly Color Cyan = Color.FromArgb("38BDF8");
    static readonly Color Phosphor = Color.FromArgb("00FF88");
    static readonly Color Amber = Color.FromArgb("F59E0B");
    DateTime? sessionBegan;
    DateTime lastAudioAt;
    TimeSpan lastSessionDuration;
    Action? showLivePanel;
    readonly LiveWaveDrawing centralWave = new();
    readonly GraphicsView centralWaveView = new() { HeightRequest = 190 };

    View BuildDashboard(View audioControls, View analysisPanel, View reviewPanel)
    {
        Title = "WHISPERS OF THE SPIRIT";
        centralWaveView.Drawable = centralWave;
        SemanticProperties.SetDescription(centralWaveView, "Actual incoming audio waveform before effects");
        var sourceBadge = Badge("RECEIVER IDLE", Cyan);
        var recorderBadge = Badge("RECORDER IDLE", Color.FromArgb("94A3B8"));
        var utc = Badge("--:--:-- UTC", Amber);
        var header = BuildHeader(sourceBadge, recorderBadge, utc);

        form.IsVisible = true;
        var receiver = new VerticalStackLayout { Spacing = 14, Children = { ModuleTitle("01 / RADIO RECEIVER TUNER RACK", "REAL RF / STREAM INPUT", Cyan), form } };
        frequency.FontSize = 52;
        frequency.FontAttributes = FontAttributes.Bold;
        frequency.FontFamily = "Consolas";
        frequency.TextColor = Phosphor;
        var elapsed = new Label { Text = "00:00", FontSize = 21, FontFamily = "Consolas", HorizontalTextAlignment = TextAlignment.Center, TextColor = Phosphor };
        var statistics = new Label { FontSize = 10, CharacterSpacing = 1, HorizontalTextAlignment = TextAlignment.Center, TextColor = Color.FromArgb("7DD3FC") };
        var speak = ConsoleButton("♩  SPEAK · RECORD QUESTION", Cyan);
        speak.Clicked += RecordQuestion;
        ToolTipProperties.SetText(speak, "Records the chosen microphone until stopped. Microphone audio is stored separately from received audio.");
        var record = new Button { Text = "●", FontSize = 31, WidthRequest = 76, HeightRequest = 76, CornerRadius = 38, BackgroundColor = Color.FromArgb("76131B"), BorderColor = Color.FromArgb("EF4444"), BorderWidth = 2, TextColor = Color.FromArgb("FF5252"), HorizontalOptions = LayoutOptions.Center };
        record.Clicked += RecordRadio;
        ToolTipProperties.SetText(record, "Starts or stops a recording of actual received audio. Demo audio cannot be recorded as reception.");
        var recordStatus = new Label { Text = "RECORDER IDLE", FontSize = 10, CharacterSpacing = 1, HorizontalTextAlignment = TextAlignment.Center };
        var livePanel = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                ModuleTitle("02 / LIVE RECEIVER", "PRE-EFFECT SIGNAL", Phosphor),
                new GraphicsView { Drawable = new HudSignalMark(), HeightRequest = 42 },
                new Label { Text = "WHISPERS OF THE SPIRIT", FontSize = 15, CharacterSpacing = 3, HorizontalTextAlignment = TextAlignment.Center, TextColor = Color.FromArgb("34D399") },
                frequency, status, detail, Crt(centralWaveView), audioStatus, elapsed, statistics, speak, record, recordStatus, error,
                new Label { Text = "CONNECT → LISTEN → RECORD → REVIEW → CLEAN → COMPARE → SAVE", FontSize = 9, CharacterSpacing = 1, HorizontalTextAlignment = TextAlignment.Center, TextColor = Color.FromArgb("34D399") },
                new Label { Text = "Original reception and microphone audio remain separate. Effects never establish received speech.", FontSize = 10, HorizontalTextAlignment = TextAlignment.Center, TextColor = Color.FromArgb("94A3B8") }
            }
        };

        var cleanup = BuildCleanupPanel();
        var voicePanel = BuildVoicePanel();
        var pages = new[] { cleanup, voicePanel, audioControls, reviewPanel };
        var names = new[] { "CLEANUP", "VOICE", "EFFECTS", "REVIEW" };
        var tabs = new Grid { ColumnSpacing = 5, RowSpacing = 5, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto)] };
        var buttons = names.Select(n => ConsoleButton(n, Cyan, 10)).ToArray();
        void Choose(int index)
        {
            for (var i = 0; i < pages.Length; i++)
            {
                pages[i].IsVisible = i == index;
                buttons[i].BackgroundColor = i == index ? Color.FromArgb("164E63") : Groove;
                buttons[i].TextColor = i == index ? Color.FromArgb("67E8F9") : Color.FromArgb("94A3B8");
            }
        }
        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            buttons[i].Clicked += (_, _) => Choose(index);
            tabs.Add(buttons[i], i % 2, i / 2);
        }
        var tools = new VerticalStackLayout { Spacing = 14, Children = { ModuleTitle("06 / INVESTIGATION WORKBENCH", "LOCAL DSP / REVIEW", Amber), tabs } };
        foreach (var page in pages) tools.Add(page);
        Choose(0);

        var analogVolume = new Slider { Minimum = 0, Maximum = 100, Value = volume.Value * 100 };
        var analogNoise = new Slider { Minimum = 0, Maximum = 100, Value = noiseReduction.Value * 100 };
        var analogReverb = new Slider { Minimum = 0, Maximum = 80, Value = workControls["reverb"].Amount.Value * 100 };
        var volumeReadout = Badge($"{analogVolume.Value:0}%", Cyan);
        var noiseReadout = Badge($"{analogNoise.Value:0}%", Phosphor);
        var reverbReadout = Badge($"{analogReverb.Value:0}%", Amber);
        analogVolume.ValueChanged += (_, e) => { volume.Value = e.NewValue / 100; volumeReadout.Text = $"{e.NewValue:0}%"; };
        analogNoise.ValueChanged += (_, e) => { noiseReduction.Value = e.NewValue / 100; reduceNoise.IsToggled = e.NewValue > 0; noiseReadout.Text = $"{e.NewValue:0}%"; };
        analogReverb.ValueChanged += (_, e) => { var w = workControls["reverb"]; w.Amount.Value = e.NewValue / 100; w.Enabled.IsToggled = e.NewValue > 0; reverbReadout.Text = $"{e.NewValue:0}%"; };
        View Dial(string title, Slider slider, Label readout, Color color) => new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                new Border { WidthRequest = 62, HeightRequest = 62, Stroke = Rim, StrokeThickness = 3, BackgroundColor = Groove, StrokeShape = new RoundRectangle { CornerRadius = 31 }, HorizontalOptions = LayoutOptions.Center, Content = new Label { Text = "●", FontSize = 28, TextColor = color, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center } },
                new Label { Text = title, FontSize = 8, CharacterSpacing = 1, HorizontalTextAlignment = TextAlignment.Center },
                readout,
                slider
            }
        };
        var dials = new Grid { ColumnSpacing = 8, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)] };
        dials.Add(Dial("MONITOR", analogVolume, volumeReadout, Cyan), 0, 0);
        dials.Add(Dial("DENOISE", analogNoise, noiseReadout, Phosphor), 1, 0);
        dials.Add(Dial("REVERB", analogReverb, reverbReadout, Amber), 2, 0);
        View ToggleRow(string title, Switch control)
        {
            var row = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
            row.Add(new Label { Text = title, VerticalOptions = LayoutOptions.Center }, 0, 0);
            row.Add(control, 1, 0);
            return row;
        }
        Switch ProxySwitch(Switch source)
        {
            var proxy = new Switch { IsToggled = source.IsToggled };
            proxy.Toggled += (_, e) => source.IsToggled = e.Value;
            source.Toggled += (_, e) => proxy.IsToggled = e.Value;
            return proxy;
        }
        var analog = new VerticalStackLayout
        {
            Spacing = 14,
            Children =
            {
                ModuleTitle("03 / ANALOG ROTARY & GAIN", "REAL DSP VALUES", Cyan),
                dials,
                new BoxView { HeightRequest = 1, Color = Rim },
                new Label { Text = "PRECISION SIGNAL ROUTING", FontSize = 9, CharacterSpacing = 1, TextColor = Color.FromArgb("94A3B8") },
                ToggleRow("NOISE FILTER", ProxySwitch(reduceNoise)),
                ToggleRow("MUTE WHILE SPEAKING", ProxySwitch(muteWhenSpeaking)),
                ToggleRow("AUTO LISTEN CAPTURE", ProxySwitch(autoListen)),
                new Label { Text = "Controls above are synchronized with the existing audio engine. Raw receiver recordings remain unchanged.", FontSize = 10, TextColor = Color.FromArgb("94A3B8") },
                new Border { Padding = 12, Stroke = Color.FromArgb("7C4A03"), BackgroundColor = Color.FromArgb("160D02"), StrokeShape = new RoundRectangle { CornerRadius = 8 }, Content = new VerticalStackLayout { Children = { new Label { Text = "SIGNAL INTEGRITY", TextColor = Amber, FontSize = 10, CharacterSpacing = 1 }, new Label { Text = "PRE-EFFECT ANALYSIS\nRAW / PROCESSED SEPARATION\nLOCAL FILE PRESERVATION", FontFamily = "Consolas", FontSize = 11, TextColor = Color.FromArgb("FBBF24") } } } }
            }
        };

        var filterRack = new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                ModuleTitle("AETHERIC DSP FILTERS", "PASSBAND / ACTIVE", Phosphor),
                ToggleRow("SPECTRAL DENOISE FILTER", ProxySwitch(reduceNoise)),
                ToggleRow("FAST BOUNCE SWEEPER", ProxySwitch(workControls["sweep"].Enabled)),
                ToggleRow("VOICE-BAND FOCUS", ProxySwitch(workControls["voice"].Enabled)),
                new Label { Text = "These switches operate the real monitor/processing controls. Raw received audio is preserved separately.", FontSize = 9, TextColor = Color.FromArgb("64748B") }
            }
        };
        var vectorView = new GraphicsView { HeightRequest = 150, Drawable = new MiniScopeDrawing(centralWave, false) };
        var amberView = new GraphicsView { HeightRequest = 150, Drawable = new MiniScopeDrawing(centralWave, true) };
        var scopes = new Grid { ColumnSpacing = 12, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)] };
        scopes.Add(RackCard(new VerticalStackLayout { Children = { ModuleTitle("VECTOR SCOPE", "LIVE PCM", Phosphor), vectorView } }, 12, Phosphor), 0, 0);
        scopes.Add(RackCard(new VerticalStackLayout { Children = { ModuleTitle("SIGNAL ENERGY", "LIVE PCM", Amber), amberView } }, 12, Amber), 1, 0);
        var voiceProxy = new Slider { Minimum = 0, Maximum = 8, Value = workControls["voice"].Amount.Value };
        var speedProxy = new Slider { Minimum = .25, Maximum = 2, Value = workControls["speed"].Amount.Value };
        var voiceValue = Badge($"{voiceProxy.Value:0.0} dB", Cyan);
        var speedValue = Badge($"{speedProxy.Value:0.00}×", Phosphor);
        voiceProxy.ValueChanged += (_, e) => { var w = workControls["voice"]; w.Amount.Value = e.NewValue; w.Enabled.IsToggled = e.NewValue > 0; voiceValue.Text = $"{e.NewValue:0.0} dB"; };
        speedProxy.ValueChanged += (_, e) => { var w = workControls["speed"]; w.Amount.Value = e.NewValue; w.Enabled.IsToggled = Math.Abs(e.NewValue - 1) > .001; speedValue.Text = $"{e.NewValue:0.00}×"; };
        var voiceRack = new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                ModuleTitle("04 / VOICE PROCESSING", "RECORDED AUDIO", Cyan),
                new Label { Text = "VOICE-BAND EQ", FontSize = 9 }, voiceValue, voiceProxy,
                new Label { Text = "REVIEW SPEED", FontSize = 9 }, speedValue, speedProxy,
                ToggleRow("PRESERVE PITCH", ProxySwitch(preservePitch)),
                new Label { Text = "Voice controls process review copies only; they do not detect entities, reconstruct words, or alter originals.", FontSize = 9, TextColor = Color.FromArgb("94A3B8") }
            }
        };

        var consoleLog = new Label { Text = "[READY] Select a receiver source and begin an investigation.", FontFamily = "Consolas", FontSize = 10, TextColor = Color.FromArgb("34D399"), LineBreakMode = LineBreakMode.TailTruncation };
        var logPanel = new VerticalStackLayout { Spacing = 7, Children = { ModuleTitle("LIVE RECEIVER CONSOLE LOG", "LOCAL AUDIO STATE", Phosphor), consoleLog } };
        var mainColumns = new Grid { ColumnSpacing = 14, HeightRequest = 720, ColumnDefinitions = [new(new GridLength(4, GridUnitType.Star)), new(new GridLength(5, GridUnitType.Star)), new(new GridLength(3, GridUnitType.Star))] };
        mainColumns.Add(new ScrollView { Content = RackCard(receiver, 18) }, 0, 0);
        mainColumns.Add(new ScrollView { Content = RackCard(livePanel, 16, Phosphor) }, 1, 0);
        mainColumns.Add(new ScrollView { Content = RackCard(analog, 16) }, 2, 0);

        var lowerModules = new Grid { ColumnSpacing = 14, HeightRequest = 720, ColumnDefinitions = [new(new GridLength(7, GridUnitType.Star)), new(new GridLength(5, GridUnitType.Star))] };
        var analyzer = new VerticalStackLayout { Spacing = 12, Children = { ModuleTitle("05 / SOUND & MUSIC HARMONIC SPECTRAL ANALYZER", "PRE-EFFECT AUDIO", Phosphor), analysisPanel } };
        lowerModules.Add(new ScrollView { Content = RackCard(analyzer, 18, Phosphor) }, 0, 0);
        lowerModules.Add(new ScrollView { Content = RackCard(tools, 18, Amber) }, 1, 0);

        var instrumentStrip = new Grid { ColumnSpacing = 14, HeightRequest = 230, ColumnDefinitions = [new(new GridLength(4, GridUnitType.Star)), new(new GridLength(5, GridUnitType.Star)), new(new GridLength(3, GridUnitType.Star))] };
        instrumentStrip.Add(RackCard(filterRack, 16), 0, 0);
        instrumentStrip.Add(scopes, 1, 0);
        instrumentStrip.Add(RackCard(voiceRack, 16), 2, 0);

        showLivePanel = () => { };
        var transport = new Grid { Padding = new Thickness(18, 12), ColumnSpacing = 10, ColumnDefinitions = [new(new GridLength(1.4, GridUnitType.Star)), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(GridLength.Star), new(new GridLength(1.2, GridUnitType.Star))] };
        var transportRecord = ActionButton("●  REC AUDIO", () => RecordRadio(null, EventArgs.Empty), "Records actual received audio; microphone remains separate.");
        var pause = ActionButton("Ⅱ  PAUSE", () => workPaused = !workPaused, "Pauses or resumes review playback; live scanning is not interrupted.");
        var play = ActionButton("▷  PLAY", PlayWorkbench, "Plays the selected Original or Processed review range.");
        var save = ActionButton("▣  SAVE WAV", async () => { if (reviewOriginal is not null) await ExportWorkAsync(true); else SaveConversation(null, EventArgs.Empty); }, "Exports reviewed audio or saves the current conversation WAV.");
        start.Text = "▷  START RF SWEEP";
        start.BackgroundColor = Color.FromArgb("123447");
        start.BorderColor = Cyan;
        start.BorderWidth = 1;
        start.FontSize = 11;
        stop.FontSize = 11;
        foreach (var b in new[] { stop, transportRecord, pause, play, save }) { b.BackgroundColor = Groove; b.BorderColor = Rim; b.BorderWidth = 1; b.FontSize = 11; }
        transportRecord.BorderColor = Color.FromArgb("EF4444");
        transportRecord.TextColor = Color.FromArgb("F87171");
        transport.Add(start, 0, 0); transport.Add(stop, 1, 0); transport.Add(transportRecord, 2, 0); transport.Add(pause, 3, 0); transport.Add(play, 4, 0); transport.Add(save, 5, 0);

        var body = new VerticalStackLayout { Padding = new Thickness(18, 12), Spacing = 14, Children = { mainColumns, instrumentStrip, lowerModules, RackCard(logPanel, 12), RackCard(transport, 4) } };
        var root = new Grid { RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)] };
        root.Add(header, 0, 0);
        root.Add(new ScrollView { Content = body }, 0, 1);
        var layered = new Grid();
        layered.Add(new GraphicsView { Drawable = new HudBackdrop(), InputTransparent = true });
        layered.Add(root);

        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(250);
        timer.Tick += (_, _) =>
        {
            var duration = sessionBegan.HasValue ? DateTime.UtcNow - sessionBegan.Value : lastSessionDuration;
            elapsed.Text = duration.ToString(@"mm\:ss");
            statistics.Text = $"{hops:N0} HOPS  ·  {savedPaths.Length} SAVED TAKES";
            utc.Text = DateTime.UtcNow.ToString("HH:mm:ss 'UTC'");
            sourceBadge.Text = cancellation is null ? "RECEIVER IDLE" : demo.IsToggled ? "DEMO · SYNTHETIC" : DateTime.UtcNow - lastAudioAt < TimeSpan.FromSeconds(3) ? useRemoteSdr ? "RF LIVE · REMOTE ANTENNA" : onlineEnabled.IsToggled ? "NET STREAM LIVE" : "GQRX AUDIO LIVE" : "CONNECTING / WAITING";
            if (cancellation is not null && EffectValue("white") > 0) sourceBadge.Text += " + SYNTHETIC FX";
            recordStatus.Text = recorderBadge.Text = microphoneCancellation is not null ? "MICROPHONE RECORDING" : captureCancellation is not null ? "RECEIVER RECORDING" : "RECORDER IDLE";
            recorderBadge.TextColor = microphoneCancellation is not null || captureCancellation is not null ? Color.FromArgb("F87171") : Color.FromArgb("94A3B8");
            speak.Text = microphoneCancellation is not null ? "■  STOP QUESTION · LISTEN NEXT" : "♩  SPEAK · RECORD QUESTION";
            speak.IsEnabled = captureCancellation is null && replayCancellation is null && workPlaybackCancellation is null;
            record.Text = captureCancellation is not null ? "■" : "●";
            record.IsEnabled = transportRecord.IsEnabled = radioAudioRunning && !demo.IsToggled && microphoneCancellation is null;
            pause.IsEnabled = workPlaybackCancellation is not null;
            play.IsEnabled = reviewOriginal is not null;
            pause.Text = workPaused ? "▷ RESUME" : "Ⅱ PAUSE";
            consoleLog.Text = $"[{DateTime.UtcNow:HH:mm:ss}] {sourceBadge.Text}  |  {recordStatus.Text}  |  {audioStatus.Text}";
            vectorView.Invalidate(); amberView.Invalidate();
            if (workPlaybackCancellation is not null) audioStatus.Text = $"REVIEW · {workPlaybackLabel} · {(workPaused ? "PAUSED" : "PLAYING")} · {workPlaySeconds:F1}s";
            else if (cancellation is null && audioStatus.Text?.StartsWith("REVIEW") == true) audioStatus.Text = "Review stopped";
        };
        Loaded += (_, _) => timer.Start();
        Unloaded += (_, _) => timer.Stop();
        timer.Start();
        return layered;
    }

    static Grid BuildHeader(Label sourceBadge, Label recorderBadge, Label utc)
    {
        var brand = new VerticalStackLayout { Spacing = 2, Children = { new Label { Text = "◆  WHISPERS OF THE SPIRIT", FontSize = 20, FontAttributes = FontAttributes.Bold, CharacterSpacing = 3, TextColor = Color.FromArgb("E2E8F0") }, new Label { Text = "WIDEBAND RECEIVER  ·  FREQUENCY SWEEPER  ·  AUDIO INVESTIGATION", FontSize = 9, CharacterSpacing = 2, TextColor = Color.FromArgb("64748B") } } };
        var identity = new Border { Padding = new Thickness(12, 7), BackgroundColor = Groove, Stroke = Rim, StrokeShape = new RoundRectangle { CornerRadius = 4 }, Content = new Label { Text = "●  SDR CONSOLE // ONLINE", FontFamily = "Consolas", FontSize = 11, TextColor = Cyan } };
        var left = new HorizontalStackLayout { Spacing = 16, Children = { identity, brand } };
        var telemetry = new HorizontalStackLayout { Spacing = 14, HorizontalOptions = LayoutOptions.End, Children = { sourceBadge, recorderBadge, utc } };
        var header = new Grid { Padding = new Thickness(28, 14), BackgroundColor = Void, ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
        header.Add(left, 0, 0); header.Add(telemetry, 1, 0); return header;
    }

    static Border RackCard(View child, double padding, Color? accent = null) => new() { Padding = padding, Stroke = accent ?? Rim, StrokeThickness = accent is null ? 1.5 : 2, BackgroundColor = Plate, StrokeShape = new RoundRectangle { CornerRadius = 12 }, Content = child, Shadow = new Shadow { Brush = Colors.Black, Opacity = .75f, Radius = 14, Offset = new Point(0, 6) } };
    static Border Crt(View content) => new() { Padding = 9, Stroke = Color.FromArgb("075E45"), StrokeThickness = 2, BackgroundColor = Color.FromArgb("01120A"), StrokeShape = new RoundRectangle { CornerRadius = 10 }, Content = content, Shadow = new Shadow { Brush = Color.FromArgb("5500FF88"), Opacity = .55f, Radius = 16, Offset = new Point(0, 0) } };
    static Grid ModuleTitle(string title, string status, Color accent)
    {
        var line = new Grid { Padding = new Thickness(0, 0, 0, 8), ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)] };
        line.Add(new Label { Text = title, FontSize = 12, FontAttributes = FontAttributes.Bold, CharacterSpacing = 1.4, TextColor = Color.FromArgb("E2E8F0") }, 0, 0);
        line.Add(new Label { Text = status, FontSize = 8, CharacterSpacing = 1, TextColor = accent, VerticalOptions = LayoutOptions.Center }, 1, 0); return line;
    }
    static Button ConsoleButton(string text, Color accent, double fontSize = 11) => new() { Text = text, FontSize = fontSize, FontAttributes = FontAttributes.Bold, BackgroundColor = Groove, BorderColor = accent, BorderWidth = 1, TextColor = accent, CornerRadius = 5, Padding = new Thickness(10, 9) };
    static Label Badge(string text, Color color) => new() { Text = text, TextColor = color, FontFamily = "Consolas", FontSize = 10, CharacterSpacing = .8, VerticalTextAlignment = TextAlignment.Center };
}

sealed class LiveWaveDrawing : IDrawable
{
    public float[] Samples = [];
    public void Draw(ICanvas c, RectF r)
    {
        c.FillColor = Color.FromArgb("011009"); c.FillRectangle(r); c.StrokeSize = .5f; c.StrokeColor = Color.FromArgb("153D2B");
        for (float x = 0; x < r.Width; x += 20) c.DrawLine(x, 0, x, r.Height);
        for (float y = 0; y < r.Height; y += 20) c.DrawLine(0, y, r.Width, y);
        c.StrokeColor = Color.FromArgb("235E45"); c.DrawLine(r.Left, r.Center.Y, r.Right, r.Center.Y);
        if (Samples.Length < 2) return;
        c.StrokeColor = Color.FromArgb("00FF88"); c.StrokeSize = 1.8f; var path = new PathF();
        for (var i = 0; i < Samples.Length; i++) { var x = r.X + i * r.Width / (Samples.Length - 1); var y = r.Center.Y - Samples[i] * r.Height * .46f; if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y); }
        c.DrawPath(path);
    }
}

sealed class HudBackdrop : IDrawable
{
    public void Draw(ICanvas c, RectF r)
    {
        c.FillColor = Color.FromArgb("020508"); c.FillRectangle(r); c.StrokeColor = Color.FromArgb("0B1A26"); c.StrokeSize = .5f;
        for (float x = 0; x < r.Width; x += 32) c.DrawLine(x, 0, x, r.Height);
        for (float y = 0; y < r.Height; y += 32) c.DrawLine(0, y, r.Width, y);
    }
}

sealed class HudSignalMark : IDrawable
{
    public void Draw(ICanvas c, RectF r)
    {
        c.StrokeColor = Color.FromArgb("10B981"); c.StrokeSize = 3;
        for (var i = -10; i <= 10; i++) { var h = (float)(Math.Cos(i * .8) * Math.Cos(i * .8) * 15 + 3); c.DrawLine(r.Center.X + i * 8, r.Center.Y - h, r.Center.X + i * 8, r.Center.Y + h); }
    }
}

sealed class MiniScopeDrawing(LiveWaveDrawing source, bool amber) : IDrawable
{
    public void Draw(ICanvas c, RectF r)
    {
        var glow = amber ? Color.FromArgb("F59E0B") : Color.FromArgb("00FF88");
        c.FillColor = amber ? Color.FromArgb("130A01") : Color.FromArgb("011009");
        c.FillRectangle(r);
        c.StrokeColor = amber ? Color.FromArgb("4A2B05") : Color.FromArgb("153D2B");
        c.StrokeSize = .5f;
        for (float x = 0; x < r.Width; x += 20) c.DrawLine(x, 0, x, r.Height);
        for (float y = 0; y < r.Height; y += 20) c.DrawLine(0, y, r.Width, y);
        var samples = source.Samples;
        c.StrokeColor = glow;
        c.StrokeSize = 1.6f;
        var path = new PathF();
        if (samples.Length > 8)
        {
            for (var i = 0; i < samples.Length; i++)
            {
                float x, y;
                if (amber)
                {
                    x = r.X + i * r.Width / (samples.Length - 1);
                    y = r.Center.Y - samples[i] * r.Height * .42f;
                }
                else
                {
                    var j = (i + samples.Length / 4) % samples.Length;
                    x = r.Center.X + samples[i] * r.Width * .42f;
                    y = r.Center.Y - samples[j] * r.Height * .42f;
                }
                if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
            }
        }
        else
        {
            for (var i = 0; i <= 120; i++)
            {
                var t = i / 120f * MathF.PI * 2;
                var x = r.Center.X + MathF.Sin(t) * r.Width * .32f;
                var y = r.Center.Y - MathF.Sin(t * 2) * r.Height * .28f;
                if (i == 0) path.MoveTo(x, y); else path.LineTo(x, y);
            }
        }
        c.DrawPath(path);
    }
}
