
using LudoClient.Models;
using SharedCode.Constants;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;

namespace LudoClient.ControlView
{
    public partial class GameDetailList : ContentView
    {
        public int gameId = 0;
        public string RoomCode = "";
        public GameDetailList()
        {
            InitializeComponent();
        }
        public void SetTournamentDetails(int gameId, string roomCode, string gameType, decimal betAmount)
        {
            decimal priceamount = 0;
            this.gameId = gameId;
            // Set the text of the labels
            GameId.Text = "Game : "+ gameId.ToString();
            
            JoiningFeeLabel.Text = $"{betAmount}";
            RoomCode = roomCode;
            
            if(gameType == "22")
                TotalPlayersLabel.Text = "2 vs 2 Players - 2 Winners";
            else if (gameType == "2")
                TotalPlayersLabel.Text = $"1 vs {gameType} Player - 1 Winners";
            else
                TotalPlayersLabel.Text = $"1 vs {gameType} Players - 1 Winners";
            
            if (gameType == "22")
                priceamount = 2 * betAmount;
            else
                priceamount = Int32.Parse(gameType) * betAmount;

            PrizeAmountLabel.Text = $"{(int)priceamount}";
        }
        private async void Join_Tapped(object sender, EventArgs e)
        {
            Console.WriteLine("Join Tapped");
            //playerId, userName, pictureUrl, gameType, gameCost, roomName
            _ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, TotalPlayersLabel.Text.Replace("Total Players : ", ""), int.Parse(JoiningFeeLabel.Text.Replace("Entry Fee : ", "").Replace(".00", "")), RoomCode);
        }
    }
}