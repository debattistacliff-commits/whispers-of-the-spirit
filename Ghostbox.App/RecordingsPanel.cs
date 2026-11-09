using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using Ghostbox.Core;

namespace Ghostbox.App;

public sealed record TranscriptResult(string text, string language, string warning);
public sealed record SpeechRegion(double start, double end);
public sealed record SpeechDetection(double duration, double speech_seconds, SpeechRegion[] segments, string warning);

public partial class MainPage
{
    readonly RecordingTap recordingTap = new();
    readonly Picker speechLanguage = new() { ItemsSource = new[] { "Maltese / Malti (experimental)", "Italian / Italiano", "English" }, SelectedIndex = 0 };
    readonly Picker savedAudio = new() { Title = "Choose a recording" };
    readonly Picker microphoneInput = new() { Title = "Microphone input" };
    readonly Label microphoneLevel = new() { Text = "Speak during recording and check that the level moves.", FontSize = 13 };
    readonly ProgressBar microphoneMeter = new() { ProgressColor = Color.FromArgb("45A995") };
    readonly Entry speechServer = Input("http://127.0.0.1:18765", "Speech server URL");
    readonly Entry speechToken = new() { Placeholder = "Optional LAN server token", IsPassword = true };
    readonly Entry captureSeconds = Input("15", "Radio recording seconds", true);
    readonly Label recordingState = new() { Text = "No recording in progress", FontSize = 14 };
    readonly Editor transcript = new() { IsReadOnly = true, AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 100, Placeholder = "Machine transcript appears here. Always check it against the original audio." };
    readonly Button microphoneButton = new() { Text = "1. Record my question" };
    readonly Button captureButton = new() { Text = "2. Record radio audio" };
    readonly Button transcribeButton = new() { Text = "Transcribe selected recording" };
    readonly Button replayButton = new() { Text = "Replay original" };
    readonly Slider playbackSpeed = new() { Minimum = .25, Maximum = 1, Value = 1 };
    double replaySpeed = 1;
    CancellationTokenSource? microphoneCancellation, captureCancellation, speechCancellation, replayCancellation;
    bool microphoneStarting;
    readonly Switch stopMicOnSilence=new() {IsToggled=true};
    readonly Slider micSilenceSeconds=new() {Minimum=.5,Maximum=4,Value=1.5};
    readonly Slider micActivityThreshold=new() {Minimum=-60,Maximum=-15,Value=-38};
    readonly Label micEndpointSettings=new() {FontSize=12};
    bool radioAudioRunning;
    string[] savedPaths = [];
    string? questionPath;
    List<string>? captureTuneLog;
    List<string>? captureSourceLog;
    long lastFrequencyHz;
    static string RecordingDirectory => Path.Combine(FileSystem.AppDataDirectory, "Recordings");
    string LanguageCode => new[] { "mt", "it", "en" }[Math.Clamp(speechLanguage.SelectedIndex, 0, 2)];
    string? SelectedRecording => savedAudio.SelectedIndex >= 0 && savedAudio.SelectedIndex < savedPaths.Length ? savedPaths[savedAudio.SelectedIndex] : null;

