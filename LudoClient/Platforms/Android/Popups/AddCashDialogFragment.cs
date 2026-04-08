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
        private TextView _addressText;
        private global::Android.Views.View _copyBtn;
        private string _walletAddress = "";

        // Tabs
        private global::Android.Views.View _tabQR, _tabWallet, _tabBank;
        private ImageView _tabQRImg, _tabWalletImg, _tabBankImg;
        private TextView _tabQRText, _tabWalletText, _tabBankText;

        // Content
        private global::Android.Views.View _contentQR, _contentWallet, _contentBank, _footerSection;

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
            _addressText = view.FindViewById<TextView>(Resource.Id.addressText);
            _copyBtn = view.FindViewById<global::Android.Views.View>(Resource.Id.copyBtn);

            // Find Tabs
            _tabQR = view.FindViewById<global::Android.Views.View>(Resource.Id.tabQR);
            _tabWallet = view.FindViewById<global::Android.Views.View>(Resource.Id.tabWallet);
            _tabBank = view.FindViewById<global::Android.Views.View>(Resource.Id.tabBank);

            _tabQRImg = view.FindViewById<ImageView>(Resource.Id.tabQRImg);
            _tabWalletImg = view.FindViewById<ImageView>(Resource.Id.tabWalletImg);
            _tabBankImg = view.FindViewById<ImageView>(Resource.Id.tabBankImg);

            // Get the TextViews inside FrameLayouts (they don't have IDs, so we get them by position or we could have added IDs)
            // For safety, let's just find them by searching children
            _tabQRText = (TextView)((ViewGroup)_tabQR).GetChildAt(1);
            _tabWalletText = (TextView)((ViewGroup)_tabWallet).GetChildAt(1);
            _tabBankText = (TextView)((ViewGroup)_tabBank).GetChildAt(1);

            // Find Content
            _contentQR = view.FindViewById<global::Android.Views.View>(Resource.Id.contentQR);
            _contentWallet = view.FindViewById<global::Android.Views.View>(Resource.Id.contentWallet);
            _contentBank = view.FindViewById<global::Android.Views.View>(Resource.Id.contentBank);
            _footerSection = view.FindViewById<global::Android.Views.View>(Resource.Id.footerSection);

            // Click Handlers
            _copyBtn.Click += OnCopyButtonClicked;
            _tabQR.Click += (s, e) => SwitchTab(1);
            _tabWallet.Click += (s, e) => SwitchTab(2);
            _tabBank.Click += (s, e) => SwitchTab(3);

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
            _footerSection.Visibility = tabIndex == 1 ? ViewStates.Visible : ViewStates.Gone;
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
                    _addressText.Text = wallet.WalletAddress;

                    wallet.BalanceChanged += OnBalanceChanged;
                }

                GenerateQRCode();
            }
        }

        private void OnBalanceChanged(decimal balance)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_coinsText != null)
                {
                    _coinsText.Text = ClientGlobalConstants.NormalizeCoins(balance);
                }
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
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating QR Code: {ex.Message}");
                }
            }
        }

        private void OnCopyButtonClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            
            if (!string.IsNullOrEmpty(_walletAddress))
            {
                Clipboard.Default.SetTextAsync(_walletAddress);
                
                // Show native Toast or MAUI Toast
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CommunityToolkit.Maui.Alerts.Toast.Make("Copied to Clipboard", ToastDuration.Short, 22).Show();
                });
            }
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