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
using System.IO;

namespace LudoClient.Platforms.Android.Popups
{
    public class AddCashDialogFragment : DialogFragment
    {
        private TextView _coinsText;
        private ImageView _qrCodeImage;
        private global::Android.Views.View _copyBtn;
        private string _walletAddress = "";

        // Tabs
        private global::Android.Views.View _tabQR, _tabWallet, _tabBank;
        private ImageView _tabQRImg, _tabWalletImg, _tabBankImg;
        private TextView _tabQRText, _tabWalletText, _tabBankText;

        // Content
        private global::Android.Views.View _contentQR, _contentWallet, _contentBank, _footerSection;

        // Wallet Hub Elements
        private global::Android.Views.View _btnPhantomConnect, _btnWalletTransfer, _btnWalletSwap;
        private TextView _infoAddressTitle, _infoAddressText, _phantomBtnText;
        private EditText _walletTransferAmount;
        private string _externalWalletAddress = "";

        public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Dialog != null && Dialog.Window != null)
            {
                Dialog.Window.SetBackgroundDrawable(new ColorDrawable(global::Android.Graphics.Color.Transparent));
                Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            }

            var view = inflater.Inflate(Resource.Layout.dialog_add_cash, container, false);

            _coinsText = view.FindViewById<TextView>(Resource.Id.coinsText);
            _qrCodeImage = view.FindViewById<ImageView>(Resource.Id.qrCodeImage);
            _copyBtn = view.FindViewById<global::Android.Views.View>(Resource.Id.copyBtn);

            // Tabs
            _tabQR = view.FindViewById<global::Android.Views.View>(Resource.Id.tabQR);
            _tabWallet = view.FindViewById<global::Android.Views.View>(Resource.Id.tabWallet);
            _tabBank = view.FindViewById<global::Android.Views.View>(Resource.Id.tabBank);
            _tabQRImg = view.FindViewById<ImageView>(Resource.Id.tabQRImg);
            _tabWalletImg = view.FindViewById<ImageView>(Resource.Id.tabWalletImg);
            _tabBankImg = view.FindViewById<ImageView>(Resource.Id.tabBankImg);
            
            // Get TextViews safely
            _tabQRText = (TextView)((ViewGroup)_tabQR).GetChildAt(1);
            _tabWalletText = (TextView)((ViewGroup)_tabWallet).GetChildAt(1);
            _tabBankText = (TextView)((ViewGroup)_tabBank).GetChildAt(1);

            // Content
            _contentQR = view.FindViewById<global::Android.Views.View>(Resource.Id.contentQR);
            _contentWallet = view.FindViewById<global::Android.Views.View>(Resource.Id.contentWallet);
            _contentBank = view.FindViewById<global::Android.Views.View>(Resource.Id.contentBank);
            _footerSection = view.FindViewById<global::Android.Views.View>(Resource.Id.footerSection);

            // Wallet Tab Elements
            _btnPhantomConnect = view.FindViewById<global::Android.Views.View>(Resource.Id.btnPhantomConnect);
            _btnWalletTransfer = view.FindViewById<global::Android.Views.View>(Resource.Id.btnWalletTransfer);
            _btnWalletSwap = view.FindViewById<global::Android.Views.View>(Resource.Id.btnWalletSwap);
            _infoAddressTitle = view.FindViewById<TextView>(Resource.Id.infoAddressTitle);
            _infoAddressText = view.FindViewById<TextView>(Resource.Id.infoAddressText);
            _phantomBtnText = view.FindViewById<TextView>(Resource.Id.phantomBtnText);
            _walletTransferAmount = view.FindViewById<EditText>(Resource.Id.walletTransferAmount);

            // Click Handlers
            _copyBtn.Click += OnCopyButtonClicked;
            _tabQR.Click += (s, e) => SwitchTab(1);
            _tabWallet.Click += (s, e) => SwitchTab(2);
            _tabBank.Click += (s, e) => SwitchTab(3);

