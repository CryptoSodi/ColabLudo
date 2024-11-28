using LudoClient.ControlView;
using LudoClient.CoreEngine;

namespace LudoClient;

public partial class OfflinePage : ContentPage
{
    String gametype = "Computer";
    String playerColor = "Red";
    String playerCount = "2";
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
        ImageSwitch activeTab = sender as ImageSwitch;
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
        // Add logic here to change the content based on the active tab
        if (Tab1 == activeTab)
            playerCount = "2";
        if (Tab2 == activeTab)
            playerCount = "3";
        if (Tab3 == activeTab)
            playerCount = "4";
        if (Tab4 == activeTab)
            playerCount = "22";
    }
    private void TabRequestedActivateC(object sender, EventArgs e)
    {
        ImageSwitch activeTab = sender as ImageSwitch;
        TabC1.SwitchSource = TabC1 == activeTab ? TabC1.SwitchOn : TabC1.SwitchOff;
        TabC2.SwitchSource = TabC2 == activeTab ? TabC2.SwitchOn : TabC2.SwitchOff;
        TabC3.SwitchSource = TabC3 == activeTab ? TabC3.SwitchOn : TabC3.SwitchOff;
        TabC4.SwitchSource = TabC4 == activeTab ? TabC4.SwitchOn : TabC4.SwitchOff;
        // Add logic here to change the content based on the active tab
        if (TabC1 == activeTab)
            playerColor = "Red";
        if (TabC2 == activeTab)
            playerColor = "Green";
        if (TabC3 == activeTab)
            playerColor = "Blue";
        if (TabC4 == activeTab)
            playerColor = "Yellow";
    }
    private void TabRequestedActivateP(object sender, EventArgs e)
    {
        ImageSwitch activeTab = sender as ImageSwitch;

        TabP1.SwitchSource = TabP1 == activeTab ? TabP1.SwitchOn : TabP1.SwitchOff;
        TabP2.SwitchSource = TabP2 == activeTab ? TabP2.SwitchOn : TabP2.SwitchOff;
        
        // Add logic here to change the content based on the active tab
        if (TabP1 == activeTab)
            gametype = "Computer";
        if (TabP2 == activeTab)
            gametype = "Local";
    }
    private void JoinOfflineTapped(object sender, EventArgs e)
    {
        // Add logic here to join an offline game
        Application.Current.MainPage = new Game(gametype, playerCount, playerColor);
    }
}