using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using LudoClient.Constants;
using SharedCode.Constants;

namespace LudoClient.Platforms.Android.Popups
{
    public class AddCashDialogFragment : global::AndroidX.Fragment.App.DialogFragment
    {
        private TextView _coinsText, _infoAddressTitle, _infoAddressText, _phantomBtnText, _receiptStatus, _selectedAccountNumber;
        private ImageView _qrCodeImage, _receiptPreview, _tabQRImg, _tabWalletImg, _tabBankImg, _phantomBtnBg;
        private TextView _tabQRText, _tabWalletText, _tabBankText;
        private EditText _walletTransferAmount, _swapInputAmount, _manualAmount;
        private Spinner _swapInputAsset, _manualMethod;
        private global::Android.Views.View _copyBtn, _tabQR, _tabWallet, _tabBank, _contentQR, _contentWallet, _contentBank, _footerSection;
        private global::Android.Views.View _btnPhantomConnect, _btnWalletTransfer, _btnWalletSwap, _btnSwapPreview, _btnPickImage, _btnBankCopy, _btnBankView, _btnSubmitManual;
        private TextView _btnDepositMax, _btnSwapMax;

        private string _walletAddress = "";
        private decimal _phantomLudcBalance = 0;
        private string _selectedBase64Image = "";
        private decimal _currentBalance = 0;
        private bool _isWalletConnected = false;
        private bool _isWalletConnecting = false;
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
            _phantomBtnBg = view.FindViewById<ImageView>(Resource.Id.infoAddressBg); // Defaulting to this, but we'll use btn_verify_account and btn_pink
            // Actually, we need to find the ImageView INSIDE btnPhantomConnect
            _phantomBtnBg = (ImageView)((ViewGroup)_btnPhantomConnect).GetChildAt(0);
            
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

            // 2. Wallet Tab Actions
            _btnDepositMax.Click += (s, e) => OnMaxButtonClicked();
            _btnWalletTransfer.Click += (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            };
            _btnSwapMax.Click += (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _swapInputAmount.Text = _phantomLudcBalance.ToString("F2"); 
            };
            _btnSwapPreview.Click += (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            };
            _btnWalletSwap.Click += (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            };
            _btnPhantomConnect.Click += async (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await ToggleWalletConnection();
            };

