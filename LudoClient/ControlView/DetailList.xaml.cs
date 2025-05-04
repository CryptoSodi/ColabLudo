using Microsoft.Maui.Graphics.Text;
using SharedCode;
using SharedCode.Constants;

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
                    if (playerCard.status == "")
                    {
                        TappedActionText.Text = "ADD FRIEND";
                    }
                    PlayerMadel.IsVisible = false;
                    RankingText.IsVisible = false;
                    PlayerLocationImage.Margin = new Thickness(4, 0, 0, 0);
                    PlayerName.Margin = new Thickness(4, 0, 0, 0);
                    break;
                case "Friend":

                    break;
                case "Leaderboard":
                    TappedAction.IsVisible = false;
                    break;
            }
            if (playerCard.status == "AddFriend")
            {
                //TappedActionImage.Source = "btn_red.png";
                TappedActionText.Text = "ADDFRIEND";
            }
            if (playerCard.status == "Blocked")
            {
                TappedActionImage.Source = "btn_red.png";
                TappedActionText.Text = "UNBLOCK";
            }
            if(type!= "Header")
                RankingText.Text = playerCard.rank > 99 ? "99+" : playerCard.rank.ToString();
        }
        private async void Action_Tapped(object sender, EventArgs e)
        {
            if(playerCard.status == "")//Default status is empy you can add friend
            {
                if (!GlobalConstants.MatchMaker.Connected)
                    return;
                ChatPage cp = new ChatPage(playerCard);

                Navigation.PushAsync(cp).Wait();
                //await GlobalConstants.FriendsList.AddFriendAsync(playerCard.playerID);
            }
            else
            {
                TappedActionImage.Source = "friend.png";
                TappedAction.IsVisible = true;
                //await GlobalConstants.FriendsList.RemoveFriendAsync(playerCard.playerID);
            }
            Console.WriteLine("Join Tapped");
            //playerId, userName, pictureUrl, gameType, gameCost, roomName
            //_ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, gameType, betAmount, RoomCode);
        }
    }
}