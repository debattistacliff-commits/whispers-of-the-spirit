namespace Ghostbox.App;

public partial class MainPage
{
    async Task ShareSavedAudioAsync()
    {
        if(sharingExport){processState.Text="A share request is already open. Close it with Escape, or use Show saved file to attach the audio directly.";return;}
        if(lastSavedExport is null||!File.Exists(lastSavedExport)){ShowError("Export and save an audio file first.");return;}
        sharingExport=true;
        try
        {
            processState.Text="Choose a sharing app. If Windows Share stalls, press Escape and use Show saved file.";
            string mime=Path.GetExtension(lastSavedExport).Equals(".mp3",StringComparison.OrdinalIgnoreCase)?"audio/mpeg":"audio/wav";
            // Share only the finished audio. JSON sidecars are retained locally, never attached implicitly.
            await Share.Default.RequestAsync(new ShareFileRequest {Title="Share audio",File=new ShareFile(lastSavedExport,mime)});
        }
        catch(Exception ex){ShowError("Windows sharing: "+ex.Message+". Use Show saved file and attach the audio in your destination app.");}
        finally{sharingExport=false;}
    }
    async Task ShowSavedExportAsync()
    {
        try
        {
            if(lastSavedExport is null||!File.Exists(lastSavedExport))throw new IOException("Export and save an audio file first.");
#if WINDOWS
            var file=await Windows.Storage.StorageFile.GetFileFromPathAsync(lastSavedExport);
            var folder=await file.GetParentAsync();
            var options=new Windows.System.FolderLauncherOptions();options.ItemsToSelect.Add(file);
            if(!await Windows.System.Launcher.LaunchFolderAsync(folder,options))throw new IOException("File Explorer could not open the saved audio folder.");
#else
            await Launcher.Default.OpenAsync(new OpenFileRequest("Saved audio",new ReadOnlyFile(lastSavedExport)));
#endif
        }
        catch(Exception ex){ShowError(ex.Message);}
    }
    async Task<string?> SaveExportFileAsync(string source,string suggestedName,string? recipe=null)
    {
#if WINDOWS
        string extension=Path.GetExtension(source);
        var picker=new Windows.Storage.Pickers.FileSavePicker {SuggestedFileName=Path.GetFileNameWithoutExtension(suggestedName),SuggestedStartLocation=Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary};
        picker.FileTypeChoices.Add(extension.ToLowerInvariant() switch {".zip"=>"Recordings archive",".mp3"=>"MP3 audio",_=>"WAV audio"},new List<string>{extension});
        var native=(Microsoft.UI.Xaml.Window)Application.Current!.Windows[0].Handler!.PlatformView!;
        WinRT.Interop.InitializeWithWindow.Initialize(picker,WinRT.Interop.WindowNative.GetWindowHandle(native));
        var destination=await picker.PickSaveFileAsync();
        if(destination is null)return null;
        string path=Path.GetFullPath(destination.Path);
        foreach(string? original in new[]{reviewOriginal,reviewCanonical,source})
            if(original is not null&&path.Equals(Path.GetFullPath(original),StringComparison.OrdinalIgnoreCase))throw new IOException("Choose a different filename. Original and internal preview files cannot be overwritten.");
        if(path.StartsWith(Path.GetFullPath(RecordingDirectory)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Save exports outside the original recordings folder.");
        using(var input=File.OpenRead(source))using(var output=await destination.OpenStreamForWriteAsync())
        {output.Position=0;await input.CopyToAsync(output);output.SetLength(input.Length);}
        if(recipe is not null)
        {
            string metadata=destination.Path+".recipe.json";
            if(File.Exists(metadata))metadata=destination.Path+"."+Guid.NewGuid().ToString("N")[..8]+".recipe.json";
            File.Copy(recipe,metadata,overwrite:false);
        }
        return destination.Path;
#elif LINUX
        // Linux build (Ghostbox.Linux): GTK Save dialog, same safety rules as Windows.
        string? chosen=await Ghostbox.Linux.LinuxHost.SaveFileAsync("Save audio",suggestedName);
        if(chosen is null)return null;
        string path=Path.GetFullPath(chosen);
        foreach(string? original in new[]{reviewOriginal,reviewCanonical,source})
            if(original is not null&&path.Equals(Path.GetFullPath(original),StringComparison.Ordinal))throw new IOException("Choose a different filename. Original and internal preview files cannot be overwritten.");
        if(path.StartsWith(Path.GetFullPath(RecordingDirectory)+Path.DirectorySeparatorChar,StringComparison.Ordinal))throw new IOException("Save exports outside the original recordings folder.");
        File.Copy(source,path,overwrite:true);
        if(recipe is not null)
        {
            string metadata=path+".recipe.json";
            if(File.Exists(metadata))metadata=path+"."+Guid.NewGuid().ToString("N")[..8]+".recipe.json";
            File.Copy(recipe,metadata,overwrite:false);
        }
        return path;
#else
        var files=new List<ShareFile>{new(source)};if(recipe is not null)files.Add(new(recipe));
        await Share.Default.RequestAsync(new ShareMultipleFilesRequest {Title="Export audio",Files=files});return source;
#endif
    }
}
