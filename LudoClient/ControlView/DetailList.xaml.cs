using LudoClient.Models;
using Microsoft.Maui.Graphics.Text;
using SharedCode;
using SharedCode.Constants;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace LudoClient.ControlView
{
    public partial class DetailList : ContentView
    {
        public PlayerCard playerCard = new PlayerCard();
        public String cardActionType = "";
        public DetailList()
        {
            InitializeComponent();
        }
        public void SetDetails(PlayerCard playerCard, String type)
        {//detailgold.png
            this.playerCard = playerCard;
            PlayerImage.Source = playerCard.playerPicture;
            PlayerName.Text = playerCard.playerName;

            switch (playerCard.rank)
            {
                case 1:
                    PlayerMadel.Source = "number_1.png";
                    PlayerName.TextColor = Color.Parse("Black");
                    BackgroundLayerImage.ImageSource = "detailgold.png";
                    break;
                case 2:
                    PlayerMadel.Source = "number_2.png";
                    PlayerName.TextColor = Color.Parse("Black");
                    BackgroundLayerImage.ImageSource = "detailwhite.png";
                    break;
                case 3:
                    PlayerMadel.Source = "number_3.png";
                    PlayerName.TextColor = Color.Parse("Black");
                    BackgroundLayerImage.ImageSource = "detailwhite.png";
                    break;
                default:
                    PlayerMadel.Source = "number_0.png";
                    PlayerName.TextColor = Color.Parse("White");
                    BackgroundLayerImage.ImageSource = "friendlong.png";
                    break;
            }
            switch (type)
            {
                case "Header":
                    switch (playerCard.status)
                    {
                        case "ADD FRIEND":
                            TappedActionText.Text = "UN FRIEND";
                            BlockActionText.Text = "BLOCK";
                            break;
                        case "UN FRIEND":
                        case "UN BLOCK":
                            TappedActionText.Text = "ADD FRIEND";
                            BlockActionText.Text = "BLOCK";
                            TappedAction.IsVisible = true;
                            break;
                        case "BLOCK":
                            TappedAction.IsVisible = false;
                            BlockActionText.Text = "UN BLOCK";
                            break;
                    }
                    PlayerMadel.IsVisible = false;
                    RankingText.IsVisible = false;
                    PlayerLocationImage.Margin = new Thickness(4, 0, 0, 0);
                    PlayerName.Margin = new Thickness(4, 0, 0, 0);
                    break;
                case "Friend":
                    if (playerCard.status == "UN FRIEND")
                    {
                        //TappedActionImage.Source = "btn_red.png";
                        TappedActionText.Text = "MESSAGE";
                    }
                    if (playerCard.status == "BLOCK")
                    {
                        TappedActionImage.Source = "btn_red.png";
                        TappedActionText.Text = "UN BLOCK";
                    }
                    BlockAction.IsVisible = false;
                    break;
                case "Leaderboard":
                    TappedAction.IsVisible = false;
                    BlockAction.IsVisible = false;
                    break;
            }
            if(type!= "Header")
                RankingText.Text = playerCard.rank > 99 ? "99+" : playerCard.rank.ToString();
        }
        private async void Block_Action_Tapped(object sender, EventArgs e)
        {
            SendFriendRequestAsync(UserInfo.Instance.Id, playerCard.playerID, BlockActionText.Text);
        }
        private async void Action_Tapped(object sender, EventArgs e)
        {
            if (TappedActionText.Text == "MESSAGE")//Default status is empy you can add friend
            {
                if (!GlobalConstants.MatchMaker.Connected)
                    return;
                ChatPage cp = new ChatPage(playerCard);

                Navigation.PushAsync(cp).Wait();
            }
            else 
                SendFriendRequestAsync(UserInfo.Instance.Id, playerCard.playerID, TappedActionText.Text);

            Console.WriteLine("Join Tapped");
            //playerId, userName, pictureUrl, gameType, gameCost, roomName
            //_ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, gameType, betAmount, RoomCode);
        }

        public async Task SendFriendRequestAsync(int senderId, int receiverId, string status)
        {
            try
            {
                // Send the HTTP POST request
                HttpResponseMessage response = await GlobalConstants.httpClient.GetAsync($"api/friends/friendrequest/?senderId={senderId}&receiverId={receiverId}&status={status}");
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if(status == responseBody)
                    {
                        switch (status)
                        {
                            case "ADD FRIEND":
                                TappedActionText.Text = "UN FRIEND";
                                BlockActionText.Text = "BLOCK";
                                break;
                            case "UN FRIEND":
                            case "UN BLOCK":
                                TappedActionText.Text = "ADD FRIEND";
                                BlockActionText.Text = "BLOCK";
                                TappedAction.IsVisible = true;
                                break;
                            case "BLOCK":
                                TappedAction.IsVisible = false;
                                BlockActionText.Text = "UN BLOCK";
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}