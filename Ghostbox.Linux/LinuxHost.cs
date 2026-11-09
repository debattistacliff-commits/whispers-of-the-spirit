using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using Ghostbox.App;
using Photino.NET;

namespace Ghostbox.Linux;

/// <summary>
/// Starts the Linux app: a WebKitGTK window (Photino) on the main thread showing the console page,
/// and the shared app logic (MainPage) on its own "UI" thread, like the MAUI UI thread on Windows.
/// </summary>
static class LinuxHost
{
    static PhotinoWindow? window;
    internal static AppLoop Loop { get; } = new();
    internal static readonly bool Trace = Environment.GetEnvironmentVariable("WHISPERS_TRACE") == "1";

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Contains("--version")) { Console.WriteLine("Whispers of the Spirit 2.0 (Linux x64)"); return 0; }
        UseSystemLibVlc();
        DispatcherProvider.SetCurrent(new AppDispatcherProvider(Loop));

        MainPage? page = null;
        Loop.Send(() => page = new MainPage());
        var console = (page!.Content as Ghostbox.App.WebView) ?? throw new InvalidOperationException("The console page was not created.");

        string icon = Path.Combine(AppContext.BaseDirectory, "whispers-of-the-spirit.png");
        window = new PhotinoWindow()
            .SetLogVerbosity(0)
            .SetTitle("Whispers of the Spirit")
            .SetUseOsDefaultSize(false)
            .SetSize(1600, 1000)
            .SetMinSize(1240, 760)
            .SetResizable(true)
            .SetContextMenuEnabled(false)
            .SetDevToolsEnabled(Trace)
            .Center();
        if (File.Exists(icon)) window.SetIconFile(icon);
        window.RegisterWebMessageReceivedHandler((_, message) => Loop.Post(() => console.Receive(message)));
        window.RegisterWindowClosingHandler((_, _) => { Loop.Send(() => page.StopScanning()); return false; });
        window.LoadRawString(console.PageHtml());
        // Timers in the shared code start on Loaded, which MAUI raises when the page is shown.
        Loop.Post(() => RaiseLoaded(page));
        window.WaitForClose();
        Loop.Stop();
        return 0;
    }

    /// <summary>Runs JavaScript in the console page (from any thread).</summary>
    internal static void RunScript(string script)
    {
        var w = window; if (w is null) return;
        try { w.Invoke(() => w.SendWebMessage(script)); }
        catch (Exception ex) { if (Trace) Console.Error.WriteLine("[whispers] script: " + ex.Message); }
    }

    static (string Name, string[] Extensions)[] Filters(params string[] extensions) =>
        [("Audio (" + string.Join(", ", extensions) + ")", extensions.Select(e => "*" + e).ToArray())];

    internal static Task<string?> OpenFileAsync(string title, params string[] extensions) => OnWindowThread(() =>
    {
        var files = window!.ShowOpenFile(title, DefaultFolder(), false, Filters(extensions));
        return files is { Length: > 0 } && !string.IsNullOrEmpty(files[0]) ? files[0] : null;
    });

    internal static Task<string?> SaveFileAsync(string title, string suggestedName) => OnWindowThread(() =>
    {
        string extension = Path.GetExtension(suggestedName);
        string? path = window!.ShowSaveFile(title, Path.Combine(DefaultFolder(), suggestedName), Filters(extension));
        if (string.IsNullOrEmpty(path)) return null;
        return path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path : path + extension;
    });

    static string DefaultFolder()
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        foreach (var name in new[] { "Music", "Documents" }) { var d = Path.Combine(home, name); if (Directory.Exists(d)) return d; }
        return home;
    }

    /// <summary>GTK dialogs must run on the window thread; the app thread waits without blocking its queue.</summary>
    static Task<T> OnWindowThread<T>(Func<T> work)
    {
        var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task.Run(() =>
        {
            try { T result = default!; window!.Invoke(() => result = work()); done.SetResult(result); }
            catch (Exception ex) { done.SetException(ex); }
        });
        return done.Task;
    }

    static void RaiseLoaded(VisualElement element)
    {
        if (typeof(VisualElement).GetField("_loaded", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(element) is EventHandler loaded)
            loaded(element, EventArgs.Empty);
        else
            typeof(VisualElement).GetMethod("SendLoaded", BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes)?.Invoke(element, null);
    }

    /// <summary>Internet radio uses the distribution's VLC library (package libvlc5 / vlc).</summary>
    static void UseSystemLibVlc()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(LibVLCSharp.Shared.LibVLC).Assembly, (name, assembly, path) =>
            {
                string[] candidates = name switch
                {
                    "libvlc" => ["libvlc.so.5", "libvlc.so"],
                    "libvlccore" => ["libvlccore.so.9", "libvlccore.so"],
                    _ => []
                };
                foreach (var candidate in candidates) if (NativeLibrary.TryLoad(candidate, out var handle)) return handle;
                return IntPtr.Zero;
            });
        }
        catch (InvalidOperationException) { /* A resolver is already set. */ }
    }
}

