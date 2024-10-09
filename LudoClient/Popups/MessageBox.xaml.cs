using CommunityToolkit.Maui.Views;
namespace LudoClient.Popups;

public partial class MessageBox : BasePopup
{
    public MessageBox()
    {
        InitializeComponent();
        Message.Text = "Are you sure you want to exit?";
        SubMessage.Text = "You will lose your bet amount!";
        Title.Title = "Exit";
    }
}