using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using SharedCode.Constants;
namespace LudoClient.Popups;

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Services;
public partial class Settings : BasePopup
{
    public Settings()
    {
        InitializeComponent();
        SoundSwitch.init("line_bg.png");
        MusicSwitch.init("line_bg.png");
        NotificationSwitch.init("line_bg.png");

        string version = VersionTracking.CurrentVersion;          // e.g., "1.1.0"
        string build = VersionTracking.CurrentBuild;

        VersionText.Text = "Version : " + version;
    }
    private void OnHelpTapped(object sender, EventArgs e)
    {
        Close();
        ClientGlobalConstants.helpDesk = new HelpDesk();
        Application.Current.MainPage.ShowPopup(ClientGlobalConstants.helpDesk);
        // Close the popup when the background is tapped
    }
    private void SignOutTapped(object sender, EventArgs e)
    {
#if ANDROID
    var authService = DependencyService.Get<IGoogleAuthService>();
    authService.SignOutAsync().ContinueWith(task =>
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (task.IsCompletedSuccessfully && task.Result)
            {
                Close(); // If this is your cleanup method
                UserInfo.Logout();
                Application.Current.MainPage = new LoginPage();
            }
            else
            {
                // Sign-out failed
                Toast.Make("Logout failed. Try again.", ToastDuration.Long, 24).Show();
            }
        });
    });
#else
        Close();
        UserInfo.Logout();
        Application.Current.MainPage = new LoginPage();
#endif
    }
}