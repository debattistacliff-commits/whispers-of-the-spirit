using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Ghostbox.App;

/// <summary>
/// SLS camera recordings (WebM video), pictures (JPEG) and their details (JSON), kept in the app's
/// own folder "SLS". Used on every platform: the phone calls it through SlsStore, Windows and Linux
/// through their page hosts. File names are always made here and checked, never taken from the page
/// as paths, so the page cannot reach other files.
/// </summary>
static partial class SlsFiles
{
    public static string Folder => Directory.CreateDirectory(Path.Combine(FileSystem.AppDataDirectory, "SLS")).FullName;
    [GeneratedRegex(@"^SLS-\d{8}-\d{6}(-\d+)?\.(webm|jpg|json)$")] private static partial Regex SafeName();
    const int MaxPlace = 60;

    /// <summary>Full path of a saved SLS file, or null when the name is not one of ours.</summary>
    public static string? PathOf(string name) => SafeName().IsMatch(name) ? Path.Combine(Folder, name) : null;

    /// <summary>Starts a new item; returns its base name, e.g. "SLS-20261111-140512".</summary>
    public static string Begin()
    {
        string stamp = "SLS-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), name = stamp;
        for (int i = 2; File.Exists(Path.Combine(Folder, name + ".json")) || File.Exists(Path.Combine(Folder, name + ".webm")); i++)
            name = stamp + "-" + i;
        return name;
    }

    /// <summary>Appends base64 data to "name.ext" (ext = webm or jpg).</summary>
    public static bool Append(string name, string ext, string base64)
    {
        try
        {
            string? path = ext is "webm" or "jpg" ? PathOf(name + "." + ext) : null;
            if (path is null) return false;
            var bytes = Convert.FromBase64String(base64);
            using var file = new FileStream(path, FileMode.Append, FileAccess.Write);
            file.Write(bytes, 0, bytes.Length);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Saves the item's details (a JSON object from the page).</summary>
    public static bool Finish(string name, string json)
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
    public static string List()
    {
        var items = new JsonArray();
        foreach (var file in Directory.GetFiles(Folder, "SLS-*.json").OrderByDescending(f => f))
        {
            try { if (JsonNode.Parse(File.ReadAllText(file)) is JsonObject o) items.Add(o); } catch { }
        }
        return items.ToJsonString();
    }

    /// <summary>Changes the place name of an item.</summary>
    public static bool Rename(string name, string? place)
    {
        try
        {
            string? path = PathOf(name + ".json");
            if (path is null || !File.Exists(path) || JsonNode.Parse(File.ReadAllText(path)) is not JsonObject meta) return false;
            string text = (place ?? "").Trim();
            meta["place"] = text.Length > MaxPlace ? text[..MaxPlace] : text;
            File.WriteAllText(path, meta.ToJsonString());
            return true;
        }
        catch { return false; }
    }

    /// <summary>Deletes all files of an item.</summary>
    public static bool Remove(string name)
    {
        bool any = false;
        foreach (var ext in new[] { "webm", "jpg", "json" })
        {
            string? path = PathOf(name + "." + ext);
            if (path is not null && File.Exists(path)) { File.Delete(path); any = true; }
        }
        return any;
    }

    /// <summary>The video of an item, or its picture when there is no video; null when neither exists.</summary>
    public static string? MainFile(string name)
    {
        string? video = PathOf(name + ".webm"), picture = PathOf(name + ".jpg");
        return video is not null && File.Exists(video) ? video : picture is not null && File.Exists(picture) ? picture : null;
    }

    public static string ContentType(string file) => Path.GetExtension(file) switch
    {
        ".webm" => "video/webm",
        ".jpg" => "image/jpeg",
        _ => "application/json",
    };

    /// <summary>Computer only: shows the saved video (or picture) in the file manager.</summary>
    public static void Reveal(string name)
    {
        string? file = MainFile(name);
        if (file is null) return;
        try
        {
            if (OperatingSystem.IsWindows())
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe") { ArgumentList = { "/select,", file } });
            else
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("xdg-open") { ArgumentList = { Folder }, UseShellExecute = false });
        }
        catch { }
    }

    /// <summary>
    /// Computer only: answers the SLS camera page's requests to the app's own page address.
    /// "sls-api/…" saves, lists, renames, reveals and deletes; "sls-files/…" returns a saved file.
    /// Returns null for any other address.
    /// </summary>
    public static (byte[] Body, string Type, int Status)? Serve(string path, string query, Func<string> body)
    {
        static (byte[], string, int) Text(string s, string type = "text/plain; charset=utf-8") => (System.Text.Encoding.UTF8.GetBytes(s), type, 200);
        if (path.StartsWith("sls-files/", StringComparison.Ordinal))
        {
            string? file = PathOf(path["sls-files/".Length..]);
            return file is not null && File.Exists(file) ? (File.ReadAllBytes(file), ContentType(file), 200) : (Array.Empty<byte>(), "text/plain", 404);
        }
        if (!path.StartsWith("sls-api/", StringComparison.Ordinal)) return null;
        var args = System.Web.HttpUtility.ParseQueryString(query);
        string name = args["name"] ?? "", action = path["sls-api/".Length..];
        if (action == "share") { Reveal(name); return Text("1"); }
        return action switch
        {
            "begin" => Text(Begin()),
            "append" => Text(Append(name, args["ext"] ?? "", body()) ? "1" : "0"),
            "finish" => Text(Finish(name, body()) ? "1" : "0"),
            "list" => Text(List(), "application/json"),
            "rename" => Text(Rename(name, body()) ? "1" : "0"),
            "remove" => Text(Remove(name) ? "1" : "0"),
            _ => (Array.Empty<byte>(), "text/plain", 404),
        };
    }
}
