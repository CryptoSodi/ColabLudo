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
        GenerateQRCodeAsync();
    }
    public void GenerateQRCodeAsync()
    {
        // Update the image source asynchronously (UI thread)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Address = UserInfo.Instance.player.Wallets.FirstOrDefault()?.WalletAddress;
            Coins.Text = UserInfo.Instance.player.Wallets.FirstOrDefault()?.AvailableBalance + " SOL";
            AddressText.Text = UserInfo.Instance.player.Wallets.FirstOrDefault()?.WalletAddress;
            QRCodeImage.Source = UserInfo.ConvertBase64ToImage(UserInfo.Instance.AddressQRBlob);
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