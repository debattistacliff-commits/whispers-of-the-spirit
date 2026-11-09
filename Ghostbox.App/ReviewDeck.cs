using System.Globalization;
using System.Text;
using System.Text.Json;
using Ghostbox.Core;

namespace Ghostbox.App;

/// <summary>Native side of the console's full-screen Review deck: lane analysis, slowed playback, cleanup and markers.</summary>
public partial class MainPage
{
    short[] deckSamples = [];
    double deckNoiseFloor = -90, deckPlayhead, deckSpeed = 1;
    bool deckKeepPitch = true;
    readonly HashSet<string> deckCleanup = [];
    string deckStatus = "Import a recording or choose a saved take";
    int deckViewRequest;
    double deckPlayFrom, deckPlayRate = 1;
    long deckPlayStartTicks;

    static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    async Task DeckLoadedAsync()
    {
        if (reviewCanonical is null) return;
        try
        {
            deckStatus = "Analysing recording…";
            deckSamples = await Task.Run(() => ReviewAnalysis.ReadPcm(reviewCanonical));
            deckNoiseFloor = await Task.Run(() => ReviewAnalysis.NoiseFloorDb(deckSamples));
            deckPlayhead = 0;
            var overview = await Task.Run(() => ReviewAnalysis.Analyze(deckSamples, 0, reviewDuration, 1200, deckNoiseFloor));
            await DeckScriptAsync($"window.whispersDeckOpen?.({{duration:{F(reviewDuration)},file:{JsonSerializer.Serialize(Path.GetFileName(reviewOriginal) ?? "")}}})");
            await DeckScriptAsync($"window.whispersDeckData?.({LanesJson(overview, "overview")})");
            deckStatus = "Ready • drag to select, click to set the playhead, wheel to zoom";
        }
        catch (Exception ex) { deckStatus = "Analysis failed: " + ex.Message; }
    }

