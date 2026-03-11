using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Constants;
using SharedCode.Constants;

namespace LudoClient.Popups;

public partial class AddCash : BasePopup
{
    String Address = "";
    public AddCash()
    {
        InitializeComponent();
        Loaded += AddCash_Loaded;
        Unloaded += AddCash_Unloaded;
        GenerateQRCodeAsync();
    }
    private void AddCash_Loaded(object sender, EventArgs e)
    {
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

    private void AddCash_Unloaded(object sender, EventArgs e)
    {
        if (UserInfo.Instance.player != null)
        {
            var wallet = UserInfo.Instance.player?.Wallet;

            // if (wallet != null)
            //  wallet.BalanceChanged -= Wallet_BalanceChanged;
        }
    }
    private void Wallet_BalanceChanged(decimal balance)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Coins.Text = ClientGlobalConstants.NormalizeCoins(balance);
        });
    }
    public void GenerateQRCodeAsync()
    {
        // Update the image source asynchronously (UI thread)
        MainThread.BeginInvokeOnMainThread(() =>
        { 
            var wallet = UserInfo.Instance.player.Wallet;
            if (wallet != null)
            {
                Address = wallet.WalletAddress;
                Coins.Text = ClientGlobalConstants.NormalizeCoins(wallet.AvailableBalance);
                AddressText.Text = wallet.WalletAddress;
            }
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