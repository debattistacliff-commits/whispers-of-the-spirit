using Android.Webkit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AWebView = Android.Webkit.WebView;

namespace Ghostbox.App;

/// <summary>
/// Android only: lets the console page use the camera for the SLS camera.
/// - The page is loaded with the secure base address https://appassets.androidplatform.net/
///   (see WebConsole.cs). Android allows the camera only on secure pages.
/// - Files under that address in /sls/ (the MediaPipe body-detection engine and model) are served
///   from the app's own Android assets, so it works offline; nothing is fetched from the internet.
/// - When the page asks for the camera, the normal Android camera permission is requested first.
/// </summary>
static class SlsWebHost
{
    public const string Host = "appassets.androidplatform.net";
    public const string BaseUrl = "https://" + Host + "/";

    public static void Register() =>
        // Prepend: the page bridge must exist before the console page starts loading.
        WebViewHandler.Mapper.PrependToMapping("WhispersSlsHost", (handler, view) =>
        {
            if (handler is not WebViewHandler web) return;
            web.PlatformView.SetWebViewClient(new AssetClient(web));
            web.PlatformView.SetWebChromeClient(new CameraChromeClient(web));
            web.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
            web.PlatformView.AddJavascriptInterface(new SlsStore(), "WhispersSls");
        });

    sealed class AssetClient(WebViewHandler handler) : MauiWebViewClient(handler)
    {
        public override WebResourceResponse? ShouldInterceptRequest(AWebView? view, IWebResourceRequest? request)
        {
            var url = request?.Url;
            if (url is null || url.Host != Host) return base.ShouldInterceptRequest(view, request);
            string path = (url.Path ?? "").TrimStart('/');
            var headers = new Dictionary<string, string> { ["Access-Control-Allow-Origin"] = "*", ["Cache-Control"] = "no-cache" };
            // Saved SLS recordings and snapshots (History screen).
            if (path.StartsWith("sls-files/", StringComparison.Ordinal))
            {
                string? file = SlsFiles.PathOf(path["sls-files/".Length..]);
                if (file is null || !File.Exists(file))
                    return new WebResourceResponse("text/plain", "utf-8", 404, "Not Found", headers, new MemoryStream());
                string type = SlsFiles.ContentType(file);
                return new WebResourceResponse(type, null, 200, "OK", headers, File.OpenRead(file));
            }
            if (!path.StartsWith("sls/", StringComparison.Ordinal) || path.Contains(".."))
                return new WebResourceResponse("text/plain", "utf-8", 404, "Not Found", headers, new MemoryStream());
            try
            {
                var stream = Android.App.Application.Context.Assets!.Open(path);
                string mime = Path.GetExtension(path) switch
                {
                    ".mjs" or ".js" => "text/javascript",
                    ".wasm" => "application/wasm",
                    _ => "application/octet-stream",
                };
                return new WebResourceResponse(mime, null, 200, "OK", headers, stream);
            }
            catch (Exception)
            {
                return new WebResourceResponse("text/plain", "utf-8", 404, "Not Found", headers, new MemoryStream());
            }
        }
    }

    sealed class CameraChromeClient(WebViewHandler handler) : MauiWebChromeClient(handler)
    {
        public override void OnPermissionRequest(PermissionRequest? request)
        {
            if (request is null) return;
            var wanted = request.GetResources() ?? [];
            if (!wanted.Contains(PermissionRequest.ResourceVideoCapture)) { request.Deny(); return; }
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var status = await Permissions.RequestAsync<Permissions.Camera>();
                    if (status == PermissionStatus.Granted) request.Grant([PermissionRequest.ResourceVideoCapture]);
                    else request.Deny();
                }
                catch { request.Deny(); }
            });
        }
    }
}
