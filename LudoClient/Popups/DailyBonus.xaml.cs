using CommunityToolkit.Maui.Views;
namespace LudoClient;
public partial class DailyBonus : Popup
{
    public DailyBonus()
    {
        InitializeComponent();
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
}