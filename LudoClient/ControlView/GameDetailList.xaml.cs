using LudoClient.Constants;
using SharedCode;
using SharedCode.Constants;

namespace LudoClient.ControlView
{
    public partial class GameDetailList : ContentView
    {
        public int gameId = 0;
        int betAmount;
        string RoomCode;
        string gameType;
        public GameDetailList()
        {
            InitializeComponent();
        }
        public void SetTournamentDetails(int gameId, string roomCode, string gameType, decimal betAmount)
        {
            decimal priceamount = 0;
            this.gameId = gameId;
            this.gameType = gameType;
            this.betAmount = (int)betAmount;
            // Set the text of the labels
            GameId.Text = "Game : " + gameId.ToString();

            JoiningFeeLabel.Text = ClientGlobalConstants.NormalizeCoinsDecimal(betAmount).ToString();
            RoomCode = roomCode;

            if (gameType == "22")
                TotalPlayersLabel.Text = "2 vs 2 : 4 Players - 2 Winners";
            else if (gameType == "2")
                TotalPlayersLabel.Text = $"1 vs 1 : 2 Players - 1 Winner";
            else
                TotalPlayersLabel.Text = $"1 vs {gameType} Players - 1 Winner";
            
            if (gameType == "22")
                priceamount = ClientGlobalConstants.NormalizeCoinsDecimal(2 * betAmount);
            else
                priceamount = ClientGlobalConstants.NormalizeCoinsDecimal(Int32.Parse(gameType) * betAmount);

            PrizeAmountLabel.Text = $"{priceamount}";
        }
        bool _NavigationCooldown = false;
        private void Join_Tapped(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            Console.WriteLine("Join Tapped");
            if (_NavigationCooldown)
                return;
            _NavigationCooldown = true;

            GameDto gameDto = new GameDto();
            gameDto.GameType = gameType; // Set the game type based on the active tab
            gameDto.IsPracticeGame = false; // Set the practice game flag
            gameDto.BetAmount = betAmount;
            gameDto.RoomCode = RoomCode;
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
            return;
        }
    }
}