    View BuildRecordingsPanel()
    {
        Directory.CreateDirectory(RecordingDirectory);
        try
        {
            var inputs = MicrophoneRecorder.Inputs(); microphoneInput.ItemsSource = inputs;
            int savedInput = Array.IndexOf(inputs, Preferences.Default.Get("microphone-name", "Windows default microphone"));
            microphoneInput.SelectedIndex = Math.Max(0, savedInput);
            microphoneInput.SelectedIndexChanged += (_, _) => Preferences.Default.Set("microphone-name", microphoneInput.SelectedItem as string ?? "");
        }
        catch (Exception ex) { microphoneLevel.Text = "Microphone enumeration: " + ex.Message; }
        speechServer.Text = Preferences.Default.Get("speech-server", "http://127.0.0.1:18765");
        speechLanguage.SelectedIndex = Preferences.Default.Get("speech-language", 0);
        if (speechLanguage.SelectedIndex is < 0 or > 2) speechLanguage.SelectedIndex = 0;
        microphoneButton.Clicked += RecordQuestion;
        captureButton.Clicked += RecordRadio;
        transcribeButton.Clicked += TranscribeRecording;
        replayButton.Clicked += ReplayRecording;
        playbackSpeed.ValueChanged += (_, e) => Volatile.Write(ref replaySpeed, e.NewValue);
        var export = new Button { Text = "Export recordings and transcripts" };
        var detectSpeech = new Button { Text = "Detect speech in selected recording" }; detectSpeech.Clicked += DetectSpeech;
        export.Clicked += ExportRecordings;
        RefreshRecordings();
        return Section("Record & transcribe", Field("Transcription language", speechLanguage),
            new Label { Text = "This selects the speech recognizer's language. It does not change what stations broadcast. Maltese recognition is experimental; noise can be mistaken for words.", FontSize = 13 },
            Field("Microphone input", microphoneInput), BuildMicEndpointControls(), microphoneButton, microphoneMeter, microphoneLevel,
            Field("Radio capture duration (1–120 seconds)", captureSeconds), captureButton, recordingState,
            new Label { Text = "Press Speak to record your question. Stop manually or enable automatic stop after a quiet pause. With live reception running, automatic listening can record the receiver afterward. Mute-while-speaking and automatic capture are in Effects. Your question and received audio remain separate files.", FontSize = 13 },
            BuildConversationControls(), savedAudio,
            SliderField("Slow-motion replay speed", playbackSpeed, v => $"{v:F2}×"),
            new Label { Text = "¼ speed to normal; adjustable during replay. Slower playback lowers pitch. Live reception and saved WAVs keep their original speed. Manual mode stays on until stopped; automatic mode ends after the configured quiet pause.", FontSize = 12 },
            Row(replayButton, export),
            Field("Local speech server on your computer", speechServer), speechToken,
            new Label { Text = "Start the included SpeechServer on your computer. On Android, enter that computer's LAN IP. Only pressing Transcribe sends the selected raw recording to this address; it is not sent to a cloud API.", FontSize = 12 },
            detectSpeech, transcribeButton, transcript);
    }
    void RefreshRecordings(string? select = null)
    {
        savedPaths = Directory.GetFiles(RecordingDirectory, "*.wav").Where(p => !p.EndsWith("-processed.wav", StringComparison.OrdinalIgnoreCase)).OrderByDescending(Path.GetFileName).Take(100).ToArray();
        savedAudio.ItemsSource = savedPaths.Select(Path.GetFileNameWithoutExtension).ToList();
        savedAudio.SelectedIndex = select is null ? (savedPaths.Length > 0 ? 0 : -1) : Array.IndexOf(savedPaths, select);
    }
    string NewRecordingPath(string source) => Path.Combine(RecordingDirectory, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid().ToString("N")[..6]}-{source}.wav");
    async void DetectSpeech(object? sender, EventArgs e)
    {
        var file = SelectedRecording;
        if (file is null) { ShowError("Select a saved recording first."); return; }
        if (speechCancellation is not null || microphoneCancellation is not null || captureCancellation is not null) { ShowError("Finish recording or the current speech check first."); return; }
        try
        {
            if (ConversationFile.Inspect(file).Length > 120 * 96000) { ShowError("Choose a clip up to 120 seconds for speech detection."); return; }
            if (!Uri.TryCreate(speechServer.Text?.Trim().TrimEnd('/') + "/detect-speech", UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new IOException("Enter a valid local speech-server address.");
            using var active = new CancellationTokenSource(TimeSpan.FromMinutes(2)); speechCancellation = active;
            try
            {
                transcript.Text = "Checking for possible speech activity…";
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
                using var request = new HttpRequestMessage(HttpMethod.Post, uri);
                if (!string.IsNullOrWhiteSpace(speechToken.Text)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", speechToken.Text.Trim());
                request.Content = new ByteArrayContent(await File.ReadAllBytesAsync(file, active.Token)); request.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                using var response = await client.SendAsync(request, active.Token);
                string body = await response.Content.ReadAsStringAsync(active.Token);
                if (!response.IsSuccessStatusCode) throw new IOException($"Speech detector returned {(int)response.StatusCode}. Start the updated included SpeechServer.");
                var result = JsonSerializer.Deserialize(body, SettingsJsonContext.Default.SpeechDetection) ?? throw new IOException("Invalid speech detector result.");
                transcript.Text = $"POSSIBLE SPEECH ACTIVITY • {result.speech_seconds:F1}s / {result.duration:F1}s\n\n" +
                    (result.segments.Length == 0 ? "No speech-like segments detected. This does not prove speech is absent." : string.Join("\n", result.segments.Select(s => $"{s.start:F2}–{s.end:F2}s • possible speech"))) + "\n\n" + result.warning;
                await File.WriteAllTextAsync(Path.ChangeExtension(file, ".speech-detection.txt"), transcript.Text, active.Token);
                if(string.Equals(reviewOriginal,file,StringComparison.OrdinalIgnoreCase))
                {
                    investigationMarkers.RemoveAll(m=>m.Note.StartsWith("[Speech detector]",StringComparison.Ordinal));
                    foreach(var segment in result.segments.Where(s=>double.IsFinite(s.start)&&double.IsFinite(s.end)&&s.start>=0&&s.end>s.start&&s.end<=reviewDuration+.1))
                        investigationMarkers.Add(new(segment.start,$"[Speech detector] Possible speech {segment.start:F2}–{segment.end:F2}s","Detected from original recording; verify by listening."));
                    UpdateReviewMarkers();
                }
            }
            finally { speechCancellation = null; }
        }
        catch (Exception ex) { transcript.Text = "Speech detection unavailable; the original recording is unchanged."; ShowError(ex.Message); }
    }
    View BuildMicEndpointControls()
    {
        stopMicOnSilence.IsToggled=Preferences.Default.Get("mic-auto-stop",true);
        micSilenceSeconds.Value=Math.Clamp(Preferences.Default.Get("mic-silence",1.5),.5,4);
        micActivityThreshold.Value=Math.Clamp(Preferences.Default.Get("mic-threshold",-38d),-60,-15);
        void Update()
        {
            micEndpointSettings.Text=$"Stop after {micSilenceSeconds.Value:F1}s quiet • activity threshold {micActivityThreshold.Value:F0} dBFS";
            Preferences.Default.Set("mic-auto-stop",stopMicOnSilence.IsToggled);Preferences.Default.Set("mic-silence",micSilenceSeconds.Value);Preferences.Default.Set("mic-threshold",micActivityThreshold.Value);
        }
        stopMicOnSilence.Toggled+=(_,_)=>Update();micSilenceSeconds.ValueChanged+=(_,_)=>Update();micActivityThreshold.ValueChanged+=(_,_)=>Update();Update();
        ToolTipProperties.SetText(micActivityThreshold,"Lower values detect quieter speech but may include background noise. A level gate cannot reliably distinguish voices from other sounds.");
        ToolTipProperties.SetText(micSilenceSeconds,"Time below the threshold after sustained audio before the microphone stops. Increase this if pauses cut off your question.");
        return Section("Microphone automatic stop",Row(new Label {Text="Stop after I finish speaking"},stopMicOnSilence),micEndpointSettings,Field("Quiet pause (0.5–4 seconds)",micSilenceSeconds),Field("Activity threshold (dBFS)",micActivityThreshold),new Label {Text="Press Speak to begin. Automatic mode stops after sustained sound followed by quiet; it waits up to 20 seconds for activity. Background sound can keep it open. Raw microphone audio is preserved; use Cleanup for noise reduction. Turn this off for manual recording.",FontSize=12});
    }
    async void RecordQuestion(object? sender, EventArgs e)
    {
        if (microphoneCancellation is not null) { microphoneCancellation.Cancel(); return; }
        if (microphoneStarting || captureCancellation is not null) { ShowError("Finish the current recording first."); return; }
        if (replayCancellation is not null || workPlaybackCancellation is not null) { ShowError("Stop review playback before recording your question."); return; }
        microphoneStarting = true;
        try
        {
            if (await Permissions.RequestAsync<Permissions.Microphone>() != PermissionStatus.Granted) { ShowError("Microphone permission is needed to record your question."); return; }
            string path = NewRecordingPath("question-microphone");
            var active = microphoneCancellation = new CancellationTokenSource();
            microphoneButton.Text = "Stop recording question"; captureButton.IsEnabled = false; start.IsEnabled = false;
            microphoneInput.IsEnabled = false;
            monitorMuted = muteWhenSpeaking.IsToggled;
            recordingState.Text = "Microphone ON • keep talking; press Stop recording question when finished";
            if(stopMicOnSilence.IsToggled)recordingState.Text="Microphone ON • automatic stop after a quiet pause; you can also stop manually";
            try
            {
                int device = microphoneInput.SelectedIndex - 1;
                double maxLevel = -120;
                var level = new Progress<double>(db =>
                {
                    if (microphoneCancellation != active) return;
                    maxLevel = Math.Max(maxLevel, db);
                    microphoneMeter.Progress = Math.Clamp((db + 72) / 72, 0, 1);
                    microphoneLevel.Text = $"Microphone: {db:F1} dBFS" + (db < -55 ? " • very quiet; check the selected input" : " • receiving sound");
                });
                var endpoint=stopMicOnSilence.IsToggled?new SilenceStop(micActivityThreshold.Value,micSilenceSeconds.Value):null;
                await Task.Run(() => MicrophoneRecorder.RecordAsync(path, device, level, active.Token,endpoint));
                questionPath = path;
                await File.WriteAllTextAsync(Path.ChangeExtension(path, ".txt"), "SOURCE: Microphone question. Not radio reception.\nUTC: " + DateTime.UtcNow.ToString("O"));
                recordingState.Text = maxLevel < -55 ? "Question saved, but the microphone was very quiet. Choose another input and try again." : "Question saved. Start live radio audio, then record what is received.";
                if(endpoint is not null)recordingState.Text=endpoint.TimedOut?"Stopped after 20 seconds without sustained audio above the threshold. Question audio saved; check input and sensitivity.":"Question saved • microphone stopped. Press Speak for your next question.";
                RefreshRecordings(path);
                await AppendConversationAsync(path, "MICROPHONE QUESTION");
            }
            finally { microphoneCancellation = null; active.Dispose(); monitorMuted = false; microphoneButton.Text = "1. Record my question"; captureButton.IsEnabled = true; start.IsEnabled = cancellation is null; microphoneInput.IsEnabled = true; }
            if (autoListen.IsToggled && radioAudioRunning && cancellation is { IsCancellationRequested: false }) RecordRadio(autoListen, EventArgs.Empty);
        }
        catch (Exception ex) { recordingState.Text = "Question recording stopped"; ShowError("Microphone: " + ex.Message); }
        finally { microphoneStarting = false; }
    }
    async void RecordRadio(object? sender, EventArgs e)
    {
        if (captureCancellation is not null) { captureCancellation.Cancel(); return; }
        if (!radioAudioRunning || cancellation is null || demo.IsToggled) { ShowError("Start live online radio or Gqrx audio first. Demo sound cannot be saved as radio reception."); return; }
        bool timed = ReferenceEquals(sender, autoListen);
        int seconds = 0;
        try { if (timed) { seconds = Integer(captureSeconds, "Duration"); if (seconds is < 1 or > 120) throw new ArgumentException("Duration must be 1–120 seconds."); } }
        catch (ArgumentException ex) { ShowError(ex.Message); return; }
        var active = captureCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
        string raw = NewRecordingPath("radio-raw"), processed = raw.Replace("-raw.wav", "-processed.wav");
        captureAudioPath = raw;
        captureButton.Text = "Stop radio recording"; microphoneButton.IsEnabled = false;
        try
        {
            captureTuneLog = ["utc,requested_frequency_hz", $"{DateTime.UtcNow:O},{lastFrequencyHz}"];
            captureSourceLog = [$"{DateTime.UtcNow:O} {liveSource}"];
            recordingTap.Begin(raw, processed, recordEffects.IsToggled);
            liveEffectLog=[DateTime.UtcNow.ToString("O")+" "+JsonSerializer.Serialize(effects.Settings,SettingsJsonContext.Default.EffectSettings)];
            if (!timed)
            {
                recordingState.Text = $"Recording until stopped • {Path.GetFileName(raw)}";
                await Task.Delay(Timeout.Infinite, active.Token);
            }
            for (int remaining = seconds; remaining > 0; remaining--)
            { recordingState.Text = $"Recording real incoming radio • {remaining}s remaining"; await Task.Delay(1000, active.Token); }
        }
        catch (OperationCanceledException) when (active.IsCancellationRequested) { }
        catch (Exception ex) { ShowError("Recording: " + ex.Message); }
        finally
        {
            try
            {
                long bytes = recordingTap.End();
                var tuning = captureTuneLog?.ToArray(); captureTuneLog = null;
                var sources = captureSourceLog?.ToArray(); captureSourceLog = null;
                recordingState.Text = bytes > 0 ? $"Saved {bytes / 96000.0:F1}s of raw received audio"+(File.Exists(processed)?" + opted-in effects copy.":".") : "No incoming audio was recorded. No response was generated.";
                if(File.Exists(processed))await File.WriteAllLinesAsync(processed+".settings-log.txt",liveEffectLog??[]);
                liveEffectLog=null;
                await File.WriteAllTextAsync(Path.ChangeExtension(raw, ".txt"), $"SOURCE: {liveSource}, raw pre-effects.\nLanguage selection: {LanguageCode}\nReceived audio duration: {bytes / 96000.0:F3}s\nQuestion file: {Path.GetFileName(questionPath)}\nA recording after a question is not proof of a response.\n" + string.Join("\n", sources ?? []));
                if (tuning is not null && !onlineEnabled.IsToggled) await File.WriteAllLinesAsync(Path.ChangeExtension(raw, ".tuning.csv"), tuning);
                RefreshRecordings(raw);
                if (bytes > 0)
                {
                    string mp3 = Path.ChangeExtension(raw, ".mp3");
                    try
                    {
                        Investigation.EnginePath = enginePath.Text ?? "ffmpeg";
                        await Investigation.RunAsync(["-nostdin", "-n", "-i", raw, "-vn", "-c:a", "libmp3lame", "-b:a", "192k", mp3], CancellationToken.None);
                        recordingState.Text = $"Saved WAV + MP3 in {RecordingDirectory}: {Path.GetFileName(mp3)}";
                    }
                    catch (Exception ex) { recordingState.Text = $"WAV saved at {raw}. MP3 encoding failed: {ex.Message}"; }
                }
                if (bytes > 0) await AppendConversationAsync(raw, "RECEIVED RADIO AUDIO");
            }
            catch (Exception ex) { ShowError("Saving recording: " + ex.Message); }
            captureAudioPath = null;
            captureTuneLog = null; captureSourceLog = null; captureCancellation = null; active.Dispose(); captureButton.Text = "2. Record radio audio"; microphoneButton.IsEnabled = true;
        }
    }
    async void TranscribeRecording(object? sender, EventArgs e)
    {
        if (speechCancellation is not null) { speechCancellation.Cancel(); return; }
        string? file = SelectedRecording;
        if (file is null || !File.Exists(file) || new FileInfo(file).Length <= 44) { ShowError("Select a recording containing real audio first."); return; }
        try { if (ConversationFile.Inspect(file).Length > 120 * 96000) { ShowError("This conversation is longer than the speech server's 120-second limit. Choose an individual original clip to transcribe."); return; } }
        catch (IOException ex) { ShowError("Recording: " + ex.Message); return; }
        if (captureCancellation is not null || microphoneCancellation is not null) { ShowError("Finish recording before transcribing."); return; }
        if (!Uri.TryCreate(speechServer.Text?.Trim().TrimEnd('/') + "/transcribe?language=" + LanguageCode, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) { ShowError("Enter a valid speech-server HTTP or HTTPS address."); return; }
        var active = speechCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        transcribeButton.Text = "Cancel transcription"; transcript.Text = "Transcribing the selected recording…";
        Preferences.Default.Set("speech-server", speechServer.Text); Preferences.Default.Set("speech-language", speechLanguage.SelectedIndex);
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            if (!string.IsNullOrWhiteSpace(speechToken.Text)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", speechToken.Text.Trim());
            request.Content = new ByteArrayContent(await File.ReadAllBytesAsync(file, active.Token)); request.Content.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            using var response = await client.SendAsync(request, active.Token);
            string body = await response.Content.ReadAsStringAsync(active.Token);
            if (!response.IsSuccessStatusCode) throw new IOException($"Speech server returned {(int)response.StatusCode}: {body[..Math.Min(body.Length, 300)]}");
            var result = JsonSerializer.Deserialize(body, SettingsJsonContext.Default.TranscriptResult) ?? throw new IOException("Invalid server response.");
            transcript.Text = $"MACHINE TRANSCRIPT ({result.language}) — verify against raw audio\n\n{(string.IsNullOrWhiteSpace(result.text) ? "No clear speech detected." : result.text)}\n\n{result.warning}";
            await File.WriteAllTextAsync(Path.ChangeExtension(file, ".transcript.txt"), transcript.Text, active.Token);
        }
        catch (OperationCanceledException) { transcript.Text = "Transcription cancelled or timed out. The original recording is unchanged."; }
        catch (Exception ex) { transcript.Text = "Transcription unavailable. The original recording is saved."; ShowError(ex.Message); }
        finally { speechCancellation = null; active.Dispose(); transcribeButton.Text = "Transcribe selected recording"; }
    }
    async void ReplayRecording(object? sender, EventArgs e)
    {
        if (replayCancellation is not null) { replayCancellation.Cancel(); return; }
        var file = SelectedRecording;
        if (file is null) { ShowError("Select a saved recording."); return; }
        if (cancellation is not null || microphoneCancellation is not null) { ShowError("Stop the current audio session before replaying."); return; }
        var active = replayCancellation = new CancellationTokenSource(); replayButton.Text = "Stop replay"; start.IsEnabled = false; microphoneButton.IsEnabled = false;
        double startSeconds = markerAudioPath == file && eventPicker.SelectedIndex >= 0 && eventPicker.SelectedIndex < eventSeconds.Count ? eventSeconds[eventPicker.SelectedIndex] : 0;
        replayAudioPath = file; Interlocked.Exchange(ref replayBytes, 0);
        heatDrawing.Reset(); heatView.Invalidate();
        try
        {
            await Task.Run(async () =>
            {
                var wave = ConversationFile.Inspect(file);
                long skip = Math.Min(wave.Length, (long)(Math.Min(startSeconds, wave.Length / 96000.0) * 48000) * 2);
                using var input = File.OpenRead(file); input.Position = wave.Offset + skip; long remaining = wave.Length - skip;
                Interlocked.Exchange(ref replayBytes, skip);
                var replayAnalyzer = new AudioAnalyzer();
                var speedConverter = new PlaybackRate();
                using var output = new AudioOutput(); var buffer = new byte[1920];
                while (true)
                {
                    if (remaining == 0) break;
                    int read = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), active.Token); if (read == 0) break; remaining -= read;
                    var slowed = speedConverter.Process(read == buffer.Length ? buffer : buffer[..read], Volatile.Read(ref replaySpeed));
                    output.Write(slowed); await Task.Delay(TimeSpan.FromSeconds(slowed.Length / 96000.0), active.Token);
                    Interlocked.Add(ref replayBytes, read);
                    if (replayAnalyzer.Add(read == buffer.Length ? buffer : buffer[..read]) is { } frame)
                        MainThread.BeginInvokeOnMainThread(() => { heatDrawing.Add(frame); heatView.Invalidate(); heatSource.Text = "SAVED AUDIO • " + Path.GetFileName(file); });
                }
                var tail = speedConverter.Flush(Volatile.Read(ref replaySpeed)); output.Write(tail);
                await Task.Delay(100, active.Token);
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ShowError("Replay: " + ex.Message); }
        finally { replayAudioPath = null; replayCancellation = null; active.Dispose(); replayButton.Text = "Replay original"; start.IsEnabled = true; microphoneButton.IsEnabled = true; }
    }
    async void ExportRecordings(object? sender, EventArgs e)
    {
        if (captureCancellation is not null || microphoneCancellation is not null) { ShowError("Finish recording before exporting."); return; }
        if (savedPaths.Length == 0) { ShowError("Record audio first."); return; }
        try
        {
            string zip = Path.Combine(FileSystem.CacheDirectory, $"Ghostbox-recordings-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip");
            ZipFile.CreateFromDirectory(RecordingDirectory, zip);
            string? saved=await SaveExportFileAsync(zip,"Whispers-recordings-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".zip");
            recordingState.Text=saved is null?"Save cancelled • recordings kept":"Recordings archive saved: "+saved;
        }
        catch (Exception ex) { ShowError("Export: " + ex.Message); }
    }
    void StopRecordingOperations()
    {
        processingCancellation?.Cancel(); workPlaybackCancellation?.Cancel();
        microphoneCancellation?.Cancel(); captureCancellation?.Cancel(); speechCancellation?.Cancel(); replayCancellation?.Cancel();
    }
}

