using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Constants;
using SharedCode;
using SharedCode.Constants;

namespace LudoClient.Popups;

public partial class AddCash : BasePopup
{
    String Address = "";
    public AddCash()
    {
        InitializeComponent();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            GenerateQRCodeAsync();
        });
    }
    public async Task GenerateQRCodeAsync()
    {
        const string BaseUrl = "https://quickchart.io/qr";        
        // You can tweak these hex colors and size as you like:
        var lightColor = "4031af";
        var darkColor = "ededed";
        var size = 200;

        String QrUrl = $"{BaseUrl}"
              + $"?text={UserInfo.Instance.player.Wallets.FirstOrDefault()?.WalletAddress}"
              + $"&light={lightColor}"
              + $"&dark={darkColor}"
              + $"&size={size}";
        
        Address = UserInfo.Instance.player.Wallets.FirstOrDefault()?.WalletAddress;
        // Update the image source asynchronously (UI thread)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Coins.Text = UserInfo.Instance.player.Wallets.FirstOrDefault()?.AvailableBalance + " SOL";
            AddressText.Text = UserInfo.Instance.player.Wallets.FirstOrDefault()?.WalletAddress;
            QRCodeImage.Source = QrUrl;
        });
    }
    private void OnCopyButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        Clipboard.Default.SetTextAsync(Address);
        // Show toast message
        Toast.Make("Copied to Clipboard", ToastDuration.Short, 22).Show();
    }
}