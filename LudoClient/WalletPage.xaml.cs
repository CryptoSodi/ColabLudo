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
        Coins.Text = UserInfo.Instance.LudoCoins + "";
        //this.ShowPopup(new AddCash());
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