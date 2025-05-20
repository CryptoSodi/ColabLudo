using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using SharedCode.Constants;
namespace LudoClient.Popups;
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
        Application.Current.MainPage.ShowPopup(ClientGlobalConstants.helpDesk);
        // Close the popup when the background is tapped
    }
    private void SignOutTapped(object sender, EventArgs e)
    {
        Close();
        UserInfo.Logout();
        Application.Current.MainPage = new LoginPage();
    }
}