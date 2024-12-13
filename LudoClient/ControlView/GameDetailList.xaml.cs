
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
        public GameDetailList()
        {
            InitializeComponent();
            
        }
        public void SetTournamentDetails(int gameId, string roomCode, string gameType, decimal betAmount)
        {
            // Set the text of the labels
            GameId.Text = "Game : "+ gameId.ToString();
            TotalPlayersLabel.Text = "Total Players : " + gameType;
            JoiningFeeLabel.Text = $"Entry Fee : {betAmount}";
            RoomCode.Text = roomCode;
        }
        private async void Join_Tapped(object sender, EventArgs e)
        {
            Console.WriteLine("Join Tapped");
            //playerId, userName, pictureUrl, gameType, gameCost, roomName
            _ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, TotalPlayersLabel.Text.Replace("Total Players : ", ""), int.Parse(JoiningFeeLabel.Text.Replace("Entry Fee : ", "").Replace(".00", "")), RoomCode.Text);
        }
    }
}