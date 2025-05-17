using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using LudoClient.Popups;
using SharedCode.Constants;
using System.Net;
namespace LudoClient;
public partial class WalletPage : ContentPage
{
    public WalletPage()
    {
        InitializeComponent();
        Coins.Text = UserInfo.Instance.Coins + "";
        //this.ShowPopup(new AddCash());
    }
    private void OnDepositButtonClicked(object sender, TappedEventArgs e)
    {
        this.ShowPopup(new AddCash());
    }
    private void OnWithdrawButtonClicked(object sender, TappedEventArgs e)
    {
        this.ShowPopup(new WithdrawPopup());
    }
}