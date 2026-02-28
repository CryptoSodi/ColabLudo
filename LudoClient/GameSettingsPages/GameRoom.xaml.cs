using LudoClient.Constants;
using SharedCode.Constants;

namespace LudoClient;

public partial class GameRoom : ContentPage
{
    public GameRoom(string GameType, decimal GameCost)
    {
        InitializeComponent();

        GlobalConstants.MatchMaker.RoomJoined += OnRoomJoined;
        GlobalConstants.MatchMaker.PlayerSeated += PlayerSeated;
        NavigationPage.SetHasBackButton(this, false);        
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
    }

    private void PlayerSeated(object? sender, (string PlayerType, int PlayerId, string UserName, string PictureUrl) args)
    {
        Console.WriteLine("PlayerSeated event received with args: " + string.Join(", ", args));
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("playerjoin");

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (args.PlayerType == "P1")
            {
                player1.PlayerImage = args.PictureUrl;
                player1.PlayerName = args.UserName;
            }
            else if (args.PlayerType == "P2")
            {
                player2.PlayerImage = args.PictureUrl;
                player2.PlayerName = args.UserName;
            }
            else if (args.PlayerType == "P3")
            {
                player3.PlayerImage = args.PictureUrl;
                player3.PlayerName = args.UserName;
            }
            else if (args.PlayerType == "P4")
            {
                player4.PlayerImage = args.PictureUrl;
                player4.PlayerName = args.UserName;
            }
        });
    }

    private void OnRoomJoined(object? sender, (string GameType, double GameCost, string RoomCode) args)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            GlobalConstants.RoomCode = args.RoomCode;
            GlobalConstants.GameCost = args.GameCost;
            shareBox.SetShareCode(args.RoomCode);
            await GlobalConstants.MatchMaker.ReadyAsync();
        });
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GlobalConstants.MatchMaker.RoomJoined -= OnRoomJoined;
        GlobalConstants.MatchMaker.PlayerSeated -= PlayerSeated;
    }
    protected override bool OnBackButtonPressed()
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        // Prevent back navigation
        return true;
    }
}