using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Constants;
using SharedCode;
using SharedCode.Constants;
using System.Net;

namespace LudoClient.Popups;

public partial class WithdrawPopup : BasePopup
{
    String Address = "";
    String recAddress = "";
    double SolBalance=0;
    public WithdrawPopup()
    {
        InitializeComponent();
            GenerateQRCodeAsync();
    }
    public async Task GenerateQRCodeAsync()
    {
        const string BaseUrl = "https://quickchart.io/qr";
        DepositInfo info = GlobalConstants.MatchMaker.UserConnectedSetID().GetAwaiter().GetResult();
        // You can tweak these hex colors and size as you like:
        var lightColor = "4031af";
        var darkColor = "ededed";
        var size = 200;

        String QrUrl = $"{BaseUrl}"
              + $"?text={info.Address}"
              + $"&light={lightColor}"
              + $"&dark={darkColor}"
              + $"&size={size}";
        Address = info.Address;
        // Update the image source asynchronously (UI thread)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Coins.Text = Math.Floor(double.Parse(info.SolBalance) * 100) / 100.0 + "";
            AddressText.Text = info.Address;
            AmmountEntry.entryField.Text = info.SolBalance;
            SolBalance = double.Parse(info.SolBalance);
        });
    }
    private void OnSendButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        string amountInSoltext = AmmountEntry.entryField.Text;

        if (double.TryParse(amountInSoltext, out double amountInSol))
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