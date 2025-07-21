using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
namespace LudoClient.Popups;

public partial class MessageBox : BasePopup
{
    private TaskCompletionSource<string?> _resultTcs = new();
    public MessageBox(String title, String question, String message)
    {
        InitializeComponent();
        Title.Title = title;
        Message.Text = question;
        SubMessage.Text = message;
    }
    private void BTNClose(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        _resultTcs.TrySetResult("Cancel"); // Set result
        CloseAsync();
    }
    private void BTNApprove(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        _resultTcs.TrySetResult("Approve"); // Set result
        CloseAsync();
    }
    public async Task<string?> ShowAsync()
    {
        //var popup = new MessageBox(title, question, message);
        Application.Current.MainPage?.ShowPopup(this, new PopupOptions { Shape = null });
        return await this._resultTcs.Task;
    }
}