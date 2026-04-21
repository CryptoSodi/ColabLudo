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
                    AmountEntry.entryField.Text = ""; // Start empty for security
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

        if (wallet != null)
            wallet.BalanceChanged -= OnBalanceChanged;
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

    private void TabRequestedActivate(object sender, EventArgs e)
    {
        if (sender is ControlView.ImageSwitch activeTab)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");

            // Set active states for all 3 tabs using SwitchState
            Tab1.SwitchState = (Tab1 != activeTab);
            Tab2.SwitchState = (Tab2 != activeTab);
            Tab3.SwitchState = (Tab3 != activeTab);

            // Force visual update
            Tab1.UpdateSwitchSource();
            Tab2.UpdateSwitchSource();
            Tab3.UpdateSwitchSource();

            // Toggle Content Visibility
            ContentOnChain.IsVisible = (activeTab == Tab1);
            ContentWallet.IsVisible = (activeTab == Tab2);
            ContentBank.IsVisible = (activeTab == Tab3);

            // Update Dynamic Footer
            if (activeTab == Tab1)
            {
                FooterTitle.Text = "WITHDRAW LUDC TOKEN";
                FooterText.Text = "EXTERNAL SOLANA ADDRESS";
                BtnActionText.Text = "SEND";
            }
            else if (activeTab == Tab2)
            {
                FooterTitle.Text = "PHANTOM WITHDRAWAL";
                FooterText.Text = "INTERNAL TO EXTERNAL HUB";
                BtnActionText.Text = "CONNECT";
            }
            else
            {
                FooterTitle.Text = "BANK PAYOUT HUB";
                FooterText.Text = "REQUEST MANUAL TRANSFER";
                BtnActionText.Text = "SUBMIT";
            }
        }
    }

    private async void OnSendButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");

        string amountText = AmountEntry.entryField.Text?.Replace("LUDC", "").Trim();
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
            string result = await GlobalConstants.MatchMaker.InitiateWithdrawal(recAddress, amount);

            if (result == "ERROR" || result.StartsWith("Error"))
            {
                await ShowMessage("Error: " + result);
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
    private async void OnPasteButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        
        if (Clipboard.Default.HasText)
        {
            string clipboardText = await Clipboard.Default.GetTextAsync();
            if (!string.IsNullOrEmpty(clipboardText))
            {
                AddressEntry.entryField.Text = clipboardText.Trim();
            }
        }
        else
        {
            Toast.Make("Empty Clipboard!", ToastDuration.Short, 22).Show();
        }
    }

    private void OnMaxButtonClicked(object sender, TappedEventArgs e)
    {
        ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
        AmountEntry.entryField.Text = SolBalance.ToString("F2");
    }
}