    async Task DeckViewAsync(string value)
    {
        var parts = value.Split(':');
        if (parts.Length != 3 || deckSamples.Length == 0) return;
        if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var e) ||
            !int.TryParse(parts[2], out var width) || !double.IsFinite(s) || !double.IsFinite(e) || e <= s) return;
        int request = ++deckViewRequest;
        var samples = deckSamples; double floor = deckNoiseFloor;
        var lanes = await Task.Run(() => ReviewAnalysis.Analyze(samples, s, e, width, floor));
        if (request != deckViewRequest || samples != deckSamples) return;
        await DeckScriptAsync($"window.whispersDeckData?.({LanesJson(lanes, "view")})");
    }

    static string LanesJson(ReviewLanes lanes, string kind)
    {
        static string B(byte[] data) => Convert.ToBase64String(data);
        var pitch = new byte[lanes.PitchTenthsHz.Length * 2];
        Buffer.BlockCopy(lanes.PitchTenthsHz, 0, pitch, 0, pitch.Length);
        var sb = new StringBuilder();
        sb.Append("{kind:'").Append(kind).Append("',start:").Append(F(lanes.Start)).Append(",end:").Append(F(lanes.End))
          .Append(",cols:").Append(lanes.Columns).Append(",bins:").Append(lanes.Bins)
          .Append(",minHz:").Append(F(lanes.MinHz)).Append(",maxHz:").Append(F(lanes.MaxHz))
          .Append(",wmin:'").Append(B((byte[])(Array)lanes.WaveMin)).Append("',wmax:'").Append(B((byte[])(Array)lanes.WaveMax))
          .Append("',heat:'").Append(B(lanes.Heat)).Append("',voice:'").Append(B(lanes.Voice))
          .Append("',pitch:'").Append(B(pitch)).Append("',level:'").Append(B(lanes.Level)).Append("'}");
        return sb.ToString();
    }

    async Task DeckScriptAsync(string script)
    {
        if (consoleWebView is null) return;
        try { await MainThread.InvokeOnMainThreadAsync(() => consoleWebView.EvaluateJavaScriptAsync(script)); } catch { }
    }

    /// <summary>Current playhead in source seconds, extrapolated between audio chunks so the cursor moves smoothly.</summary>
    double DeckPlayheadNow()
    {
        if (workPlaybackCancellation is null || workPaused || deckPlayStartTicks == 0) return deckPlayhead;
        double elapsed = (Environment.TickCount64 - deckPlayStartTicks) / 1000.0;
        return Math.Min(reviewDuration, deckPlayFrom + elapsed * deckPlayRate);
    }

    string DeckStateJson()
    {
        double s = 0, e = 0;
        double.TryParse(rangeStart.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out s);
        double.TryParse(rangeEnd.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out e);
        var markers = string.Join(",", investigationMarkers.Select(m => $"{{t:{F(m.Seconds)},note:{JsonSerializer.Serialize(m.Note)}}}"));
        return $"{{loaded:{(deckSamples.Length > 0 ? "true" : "false")},duration:{F(reviewDuration)},playhead:{F(DeckPlayheadNow())},speed:{F(deckSpeed)}," +
               $"keepPitch:{(deckKeepPitch ? "true" : "false")},loop:{(loopRange.IsToggled ? "true" : "false")},rangeStart:{F(s)},rangeEnd:{F(e)}," +
               $"cleanup:{JsonSerializer.Serialize(deckCleanup.ToArray())},markers:[{markers}],status:{JsonSerializer.Serialize(deckStatus)}," +
               $"busy:{(processingCancellation is not null ? "true" : "false")}}}";
    }

    void DeckSetRange(double s, double e)
    {
        if (reviewDuration <= 0) return;
        s = Math.Clamp(s, 0, reviewDuration); e = Math.Clamp(e, 0, reviewDuration);
        if (e - s < 0.02) { s = 0; e = reviewDuration; }
        rangeStart.Text = s.ToString("F3", CultureInfo.InvariantCulture);
        rangeEnd.Text = e.ToString("F6", CultureInfo.InvariantCulture);
    }

    ProcessingRecipe DeckRecipe()
    {
        var controls = new Dictionary<string, ProcessingControl>();
        if (deckCleanup.Contains("voice")) controls["voice"] = new(true, 6);
        if (deckCleanup.Contains("hiss")) controls["hiss"] = new(true, 14);
        if (deckCleanup.Contains("hum")) { controls["hum"] = new(true, 50); controls["harmonics"] = new(true, 3); }
        if (deckCleanup.Contains("clicks")) controls["clicks"] = new(true, 50);
        if (deckSpeed < 0.999) controls["speed"] = new(true, deckSpeed);
        return new ProcessingRecipe { Controls = controls, PreservePitch = deckKeepPitch, Channels = 1 };
    }

    async void PlayDeck(double from)
    {
        if (workPlaybackCancellation is not null) { workPlaybackCancellation.Cancel(); return; }
        if (replayCancellation is not null) { ShowError("Stop original replay before playing the review."); return; }
        if (reviewCanonical is null || deckSamples.Length == 0) { ShowError("Load a recording into the Review deck first."); return; }
        if (cancellation is not null) { ShowError("Stop the live sweep before review playback, so the two sources are not mixed."); return; }
        double s, e;
        try { (s, e) = ReadRange(); } catch { s = 0; e = reviewDuration; }
        bool selection = e - s < reviewDuration - 0.01;
        if (!selection || from < s || from >= e - 0.01) from = selection ? s : (from >= reviewDuration - 0.05 ? 0 : from);
        double speed = deckSpeed;
        var recipe = DeckRecipe();
        bool render = recipe.Controls.Count > 0 && (deckCleanup.Count > 0 || deckKeepPitch);
        string fallbackNote = "";
        if (render && !await DeckEngineAvailableAsync())
        {   // No FFmpeg (for example on Android): keep slowing down, tape-style, and say so.
            render = false;
            fallbackNote = " • pitch-keeping and clean-up need FFmpeg, so this plays tape-style";
        }
        // A rendered loop replays the rendered file, so render a selection from its start.
        if (render && selection && loopRange.IsToggled) from = s;
        using var active = new CancellationTokenSource(); workPlaybackCancellation = active; workPaused = false;
        string? rendered = null;
        try
        {
            string path = reviewCanonical; long first = (long)(from * 48000) * 2, last = (long)(e * 48000) * 2;
            if (render)
            {
                deckStatus = speed < 0.999 ? $"Preparing {speed:0.##}× audio{(deckKeepPitch ? " with voice pitch kept" : "")}…" : "Applying clean-up…";
                await PushConsoleStateAsync();
                rendered = Path.Combine(WorkDirectory, Guid.NewGuid().ToString("N") + "-deck.wav");
                Investigation.EnginePath = enginePath.Text ?? "ffmpeg";
                await Investigation.RenderAsync(reviewCanonical, rendered, recipe, from, e, false, active.Token);
                path = rendered; var wave = ConversationFile.Inspect(rendered); first = 0; last = wave.Length;
            }
            deckStatus = fallbackNote.Length > 0 ? $"Playing {speed:0.##}×{fallbackNote}" :
                $"Playing {speed:0.##}×{(speed < 0.999 ? deckKeepPitch ? " • pitch kept" : " • tape-style" : "")}{(deckCleanup.Count > 0 ? " • clean-up: " + string.Join(", ", deckCleanup) : "")}";
            await Task.Run(async () =>
            {
                do
                {
                    var wave = ConversationFile.Inspect(path);
                    using var input = File.OpenRead(path); input.Position = wave.Offset + first;
                    using var output = new AudioOutput(); var buffer = new byte[1920]; var rate = new PlaybackRate();
                    deckPlayFrom = from; deckPlayRate = speed; deckPlayStartTicks = Environment.TickCount64; long pausedAt = 0;
                    for (long position = first; position < last;)
                    {
                        if (workPaused)
                        {
                            pausedAt = Environment.TickCount64;
                            while (workPaused) await Task.Delay(30, active.Token);
                            deckPlayStartTicks += Environment.TickCount64 - pausedAt;
                        }
                        int count = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, last - position)), active.Token); if (count == 0) break;
                        var audio = count == buffer.Length ? buffer : buffer[..count];
                        if (!render && speed < 0.999) audio = rate.Process(audio, speed);
                        output.Write(audio); position += count;
                        double sourceSeconds = render ? (position - first) / 96000.0 * speed : (position - first) / 96000.0;
                        deckPlayhead = Math.Min(e, from + sourceSeconds);
                        await Task.Delay(TimeSpan.FromSeconds(audio.Length / 96000.0), active.Token);
                    }
                    if (!render && speed < 0.999) output.Write(rate.Flush(speed));
                    await Task.Delay(100, active.Token);
                    // Loop: the selection (or, without one, the whole recording) starts again from its beginning.
                    if (!render) { from = selection ? s : 0; first = (long)(from * 48000) * 2; }
                } while (loopRange.IsToggled && !active.IsCancellationRequested);
            }, active.Token);
            deckStatus = "Stopped";
        }
        catch (OperationCanceledException) { deckStatus = "Stopped"; }
        catch (Exception ex) { deckStatus = "Playback failed"; ShowError("Review playback: " + ex.Message); }
        finally
        {
            workPlaybackCancellation = null; workPaused = false; deckPlayStartTicks = 0;
            if (rendered is not null) try { File.Delete(rendered); } catch { }
        }
    }

    async Task<bool> HandleDeckCommandAsync(string action, Dictionary<string, string> query)
    {
        string value = query.GetValueOrDefault("value", "");
        static bool D(string text, out double result) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result) && double.IsFinite(result);
        switch (action)
        {
            case "deckview": await DeckViewAsync(value); return true;
            case "deckplay": PlayDeck(D(value, out var from) ? from : deckPlayhead); return true;
            case "deckstop": workPlaybackCancellation?.Cancel(); return true;
            case "deckseek":
                if (D(value, out var seek))
                {
                    bool restart = workPlaybackCancellation is not null;
                    workPlaybackCancellation?.Cancel(); deckPlayhead = Math.Clamp(seek, 0, reviewDuration);
                    if (restart) { await Task.Delay(150); PlayDeck(deckPlayhead); }
                }
                return true;
            case "deckrange":
                var r = value.Split(':');
                if (r.Length == 2 && D(r[0], out var rs) && D(r[1], out var re)) { DeckSetRange(Math.Min(rs, re), Math.Max(rs, re)); deckPlayhead = Math.Min(rs, re); }
                return true;
            case "deckspeed":
                if (D(value, out var speed)) deckSpeed = Math.Clamp(speed, 0.1, 1);
                await RestartDeckIfPlayingAsync(); return true;
            case "deckpitch": deckKeepPitch = value == "1"; await RestartDeckIfPlayingAsync(); return true;
            case "deckloop": loopRange.IsToggled = value == "1"; return true;
            case "deckclean":
                var c = value.Split(':');
                if (c.Length == 2 && c[0] is "voice" or "hiss" or "hum" or "clicks") { if (c[1] == "1") deckCleanup.Add(c[0]); else deckCleanup.Remove(c[0]); }
                await RestartDeckIfPlayingAsync(); return true;
            case "deckmarker":
                var m = value.Split('|', 2);
                if (D(m[0], out var at) && reviewDuration > 0)
                {
                    investigationMarkers.Add(new(Math.Clamp(at, 0, reviewDuration), m.Length > 1 ? m[1].Trim() : "", RecipeSummary()));
                    investigationMarkers.Sort((a, b) => a.Seconds.CompareTo(b.Seconds)); UpdateReviewMarkers();
                }
                return true;
            case "deckmarkerdel":
                if (int.TryParse(value, out var index) && index >= 0 && index < investigationMarkers.Count) { investigationMarkers.RemoveAt(index); UpdateReviewMarkers(); }
                return true;
            case "decksave":
                if (reviewOriginal is null) { ShowError("Load a recording first."); return true; }
                if (!string.IsNullOrWhiteSpace(value)) sessionName.Text = value;
                await SaveWorkSessionAsync(); deckStatus = processState.Text; return true;
            case "deckexport":
                if (reviewOriginal is null) { ShowError("Load a recording first."); return true; }
                exportFormat.SelectedIndex = value == "mp3" ? 1 : 0; compareAudio.SelectedIndex = 0;
                await ExportWorkAsync(false); deckStatus = processState.Text; return true;
        }
        return false;
    }

    bool? deckEngine;
    string? deckEnginePath;
    async Task<bool> DeckEngineAvailableAsync()
    {
        string path = enginePath.Text ?? "ffmpeg";
        if (deckEngine.HasValue && deckEnginePath == path) return deckEngine.Value;
        Investigation.EnginePath = path; deckEnginePath = path;
        try { await Investigation.RunAsync(["-hide_banner", "-loglevel", "error", "-version"], CancellationToken.None); deckEngine = true; }
        catch { deckEngine = false; }
        return deckEngine.Value;
    }

    async Task RestartDeckIfPlayingAsync()
    {
        if (workPlaybackCancellation is null) return;
        double at = DeckPlayheadNow();
        workPlaybackCancellation.Cancel();
        await Task.Delay(150);
        PlayDeck(at);
    }
}
