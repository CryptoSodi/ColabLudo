using SharedCode.Constants;

namespace LudoClient.Popups;

public partial class ProfileInfo : BasePopup
{
    public ProfileInfo()
    {
        InitializeComponent();
        player.playerImageItem.Source = UserInfo.ConvertBase64ToImage(UserInfo.Instance.PictureBlob);
        player.PlayerName = UserInfo.Instance.Name;
        Email.Text = UserInfo.Instance.Email;
        Number.Text = UserInfo.Instance.PhoneNumber;
        Location.Text = UserInfo.Instance.City;
        Coins.Text = UserInfo.Instance.Coins + "";
    }
}