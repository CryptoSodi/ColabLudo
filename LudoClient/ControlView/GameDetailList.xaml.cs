using LudoClient.Constants;
using LudoClient.Models;
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
        /// <summary>
        /// Sets the tournament details on the UI and starts the countdown timer.
        /// </summary>
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
            GlobalConstants.MatchMaker.CreateJoinLobby(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, "", 0, RoomCode.Text, null);
        }
    }
}