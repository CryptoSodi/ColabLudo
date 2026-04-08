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
        private global::Android.Views.View _btnPhantomConnect, _btnWalletTransfer, _btnWalletSwap, _btnSwapPreview;
        private TextView _infoAddressTitle, _infoAddressText, _phantomBtnText, _btnDepositMax, _btnSwapMax;
        private EditText _walletTransferAmount, _swapInputAmount;
        private Spinner _swapInputAsset;
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
            _btnSwapPreview = view.FindViewById<global::Android.Views.View>(Resource.Id.btnSwapPreview);
            _infoAddressTitle = view.FindViewById<TextView>(Resource.Id.infoAddressTitle);
            _infoAddressText = view.FindViewById<TextView>(Resource.Id.infoAddressText);
            _phantomBtnText = view.FindViewById<TextView>(Resource.Id.phantomBtnText);
            _btnDepositMax = view.FindViewById<TextView>(Resource.Id.btnDepositMax);
            _btnSwapMax = view.FindViewById<TextView>(Resource.Id.btnSwapMax);
            _walletTransferAmount = view.FindViewById<EditText>(Resource.Id.walletTransferAmount);
            _swapInputAmount = view.FindViewById<EditText>(Resource.Id.swapInputAmount);
            _swapInputAsset = view.FindViewById<Spinner>(Resource.Id.swapInputAsset);

            // Click Handlers
            _copyBtn.Click += OnCopyButtonClicked;
            _tabQR.Click += (s, e) => SwitchTab(1);
            _tabWallet.Click += (s, e) => SwitchTab(2);
            _tabBank.Click += (s, e) => SwitchTab(3);

            _btnPhantomConnect.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                global::Android.Widget.Toast.MakeText(Context, "Initializing Web3 Connection...", global::Android.Widget.ToastLength.Short).Show();
            };

            _btnDepositMax.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                // TODO: Fetch Phantom LUDC balance and set here
                global::Android.Widget.Toast.MakeText(Context, "Fetching wallet balance...", global::Android.Widget.ToastLength.Short).Show();
            };

            _btnSwapMax.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                // TODO: Fetch selected asset balance and set here
                global::Android.Widget.Toast.MakeText(Context, "Fetching asset balance...", global::Android.Widget.ToastLength.Short).Show();
            };

            _btnSwapPreview.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                global::Android.Widget.Toast.MakeText(Context, "Fetching Swap Preview...", global::Android.Widget.ToastLength.Short).Show();
            };

            _btnWalletSwap.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                global::Android.Widget.Toast.MakeText(Context, "Confirming Swap Transaction...", global::Android.Widget.ToastLength.Short).Show();
            };

            InitializeData();
            InitializeSpinners();

            return view;
        }

        private void InitializeSpinners()
        {
            var assets = new List<string> { "SOL", "USDC" };
            // Use primary HUD layout for the collapsed selection box (consistent with EditText)
            var adapter = new ArrayAdapter<string>(Context, Resource.Layout.spinner_item_hud, assets);
            // Use dropdown-specific layout for the expanded list view
            adapter.SetDropDownViewResource(Resource.Layout.spinner_dropdown_item_hud);
            
            _swapInputAsset.Adapter = adapter;
            _swapInputAsset.SetSelection(0);
        }

        private void SwitchTab(int tabIndex)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");

            _tabQRImg.SetImageResource(tabIndex == 1 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);
            _tabWalletImg.SetImageResource(tabIndex == 2 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);
            _tabBankImg.SetImageResource(tabIndex == 3 ? Resource.Drawable.tab_active : Resource.Drawable.tab_normal);

            _tabQRText.SetTextColor(global::Android.Graphics.Color.White);
            _tabWalletText.SetTextColor(global::Android.Graphics.Color.White);
            _tabBankText.SetTextColor(global::Android.Graphics.Color.White);

            _contentQR.Visibility = tabIndex == 1 ? ViewStates.Visible : ViewStates.Gone;
            _contentWallet.Visibility = tabIndex == 2 ? ViewStates.Visible : ViewStates.Gone;
            _contentBank.Visibility = tabIndex == 3 ? ViewStates.Visible : ViewStates.Gone;

            if (tabIndex == 1)
            {
                _footerSection.Visibility = ViewStates.Visible;
                _copyBtn.Visibility = ViewStates.Visible;
                _btnPhantomConnect.Visibility = ViewStates.Gone;
                _infoAddressTitle.Text = "DEPOSIT LUDC TOKEN";
                _infoAddressText.Text = _walletAddress;
            }
            else if (tabIndex == 2)
            {
                _footerSection.Visibility = ViewStates.Visible;
                _copyBtn.Visibility = ViewStates.Gone;
                _btnPhantomConnect.Visibility = ViewStates.Visible;
                _infoAddressTitle.Text = "CONNECTED WALLET";
                _infoAddressText.Text = string.IsNullOrEmpty(_externalWalletAddress) ? "NOT CONNECTED" : _externalWalletAddress;
            }
            else
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
                    _infoAddressText.Text = wallet.WalletAddress;
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