/// <summary>A single-threaded message loop with a SynchronizationContext, standing in for the MAUI UI thread.</summary>
sealed class AppLoop : SynchronizationContext
{
    readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> queue = new();
    public Thread Thread { get; }

    public AppLoop()
    {
        Thread = new Thread(Run) { IsBackground = true, Name = "Whispers app" };
        Thread.Start();
    }

    void Run()
    {
        SetSynchronizationContext(this);
        foreach (var (callback, state) in queue.GetConsumingEnumerable())
        {
            try { callback(state); }
            catch (Exception ex) { Console.Error.WriteLine("[whispers] " + ex); }
        }
    }

    public override void Post(SendOrPostCallback d, object? state) { if (!queue.IsAddingCompleted) queue.Add((d, state)); }
    public void Post(Action action) => Post(_ => action(), null);

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Thread.CurrentThread == Thread) { d(state); return; }
        using var done = new ManualResetEventSlim();
        Exception? error = null;
        Post(_ => { try { d(state); } catch (Exception ex) { error = ex; } finally { done.Set(); } }, null);
        done.Wait();
        if (error is not null) throw new InvalidOperationException(error.Message, error);
    }
    public void Send(Action action) => Send(_ => action(), null);

    public override SynchronizationContext CreateCopy() => this;
    public void Stop() => queue.CompleteAdding();
}

sealed class AppDispatcherProvider(AppLoop loop) : IDispatcherProvider
{
    readonly AppDispatcher dispatcher = new(loop);
    public IDispatcher? GetForCurrentThread() => dispatcher;
}

sealed class AppDispatcher(AppLoop loop) : IDispatcher
{
    public bool IsDispatchRequired => Thread.CurrentThread != loop.Thread;
    public bool Dispatch(Action action) { loop.Post(action); return true; }
    public bool DispatchDelayed(TimeSpan delay, Action action) { _ = Task.Delay(delay).ContinueWith(_ => loop.Post(action)); return true; }
    public IDispatcherTimer CreateTimer() => new AppTimer(loop);
}

/// <summary>Repeating timer whose Tick runs on the app thread. Ticks are skipped (not queued up) while the app thread is busy.</summary>
sealed class AppTimer(AppLoop loop) : IDispatcherTimer
{
    Timer? timer;
    int pending;
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(1);
    public bool IsRepeating { get; set; } = true;
    public bool IsRunning => timer is not null;
    public event EventHandler? Tick;

    public void Start()
    {
        if (timer is not null) return;
        timer = new Timer(_ =>
        {
            if (Interlocked.Exchange(ref pending, 1) == 1) return;
            loop.Post(() =>
            {
                pending = 0;
                if (timer is null) return;
                Tick?.Invoke(this, EventArgs.Empty);
                if (!IsRepeating) Stop();
            });
        }, null, Interval, Interval);
    }

    public void Stop() { timer?.Dispose(); timer = null; }
}
