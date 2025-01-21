using LudoClient.ControlView;
namespace LudoClient;
public partial class PracticePage : ContentPage
{
    public PracticePage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4.SwitchOff;

    }
    private void ActivateTab(object sender, EventArgs e)
    {
        ImageSwitch activeTab = sender as ImageSwitch;
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
        // Add logic here to change the content based on the active tab
    }
    private void JoinPracticeTapped(object sender, EventArgs e)
    {
        // Add logic here to join an offline game
        //Application.Current.MainPage = new Game(gametype, playerCount, playerColor);
    }
}