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
        this.ShowPopup(new AddCash(), new PopupOptions { Shape = null });
    }
    private void OnWithdrawButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        this.ShowPopup(new WithdrawPopup(), new PopupOptions { Shape = null });
    }
}