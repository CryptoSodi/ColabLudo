using SharedCode.Constants;

namespace LudoClient;

public partial class GameRoom : ContentPage
{
    string GameType = "0";

    [Obsolete]
    public GameRoom(string GameType, int GameCost, string roomCode)
    {
        InitializeComponent();
        this.GameType = GameType;
        shareBox.SetShareCode(roomCode);
        NavigationPage.SetHasBackButton(this, false);
        GlobalConstants.MatchMaker.Ready(roomCode);
        switch (GameType)
        {
            case "2":
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 1);
                Grid.SetRow(player2, 5);
                Grid.SetColumn(player2, 3);
                grid.Children.Remove(player3);
                grid.Children.Remove(player4);
                thunder.Source = "thunder_" + GameType + ".gif";
                break;
            case "3":
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 5);
                Grid.SetColumn(player2, 1);
                Grid.SetRow(player3, 5);
                Grid.SetColumn(player3, 3);
                grid.Children.Remove(player4);
                thunder.Source = "thunder_" + GameType + ".gif";
                break;
            case "4":
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 4);
                Grid.SetColumn(player2, 1);
                Grid.SetRow(player3, 4);
                Grid.SetColumn(player3, 3);
                Grid.SetRow(player4, 5);
                Grid.SetColumn(player4, 2);
                thunder.Source = "thunder_" + GameType + ".gif";
                break;
            case "22":
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 4);
                Grid.SetColumn(player2, 1);
                Grid.SetRow(player3, 4);
                Grid.SetColumn(player3, 3);
                Grid.SetRow(player4, 5);
                Grid.SetColumn(player4, 2);
                thunder.Source = "thunder_" + 2 + ".gif";
                break;
        }
        GlobalConstants.MatchMaker.PlayerSeat += (playerType, playerId, userName, pictureUrl) =>
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if(playerType=="P1")
                {
                    player1.PlayerImage = pictureUrl;
                    player1.PlayerName = userName;
                }
                else if (playerType == "P2")
                {
                    player2.PlayerImage = pictureUrl;
                    player2.PlayerName = userName;
                }
                else if (playerType == "P3")
                {
                    player3.PlayerImage = pictureUrl;
                    player3.PlayerName = userName;
                }
                else if (playerType == "P4")
                {
                    player4.PlayerImage = pictureUrl;
                    player4.PlayerName = userName;
                }
                // Handle the request here
            });
        };
    }
    protected override bool OnBackButtonPressed()
    {
        // Prevent back navigation
        return true;
    }
}