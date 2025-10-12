using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Constants;
using SharedCode.Constants;

namespace LudoClient.Popups;

public partial class WithdrawPopup : BasePopup
{
    String Address = "";
    String recAddress = "";
    decimal SolBalance=0;
    private System.Timers.Timer _qrCodeTimer;
    public WithdrawPopup()
    {
        InitializeComponent();
        // Update the image source asynchronously (UI thread)
        // Initialize and start the timer
        _qrCodeTimer = new System.Timers.Timer(1000); // 60,000 milliseconds = 60 seconds
        _qrCodeTimer.Elapsed += async (sender, e) => await UpdateBalance();
        _qrCodeTimer.AutoReset = true;
        _qrCodeTimer.Enabled = true;
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
      
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                AmmountEntry.entryField.Text = ClientGlobalConstants.NormalizeCoins(UserInfo.Instance.player.Wallet.AvailableBalance);
            }
            catch (Exception)
            {
            }
        });
        UpdateBalance();
    }
    public async Task UpdateBalance()
    {
        if (GlobalConstants.MatchMaker != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    Coins.Text = ClientGlobalConstants.NormalizeCoins(UserInfo.Instance.player.Wallet.AvailableBalance);
                    SolBalance = (decimal)UserInfo.Instance.player.Wallet?.AvailableBalance;

                }
                catch (Exception)
                {
                }
            });
        }
    }
    private void OnSendButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        string amountInSoltext = AmmountEntry.entryField.Text.Replace("LUDC", "");
        recAddress = AddressEntry.entryField.Text;
        if (decimal.TryParse(amountInSoltext, out decimal amountInSol))
        {
            if (SolBalance < amountInSol)
            {
#if ANDROID
                Toast.Make("Insufficient Balance!", ToastDuration.Short, 22).Show();
            #else
                Application.Current.MainPage.DisplayAlert("Info", "Insufficient Balance!", "OK");
#endif
                return;
            }
            String result = GlobalConstants.MatchMaker.SendSolAsync(recAddress, amountInSol).GetAwaiter().GetResult();
            if (result == "ERROR")
            {
#if ANDROID
                Toast.Make("ERROR SENDING FAILED!", ToastDuration.Short, 22).Show();
#else
                Application.Current.MainPage.DisplayAlert("Info", "ERROR SENDING FAILED!", "OK");
#endif
            }
            else
            {
#if ANDROID
                Toast.Make("Success!", ToastDuration.Short, 22).Show();
#else
                Application.Current.MainPage.DisplayAlert("Info", "Success", "OK");
#endif
            }
        }
        else
        {
#if ANDROID
            Toast.Make("Please enter a valid number.", ToastDuration.Short, 22).Show();
#else
            Application.Current.MainPage.DisplayAlert("Info", "Please enter a valid number.", "OK");
#endif
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