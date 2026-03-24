using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using LudoClient.Constants;
using Microsoft.Maui.ApplicationModel;
using SharedCode.Constants;
using System;
using System.Threading.Tasks;

namespace LudoClient.Platforms.Android.Popups
{
    public class WithdrawDialogFragment : DialogFragment
    {
        private TextView _coinsText;
        private EditText _addressEntry;
        private EditText _amountEntry;
        private global::Android.Views.View _pasteBtn;
        private global::Android.Views.View _sendBtn;
        
        private string _userWalletAddress = "";
        private decimal _solBalance = 0;

        public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Dialog != null && Dialog.Window != null)
            {
                Dialog.Window.SetBackgroundDrawable(new ColorDrawable(global::Android.Graphics.Color.Transparent));
                Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            }

            var view = inflater.Inflate(Resource.Layout.dialog_withdraw, container, false);

            _coinsText = view.FindViewById<TextView>(Resource.Id.coinsText);
            _addressEntry = view.FindViewById<EditText>(Resource.Id.addressEntry);
            _amountEntry = view.FindViewById<EditText>(Resource.Id.amountEntry);
            _pasteBtn = view.FindViewById<global::Android.Views.View>(Resource.Id.pasteBtn);
            _sendBtn = view.FindViewById<global::Android.Views.View>(Resource.Id.sendBtn);

            _pasteBtn.Click += OnPasteButtonClicked;
            _sendBtn.Click += OnSendButtonClicked;

            InitializeData();

            return view;
        }

        private void InitializeData()
        {
            if (UserInfo.Instance.player != null)
            {
                var wallet = UserInfo.Instance.player.Wallet;
                if (wallet != null)
                {
                    if (wallet.WalletAddress != null)
                        _userWalletAddress = wallet.WalletAddress;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _amountEntry.Text = ClientGlobalConstants.NormalizeCoins(wallet.AvailableBalance);
                        _coinsText.Text = ClientGlobalConstants.NormalizeCoins(wallet.AvailableBalance);
                        _solBalance = (decimal)wallet.AvailableBalance;
                    });

                    wallet.BalanceChanged += OnBalanceChanged;
                }
            }
        }

        private void OnBalanceChanged(decimal balance)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_coinsText != null)
                {
                    _coinsText.Text = ClientGlobalConstants.NormalizeCoins(balance);
                    _solBalance = balance;
                }
            });
        }

        private async void OnSendButtonClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");

            string amountText = _amountEntry.Text?.Replace("LUDC", "").Trim();
            string recAddress = _addressEntry.Text?.Trim();

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

            if (_solBalance < amount)
            {
                await ShowMessage("Insufficient Balance!");
                return;
            }

            try
            {
                if (GlobalConstants.MatchMaker != null)
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
            }
            catch (Exception ex)
            {
                await ShowMessage("Unexpected error occurred.");
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void OnPasteButtonClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            
            // Replicating MAUI logic: Clipboard.Default.SetTextAsync(_userWalletAddress) 
            // Wait, the original code had: Clipboard.Default.SetTextAsync(Address);
            // which sets the user's own address TO the clipboard, then reads it back?
            // Actually, the original OnPasteButtonClicked was:
            /*
            Clipboard.Default.SetTextAsync(Address);
            if (Clipboard.Default.HasText)
            {
                string? clipboardText = Clipboard.Default.GetTextAsync().GetAwaiter().GetResult();
                recAddress = clipboardText ?? string.Empty;
                AddressEntry.entryField.Text = recAddress;
            }
            */
            // This seems to be a shortcut to paste the user's OWN wallet address if it's in the clipboard.
            
            if (Clipboard.Default.HasText)
            {
                string clipboardText = await Clipboard.Default.GetTextAsync();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    _addressEntry.Text = clipboardText;
                }
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CommunityToolkit.Maui.Alerts.Toast.Make("Empty Clipboard!", ToastDuration.Short, 22).Show();
                });
            }
        }

        private async Task ShowMessage(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CommunityToolkit.Maui.Alerts.Toast.Make(message, ToastDuration.Short, 22).Show();
            });
            await Task.CompletedTask;
        }

        public override void OnDestroyView()
        {
            if (UserInfo.Instance.player != null && UserInfo.Instance.player.Wallet != null)
            {
                UserInfo.Instance.player.Wallet.BalanceChanged -= OnBalanceChanged;
            }
            base.OnDestroyView();
        }
    }
}