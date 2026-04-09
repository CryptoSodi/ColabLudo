using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Provider;
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
using System.Threading.Tasks;

namespace LudoClient.Platforms.Android.Popups
{
    public class AddCashDialogFragment : global::AndroidX.Fragment.App.DialogFragment
    {
        private TextView _coinsText, _infoAddressTitle, _infoAddressText, _phantomBtnText, _receiptStatus, _selectedAccountNumber;
        private ImageView _qrCodeImage, _receiptPreview, _tabQRImg, _tabWalletImg, _tabBankImg;
        private TextView _tabQRText, _tabWalletText, _tabBankText;
        private EditText _walletTransferAmount, _swapInputAmount, _manualAmount;
        private Spinner _swapInputAsset, _manualMethod;
        private global::Android.Views.View _copyBtn, _tabQR, _tabWallet, _tabBank, _contentQR, _contentWallet, _contentBank, _footerSection;
        private global::Android.Views.View _btnPhantomConnect, _btnWalletTransfer, _btnWalletSwap, _btnSwapPreview, _btnPickImage, _btnBankCopy, _btnBankView, _btnSubmitManual;
        private TextView _btnDepositMax, _btnSwapMax;

        private string _walletAddress = "";
        private string _selectedBase64Image = "";
        private decimal _currentBalance = 0;
        private const int PickImageRequest = 1001;

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

            // Content Containers
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

            // Bank Tab Elements
            _manualAmount = view.FindViewById<EditText>(Resource.Id.manualAmount);
            _manualMethod = view.FindViewById<Spinner>(Resource.Id.manualMethod);
            _btnPickImage = view.FindViewById<global::Android.Views.View>(Resource.Id.btnPickImage);
            _receiptPreview = view.FindViewById<ImageView>(Resource.Id.receiptPreview);
            _receiptStatus = view.FindViewById<TextView>(Resource.Id.receiptStatus);
            _selectedAccountNumber = view.FindViewById<TextView>(Resource.Id.selectedAccountNumber);
            _btnBankCopy = view.FindViewById<global::Android.Views.View>(Resource.Id.btnBankCopy);
            _btnBankView = view.FindViewById<global::Android.Views.View>(Resource.Id.btnBankView);
            _btnSubmitManual = view.FindViewById<global::Android.Views.View>(Resource.Id.btnSubmitManual);

            // Global Tab Click Handlers
            _tabQR.Click += (s, e) => SwitchTab(1);
            _tabWallet.Click += (s, e) => SwitchTab(2);
            _tabBank.Click += (s, e) => SwitchTab(3);

            // 1. QR Tab Action
            _copyBtn.Click += OnCopyButtonClicked;

