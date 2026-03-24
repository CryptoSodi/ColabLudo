using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using LudoClient.Constants;
using LudoClient.Popups;
using SharedCode.Constants;
namespace LudoClient;
public partial class WalletPage : ContentPage
{
    public WalletPage()
    {
        InitializeComponent();
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (UserInfo.Instance.player != null)
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
    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        var wallet = UserInfo.Instance.player?.Wallet;

        if (wallet != null)
        {
            wallet.BalanceChanged -= Wallet_BalanceChanged;
        }
    }
    public void Wallet_BalanceChanged(decimal balance)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (UserInfo.Instance.player != null)
                    {
                        Coins.Text = ClientGlobalConstants.NormalizeCoins(balance);
                        balance = UserInfo.Instance.player.Wallet?.SignupBonus ?? 0;
                        SignupBonus.Text = ClientGlobalConstants.NormalizeCoins(balance);
                    }
                }
                catch (Exception)
                {
                }
            });
    }
    private void OnDepositButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
        if (activity != null)
        {
            var dialog = new LudoClient.Platforms.Android.Popups.AddCashDialogFragment();
            dialog.Show(activity.SupportFragmentManager, "AddCashDialog");
        }
#else
        this.ShowPopup(ClientGlobalConstants.addCash, new PopupOptions { Shape = null });
#endif
    }
    private void OnWithdrawButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
#if ANDROID
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AndroidX.AppCompat.App.AppCompatActivity;
        if (activity != null)
        {
            var dialog = new LudoClient.Platforms.Android.Popups.WithdrawDialogFragment();
            dialog.Show(activity.SupportFragmentManager, "WithdrawDialog");
        }
#else
        this.ShowPopup(ClientGlobalConstants.withdrawPopup, new PopupOptions { Shape = null });
#endif
    }
}