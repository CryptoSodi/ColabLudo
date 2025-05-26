using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using SimpleToolkit.Core;
using SimpleToolkit.SimpleShell;
using Xe.AcrylicView;
#if ANDROID
using LudoClient.Services;
using LudoClient.Platforms.Android;
#endif

namespace LudoClient
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
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
                .UseMauiApp<App>()
                .UseSimpleToolkit()
                .UseSimpleShell();
#if ANDROID
            builder.Services.AddSingleton<IDeviceIdentifierService, DeviceIdentifierService>();
            builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
#endif
#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.AddAudio();
            return builder.Build();
        }
    }
}
