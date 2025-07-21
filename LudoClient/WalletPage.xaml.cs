using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using LudoClient.Constants;
using LudoClient.Popups;
using SharedCode.Constants;
namespace LudoClient;
public partial class WalletPage : ContentPage
{
    public WalletPage()
    {
        InitializeComponent();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var balance = UserInfo.Instance.player.Wallet?.AvailableBalance ?? 0;
            Coins.Text = ClientGlobalConstants.NormalizeCoins(balance);
        });
        //this.ShowPopup(new AddCash());
        // Initialize and start the timer        
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Force layout to update ContentSize
        Task.Run(UpdateBalance); // Run async without blocking constructor
    }
    public void UpdateBalance()
    {
        MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    if (UserInfo.Instance.player != null)
                    {
                        var balance = UserInfo.Instance.player.Wallet?.AvailableBalance ?? 0;                        
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