            // 2. Wallet Tab Actions (FIXED: Wiring missing handlers)
            _btnDepositMax.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _walletTransferAmount.Text = "100.00"; // Example Max
            };
            _btnWalletTransfer.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                global::Android.Widget.Toast.MakeText(Context, "Initiating Deposit...", global::Android.Widget.ToastLength.Short).Show();
            };
            _btnSwapMax.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _swapInputAmount.Text = "50.00"; // Example Max
            };
            _btnSwapPreview.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                global::Android.Widget.Toast.MakeText(Context, "Calculating Swap...", global::Android.Widget.ToastLength.Short).Show();
            };
            _btnWalletSwap.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                global::Android.Widget.Toast.MakeText(Context, "Confirming Swap...", global::Android.Widget.ToastLength.Short).Show();
            };
            _btnPhantomConnect.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                global::Android.Widget.Toast.MakeText(Context, "Connecting...", global::Android.Widget.ToastLength.Short).Show();
            };

            // 3. Bank Tab Actions
            _btnPickImage.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                Intent intent = new Intent(Intent.ActionPick, MediaStore.Images.Media.ExternalContentUri);
                StartActivityForResult(intent, PickImageRequest);
            };
            _btnBankCopy.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                string acc = _selectedAccountNumber.Text;
                if (!string.IsNullOrEmpty(acc) && acc != "SELECT METHOD") {
                    Clipboard.Default.SetTextAsync(acc);
                    global::Android.Widget.Toast.MakeText(Context, "Account Copied!", global::Android.Widget.ToastLength.Short).Show();
                }
            };
            _btnBankView.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                if (!string.IsNullOrEmpty(_manualAmount.Text)) {
                    global::Android.Widget.Toast.MakeText(Context, $"Previewing {_manualAmount.Text} LUDC deposit.", global::Android.Widget.ToastLength.Short).Show();
                } else ShowMessage("Enter an amount first.");
            };
            _btnSubmitManual.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                if (string.IsNullOrEmpty(_manualAmount.Text) || string.IsNullOrEmpty(_selectedBase64Image)) ShowMessage("Fill amount and pick image.");
                else ShowMessage("Submitting Receipt...");
            };

            _manualMethod.ItemSelected += (s, e) => UpdateContextualAccountInfo();

            InitializeData();
            InitializeSpinners();
            return view;
        }

        private void UpdateContextualAccountInfo()
        {
            string method = _manualMethod.SelectedItem?.ToString();
            switch (method) {
                case "JazzCash": _selectedAccountNumber.Text = "0345-XXXXXXX"; break;
                case "PayTM": _selectedAccountNumber.Text = "+91 XXXXX XXXXX"; break;
                case "EasyPaisa": _selectedAccountNumber.Text = "0300-XXXXXXX"; break;
                default: _selectedAccountNumber.Text = "SELECT METHOD"; break;
            }
        }

        public override void OnActivityResult(int requestCode, int resultCode, global::Android.Content.Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == PickImageRequest && resultCode == (int)global::Android.App.Result.Ok && data != null) {
                var uri = data.Data;
                try {
                    using (var stream = Context.ContentResolver.OpenInputStream(uri)) {
                        var bitmap = BitmapFactory.DecodeStream(stream);
                        _receiptPreview.SetImageBitmap(bitmap);
                        _receiptStatus.Text = "IMAGE ATTACHED SUCCESSFUL";
                        _receiptStatus.SetTextColor(global::Android.Graphics.Color.White);
                        using (var ms = new MemoryStream()) {
                            bitmap.Compress(Bitmap.CompressFormat.Jpeg, 70, ms);
                            _selectedBase64Image = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                } catch (Exception) { ShowMessage("Failed to load image."); }
            }
        }

        private void InitializeSpinners()
        {
            var assets = new List<string> { "SOL", "USDC" };
            var swapAdapter = new ArrayAdapter<string>(Context, Resource.Layout.spinner_item_hud, assets);
            swapAdapter.SetDropDownViewResource(Resource.Layout.spinner_dropdown_item_hud);
            _swapInputAsset.Adapter = swapAdapter;
            _swapInputAsset.SetSelection(0);

            var methods = new List<string> { "JazzCash", "PayTM", "EasyPaisa" };
            var methodAdapter = new ArrayAdapter<string>(Context, Resource.Layout.spinner_item_hud, methods);
            methodAdapter.SetDropDownViewResource(Resource.Layout.spinner_dropdown_item_hud);
            _manualMethod.Adapter = methodAdapter;
            _manualMethod.SetSelection(0);
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

            if (tabIndex == 1) {
                _footerSection.Visibility = ViewStates.Visible;
                _copyBtn.Visibility = ViewStates.Visible;
                _btnPhantomConnect.Visibility = ViewStates.Gone;
                _btnSubmitManual.Visibility = ViewStates.Gone;
                _infoAddressTitle.Text = "DEPOSIT LUDC TOKEN";
                _infoAddressText.Text = _walletAddress;
            } else if (tabIndex == 2) {
                _footerSection.Visibility = ViewStates.Visible;
                _copyBtn.Visibility = ViewStates.Gone;
                _btnPhantomConnect.Visibility = ViewStates.Visible;
                _btnSubmitManual.Visibility = ViewStates.Gone;
                _infoAddressTitle.Text = "PHANTOM DEPOSIT HUB";
                _infoAddressText.Text = "CONNECT WALLET TO START";
            } else {
                _footerSection.Visibility = ViewStates.Visible;
                _copyBtn.Visibility = ViewStates.Gone;
                _btnPhantomConnect.Visibility = ViewStates.Gone;
                _btnSubmitManual.Visibility = ViewStates.Visible;
                _infoAddressTitle.Text = "MANUAL DEPOSIT PORTAL";
                _infoAddressText.Text = "ATTACH PROOF BEFORE SUBMITTING";
            }
        }

        private void InitializeData()
        {
            if (UserInfo.Instance.player != null) {
                var wallet = UserInfo.Instance.player.Wallet;
                if (wallet != null) {
                    if (wallet.WalletAddress != null) _walletAddress = wallet.WalletAddress;
                    
                    MainThread.BeginInvokeOnMainThread(() => {
                        _coinsText.Text = ClientGlobalConstants.NormalizeCoins(wallet.AvailableBalance);
                        _currentBalance = (decimal)wallet.AvailableBalance;

                        // Restore QR Code Logic
                        if (!string.IsNullOrEmpty(UserInfo.Instance.AddressQRBlob))
                        {
                            try
                            {
                                byte[] imageBytes = Convert.FromBase64String(UserInfo.Instance.AddressQRBlob);
                                var bitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                                _qrCodeImage.SetImageBitmap(bitmap);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"QR Error: {ex.Message}");
                            }
                        }
                    });
                }
            }
        }

        private void OnCopyButtonClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (!string.IsNullOrEmpty(_walletAddress)) {
                Clipboard.Default.SetTextAsync(_walletAddress);
                ShowMessage("Wallet Address Copied!");
            }
        }

        private void ShowMessage(string message) {
            MainThread.BeginInvokeOnMainThread(() => {
                global::Android.Widget.Toast.MakeText(Context, message, global::Android.Widget.ToastLength.Short).Show();
            });
        }
    }
}