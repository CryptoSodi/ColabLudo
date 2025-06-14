using LudoClient.Constants;
using LudoClient.ControlView;
using LudoClient.CoreEngine;
using System.Security.AccessControl;

namespace LudoClient;

public partial class OfflinePage : ContentPage
{
    String playerColor = "Red";
    String gameMode = "Computer";
    String gameType = "2";
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
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ImageSwitch activeTab = sender as ImageSwitch;
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
        // Add logic here to change the content based on the active tab
        if (Tab1 == activeTab)
            gameType = "2";
        if (Tab2 == activeTab)
            gameType = "3";
        if (Tab3 == activeTab)
            gameType = "4";
        if (Tab4 == activeTab)
            gameType = "22";
    }
    private void TabRequestedActivateC(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        var activeTab = e.Parameter as string;
        TabC1.SwitchSource = "TabC1" == activeTab ? TabC1.SwitchOn : TabC1.SwitchOff;
        TabC2.SwitchSource = "TabC2" == activeTab ? TabC2.SwitchOn : TabC2.SwitchOff;
        TabC3.SwitchSource = "TabC3" == activeTab ? TabC3.SwitchOn : TabC3.SwitchOff;
        TabC4.SwitchSource = "TabC4" == activeTab ? TabC4.SwitchOn : TabC4.SwitchOff;
        // Add logic here to change the content based on the active tab
        if ("TabC1" == activeTab)
            playerColor = "Red";
        if ("TabC2" == activeTab)
            playerColor = "Green";
        if ("TabC3" == activeTab)
            playerColor = "Blue";
        if ("TabC4" == activeTab)
            playerColor = "Yellow";
    }
    private void TabRequestedActivateP(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        var activeTab = e.Parameter as string;

        TabP1.SwitchSource = "TabP1" == activeTab ? TabP1.SwitchOn : TabP1.SwitchOff;
        TabP2.SwitchSource = "TabP2" == activeTab ? TabP2.SwitchOn : TabP2.SwitchOff;
        
        // Add logic here to change the content based on the active tab
        if ("TabP1" == activeTab)
            gameMode = "Computer";
        if ("TabP2" == activeTab)
            gameMode = "Local";
    }
    private void JoinOfflineTapped(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        
        ClientGlobalConstants.game = new Game(gameMode, gameType, playerColor);
        ClientGlobalConstants.dashBoard.Navigation.PushAsync(ClientGlobalConstants.game);

        ClientGlobalConstants.FlushOld();
    }
}