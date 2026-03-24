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

            _copyBtn.Click += OnCopyButtonClicked;

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