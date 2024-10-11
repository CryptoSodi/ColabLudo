using LudoClient.Constants;
using LudoClient.Network;

namespace LudoClient;

public partial class GameRoom : ContentPage
{
    int GameType = 0;
    Client MatchMaker = new Client();
    public GameRoom(int GameType, int GameCost)
    {
        this.GameType = GameType;
        InitializeComponent();
        NavigationPage.SetHasBackButton(this, false);
        switch (GameType)
        {
            case 2:
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 1);
                Grid.SetRow(player2, 5);
                Grid.SetColumn(player2, 3);
                grid.Children.Remove(player3);
                grid.Children.Remove(player4);
                thunder.Source = "thunder_" + GameType + ".gif";
                break;
            case 3:
                Grid.SetRow(player1, 3);
                Grid.SetColumn(player1, 2);
                Grid.SetRow(player2, 5);
                Grid.SetColumn(player2, 1);
                Grid.SetRow(player3, 5);
                Grid.SetColumn(player3, 3);
                grid.Children.Remove(player4);
                thunder.Source = "thunder_" + GameType + ".gif";
                break;
            case 4:
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
            case 22:
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
        MatchMaker.PlayerSeat += (playerType, playerId, userName, pictureUrl) =>
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                // Handle the request here
            });
        };
        MatchMaker.CreateJoinRoom(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, GameType + "", GameCost, "", shareBox);
    }
    protected override bool OnBackButtonPressed()
    {
        MatchMaker.Disconnect();
        // Prevent back navigation
        return true;
    }
}