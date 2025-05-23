using Android.App;
using Android.Runtime;
using Android.Gms.Games;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using LudoClient.Platforms.Android;
using LudoClient.Services;

namespace LudoClient
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }
        //Old code
        //protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
        //This code will remove the underline from the Entery Field in Android version
        protected override MauiApp CreateMauiApp()
        {
            // Remove Entry control underline
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (h, v) =>
            {
                h.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Colors.Transparent.ToAndroid());
            });

            return MauiProgram.CreateMauiApp();
        }
        public override void OnCreate()
        {
            base.OnCreate();
            
            Microsoft.Maui.Controls.DependencyService.Register<IDeviceIdentifierService, DeviceIdentifierService>();
            Microsoft.Maui.Controls.DependencyService.Register<IGoogleAuthService, GoogleAuthService>();
            // Initialize Google Play Games SDK
            PlayGamesSdk.Initialize(this);
        }
    }
}
