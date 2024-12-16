using LudoClient.ControlView;
using SharedCode.Constants;

namespace LudoClient;

public partial class CashGame : ContentPage
{
    public int entry = GlobalConstants.initialEntry;
    public int win = GlobalConstants.initialEntry * 2;
    public string activeTab = string.Empty;
    public bool defaultTabSelection = true;
    public CashGame()
    {
        InitializeComponent();
        Tab1.SwitchSource = Tab1.SwitchOn;
        Tab2.SwitchSource = Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4.SwitchOff;
        EntryLabel.Text = GlobalConstants.initialEntry.ToString();
        WinLabel.Text = (GlobalConstants.initialEntry * 2).ToString();
    }
    private void TabRequestedActivate(object sender, EventArgs e)
    {
        ActivateTab(sender as ImageSwitch);
    }
    private void ActivateTab(ImageSwitch activeTab)
    {
        var configTab1 = Tab1 == activeTab ? (Source: Tab1.SwitchOn, Active: true) : (Source: Tab1.SwitchOff, Active: false);
        Tab1.SwitchSource = configTab1.Source;
        Tab1.IsActive = configTab1.Active;
        var configTab2 = Tab2 == activeTab ? (Source: Tab2.SwitchOn, Active: true) : (Source: Tab2.SwitchOff, Active: false);
        Tab2.SwitchSource = configTab2.Source;
        Tab2.IsActive = configTab2.Active;
        var configTab3 = Tab3 == activeTab ? (Source: Tab3.SwitchOn, Active: true) : (Source: Tab3.SwitchOff, Active: false);
        Tab3.SwitchSource = configTab3.Source;
        Tab3.IsActive = configTab3.Active;
        var configTab4 = Tab4 == activeTab ? (Source: Tab4.SwitchOn, Active: true) : (Source: Tab4.SwitchOff, Active: false);
        Tab4.SwitchSource = configTab4.Source;
        Tab4.IsActive = configTab4.Active;
        // Add logic here to change the content based on the active tab
        defaultTabSelection = false;
        CalculateWin();
    }
    private void BtnPlus(object sender, EventArgs e)
    {
        entry += GlobalConstants.initialEntry;
        EntryLabel.Text = entry.ToString();
        CalculateWin();
    }
    private void BtnMinus(object sender, EventArgs e)
    {
        if (entry > GlobalConstants.initialEntry)
        {
            entry -= GlobalConstants.initialEntry;
            EntryLabel.Text = entry.ToString();
            CalculateWin();
        }
    }
    private void CalculateWin()
    {
        if (Tab1.IsActive || Tab4.IsActive || defaultTabSelection)
        {
            win = entry * 2;
        }
        else if (Tab2.IsActive)
        {
            win = entry * 3;
        }
        else if (Tab3.IsActive)
        {
            win = entry * 4;
        }
        WinLabel.Text = win.ToString();
    }
    private void JoinRoom_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new GamesListPage());
    }
    private void CreateRoom_Clicked(object sender, EventArgs e)
    {
        int gameType = 2;
        if (Tab1.IsActive)
            gameType = 2;
        if (Tab2.IsActive)
            gameType = 3;
        if (Tab3.IsActive)
            gameType = 4;
        if (Tab4.IsActive)
            gameType = 22;

        //Navigation.PushAsync(new GameRoom(gameType, entry));
        _ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, gameType + "", entry, "");

    }
}