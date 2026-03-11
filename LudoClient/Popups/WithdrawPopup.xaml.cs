using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Constants;
using SharedCode.Constants;
namespace LudoClient.Popups;
public partial class WithdrawPopup : BasePopup
{
    String Address { get; set; } = "";
    String recAddress = "";
    decimal SolBalance = 0;
    public WithdrawPopup()
    {
        InitializeComponent();
        Loaded += WithdrawPopup_Loaded;
        Unloaded += WithdrawPopup_Unloaded;
    }
    private void WithdrawPopup_Loaded(object sender, EventArgs e)
    {
        var wallet = UserInfo.Instance.player.Wallet;
        if (wallet != null)
        {
            if (wallet.WalletAddress != null)
                Address = wallet.WalletAddress;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    AmmountEntry.entryField.Text = ClientGlobalConstants.NormalizeCoins(wallet.AvailableBalance);
                }
                catch (Exception)
                {
                }
            });

            OnBalanceChanged((decimal)wallet.AvailableBalance);
            wallet.BalanceChanged += OnBalanceChanged;
        }
    }
    private void WithdrawPopup_Unloaded(object sender, EventArgs e)
    {
        var wallet = UserInfo.Instance.player.Wallet;

       // if (wallet != null)
         //   wallet.BalanceChanged -= OnBalanceChanged;
    }
    void OnBalanceChanged(decimal balance)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (GlobalConstants.MatchMaker != null)
                try
                {
                    Coins.Text = ClientGlobalConstants.NormalizeCoins(balance);
                    SolBalance = balance;
                }
                catch (Exception)
                {
                }
        });
    }
    private async void OnSendButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");

        string amountText = AmmountEntry.entryField.Text?.Replace("LUDC", "").Trim();
        string recAddress = AddressEntry.entryField.Text?.Trim();

        if (string.IsNullOrWhiteSpace(amountText) || !decimal.TryParse(amountText, out decimal amount))
        {
            await ShowMessage("Please enter a valid number.");
            return;
        }

        if (string.IsNullOrWhiteSpace(recAddress))
        {
            await ShowMessage("Please enter a valid address.");
            return;
        }

        if (recAddress.Length < 32 || recAddress.Length > 44)
        {
            await ShowMessage("Invalid wallet address.");
            return;
        }

        if (SolBalance < amount)
        {
            await ShowMessage("Insufficient Balance!");
            return;
        }

        try
        {
            string result = await GlobalConstants.MatchMaker.Withdraw(recAddress, amount);

            if (result == "ERROR")
            {
                await ShowMessage("Error sending transaction!");
            }
            else
            {
                await ShowMessage("Transaction Successful!");
            }
        }
        catch (Exception ex)
        {
            await ShowMessage("Unexpected error occurred.");
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
    private async Task ShowMessage(string message)
    {
#if ANDROID
        Toast.Make(message, ToastDuration.Short, 22).Show();
#else
    await Application.Current.MainPage.DisplayAlert("Info", message, "OK");
#endif
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