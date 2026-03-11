using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using LudoClient.Constants;
using LudoClient.Popups;
using SharedCode.Constants;
namespace LudoClient;

public partial class HeaderCV : ContentView
{
    public HeaderCV()
    {
        InitializeComponent();
        PlayerImageItem.Source = UserInfo.Instance.ProfileImageSource;
        Loaded += HeaderCV_Loaded;
        Unloaded += HeaderCV_Unloaded;
    }
    private void HeaderCV_Loaded(object sender, EventArgs e)
    {
        if(UserInfo.Instance.player != null)
        {
            var wallet = UserInfo.Instance.player?.Wallet;
            if (wallet != null)
            {
                wallet.BalanceChanged += Wallet_BalanceChanged;
                // Update immediately
                Wallet_BalanceChanged(wallet.AvailableBalance);
            }
        }
    }

    private void HeaderCV_Unloaded(object sender, EventArgs e)
    {
        if (UserInfo.Instance.player != null)
        {
            var wallet = UserInfo.Instance.player?.Wallet;

            if (wallet != null)
                wallet.BalanceChanged -= Wallet_BalanceChanged;
        }
    }
    private void Wallet_BalanceChanged(decimal balance)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Coins.Text = ClientGlobalConstants.NormalizeCoins(balance);
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