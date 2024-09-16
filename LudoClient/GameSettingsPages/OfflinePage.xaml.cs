using LudoClient.ControlView;

namespace LudoClient;

public partial class OfflinePage : ContentPage
{
    public OfflinePage()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4.SwitchOff;

        TabC1.SwitchSource = TabC1.SwitchOn;
        TabC2.SwitchSource = TabC2.SwitchOff;
        TabC3.SwitchSource = TabC3.SwitchOff;
        TabC4.SwitchSource = TabC4.SwitchOff;

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
    private void TabRequestedActivateC(object sender, EventArgs e)
    {
        ActivateTabC(sender as ImageSwitch);
    }
    private void ActivateTabC(ImageSwitch activeTab)
    {
        TabC1.SwitchSource = TabC1 == activeTab ? TabC1.SwitchOn : TabC1.SwitchOff;
        TabC2.SwitchSource = TabC2 == activeTab ? TabC2.SwitchOn : TabC2.SwitchOff;
        TabC3.SwitchSource = TabC3 == activeTab ? TabC3.SwitchOn : TabC3.SwitchOff;
        TabC4.SwitchSource = TabC4 == activeTab ? TabC4.SwitchOn : TabC4.SwitchOff;
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
            //  TabJoin.IsVisible = false;
            //  TabCreate.IsVisible = true;
        }
        else
        {
            // TabJoin.IsVisible = true;
            //  TabCreate.IsVisible = false;
        }
        // Add logic here to change the content based on the active tab
    }
}