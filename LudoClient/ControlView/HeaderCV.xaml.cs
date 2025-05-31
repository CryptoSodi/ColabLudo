using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.Popups;
using SharedCode;
using SharedCode.Constants;
namespace LudoClient;

public partial class HeaderCV : ContentView
{
    private System.Timers.Timer _qrCodeTimer;
    public HeaderCV()
    {
        InitializeComponent();

        loadHeaderImage();

        // Initialize and start the timer
        _qrCodeTimer = new System.Timers.Timer(30000); // 60,000 milliseconds = 60 seconds
        _qrCodeTimer.Elapsed += async (sender, e) => await UpdateBalance();
        _qrCodeTimer.AutoReset = true;
        _qrCodeTimer.Enabled = true;

        UpdateBalance();
    }
    private async void loadHeaderImage()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlayerImageItem.Source = UserInfo.ConvertBase64ToImage(UserInfo.Instance.PictureUrlBlob);
        });
    }
    public async Task UpdateBalance()
    {
        if (GlobalConstants.MatchMaker != null)
        {
            try
            {
                DepositInfo info = GlobalConstants.MatchMaker.UserConnectedSetID().GetAwaiter().GetResult();
                // You can tweak these hex colors and size as you like:
                // Update the image source asynchronously (UI thread)
                UserInfo.Instance.CryptoAddress = info.Address;
                UserInfo.Instance.LudoCoins = Double.Parse(info.SolBalance);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Coins.Text = info.SolBalance + " SOL";
                });
            }
            catch (Exception)
            {
            }
        }
        else
        {
          await Task.Delay(100);
          UpdateBalance();
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