using CommunityToolkit.Maui.Views;
namespace LudoClient.Popups;

public partial class MessageBox : BasePopup
{
    public MessageBox(String title, String question, String message)
    {
        InitializeComponent();
        Title.Title = title;
        Message.Text = question;
        SubMessage.Text = message;
    }
    private void BTNClose(object sender, EventArgs e)
    {
        Close("Cancel");
    }
    private void BTNApprove(object sender, EventArgs e)
    {
        Close("Approve");
    }
}