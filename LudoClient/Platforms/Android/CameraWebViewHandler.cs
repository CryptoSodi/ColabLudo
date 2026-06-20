using Android.Webkit;
using Microsoft.Maui.Handlers;

namespace LudoClient.Platforms.Android
{
    internal class CameraWebViewHandler : WebViewHandler
    {
        protected override void ConnectHandler(global::Android.Webkit.WebView platformView)
        {
            Microsoft.Maui.Handlers.WebViewHandler.Mapper.Add("WebChromeClient", (handler, view) =>
            {
                handler.PlatformView.SetWebChromeClient(new CameraWebChromeClient());
            });
            base.ConnectHandler(platformView);
#if DEBUG
            global::Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
#endif
            platformView.Settings.JavaScriptEnabled = true;
            platformView.Settings.DomStorageEnabled = true;
            platformView.Settings.MediaPlaybackRequiresUserGesture = false;
            platformView.Settings.AllowFileAccess = true;
            platformView.Settings.AllowContentAccess = true;

            platformView.SetWebChromeClient(new CameraWebChromeClient());
            platformView.SetWebViewClient(new CameraWebViewClient());
        }

        protected override void DisconnectHandler(global::Android.Webkit.WebView platformView)
        {
            try
            {
                platformView.StopLoading();
                platformView.SetWebChromeClient(null);
                platformView.SetWebViewClient(null);
            }
            catch
            {
            }

            base.DisconnectHandler(platformView);
        }

        public class CameraWebChromeClient : WebChromeClient
        {
            public override void OnPermissionRequest(PermissionRequest request)
            {
                try
                {
                    request?.Grant(request.GetResources());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebRTC] Permission request grant failed: {ex.Message}");
                }
                //base.OnPermissionRequest(request);
            }
        }

        public class CameraWebViewClient : WebViewClient
        {
            public override void OnReceivedError(global::Android.Webkit.WebView? view, IWebResourceRequest? request, WebResourceError? error)
            {
                try
                {
                    Console.WriteLine($"[WebRTC] WebView error url={request?.Url} code={error?.ErrorCode} desc={error?.Description}");
                }
                catch
                {
                }

                base.OnReceivedError(view, request, error);
            }

            public override bool OnRenderProcessGone(global::Android.Webkit.WebView? view, RenderProcessGoneDetail? detail)
            {
                try
                {
                    Console.WriteLine($"[WebRTC] WebView render process gone. DidCrash={detail?.DidCrash()}");
                    view?.StopLoading();
                    view?.LoadUrl("about:blank");
                    view?.Destroy();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebRTC] Failed handling render process exit: {ex.Message}");
                }

                return true;
            }
        }
    }
}
