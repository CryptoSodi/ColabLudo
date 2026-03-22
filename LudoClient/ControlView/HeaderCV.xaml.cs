using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using LudoClient.Constants;
using LudoClient.Popups;
using SharedCode.Constants;
using System.Diagnostics;
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
            ClientGlobalConstants.sw = Stopwatch.StartNew();
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.settings, new PopupOptions { Shape = null });
#if ANDROID
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
            if (activity != null)
            {
                var dialog = new LudoClient.Platforms.Android.SettingsDialogFragment();
                dialog.Show(activity.SupportFragmentManager, "SettingsDialog");
            }
#else
            Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.settings, new PopupOptions { Shape = null });
#endif
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
            ClientGlobalConstants.sw = Stopwatch.StartNew();
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            Application.Current?.MainPage?.ShowPopup(ClientGlobalConstants.profileInfo, new PopupOptions { Shape = null });
        }
        finally
        {
            await Task.Delay(500); // half-second cooldown
            _NavigationCooldown = false;
        }
    }
}