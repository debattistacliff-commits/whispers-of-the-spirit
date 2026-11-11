using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Maui.Handlers;
using Microsoft.Web.WebView2.Core;

namespace Ghostbox.App;

/// <summary>
/// Windows only: serves the console page from the secure address https://whispers.local/ so the
/// SLS camera may use the webcam (browsers allow cameras only on secure pages). Files under
/// /sls/ (the MediaPipe body-detection engine and model) come from the app folder, so it works
/// offline; nothing is fetched from the internet. The webcam is used only after the person allows
/// it in WebView2's own permission question, and only while the SLS camera window is open.
/// </summary>
static class DesktopWebHost
{
    public const string Host = "whispers.local";
    public const string BaseUrl = "https://" + Host + "/";
    const string ConsoleTag = "whispers-console";
    static string page = "";

    /// <summary>Keeps the finished console page; returns the tag that marks the console WebView.</summary>
    public static string Prepare(string html) { page = html; return ConsoleTag; }

    public static void Register() =>
        WebViewHandler.Mapper.AppendToMapping("WhispersDesktopHost", async (handler, view) =>
        {
            if (view is not WebView { StyleId: ConsoleTag }) return;
            var web = handler.PlatformView;
            await web.EnsureCoreWebView2Async();
            var core = web.CoreWebView2;
            core.AddWebResourceRequestedFilter(BaseUrl + "*", CoreWebView2WebResourceContext.All);
            core.WebResourceRequested += (_, e) => Handle(core, e);
            core.Navigate(BaseUrl + "console.html");
        });

    static void Handle(CoreWebView2 core, CoreWebView2WebResourceRequestedEventArgs e)
    {
        Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri);
        string path = uri?.AbsolutePath.TrimStart('/') ?? "";
        if (!path.StartsWith("kinect/", StringComparison.Ordinal)) { e.Response = Serve(core, e.Request); return; }
        // Kinect (optional): answered off the window's thread, because starting the Kinect takes a moment.
        var args = System.Web.HttpUtility.ParseQueryString(uri?.Query ?? "");
        var deferral = e.GetDeferral();
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Task.Run(() =>
        {
            byte[] body = []; string headers = "Content-Type: text/plain; charset=utf-8"; int code = 200;
            switch (path)
            {
                case "kinect/status": body = Utf8(KinectBridge.Available ? "helper" : "none"); break;
                case "kinect/start": body = Utf8(KinectBridge.Start(args["mode"] ?? "ir")); break;
                case "kinect/stop": KinectBridge.Stop(); body = Utf8("ok"); break;
                case "kinect/frame":
                    long.TryParse(args["after"], out long after);
                    if (KinectBridge.Next(after) is { } f)
                    {
                        body = f.Jpeg;
                        headers = $"Content-Type: image/jpeg\nCache-Control: no-store\nX-Frame: {f.Frame}\nX-Skeleton: {f.Skeleton}";
                    }
                    else code = 204;
                    break;
                default: code = 404; break;
            }
            dispatcher.TryEnqueue(() =>
            {
                try
                {
                    e.Response = core.Environment.CreateWebResourceResponse(code == 200 ? new MemoryStream(body).AsRandomAccessStream() : null,
                        code, code == 200 ? "OK" : code == 204 ? "No Content" : "Not Found", headers);
                }
                catch { }
                deferral.Complete();
            });
        });
    }

    static byte[] Utf8(string text) => System.Text.Encoding.UTF8.GetBytes(text);

    static CoreWebView2WebResourceResponse Serve(CoreWebView2 core, CoreWebView2WebResourceRequest request)
    {
        Uri.TryCreate(request.Uri, UriKind.Absolute, out var uri);
        string path = uri?.AbsolutePath.TrimStart('/') ?? "";
        byte[]? body = null; string type = "application/octet-stream";
        // SLS camera: saving, History list and saved files (SlsFiles).
        if (SlsFiles.Serve(path, uri?.Query ?? "", () => ReadBody(request)) is { } answer)
        {
            if (answer.Status != 200) return core.Environment.CreateWebResourceResponse(null, answer.Status, "Not Found", "Content-Type: text/plain");
            body = answer.Body; type = answer.Type;
        }
        else if (path == "console.html") { body = System.Text.Encoding.UTF8.GetBytes(page); type = "text/html; charset=utf-8"; }
        else if (path.StartsWith("sls/", StringComparison.Ordinal) && !path.Contains("..") && !path.Contains('\\'))
        {
            string file = Path.Combine(AppContext.BaseDirectory, "sls", Path.GetFileName(path));
            if (File.Exists(file))
            {
                body = File.ReadAllBytes(file);
                type = Path.GetExtension(file) switch { ".mjs" or ".js" => "text/javascript", ".wasm" => "application/wasm", _ => "application/octet-stream" };
            }
        }
        if (body is null)
            return core.Environment.CreateWebResourceResponse(null, 404, "Not Found", "Content-Type: text/plain");
        return core.Environment.CreateWebResourceResponse(new MemoryStream(body).AsRandomAccessStream(), 200, "OK",
            $"Content-Type: {type}\nCache-Control: no-cache\nAccess-Control-Allow-Origin: *");
    }

    static string ReadBody(CoreWebView2WebResourceRequest request)
    {
        if (request.Content is null) return "";
        using var reader = new StreamReader(request.Content.AsStreamForRead(), System.Text.Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
