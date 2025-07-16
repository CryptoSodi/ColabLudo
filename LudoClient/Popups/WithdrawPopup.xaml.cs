using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Constants;
using Microsoft.AspNetCore.SignalR.Client;
using SharedCode;
using SharedCode.Constants;
using System.Net;
using System.Threading.Tasks;

namespace LudoClient.Popups;

public partial class WithdrawPopup : BasePopup
{
    String Address = "";
    String recAddress = "";
    decimal SolBalance=0;
    public WithdrawPopup()
    {
        InitializeComponent();
            GenerateQRCodeAsync();
    }
    public async Task GenerateQRCodeAsync()
    {
        const string BaseUrl = "https://quickchart.io/qr";
        // You can tweak these hex colors and size as you like:
        var lightColor = "4031af";
        var darkColor = "ededed";
        var size = 200;

        String QrUrl = $"{BaseUrl}"
              + $"?text={UserInfo.Instance.player.Wallet?.WalletAddress}"
              + $"&light={lightColor}"
              + $"&dark={darkColor}"
              + $"&size={size}";
        Address = UserInfo.Instance.player.Wallet?.WalletAddress;
        // Update the image source asynchronously (UI thread)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Coins.Text = ClientGlobalConstants.NormalizeCoins(UserInfo.Instance.player.Wallet.AvailableBalance);
            AddressText.Text = UserInfo.Instance.player.Wallet?.WalletAddress;
            AmmountEntry.entryField.Text = ClientGlobalConstants.NormalizeCoins(UserInfo.Instance.player.Wallet.AvailableBalance);
            SolBalance = (decimal)UserInfo.Instance.player.Wallet?.AvailableBalance;
        });
    }
    private void OnSendButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        string amountInSoltext = AmmountEntry.entryField.Text;

        if (decimal.TryParse(amountInSoltext, out decimal amountInSol))
        {
            if (SolBalance < amountInSol)
            {
                Toast.Make("Insufficient Balance!", ToastDuration.Short, 22).Show();
                return;
            }
            String result = GlobalConstants.MatchMaker.SendSolAsync(recAddress, amountInSol).GetAwaiter().GetResult();
          if(result=="ERROR")
                Toast.Make("ERROR SENDING FAILED!", ToastDuration.Short, 22).Show();
        }
        else
        {
            Toast.Make("Please enter a valid number.", ToastDuration.Short, 22).Show();
        }
    }
    private void OnPasteButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        Clipboard.Default.SetTextAsync(Address);
        // Check if there's text on the clipboard
        if (Clipboard.Default.HasText)
        {
            // Retrieve text asynchronously
            string? clipboardText = Clipboard.Default.GetTextAsync().GetAwaiter().GetResult();
            // Assign to your singleton
            recAddress = clipboardText ?? string.Empty;
            AddressEntry.entryField.Text = recAddress;
        }
        else
        {
            // Handle empty clipboard case
            Toast.Make("Empty Clipboard!", ToastDuration.Short, 22).Show();
        }
    }
}