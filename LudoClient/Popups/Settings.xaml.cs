using CommunityToolkit.Maui.Views;
namespace LudoClient;
public partial class Settings : Popup
{
    public Settings()
    {
        InitializeComponent();
        SoundSwitch.init();
        MusicSwitch.init();
        NotificationSwitch.init();

        // Get the device's main display information
        var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
        // Calculate the width and height in device-independent units
        double width = mainDisplayInfo.Width / mainDisplayInfo.Density;
        double height = mainDisplayInfo.Height / mainDisplayInfo.Density;
        // Set the popup size
        this.Size = new Size(width, height);
    }
    private void OnBackgroundTapped(object sender, EventArgs e)
    {
        // Close the popup when the background is tapped
        Close();
    }
    private void OnHelpTapped(object sender, EventArgs e)
    {
        Application.Current.MainPage.ShowPopup(new HelpDesk());
        // Close the popup when the background is tapped
        Close();
    }
}