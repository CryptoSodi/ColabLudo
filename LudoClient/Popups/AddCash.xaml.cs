using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using SharedCode;
using SharedCode.Constants;
using System.Buffers.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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
        Coins.Text = Math.Floor(double.Parse(info.SolBalance) * 100) / 100.0 + "";
        Address = info.Address;
        // Update the image source asynchronously (UI thread)
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AddressText.Text = info.Address;
            QRCodeImage.Source = QrUrl;
        });
    }
    private void OnCopyButtonClicked(object sender, TappedEventArgs e)
    {
        Clipboard.Default.SetTextAsync(Address);
        // Show toast message
        Toast.Make("Copied to Clipboard", ToastDuration.Short, 22).Show();
    }
}