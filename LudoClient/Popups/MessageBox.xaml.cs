using CommunityToolkit.Maui.Views;
namespace LudoClient.Popups;

public partial class MessageBox : BasePopup
{
    public MessageBox()
    {
        InitializeComponent();
        basePopup.capsule.ImageSource = "signin_inner_bg.png";
        Message.Text = "Are you sure you want to exit?";
        SubMessage.Text = "You will lose your bet amount!";
        Title.Title = "Exit";
    }
}