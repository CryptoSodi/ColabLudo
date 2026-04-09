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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LudoClient.Platforms.Android.Popups
{
    public class WithdrawDialogFragment : global::AndroidX.Fragment.App.DialogFragment
    {
        private TextView _coinsText;
        private EditText _addressEntry;
        private EditText _amountEntry;
        private TextView _footerTitle, _footerText, _btnActionText;
        private ImageView _btnActionBg;
        private global::Android.Views.View _btnMainAction;

        // Tabs
        private global::Android.Views.View _tabSOL, _tabWallet, _tabBank;
        private ImageView _tabSOLImg, _tabWalletImg, _tabBankImg;
        private TextView _tabSOLText, _tabWalletText, _tabBankText;

        // Content
        private global::Android.Views.View _contentSOL, _contentWallet, _contentBank;

        // Solana Tab Helpers
        private global::Android.Views.View _btnPaste;
        private TextView _btnWithdrawMax;

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

            // Find Common UI
            _coinsText = view.FindViewById<TextView>(Resource.Id.coinsText);
            _addressEntry = view.FindViewById<EditText>(Resource.Id.addressEntry);
            _amountEntry = view.FindViewById<EditText>(Resource.Id.amountEntry);
            
            // Find Tabs
            _tabSOL = view.FindViewById<global::Android.Views.View>(Resource.Id.tabSOL);
            _tabWallet = view.FindViewById<global::Android.Views.View>(Resource.Id.tabWallet);
            _tabBank = view.FindViewById<global::Android.Views.View>(Resource.Id.tabBank);
            _tabSOLImg = view.FindViewById<ImageView>(Resource.Id.tabSOLImg);
            _tabWalletImg = view.FindViewById<ImageView>(Resource.Id.tabWalletImg);
            _tabBankImg = view.FindViewById<ImageView>(Resource.Id.tabBankImg);
            _tabSOLText = (TextView)((ViewGroup)_tabSOL).GetChildAt(1);
            _tabWalletText = (TextView)((ViewGroup)_tabWallet).GetChildAt(1);
            _tabBankText = (TextView)((ViewGroup)_tabBank).GetChildAt(1);

            // Find Content
            _contentSOL = view.FindViewById<global::Android.Views.View>(Resource.Id.contentSOL);
            _contentWallet = view.FindViewById<global::Android.Views.View>(Resource.Id.contentWallet);
            _contentBank = view.FindViewById<global::Android.Views.View>(Resource.Id.contentBank);

            // Solana Tab Elements
            _btnPaste = view.FindViewById<global::Android.Views.View>(Resource.Id.btnPaste);
            _btnWithdrawMax = view.FindViewById<TextView>(Resource.Id.btnWithdrawMax);

            // Find Footer
            _footerTitle = view.FindViewById<TextView>(Resource.Id.footerTitle);
            _footerText = view.FindViewById<TextView>(Resource.Id.footerText);
            _btnActionText = view.FindViewById<TextView>(Resource.Id.btnActionText);
            _btnActionBg = view.FindViewById<ImageView>(Resource.Id.btnActionBg);
            _btnMainAction = view.FindViewById<global::Android.Views.View>(Resource.Id.btnMainAction);

            // Click Handlers
            _btnMainAction.Click += OnMainActionButtonClicked;
            _tabSOL.Click += (s, e) => SwitchTab(1);
            _tabWallet.Click += (s, e) => SwitchTab(2);
            _tabBank.Click += (s, e) => SwitchTab(3);

            _btnPaste.Click += OnPasteButtonClicked;
            _btnWithdrawMax.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _amountEntry.Text = _solBalance.ToString("F2");
            };

            InitializeData();
            return view;
        }

        private void SwitchTab(int tabIndex)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");

            _tabSOLImg.SetImageResource(tabIndex == 1 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);
            _tabWalletImg.SetImageResource(tabIndex == 2 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);
            _tabBankImg.SetImageResource(tabIndex == 3 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);

            _tabSOLText.SetTextColor(global::Android.Graphics.Color.White);
            _tabWalletText.SetTextColor(global::Android.Graphics.Color.White);
            _tabBankText.SetTextColor(global::Android.Graphics.Color.White);

            _contentSOL.Visibility = tabIndex == 1 ? ViewStates.Visible : ViewStates.Gone;
            _contentWallet.Visibility = tabIndex == 2 ? ViewStates.Visible : ViewStates.Gone;
            _contentBank.Visibility = tabIndex == 3 ? ViewStates.Visible : ViewStates.Gone;

            if (tabIndex == 1)
            {
                _footerTitle.Text = "WITHDRAW LUDC TOKEN";
                _footerText.Text = "EXTERNAL SOLANA ADDRESS";
                _btnActionText.Text = "SEND";
                _btnActionBg.SetImageResource(Resource.Drawable.btn_orange);
            }
            else if (tabIndex == 2)
            {
                _footerTitle.Text = "PHANTOM WITHDRAWAL";
                _footerText.Text = "INTERNAL TO EXTERNAL HUB";
                _btnActionText.Text = "CONNECT";
                _btnActionBg.SetImageResource(Resource.Drawable.btn_verify_account);
            }
            else
            {
                _footerTitle.Text = "BANK PAYOUT HUB";
                _footerText.Text = "REQUEST MANUAL TRANSFER";
                _btnActionText.Text = "SUBMIT";
                _btnActionBg.SetImageResource(Resource.Drawable.btn_big);
            }
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
                        _amountEntry.Text = "";
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

        private async void OnMainActionButtonClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (_contentSOL.Visibility == ViewStates.Visible) await ProcessSolanaWithdraw();
            else global::Android.Widget.Toast.MakeText(Context, "Action coming soon...", global::Android.Widget.ToastLength.Short).Show();
        }

        private async Task ProcessSolanaWithdraw()
        {
            string amountText = _amountEntry.Text?.Replace("LUDC", "").Trim();
            string recAddress = _addressEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(amountText) || !decimal.TryParse(amountText, out decimal amount)) {
                ShowMessage("Please enter a valid number."); return;
            }
            if (string.IsNullOrWhiteSpace(recAddress)) {
                ShowMessage("Please enter a valid address."); return;
            }
            if (recAddress.Length < 32 || recAddress.Length > 44) {
                ShowMessage("Invalid wallet address."); return;
            }
            if (_solBalance < amount) {
                ShowMessage("Insufficient Balance!"); return;
            }

            try
            {
                if (GlobalConstants.MatchMaker != null)
                {
                    string result = await GlobalConstants.MatchMaker.Withdraw(recAddress, amount);
                    if (result == "ERROR") ShowMessage("Error sending transaction!");
                    else ShowMessage("Transaction Successful!");
                }
            }
            catch (Exception) { ShowMessage("Unexpected error occurred."); }
        }

        private async void OnPasteButtonClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (Clipboard.Default.HasText)
            {
                string clipboardText = await Clipboard.Default.GetTextAsync();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    _addressEntry.Text = clipboardText.Trim();
                }
            }
            else
            {
                ShowMessage("Empty Clipboard!");
            }
        }

        private void ShowMessage(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                global::Android.Widget.Toast.MakeText(Context, message, global::Android.Widget.ToastLength.Short).Show();
            });
        }

        public override void OnDestroyView()
        {
            if (UserInfo.Instance.player != null && UserInfo.Instance.player.Wallet != null)
                UserInfo.Instance.player.Wallet.BalanceChanged -= OnBalanceChanged;
            base.OnDestroyView();
        }
    }
}