namespace LudoClient.ControlView;

public partial class ShareBox : ContentView
{
    public void SetShareCode(string code)
    {
        shareCode.Text = code;
    }
    public ShareBox()
    {
        InitializeComponent();
    }
    async void OnWhatsappShareButtonClicked(object sender, EventArgs e)
    {
        string roomCode = shareCode.Text; // Replace with your actual room code
        string message = $"Join my room using this code: {roomCode}";

        string encodedMessage = Uri.EscapeDataString(message);
        string whatsappUrl = $"https://wa.me/?text={encodedMessage}";

        await Launcher.OpenAsync(whatsappUrl);
    }
    async void OnShareButtonClicked(object sender, EventArgs e)
    {
        await Share.RequestAsync(new ShareTextRequest
        {
            Text = shareCode.Text,
            Title = "Share Room Code"
        });
    }
}