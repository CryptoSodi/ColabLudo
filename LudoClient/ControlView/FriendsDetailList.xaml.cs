using SharedCode;
using SharedCode.Constants;

namespace LudoClient.ControlView
{
    public partial class FriendsDetailList : ContentView
    {
        public Friends friend = new Friends();
        public FriendsDetailList()
        {
            InitializeComponent();
        }
        public void SetFriendsDetail(Friends friend)
        {
            this.friend = friend;
            PlayerImage.Source = friend.playerPicture;
            PlayerName.Text = friend.playerName;
        }
        private async void Join_Tapped(object sender, EventArgs e)
        {
            Console.WriteLine("Join Tapped");
            //playerId, userName, pictureUrl, gameType, gameCost, roomName
            //_ = GlobalConstants.MatchMaker.CreateJoinLobbyAsync(UserInfo.Instance.Id, UserInfo.Instance.Name, UserInfo.Instance.PictureUrl, gameType, betAmount, RoomCode);
        }
    }
}