            _btnWalletTransfer.Click += async (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await ProcessWalletDeposit();
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
                    ShowMessage("Account Copied!");
                }
            };
            _btnBankView.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                if (!string.IsNullOrEmpty(_manualAmount.Text)) ShowMessage($"Previewing {_manualAmount.Text} LUDC deposit.");
                else ShowMessage("Enter an amount first.");
            };
            _btnSubmitManual.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                if (string.IsNullOrEmpty(_manualAmount.Text) || string.IsNullOrEmpty(_selectedBase64Image)) ShowMessage("Fill amount and pick image.");
                else ShowMessage("Submitting Receipt...");
            };

            _manualMethod.ItemSelected += (s, e) => UpdateContextualAccountInfo();

            InitializeData();
            InitializeSpinners();
            UpdateWalletUIState(); 
            SwitchTab(1); 
            return view;
        }

        private async Task ToggleWalletConnection()
        {
            if (_isWalletConnecting)
                return;

            if (!_isWalletConnected)
            {
                SetWalletConnectingState(true);
                void HandleRemoteClosed()
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!_isWalletConnected)
                        {
                            _walletAddress = "";
                            _phantomBtnBg.SetImageResource(Resource.Drawable.btn_verify_account);
                            SetWalletConnectingState(false);
                            UpdateWalletUIState();
                        }
                    });
                }
                ClientGlobalConstants.WalletConnection.RemoteClosed += HandleRemoteClosed;

                try
                {
                    await Task.Yield();
                    bool success = await ClientGlobalConstants.WalletConnection.Connect();
                    if (success && ClientGlobalConstants.WalletConnection.Client != null)
                    {
                        var authTask = ClientGlobalConstants.WalletConnection.Client.Authorize(
                            new Uri("https://ludocities.com"),
                            new Uri("faviconhq.ico", UriKind.Relative),
                            "Ludo Cities",
                            "mainnet-beta"
                        );
                        try
                        {
                            var auth = await authTask.WaitAsync(TimeSpan.FromSeconds(12));

                            if (auth != null && auth.Accounts.Count > 0)
                            {
                                _isWalletConnected = true;
                                _walletAddress = auth.Accounts[0].DisplayAddress; // 🔥 USE BASE58 ADDRESS
                                _phantomBtnText.Text = "DISCONNECT";
                                _phantomBtnBg.SetImageResource(Resource.Drawable.btn_pink); // RED State
                                _ = LoadWalletBalance(); // 🔥 FETCH BALANCE IMMEDIATELY
                            }
                            else
                            {
                                ShowMessage("Authorization failed.");
                            }
                        }
                        catch (TimeoutException)
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (ClientGlobalConstants.WalletConnection.LastLaunchCanceled)
                        {
                            _isWalletConnected = false;
                            _walletAddress = "";
                            _phantomBtnBg.SetImageResource(Resource.Drawable.btn_verify_account);
                            SetWalletConnectingState(false);
                            UpdateWalletUIState();
                            ShowMessage("Wallet was closed before connecting.");
                        }
                        else
                            ShowMessage("Failed to connect wallet.");
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Auth Error: " + ex.Message);
                }
                finally
                {
                    ClientGlobalConstants.WalletConnection.RemoteClosed -= HandleRemoteClosed;
                    await ClientGlobalConstants.WalletConnection.DisconnectAsync();
                    SetWalletConnectingState(false);
                }
            }
            else
            {
                _isWalletConnected = false;
                _phantomBtnText.Text = "CONNECT";
                _phantomBtnBg.SetImageResource(Resource.Drawable.btn_verify_account); // GREEN State
            }

            UpdateWalletUIState();
        }

        private async Task LoadWalletBalance()
        {
            if (!_isWalletConnected || string.IsNullOrEmpty(_walletAddress))
                return;

            try
            {
                var result = await GlobalConstants.MatchMaker.GetWalletBalance(_walletAddress);
                if (result != null)
                {
                    string json = result.ToString();
                    Console.WriteLine($"[AddCash] Received Balance JSON: {json}");
                    
                    var data = Newtonsoft.Json.Linq.JObject.Parse(json);
                    
                    // Handle both PascalCase and camelCase
                    var successToken = data["Success"] ?? data["success"];
                    var balanceToken = data["PhantomLudc"] ?? data["phantomLudc"];

                    if (successToken != null && (bool)successToken == true)
                    {
                        _phantomLudcBalance = (decimal?)balanceToken ?? 0m;
                        Console.WriteLine($"[AddCash] Balance set to: {_phantomLudcBalance}");
                        
                        MainThread.BeginInvokeOnMainThread(() => {
                            if (_infoAddressText != null)
                                _infoAddressText.Text = $"BALANCE: {_phantomLudcBalance:N2} LUDC";
                        });
                    }
                    else
                    {
                        Console.WriteLine("[AddCash] Success was false or token missing in JSON.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddCash] Error loading wallet balance: {ex}");
            }
        }

        private void OnMaxButtonClicked()
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (!_isWalletConnected) return;
            
            Console.WriteLine($"[AddCash] Max Clicked. Current Balance: {_phantomLudcBalance}");
            _walletTransferAmount.Text = _phantomLudcBalance.ToString("F2");
        }

        private void SetWalletConnectingState(bool isConnecting)
        {
            _isWalletConnecting = isConnecting;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_phantomBtnText != null)
                    _phantomBtnText.Text = isConnecting ? "CONNECTING..." : (_isWalletConnected ? "DISCONNECT" : "CONNECT");

                if (_btnPhantomConnect != null)
                    _btnPhantomConnect.Enabled = !isConnecting;
            });
        }

        private async Task ProcessWalletDeposit()
        {
            try
            {
                string amountText = _walletTransferAmount.Text?.Trim();
                if (string.IsNullOrEmpty(amountText) || !decimal.TryParse(amountText, out decimal amount))
                {
                    ShowMessage("Invalid amount.");
                    return;
                }

                // Note: The DashboardClient was removed. 
                // We should integrate with LudoHub if there are deposit methods there.
            }
            catch (Exception ex)
            {
                ShowMessage($"Deposit failed: {ex.Message}");
            }
        }

        private void UpdateWalletUIState()
        {
            float alpha = _isWalletConnected ? 1.0f : 0.4f;
            if (_isWalletConnecting)
                alpha = 1.0f;
            bool enabled = _isWalletConnected;

            _btnWalletTransfer.Enabled = enabled; _btnWalletTransfer.Alpha = alpha;
            _btnWalletSwap.Enabled = enabled; _btnWalletSwap.Alpha = alpha;
            _btnSwapPreview.Enabled = enabled; _btnSwapPreview.Alpha = alpha;
            _btnDepositMax.Enabled = enabled; _btnDepositMax.Alpha = alpha;
            _btnSwapMax.Enabled = enabled; _btnSwapMax.Alpha = alpha;
            _walletTransferAmount.Enabled = enabled;
            _swapInputAmount.Enabled = enabled;
            _swapInputAsset.Enabled = enabled;

            // Update footer info if we are on the WALLET tab
            if (_contentWallet.Visibility == ViewStates.Visible) {
                _infoAddressText.Text = _isWalletConnecting ? "CONNECTING TO WALLET..." : (_isWalletConnected ? "WALLET READY FOR TRANSACTIONS" : "CONNECT WALLET TO START");
            }
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
                if (_isWalletConnected) {
                    _ = LoadWalletBalance();
                } else {
                    _infoAddressText.Text = "CONNECT WALLET TO START";
                }
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

                        if (!string.IsNullOrEmpty(UserInfo.Instance.AddressQRBlob)) {
                            try {
                                byte[] imageBytes = Convert.FromBase64String(UserInfo.Instance.AddressQRBlob);
                                var bitmap = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
                                _qrCodeImage.SetImageBitmap(bitmap);
                            } catch (Exception) { }
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
