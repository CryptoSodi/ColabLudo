using LudoClient.Constants;
using LudoClient.ControlView;
using SharedCode;
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
    string gameType = "2";
    private void ActivateTab(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ImageSwitch activeTab = sender as ImageSwitch;
        Tab1.SwitchSource = Tab1 == activeTab ? Tab1.SwitchOn : Tab1.SwitchOff;
        Tab2.SwitchSource = Tab2 == activeTab ? Tab2.SwitchOn : Tab2.SwitchOff;
        Tab3.SwitchSource = Tab3 == activeTab ? Tab3.SwitchOn : Tab3.SwitchOff;
        Tab4.SwitchSource = Tab4 == activeTab ? Tab4.SwitchOn : Tab4.SwitchOff;
        // Add logic here to change the content based on the active tab
        gameType = "2";
        if (Tab1 == activeTab)
            gameType = "2";
        if (Tab2 == activeTab)
            gameType = "3";
        if (Tab3 == activeTab)
            gameType = "4";
        if (Tab4 == activeTab)
            gameType = "22"; 
    }
    bool _NavigationCooldown = false;
    private void JoinPracticeTapped(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        if (_NavigationCooldown)
            return;
        _NavigationCooldown = true;

        GameDto gameDto = new GameDto();
        gameDto.GameType = gameType; // Set the game type based on the active tab
        gameDto.IsPracticeGame = true; // Set the practice game flag
        gameDto.BetAmount = 0;
        gameDto.RoomCode = "";
        gameDto.PlayerCount = int.Parse(gameType);
        if (gameDto.PlayerCount == 22)
            gameDto.PlayerCount = 4;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ClientGlobalConstants.dashBoard.Navigation.PushAsync(new GameRoom(gameDto.GameType, gameDto.BetAmount));
            ClientGlobalConstants.FlushOld();
        });
        try
        {
            //Navigation.PushAsync(new GameRoom(gameType, entry));
            _ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(gameDto);
        }
        finally
        {
            Task.Delay(500); // half-second cooldown
            _NavigationCooldown = false;
        }
    }
}