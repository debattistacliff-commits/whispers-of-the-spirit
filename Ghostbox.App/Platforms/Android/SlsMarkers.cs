using System.Globalization;
using Ghostbox.Core;

namespace Ghostbox.App;

/// <summary>
/// Android only: links the SLS camera with the radio.
/// - While a radio recording is running, a figure seen by the SLS camera adds a marker to that recording
///   (the same ".events.tsv" file used by "Mark this moment"), e.g. "SLS camera: figure 78%".
/// - When a recording is opened in the Review deck, the markers from its ".events.tsv" file are shown,
///   so camera moments line up with the sound on one timeline.
/// </summary>
public partial class MainPage
{
    /// <summary>Called by the console page (whispers://slsmark?value=score|figures). Returns the marker time, or -1.</summary>
    double MarkSlsFigure(string value)
    {
        string? file = captureCancellation is not null ? captureAudioPath : null;
        if (file is null) return -1;
        var parts = value.Split('|');
        int score = parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? Math.Clamp(s, 0, 100) : 0;
        int figures = parts.Length > 1 && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var f) ? Math.Clamp(f, 1, 9) : 1;
        double seconds = recordingTap.DataBytes / 96000.0;
        string note = figures > 1 ? $"SLS camera: {figures} figures {score}%" : $"SLS camera: figure {score}%";
        try
        {
            File.AppendAllText(Path.ChangeExtension(file, ".events.tsv"),
                FormattableString.Invariant($"{seconds:F3}\t{DateTime.UtcNow:O}\t{note}\n"));
            return seconds;
        }
        catch (IOException) { return -1; }
    }

    /// <summary>Adds the saved moments (camera and manual markers) of a recording to the Review deck markers.</summary>
    void LoadSavedEventMarkers(string recording)
    {
        try
        {
            string path = Path.ChangeExtension(recording, ".events.tsv");
            if (!File.Exists(path)) return;
            foreach (string row in File.ReadLines(path))
            {
                var cells = row.Split('\t');
                if (cells.Length >= 3 && double.TryParse(cells[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds)
                    && double.IsFinite(seconds) && seconds >= 0 && seconds <= reviewDuration + 1)
                    investigationMarkers.Add(new InvestigationMarker(Math.Min(seconds, reviewDuration), cells[2].Trim(), "Saved during recording."));
            }
            investigationMarkers.Sort((a, b) => a.Seconds.CompareTo(b.Seconds));
        }
        catch (IOException) { }
    }
}
