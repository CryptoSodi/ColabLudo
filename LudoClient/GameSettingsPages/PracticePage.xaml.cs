using LudoClient.ControlView;
using SharedCode.Constants;
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
        int gameType = 2;
        if (Tab1.IsActive)
            gameType = 2;
        if (Tab2.IsActive)
            gameType = 3;
        if (Tab3.IsActive)
            gameType = 4;
        if (Tab4.IsActive)
            gameType = 22;

        // Add logic here to join an offline game
        //Application.Current.MainPage = new Game(gametype, playerCount, playerColor);
        _ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, gameType + "", 0, "");
    }
}