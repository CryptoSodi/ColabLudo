using LudoClient.Constants;
using SharedCode.Constants;
namespace LudoClient.Popups;

using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using LudoClient.Services;
public partial class Settings : BasePopup
{
    public Settings()
    {
        InitializeComponent();
        SoundSwitch.init("line_bg.webp");
        VibrationSwitch.init("line_bg.webp");
        NotificationSwitch.init("line_bg.webp");

        string version = VersionTracking.CurrentVersion;// e.g., "1.1.0"
        string build = VersionTracking.CurrentBuild;

        VersionText.Text = "Version : " + build; 
        Loaded += OnAppearing;
    }
    private void OnAppearing(object sender, EventArgs e)
    {
        ClientGlobalConstants.sw.Stop();
        Console.WriteLine($"CashGame full load time: {ClientGlobalConstants.sw.ElapsedMilliseconds} ms");
    }
    private void OnHelpTapped(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        CloseAsync();
        Application.Current?.MainPage.ShowPopup(ClientGlobalConstants.helpDesk, new PopupOptions { Shape = null });
        //Close the popup when the background is tapped
    }
    private void SignOutTapped(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
#if ANDROID
        try
        {
            var authService = DependencyService.Get<IGoogleAuthService>();
            authService.SignOutAsync().ContinueWith(task =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (task.IsCompletedSuccessfully && task.Result)
                    {
                    }
                    else
                    {
                        // Sign-out failed
                        //Toast.Make("Logout failed. Try again.", ToastDuration.Long, 24).Show();
                    }
                });
            });
        }
        catch (Exception)
        {   
        }
        CloseAsync(); // If this is your cleanup method
        UserInfo.Logout();
        Application.Current.MainPage = new LoginPage();
#else
        CloseAsync();
        UserInfo.Logout();
        Application.Current.MainPage = new LoginPage();
#endif
    }
}