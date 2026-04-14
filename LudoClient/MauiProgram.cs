using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using SimpleToolkit.Core;
using SimpleToolkit.SimpleShell;
using Xe.AcrylicView;
#if ANDROID
using LudoClient.Platforms.Android;
#endif
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using LudoClient.Controls;
using LudoClient.Services;
using LudoClient.CoreEngine;

namespace LudoClient
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options =>
                {
                    options.SetShouldEnableSnackbarOnWindows(true);
                })
                .UseAcrylicView()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Comfortaa-Regular.ttf", "RegularFont");
                    fonts.AddFont("Comfortaa-Bold.ttf", "BoldFont");
                    fonts.AddFont("Comfortaa-Medium.ttf", "MediumFont");
                    fonts.AddFont("Comfortaa-SemiBold.ttf", "SemiBoldFont");
                })
                .UseSimpleToolkit()
                .UseSimpleShell();


#if ANDROID
            builder.Services.AddSingleton<IDeviceIdentifierService, DeviceIdentifierService>();
            builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddSingleton<IGamepadInputService, GamepadInputService>();
            builder.Services.AddSingleton<ISolanaWalletService, SolanaWalletService>();
#endif

#if ANDROID
builder.Services.AddSingleton<ISoundService, SoundPoolService>();
#else
builder.Services.AddSingleton<ISoundService, MauiAudioService>();
#endif

            builder.Services.AddSingleton<HepticEngine>();

            builder.ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler(typeof(CameraWebView), typeof(LudoClient.Platforms.Android.CameraWebViewHandler));
                handlers.AddHandler(typeof(NativeFriendCard), typeof(NativeFriendCardHandler));
#endif
            });

       

            AppCenter.Start("android=0c1428a3-a086-4b8d-be30-8253a18b054e;", typeof(Analytics), typeof(Crashes));

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {Crashes.TrackError(exception); // ✅ Report to App Center
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[UnhandledException] {exception}");
#endif
                }
                //  throw (Exception)e.ExceptionObject; // force crash reporting
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Crashes.TrackError(e.Exception); // ✅ Report to App Center
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[UnobservedTaskException] {e.Exception}");
#endif
                e.SetObserved(); // Prevent app from crashing
            };

#if DEBUG
            builder.Logging.AddDebug();
#endif
            
            builder.AddAudio();
            return builder.Build();
        }
    }
}
