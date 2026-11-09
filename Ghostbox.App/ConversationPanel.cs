using Ghostbox.Core;

namespace Ghostbox.App;

public partial class MainPage
{
    readonly List<ConversationSegment> conversationSegments = [];
    readonly Label conversationInfo = new() { Text = "Your questions and recorded receiver clips will be saved into one conversation WAV.", FontSize = 13 };
    string? conversationPath;

    View BuildConversationControls()
    {
        var save = new Button { Text = "Save conversation WAV" }; save.Clicked += SaveConversation;
        var reset = new Button { Text = "Start a new conversation file" };
        reset.Clicked += (_, _) =>
        {
            if (microphoneCancellation is not null || captureCancellation is not null) { ShowError("Stop the current recording first."); return; }
            conversationSegments.Clear(); conversationPath = null;
            savedAudio.SelectedIndex = -1;
            conversationInfo.Text = "New conversation ready. Press Speak to record your question.";
        };
        return Section("One conversation file", conversationInfo, save, reset,
            new Label { Text = "Every completed microphone question and receiver recording is appended in order. Original audio is kept. Source labels and UTC timestamps are embedded in the WAV; pauses between clips are omitted.", FontSize = 12 });
    }
    async Task AppendConversationAsync(string path, string source)
    {
        if (!File.Exists(path) || ConversationFile.Inspect(path).Length == 0) return;
        conversationSegments.Add(new(path, source, File.GetCreationTimeUtc(path)));
        conversationPath ??= NewRecordingPath("conversation");
        var snapshot = conversationSegments.ToArray(); string target = conversationPath;
        await Task.Run(() => ConversationFile.Save(target, snapshot));
        double seconds = ConversationFile.Inspect(target).Length / 96000.0;
        conversationInfo.Text = $"Conversation saved: {snapshot.Length} parts • {seconds:F1}s\n{Path.GetFileName(target)}";
        RefreshRecordings(target);
    }
    async void SaveConversation(object? sender, EventArgs e)
    {
        if (microphoneCancellation is not null || captureCancellation is not null) { ShowError("Stop the current recording before saving a copy."); return; }
        string? file = conversationPath;
        if (file is null && SelectedRecording?.EndsWith("-conversation.wav", StringComparison.OrdinalIgnoreCase) == true) file = SelectedRecording;
        if (file is null) { ShowError("Record a question first, then record the receiver. The conversation file is created automatically."); return; }
        try
        {
#if WINDOWS
            var picker = new Windows.Storage.Pickers.FileSavePicker { SuggestedFileName = Path.GetFileNameWithoutExtension(file), SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary };
            picker.FileTypeChoices.Add("WAV conversation audio", new List<string> { ".wav" });
            var native = (Microsoft.UI.Xaml.Window)Application.Current!.Windows[0].Handler!.PlatformView!;
            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(native));
            var destination = await picker.PickSaveFileAsync();
            if (destination is null) return;
            using var source = File.OpenRead(file); using var output = await destination.OpenStreamForWriteAsync();
            await source.CopyToAsync(output); output.SetLength(source.Length);
            conversationInfo.Text = "Conversation WAV saved to " + destination.Path;
#elif LINUX
            // Linux build (Ghostbox.Linux): GTK Save dialog.
            string? destination = await Ghostbox.Linux.LinuxHost.SaveFileAsync("Save conversation WAV", Path.GetFileName(file));
            if (destination is null) return;
            if (Path.GetFullPath(destination) == Path.GetFullPath(file)) throw new IOException("Choose a different filename.");
            File.Copy(file, destination, overwrite: true);
            conversationInfo.Text = "Conversation WAV saved to " + destination;
#else
            await Share.Default.RequestAsync(new ShareFileRequest { Title = "Save VOX conversation WAV", File = new ShareFile(file) });
#endif
        }
        catch (Exception ex) { ShowError("Saving conversation: " + ex.Message); }
    }
}
