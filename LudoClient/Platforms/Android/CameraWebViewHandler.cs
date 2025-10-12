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
        }

        public class CameraWebChromeClient : WebChromeClient
        {
            public override void OnPermissionRequest(PermissionRequest request)
            {
                request.Grant(request.GetResources());
                base.OnPermissionRequest(request);
            }
        }
    }
}
