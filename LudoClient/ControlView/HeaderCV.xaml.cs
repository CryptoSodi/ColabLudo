using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
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

        // Initialize and start the timer
        _qrCodeTimer = new System.Timers.Timer(1000); // 60,000 milliseconds = 60 seconds
        _qrCodeTimer.Elapsed += async (sender, e) => await UpdateBalance();
        _qrCodeTimer.AutoReset = _qrCodeTimer.Enabled = true;

        Loaded += async (s, e) => {
            await UpdateBalance();
            _qrCodeTimer?.Start();
        };

        Unloaded += (s, e) => {
            _qrCodeTimer?.Stop();
        };
    }
    public async Task UpdateBalance()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            // Wait until the PictureUrlBlob is populated
            while (string.IsNullOrEmpty(UserInfo.Instance.PictureUrlBlob))
                await Task.Delay(10); // wait properly

            var d = PlayerImageItem.Width;
            // Only load the image once if it's not already loaded
            if (d <= 0)
            {
                PlayerImageItem.Source = UserInfo.ConvertBase64ToImage(UserInfo.Instance.PictureUrlBlob);
                await Task.Delay(100); // wait properly
            }
        });
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
    private async void Navigate_Settings(object sender, EventArgs e)
    {
        if (_NavigationCooldown)
            return;
        _NavigationCooldown = true;
        try
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            ClientGlobalConstants.settings = new Settings();
            Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.settings, new PopupOptions { Shape = null });
        }
        finally
        {
            await Task.Delay(500); // half-second cooldown
            _NavigationCooldown = false;
        }
    }
    bool _NavigationCooldown = false;
    private async void OnPlayerTapped(object sender, EventArgs e)
    {
        if (_NavigationCooldown)
            return;
        _NavigationCooldown = true;
        try
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            ClientGlobalConstants.profileInfo = new ProfileInfo();
            Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.profileInfo, new PopupOptions { Shape = null });
        }
        finally
        {
            await Task.Delay(500); // half-second cooldown
            _NavigationCooldown = false;
        }
    }
}