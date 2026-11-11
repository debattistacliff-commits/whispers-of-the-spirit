using Android.Webkit;
using Java.Interop;

namespace Ghostbox.App;

/// <summary>
/// Android only: the console page calls these methods as <c>window.WhispersSls.*</c> to save SLS camera
/// videos and pictures and to list, rename, share and delete them. The work is done by SlsFiles.
/// </summary>
sealed class SlsStore : Java.Lang.Object
{
    [JavascriptInterface, Export("begin")] public string Begin() => SlsFiles.Begin();
    [JavascriptInterface, Export("append")] public bool Append(string name, string ext, string base64) => SlsFiles.Append(name, ext, base64);
    [JavascriptInterface, Export("finish")] public bool Finish(string name, string json) => SlsFiles.Finish(name, json);
    [JavascriptInterface, Export("list")] public string List() => SlsFiles.List();
    [JavascriptInterface, Export("rename")] public bool Rename(string name, string place) => SlsFiles.Rename(name, place);
    [JavascriptInterface, Export("remove")] public bool Remove(string name) => SlsFiles.Remove(name);

    /// <summary>Opens Android's share sheet for the video (or the picture when there is no video).</summary>
    [JavascriptInterface, Export("share")]
    public void Share(string name)
    {
        string? file = SlsFiles.MainFile(name);
        if (file is null) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Whispers of the Spirit — SLS",
                    File = new ShareFile(file, SlsFiles.ContentType(file)),
                });
            }
            catch { }
        });
    }
}