            _btnPhantomConnect.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                global::Android.Widget.Toast.MakeText(Context, "Initializing Web3 Connection...", ToastLength.Short).Show();
            };

            InitializeData();
            return view;
        }

        private void SwitchTab(int tabIndex)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");

            // Update Images
            _tabQRImg.SetImageResource(tabIndex == 1 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);
            _tabWalletImg.SetImageResource(tabIndex == 2 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);
            _tabBankImg.SetImageResource(tabIndex == 3 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);

            // Update Text Colors (Always White)
            _tabQRText.SetTextColor(global::Android.Graphics.Color.White);
            _tabWalletText.SetTextColor(global::Android.Graphics.Color.White);
            _tabBankText.SetTextColor(global::Android.Graphics.Color.White);

            // Toggle Visibility
            _contentQR.Visibility = tabIndex == 1 ? ViewStates.Visible : ViewStates.Gone;
            _contentWallet.Visibility = tabIndex == 2 ? ViewStates.Visible : ViewStates.Gone;
            _contentBank.Visibility = tabIndex == 3 ? ViewStates.Visible : ViewStates.Gone;

            // Footer Switch Logic
            if (tabIndex == 1) // QR Tab
            {
                _footerSection.Visibility = ViewStates.Visible;
                _copyBtn.Visibility = ViewStates.Visible;
                _btnPhantomConnect.Visibility = ViewStates.Gone;
                _infoAddressTitle.Text = "DEPOSIT LUDC TOKEN";
                _infoAddressText.Text = _walletAddress;
            }
            else if (tabIndex == 2) // Wallet Tab
            {
                _footerSection.Visibility = ViewStates.Visible;
                _copyBtn.Visibility = ViewStates.Gone;
                _btnPhantomConnect.Visibility = ViewStates.Visible;
                _infoAddressTitle.Text = "CONNECTED WALLET";
                _infoAddressText.Text = string.IsNullOrEmpty(_externalWalletAddress) ? "NOT CONNECTED" : _externalWalletAddress;
            }
            else // Bank or other
            {
                _footerSection.Visibility = ViewStates.Gone;
            }
        }

        private void InitializeData()
        {
            if (UserInfo.Instance.player != null)
            {
                var wallet = UserInfo.Instance.player.Wallet;
                if (wallet != null)
                {
                    _walletAddress = wallet.WalletAddress;
                    _coinsText.Text = ClientGlobalConstants.NormalizeCoins(wallet.AvailableBalance);
                    _infoAddressText.Text = wallet.WalletAddress; // Initial footer text

                    wallet.BalanceChanged += OnBalanceChanged;
                }
                GenerateQRCode();
            }
        }

        private void OnBalanceChanged(decimal balance)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                if (_coinsText != null) _coinsText.Text = ClientGlobalConstants.NormalizeCoins(balance);
            });
        }

        private void GenerateQRCode()
        {
            if (!string.IsNullOrEmpty(UserInfo.Instance.AddressQRBlob))
            {
                try
                {
                    byte[] imageBytes = Convert.FromBase64String(UserInfo.Instance.AddressQRBlob);
                    Bitmap bitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                    _qrCodeImage.SetImageBitmap(bitmap);
                }
                catch (Exception) { }
            }
        }

        private void OnCopyButtonClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (!string.IsNullOrEmpty(_walletAddress))
            {
                Clipboard.Default.SetTextAsync(_walletAddress);
                MainThread.BeginInvokeOnMainThread(() => {
                    CommunityToolkit.Maui.Alerts.Toast.Make("Copied to Clipboard", ToastDuration.Short, 22).Show();
                });
            }
        }

        public override void OnDestroyView()
        {
            if (UserInfo.Instance.player != null && UserInfo.Instance.player.Wallet != null)
                UserInfo.Instance.player.Wallet.BalanceChanged -= OnBalanceChanged;
            base.OnDestroyView();
        }
    }
}