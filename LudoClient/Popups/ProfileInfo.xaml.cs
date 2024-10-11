using LudoClient.Constants;

namespace LudoClient.Popups;

public partial class ProfileInfo : BasePopup
{
    public ProfileInfo()
    {
        InitializeComponent();
        player.PlayerImage = UserInfo.Instance.PictureUrl;
        player.PlayerName = UserInfo.Instance.Name;
        Email.Text = UserInfo.Instance.Email;
        Number.Text = UserInfo.Instance.Number;
        Location.Text = UserInfo.Instance.Location;
        Coins.Text = UserInfo.Instance.Coins + "";
    }
}