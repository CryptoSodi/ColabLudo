using CommunityToolkit.Maui.Views;
namespace LudoClient.Popups;
public partial class Settings : BasePopup
{
    public Settings()
    {
        InitializeComponent();
        SoundSwitch.init("line_bg.png");
        MusicSwitch.init("line_bg.png");
        NotificationSwitch.init("line_bg.png");

        basePopup.capsule.ImageSource = "dailybonus_popup_bg.png";
    }
    private void OnHelpTapped(object sender, EventArgs e)
    {
        Application.Current.MainPage.ShowPopup(new HelpDesk());
        // Close the popup when the background is tapped
        Close();
    }
}