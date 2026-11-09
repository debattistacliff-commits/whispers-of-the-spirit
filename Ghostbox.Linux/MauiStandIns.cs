using System.Text.Json;
using System.Text.Json.Nodes;
using Ghostbox.Linux;

// Linux stand-ins for the few MAUI platform services used by the shared Ghostbox.App code.
// They live in the Ghostbox.App namespace, so the shared files pick them up instead of the MAUI ones.
namespace Ghostbox.App;

/// <summary>Shows the console page in the Photino (WebKitGTK) window. Same surface the shared WebConsole.cs uses on Windows/Android.</summary>
public sealed class WebView : View
{
    bool ready;
    public WebViewSource? Source { get; set; }
    public event EventHandler<WebNavigatingEventArgs>? Navigating;
    public event EventHandler<WebNavigatedEventArgs>? Navigated;

    // On Windows/Android the console sends commands by navigating to whispers:// addresses. Here the same
    // addresses travel over Photino's message channel; console-live.js and review-deck.js already use
    // window.__whispersSend when it exists, and the bridge script is pointed at it below.
    const string Prelude = """
    <script>
    (function () {
      function init() {
        if (!(window.external && window.external.sendMessage && window.external.receiveMessage)) { setTimeout(init, 20); return; }
        window.__whispersSend = function (url) { window.external.sendMessage(url); };
        window.external.receiveMessage(function (script) {
          try { (0, eval)(script); } catch (e) { window.external.sendMessage('__error:' + ((e && e.stack) || e)); }
        });
        var loaded = function () { window.external.sendMessage('__loaded'); };
        if (document.readyState === 'complete') loaded(); else window.addEventListener('load', loaded);
      }
      init();
    })();
    </script>
    """;

    internal string PageHtml()
    {
        string html = (Source as HtmlWebViewSource)?.Html ?? throw new InvalidOperationException("The console page has no content.");
        int head = html.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);
        return head < 0 ? Prelude + html : html.Insert(head + "<head>".Length, Prelude);
    }

    public Task<string?> EvaluateJavaScriptAsync(string script)
    {
        if (ready) LinuxHost.RunScript(Adapt(script));
        return Task.FromResult<string?>(null);
    }

    static string Adapt(string script) => script.Replace(
        "window.location.href = 'whispers://' + action + suffix;",
        "(window.__whispersSend ? window.__whispersSend('whispers://' + action + suffix) : (window.location.href = 'whispers://' + action + suffix));");

    /// <summary>A message from the page: page loaded, a whispers:// command, or a script error.</summary>
    internal void Receive(string message)
    {
        if (message == "__loaded")
        {
            ready = true;
            Navigated?.Invoke(this, new WebNavigatedEventArgs(WebNavigationEvent.NewPage, Source, "about:blank", WebNavigationResult.Success));
            // Automated checks only: WHISPERS_TEST_SCRIPT names a script that drives the console's own buttons.
            if (Environment.GetEnvironmentVariable("WHISPERS_TEST_SCRIPT") is { Length: > 0 } test && File.Exists(test))
                LinuxHost.RunScript(File.ReadAllText(test));
        }
        else if (message.StartsWith("__log:", StringComparison.Ordinal))
            Console.Error.WriteLine("[whispers] " + message[6..]);
        else if (message.StartsWith("whispers://", StringComparison.OrdinalIgnoreCase))
        {
            if (LinuxHost.Trace) Console.Error.WriteLine($"[whispers] {DateTime.Now:HH:mm:ss.fff} command " + message);
            Navigating?.Invoke(this, new WebNavigatingEventArgs(WebNavigationEvent.NewPage, Source, message));
        }
        else if (message.StartsWith("__error:", StringComparison.Ordinal))
            Console.Error.WriteLine("[whispers] console script error: " + message[8..]);
    }
}

/// <summary>Settings saved as JSON in ~/.config/whispers-of-the-spirit/settings.json.</summary>
public sealed class Preferences
{
    public static Preferences Default { get; } = new();
    readonly object gate = new();
    readonly string path = Path.Combine(LinuxFolders.Config, "settings.json");
    readonly JsonObject values;

    Preferences()
    {
        try { values = File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? [] : []; }
        catch { values = []; }
    }

    public T Get<T>(string key, T defaultValue)
    {
        lock (gate)
        {
            try { return values[key] is { } node ? node.Deserialize<T>() ?? defaultValue : defaultValue; }
            catch { return defaultValue; }
        }
    }

    public void Set<T>(string key, T value)
    {
        lock (gate) { values[key] = JsonSerializer.SerializeToNode(value); Save(); }
    }

    public void Remove(string key)
    {
        lock (gate) { if (values.Remove(key)) Save(); }
    }

    void Save()
    {
        try { File.WriteAllText(path + ".tmp", values.ToJsonString()); File.Move(path + ".tmp", path, true); }
        catch (Exception ex) { Console.Error.WriteLine("[whispers] settings not saved: " + ex.Message); }
    }
}

/// <summary>Recordings and review work in ~/.local/share/whispers-of-the-spirit.</summary>
public static class FileSystem
{
    public static string AppDataDirectory => LinuxFolders.Data;
    public static string CacheDirectory => LinuxFolders.Cache;
}

static class LinuxFolders
{
    static string Folder(string variable, string fallback)
    {
        string root = Environment.GetEnvironmentVariable(variable) is { Length: > 0 } v && Path.IsPathRooted(v) ? v
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fallback);
        return Directory.CreateDirectory(Path.Combine(root, "whispers-of-the-spirit")).FullName;
    }
    public static readonly string Config = Folder("XDG_CONFIG_HOME", ".config");
    public static readonly string Data = Folder("XDG_DATA_HOME", Path.Combine(".local", "share"));
    public static readonly string Cache = Folder("XDG_CACHE_HOME", ".cache");
}

public static class MainThread
{
    public static void BeginInvokeOnMainThread(Action action) => LinuxHost.Loop.Post(action);

    public static Task InvokeOnMainThreadAsync(Func<Task> work) => InvokeOnMainThreadAsync(async () => { await work(); return true; });

    public static Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> work)
    {
        if (Thread.CurrentThread == LinuxHost.Loop.Thread) return work();
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        LinuxHost.Loop.Post(async () =>
        {
            try { done.SetResult(await work()); }
            catch (Exception ex) { done.SetException(ex); }
        });
        return done.Task;
    }
}

/// <summary>GTK "Open" dialog.</summary>
public sealed class FilePicker
{
    public static FilePicker Default { get; } = new();
    public async Task<FileResult?> PickAsync(PickOptions? options = null)
    {
        string? path = await LinuxHost.OpenFileAsync(options?.PickerTitle ?? "Open audio", ".wav", ".mp3", ".flac");
        return path is null ? null : new FileResult(path);
    }
}

/// <summary>Linux desktop apps do not ask for microphone permission; the sound server decides.</summary>
public static class Permissions
{
    public sealed class Microphone;
    public static Task<PermissionStatus> RequestAsync<T>() => Task.FromResult(PermissionStatus.Granted);
}

public sealed class SemanticScreenReader
{
    public static SemanticScreenReader Default { get; } = new();
    public void Announce(string text) => Console.Error.WriteLine("[whispers] " + text);
}
