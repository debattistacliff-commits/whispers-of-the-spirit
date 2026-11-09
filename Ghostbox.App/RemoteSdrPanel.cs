using Ghostbox.Core;

namespace Ghostbox.App;

public partial class MainPage
{
    readonly Picker sourcePicker = new() { ItemsSource = new[] { "Remote SDR frequency sweeper", "Internet station fragments", "Gqrx SDR receiver" }, SelectedIndex = 0 };
    readonly Picker remoteReceiverPicker = new() { Title = "Public antenna receiver" };
    readonly Picker remoteMode = new() { ItemsSource = RemoteSdr.Modes, SelectedIndex = 0 };
    readonly Picker remotePattern = new() { ItemsSource = Enum.GetNames<ScanPattern>().ToList(), SelectedIndex = 2 };
    readonly Entry remoteMin = Input("531k", "Minimum RF frequency"), remoteMax = Input("1602k", "Maximum RF frequency"), remoteStep = Input("9k", "RF step");
    readonly Entry remoteDwell = Input("250", "RF dwell milliseconds", true);
    readonly Label receiverInfo = new() { Text = "Find a public receiver to sweep actual frequencies over the internet.", FontSize = 13 };
    RemoteReceiver[] remoteReceivers = [];
    bool useRemoteSdr = true, receiverLoading;

    View BuildRadioSources(View gqrxPanel)
    {
        var find = new Button { Text = "Find public SDR receivers" };
        find.Clicked += async (_, _) => await LoadRemoteReceiversAsync();
        var bands = new Picker { Title = "Band preset", ItemsSource = new[] { "AM broadcast • 531–1602 kHz", "Shortwave AM • 5.9–6.2 MHz", "Shortwave AM • 9.5–9.9 MHz", "40 m USB • 7.0–7.2 MHz" } };
        bands.SelectedIndexChanged += (_, _) =>
        {
            switch (bands.SelectedIndex)
            {
                case 0: remoteMin.Text = "531k"; remoteMax.Text = "1602k"; remoteStep.Text = "9k"; remoteMode.SelectedIndex = 0; break;
                case 1: remoteMin.Text = "5.9M"; remoteMax.Text = "6.2M"; remoteStep.Text = "5k"; remoteMode.SelectedIndex = 0; break;
                case 2: remoteMin.Text = "9.5M"; remoteMax.Text = "9.9M"; remoteStep.Text = "5k"; remoteMode.SelectedIndex = 0; break;
                case 3: remoteMin.Text = "7M"; remoteMax.Text = "7.2M"; remoteStep.Text = "2.5k"; remoteMode.SelectedIndex = 1; break;
            }
        };
        remoteReceiverPicker.SelectedIndexChanged += (_, _) =>
        {
            if (remoteReceiverPicker.SelectedIndex < 0) return;
            var r = remoteReceivers[remoteReceiverPicker.SelectedIndex];
            receiverInfo.Text = $"{r.Location}\nAntenna range: {r.MinimumHz / 1e6:F3}–{r.MaximumHz / 1e6:F3} MHz";
        };
        var remotePanel = Section("Real frequency sweeper", find, remoteReceiverPicker, receiverInfo, bands,
            Row(Field("Minimum", remoteMin), Field("Maximum", remoteMax)), Row(Field("Step", remoteStep), Field("Sweep dwell (ms)", remoteDwell)),
            Row(Field("Demodulator", remoteMode), Field("Sweep direction", remotePattern)),
            new Label { Text = "Public KiwiSDRs provide AM/shortwave and narrow FM within their antenna range. 88–108 MHz broadcast FM and DAB need suitable VHF receiver hardware; they are not supplied by a standard 0–30 MHz KiwiSDR.", FontSize = 12 });
        var internetPanel = BuildOnlinePanel(gqrxPanel);
        void UpdateSource()
        {
            useRemoteSdr = sourcePicker.SelectedIndex == 0; onlineEnabled.IsToggled = sourcePicker.SelectedIndex == 1;
            remotePanel.IsVisible = useRemoteSdr; internetPanel.IsVisible = onlineEnabled.IsToggled; gqrxPanel.IsVisible = sourcePicker.SelectedIndex == 2;
            if (!demo.IsToggled) start.Text = useRemoteSdr ? "Start RF sweep" : onlineEnabled.IsToggled ? "Play station sweep" : "Connect & scan";
            status.Text = "Receiver idle"; detail.Text = useRemoteSdr ? "Remote antenna • actual frequency commands" : onlineEnabled.IsToggled ? "Live broadcaster streams" : "Gqrx receiver";
        }
        sourcePicker.SelectedIndexChanged += (_, _) => UpdateSource(); UpdateSource();
        return new VerticalStackLayout { Spacing = 16, Children = { Field("Receiver source", sourcePicker), remotePanel, internetPanel } };
    }
    async Task LoadRemoteReceiversAsync()
    {
        if (receiverLoading) return;
        receiverLoading = true; start.IsEnabled = false; receiverInfo.Text = "Finding API-enabled antenna receivers…";
        try
        {
            remoteReceivers = await RemoteReceivers.FindAsync(default);
            remoteReceiverPicker.ItemsSource = remoteReceivers.Select(r => r.ToString()).ToList();
            // Prefer the public receiver used in the successful antenna-audio test when available.
            int tested = Array.FindIndex(remoteReceivers, r => r.Url.Contains("22370.proxy.kiwisdr.com", StringComparison.OrdinalIgnoreCase));
            remoteReceiverPicker.SelectedIndex = Math.Max(0, tested);
        }
        catch (Exception ex) { receiverInfo.Text = "Receiver directory unavailable"; ShowError(ex.Message); }
        finally { receiverLoading = false; start.IsEnabled = true; }
    }
    (RemoteReceiver receiver, ScanSettings settings, string mode) RemoteSettings()
    {
        if (remoteReceiverPicker.SelectedIndex < 0) throw new ArgumentException("Find and select an antenna receiver first.");
        var receiver = remoteReceivers[remoteReceiverPicker.SelectedIndex];
        int dwellMs = Integer(remoteDwell, "Sweep dwell");
        if (dwellMs is < 100 or > 60000) throw new ArgumentException("Remote sweep dwell must be 100–60,000 ms.");
        var settings = new ScanSettings(MinimumHz: ScanSettings.ParseFrequency(remoteMin.Text ?? ""), MaximumHz: ScanSettings.ParseFrequency(remoteMax.Text ?? ""), StepHz: ScanSettings.ParseFrequency(remoteStep.Text ?? ""), DwellMs: dwellMs, Pattern: (ScanPattern)remotePattern.SelectedIndex);
        settings.Validate();
        if (settings.MinimumHz < receiver.MinimumHz || settings.MaximumHz > receiver.MaximumHz) throw new ArgumentException("The selected frequency range is outside this antenna receiver's supported range.");
        return (receiver, settings, remoteMode.SelectedItem as string ?? "AM");
    }
}
