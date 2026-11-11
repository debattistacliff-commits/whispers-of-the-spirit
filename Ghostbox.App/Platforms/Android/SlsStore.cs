using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Android.Webkit;
using Java.Interop;

namespace Ghostbox.App;

/// <summary>
/// Android only: saves SLS camera recordings (WebM video), snapshots (JPEG) and their details (JSON)
/// in the app's own folder "SLS", and lists, renames, shares and deletes them for the History screen.
/// The console page calls these methods as <c>window.WhispersSls.*</c>. File names are always made
/// here (never taken from the page as paths), so the page cannot reach other files.
/// </summary>
sealed partial class SlsStore : Java.Lang.Object
{
    static string Folder => Directory.CreateDirectory(Path.Combine(FileSystem.AppDataDirectory, "SLS")).FullName;
    [GeneratedRegex(@"^SLS-\d{8}-\d{6}(-\d+)?\.(webm|jpg|json)$")] private static partial Regex SafeName();

    /// <summary>Full path of a saved SLS file, or null when the name is not one of ours.</summary>
    public static string? PathOf(string name) => SafeName().IsMatch(name) ? Path.Combine(Folder, name) : null;

    /// <summary>Starts a new item; returns its base name, e.g. "SLS-20261111-140512".</summary>
    [JavascriptInterface, Export("begin")]
    public string Begin()
    {
        string name = "SLS-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        for (int i = 2; File.Exists(Path.Combine(Folder, name + ".json")) || File.Exists(Path.Combine(Folder, name + ".webm")); i++)
            name = "SLS-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + i;
        return name;
    }

    /// <summary>Appends base64 data to "name.ext" (ext = webm or jpg).</summary>
    [JavascriptInterface, Export("append")]
    public bool Append(string name, string ext, string base64)
    {
        try
        {
            string? path = PathOf(name + "." + ext);
            if (path is null || ext == "json") return false;
            using var file = new FileStream(path, FileMode.Append, FileAccess.Write);
            var bytes = Convert.FromBase64String(base64);
            file.Write(bytes, 0, bytes.Length);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Saves the item's details (JSON object from the page).</summary>
    [JavascriptInterface, Export("finish")]
    public bool Finish(string name, string json)
    {
        try
        {
            string? path = PathOf(name + ".json");
            if (path is null || JsonNode.Parse(json) is not JsonObject meta) return false;
            meta["name"] = name;
            File.WriteAllText(path, meta.ToJsonString());
            return true;
        }
        catch { return false; }
    }

    /// <summary>All saved items, newest first, as a JSON array.</summary>
    [JavascriptInterface, Export("list")]
    public string List()
    {
        var items = new JsonArray();
        foreach (var file in Directory.GetFiles(Folder, "SLS-*.json").OrderByDescending(f => f))
        {
            try { if (JsonNode.Parse(File.ReadAllText(file)) is JsonObject o) items.Add(o); } catch { }
        }
        return items.ToJsonString();
    }

    /// <summary>Changes the place name of an item.</summary>
    [JavascriptInterface, Export("rename")]
    public bool Rename(string name, string place)
    {
        try
        {
            string? path = PathOf(name + ".json");
            if (path is null || !File.Exists(path) || JsonNode.Parse(File.ReadAllText(path)) is not JsonObject meta) return false;
            meta["place"] = (place ?? "").Trim()[..Math.Min(60, (place ?? "").Trim().Length)];
            File.WriteAllText(path, meta.ToJsonString());
            return true;
        }
        catch { return false; }
    }

    /// <summary>Deletes all files of an item.</summary>
    [JavascriptInterface, Export("remove")]
    public bool Remove(string name)
    {
        bool any = false;
        foreach (var ext in new[] { "webm", "jpg", "json" })
        {
            string? path = PathOf(name + "." + ext);
            if (path is not null && File.Exists(path)) { File.Delete(path); any = true; }
        }
        return any;
    }

    /// <summary>Opens Android's share sheet for the video (or the picture when there is no video).</summary>
    [JavascriptInterface, Export("share")]
    public void Share(string name)
    {
        string? video = PathOf(name + ".webm"), picture = PathOf(name + ".jpg");
        string? file = video is not null && File.Exists(video) ? video : picture is not null && File.Exists(picture) ? picture : null;
        if (file is null) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Whispers of the Spirit — SLS",
                    File = new ShareFile(file, file.EndsWith(".webm") ? "video/webm" : "image/jpeg"),
                });
            }
            catch { }
        });
    }
}
