using Ghostbox.Core;

namespace Ghostbox.App;

public partial class MainPage
{
    readonly Switch onlineEnabled = new() { IsToggled = true }, onlineAutoHop = new() { IsToggled = true };
    readonly Picker stationLanguage = new() { ItemsSource = new[] { "Malta / Maltese (mixed schedules)", "Italian", "English" }, SelectedIndex = 0 };
    readonly Picker stationPicker = new() { Title = "Live station" };
    readonly Picker stationPattern = new() { ItemsSource = Enum.GetNames<ScanPattern>().ToList(), SelectedIndex = 3 };
    readonly Entry onlineDwell = Input("5", "Seconds per station", true);
    readonly Entry customStream = Input("", "Optional direct HTTP/HTTPS audio-stream URL");
    readonly Label stationInfo = new() { Text = "Load live stations or paste a direct audio stream URL.", FontSize = 13 };
    InternetStation[] onlineStations = [];
    bool stationLoading;
    string liveSource = "Gqrx UDP";

    View BuildOnlinePanel(View gqrxPanel)
    {
        var load = new Button { Text = "Find live stations" };
        var settings = Section("Internet radio • no SDR required", Field("Station language / country", stationLanguage), load, stationPicker,
            Row(Field("Station order", stationPattern), Field("Listen per station (seconds)", onlineDwell)),
            Row(new Label { Text = "Automatically switch stations" }, onlineAutoHop), customStream, stationInfo,
            new Label { Text = "Plays real broadcaster streams directly over the internet. Station language depends on the current programme. Internet buffering makes switching slower than an FM sweep; use Chop in Effects for the fast rhythmic sound. No AM/FM frequency is measured in this mode.", FontSize = 13 });
        load.Clicked += async (_, _) => await LoadOnlineStationsAsync();
        onlineEnabled.Toggled += (_, _) =>
        {
            settings.IsVisible = onlineEnabled.IsToggled;
            gqrxPanel.IsVisible = !onlineEnabled.IsToggled;
            if (!demo.IsToggled) start.Text = onlineEnabled.IsToggled ? "Play online radio" : "Connect & scan";
        };
        gqrxPanel.IsVisible = false;
        start.Text = "Play online radio"; status.Text = "Internet radio ready"; detail.Text = "Live station streams • no radio hardware needed";
        return settings;
    }
    async Task LoadOnlineStationsAsync()
    {
        if (stationLoading) return;
        stationLoading = true; start.IsEnabled = false; stationInfo.Text = "Finding live station streams…";
        try
        {
            string language = new[] { "mt", "it", "en" }[Math.Clamp(stationLanguage.SelectedIndex, 0, 2)];
            onlineStations = await InternetStations.FindAsync(language, CancellationToken.None);
            stationPicker.ItemsSource = onlineStations.Select(s => s.Name).ToList(); stationPicker.SelectedIndex = 0;
            stationInfo.Text = $"{onlineStations.Length} station streams found. Availability and programme language can change.";
        }
        catch (Exception ex) { stationInfo.Text = ex.Message; ShowError(ex.Message); }
        finally { stationLoading = false; start.IsEnabled = true; }
    }
    InternetStation[] SelectedOnlineStations()
    {
        if (!string.IsNullOrWhiteSpace(customStream.Text))
        {
            if (!Uri.TryCreate(customStream.Text.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new ArgumentException("Use a direct HTTP/HTTPS audio-stream URL.");
            return [new("Custom live stream", uri.AbsoluteUri, "", "")];
        }
        if (onlineStations.Length == 0) throw new ArgumentException("Find live stations first.");
        if (!onlineAutoHop.IsToggled) return [onlineStations[Math.Clamp(stationPicker.SelectedIndex, 0, onlineStations.Length - 1)]];
        return onlineStations;
    }
}
