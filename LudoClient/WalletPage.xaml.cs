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
            Coins.Text = Math.Floor(balance * 100) / 100 + " LUDC";
        });
        //this.ShowPopup(new AddCash());
        // Initialize and start the timer        
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Force layout to update ContentSize
        UpdateBalance();
        await Task.Delay(1);
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
                        Coins.Text = Math.Floor(balance * 100) / 100 + " LUDC";
                        balance = UserInfo.Instance.player.Wallet?.SignupBonus ?? 0;
                        SignupBonus.Text = Math.Floor(balance * 100) / 100 + " LUDC";
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
        this.ShowPopup(new AddCash());
    }
    private void OnWithdrawButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        this.ShowPopup(new WithdrawPopup());
    }
}