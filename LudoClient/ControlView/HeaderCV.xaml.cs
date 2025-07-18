using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.Popups;
using SharedCode.Constants;
namespace LudoClient;

public partial class HeaderCV : ContentView
{
    private System.Timers.Timer _qrCodeTimer;
    public HeaderCV()
    {
        InitializeComponent();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            while (UserInfo.Instance.PictureUrlBlob == "")
                Task.Delay(10);
            PlayerImageItem.Source = UserInfo.ConvertBase64ToImage(UserInfo.Instance.PictureUrlBlob);
        });

        // Initialize and start the timer
        _qrCodeTimer = new System.Timers.Timer(1000); // 60,000 milliseconds = 60 seconds
        _qrCodeTimer.Elapsed += async (sender, e) => await UpdateBalance();
        _qrCodeTimer.AutoReset = true;
        _qrCodeTimer.Enabled = true;

        UpdateBalance();
    }
    public async Task UpdateBalance()
    {
        if (GlobalConstants.MatchMaker != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (UserInfo.Instance.player != null && UserInfo.Instance.player.Wallet != null)
                        Coins.Text = ClientGlobalConstants.NormalizeCoins(UserInfo.Instance.player.Wallet.AvailableBalance);
                }
                catch (Exception)
                {
                }
            });
        }
    }
    private void Navigate_Settings(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ClientGlobalConstants.settings = new Settings();
        Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.settings);
    }
    private void OnPlayerTapped(object sender, EventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        ClientGlobalConstants.profileInfo = new ProfileInfo();
        Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.profileInfo);
    }
}