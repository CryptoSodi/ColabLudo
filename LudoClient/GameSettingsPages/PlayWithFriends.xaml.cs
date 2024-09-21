using LudoClient.ControlView;

namespace LudoClient;

public partial class PlayWithFriends : ContentPage
{
    public PlayWithFriends()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4.SwitchOff;

        TabP1.SwitchSource = TabP1.SwitchOn;
        TabP2.SwitchSource = TabP2.SwitchOff;
    }
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        ActivateTab(sender as ImageSwitch);
    }
    private void ActivateTab(ImageSwitch activeTab)
    {
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
        // Add logic here to change the content based on the active tab
    }
    private void ActivateTabC(ImageSwitch activeTab)
    {
        // Add logic here to change the content based on the active tab
    }
    private void TabRequestedActivateP(object sender, EventArgs e)
    {
        ActivateTabP(sender as ImageSwitch);
    }
    private void ActivateTabP(ImageSwitch activeTab)
    {
        TabP1.SwitchSource = TabP1 == activeTab ? TabP1.SwitchOn : TabP1.SwitchOff;
        TabP2.SwitchSource = TabP2 == activeTab ? TabP2.SwitchOn : TabP2.SwitchOff;
        if (TabP1 == activeTab)
        {
            TabJoin.IsVisible = false;
            TabCreate.IsVisible = true;
            actionBtn.Source = "btn_create.png";
        }
        else
        {
            TabJoin.IsVisible = true;
            TabCreate.IsVisible = false;
            actionBtn.Source = "btn_join_large.png";
        }
        // Add logic here to change the content based on the active tab
    }
}