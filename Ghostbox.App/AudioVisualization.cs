using System.Globalization;

namespace Ghostbox.App;

public partial class MainPage
{
    readonly AudioHeatDrawing heatDrawing = new();
    readonly GraphicsView heatView = new() { HeightRequest = 250 };
    readonly Label heatSource = new() { Text = "Waiting for receiver audio", FontSize = 12 };
    readonly Label markerInfo = new() { Text = "Mark a moment while recording received audio or replaying a saved clip.", FontSize = 12 };
    readonly Picker eventPicker = new() { Title = "Replay from a saved marker (or start)" };
    readonly List<double> eventSeconds = [0];
    string? captureAudioPath, replayAudioPath, markerAudioPath;
    long replayBytes;

    View BuildHeatPanel()
    {
        heatView.Drawable = heatDrawing;
        SemanticProperties.SetDescription(heatView, "Waveform above a scrolling sound spectrogram. Brighter colours mean stronger audio, not temperature.");
        var mark = new Button { Text = "Mark this moment" }; mark.Clicked += MarkAudioMoment;
        savedAudio.SelectedIndexChanged += (_, _) => LoadAudioMarkers();
        return Section("Live waveform & sound heat map", heatSource, heatView,
            new Label { Text = "Top: 43 ms waveform, fixed full-scale amplitude. Below: ~15 seconds of sound history; newest at right. Low frequencies (50 Hz) at bottom, high (16 kHz) at top; logarithmic bands. Dark → green → yellow = −80 → 0 dBFS band amplitude.", FontSize = 12 },
            new Label { Text = "Audio energy only: no temperature, direction, distance or speaker identification. Online receiver audio comes from its remote antenna or station stream.", FontSize = 12 },
            mark, markerInfo, eventPicker,
            new Label { Text = "Select a recording and marker, then press Replay in Review. Markers are included when exporting all recordings.", FontSize = 12 });
    }
    void LoadAudioMarkers()
    {
        markerAudioPath = SelectedRecording; eventSeconds.Clear(); eventSeconds.Add(0);
        var labels = new List<string> { "Start of recording" };
        if (markerAudioPath is not null)
        {
            try
            {
                string path = Path.ChangeExtension(markerAudioPath, ".events.tsv");
                if (File.Exists(path)) foreach (string row in File.ReadLines(path))
                {
                    var cells = row.Split('\t');
                    if (cells.Length >= 3 && double.TryParse(cells[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds) && double.IsFinite(seconds) && seconds >= 0)
                    { eventSeconds.Add(seconds); labels.Add($"{seconds:F2}s • {cells[2]}"); }
                }
            }
            catch (IOException ex) { markerInfo.Text = "Reading markers: " + ex.Message; }
        }
        eventPicker.ItemsSource = labels; eventPicker.SelectedIndex = 0;
    }
    void MarkAudioMoment(object? sender, EventArgs e)
    {
        string? file = captureCancellation is not null ? captureAudioPath : replayAudioPath;
        long bytes = captureCancellation is not null ? recordingTap.DataBytes : Interlocked.Read(ref replayBytes);
        if (file is null) { markerInfo.Text = "Start a receiver recording or replay first, then mark a moment."; return; }
        try
        {
            double seconds = bytes / 96000.0;
            File.AppendAllText(Path.ChangeExtension(file, ".events.tsv"), FormattableString.Invariant($"{seconds:F3}\t{DateTime.UtcNow:O}\tManual listening marker\n"));
            markerInfo.Text = $"Marked {seconds:F2}s in {Path.GetFileName(file)}";
            if (SelectedRecording == file) LoadAudioMarkers();
        }
        catch (IOException ex) { ShowError("Saving marker: " + ex.Message); }
    }
}
