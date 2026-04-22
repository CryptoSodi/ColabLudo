using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using LudoClient.Constants;
using LudoClient.SolanaWallet;
using Newtonsoft.Json.Linq;
using SharedCode;
using SharedCode.Constants;
using Solnet.Programs;
using Solnet.Rpc.Builders;
using Solnet.Wallet;

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

        private string _walletAddress = ""; // This is the PLAYER'S deposit address
        private string _connectedWalletAddress = ""; // This is the EXTERNAL connected wallet
        private decimal _phantomLudcBalance = 0;
        private decimal _phantomSolBalance = 0;
        private decimal _phantomUsdcBalance = 0;
        private string _selectedBase64Image = "";
        private string _preparedSwapTx = "";
        private string _preparedSwapRequestId = "";
        private decimal _preparedSwapOutput = 0;
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

            _btnDepositMax.Click += (s, e) => OnMaxButtonClicked();
            _btnSwapMax.Click += (s, e) => OnSwapMaxButtonClicked();
            
            _btnSwapPreview.Click += async (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await OnSwapPreviewClicked();
            };
            
            _btnWalletSwap.Click += async (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await ProcessWalletSwap();
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

            _swapInputAsset.ItemSelected += (s, e) => UpdateSwapBalanceDisplay();

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
            SwitchTab(2, false); 
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
                            _connectedWalletAddress = "";
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
                    var auth = await ClientGlobalConstants.WalletConnection.AuthorizeOrReauthorize();

                    if (auth != null && auth.Accounts.Count > 0)
                    {
                        _isWalletConnected = true;
                        _connectedWalletAddress = ClientGlobalConstants.WalletConnection.MainAddressBase58;
                        _phantomBtnText.Text = "DISCONNECT";
                        _phantomBtnBg.SetImageResource(Resource.Drawable.btn_pink); // RED State
                        _ = LoadAllBalances(); // 🔥 FETCH ALL BALANCES IMMEDIATELY
                    }
                    else
                    {
                        ShowMessage("Authorization failed.");
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage("Auth Error: " + ex.Message);
                }
                finally
                {
                    ClientGlobalConstants.WalletConnection.RemoteClosed -= HandleRemoteClosed;
                    ClientGlobalConstants.WalletConnection.DisconnectAsync(false);
                    SetWalletConnectingState(false);
                }
            }
            else
            {
                _isWalletConnected = false;
                _connectedWalletAddress = "";
                await ClientGlobalConstants.WalletConnection.DisconnectAsync(true); 
                _phantomBtnText.Text = "CONNECT";
                _phantomBtnBg.SetImageResource(Resource.Drawable.btn_verify_account); // GREEN State
            }

            UpdateWalletUIState();
        }

        private async Task LoadAllBalances()
        {
            // 🔥 RE-SYNC ADDR IF EMPTY
            if (string.IsNullOrEmpty(_connectedWalletAddress)) {
                _connectedWalletAddress = ClientGlobalConstants.WalletConnection.MainAddressBase58 ?? "";
            }

            if (!_isWalletConnected || string.IsNullOrEmpty(_connectedWalletAddress))
                return;

            try
            {
                var result = await GlobalConstants.MatchMaker.GetSwapBalances(_connectedWalletAddress);
                if (result != null)
                {
                    string json = result.ToString();
                    Console.WriteLine($"[AddCash] Received All Balances JSON: {json}");

                    var data = Newtonsoft.Json.Linq.JObject.Parse(json);

                    if ((bool?)data["success"] == true || (bool?)data["Success"] == true)
                    {
                        _phantomSolBalance = (decimal?)(data["phantomSol"] ?? data["PhantomSol"]) ?? 0m;
                        _phantomUsdcBalance = (decimal?)(data["phantomUsdc"] ?? data["PhantomUsdc"]) ?? 0m;
                        _phantomLudcBalance = (decimal?)(data["phantomLudc"] ?? data["PhantomLudc"]) ?? 0m;
                    }
                }
                
                // Fallback: If MatchMaker returned 0 or failed, try refreshing local service balances
                if (_phantomLudcBalance == 0)
                {
                    await ClientGlobalConstants.WalletConnection.RefreshBalances();
                    _phantomSolBalance = (decimal)ClientGlobalConstants.WalletConnection.SolBalance;

                    // Try Mainnet then Devnet mints
                    var ludc = ClientGlobalConstants.WalletConnection.TokenBalances.FirstOrDefault(t => t.Mint == SolanaTokenService.LUDC_MINT_MAINNET)
                            ?? ClientGlobalConstants.WalletConnection.TokenBalances.FirstOrDefault(t => t.Mint == SolanaTokenService.LUDC_MINT_DEVNET);
                    if (ludc != null) _phantomLudcBalance = ludc.Amount;
                    
                    var usdc = ClientGlobalConstants.WalletConnection.TokenBalances.FirstOrDefault(t => t.Mint == SolanaTokenService.USDC_MINT_MAINNET)
                            ?? ClientGlobalConstants.WalletConnection.TokenBalances.FirstOrDefault(t => t.Mint == SolanaTokenService.USDC_MINT_DEVNET);
                    if (usdc != null) _phantomUsdcBalance = usdc.Amount;
                }

                UpdateSwapBalanceDisplay();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddCash] Error loading all balances: {ex}");
            }
        }


        private void UpdateSwapBalanceDisplay()
        {
            MainThread.BeginInvokeOnMainThread(() => {
                if (_contentWallet.Visibility == ViewStates.Visible)
                {
                    _infoAddressText.Text = $"SOL: {_phantomSolBalance:N4} | LUDC: {_phantomLudcBalance:N2} | USDC: {_phantomUsdcBalance:N6}";
                }
            });
        }

        private void OnMaxButtonClicked()
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (!_isWalletConnected) return;
            
            // If the user clicked MAX for the deposit amount
            _walletTransferAmount.Text = _phantomLudcBalance.ToString("F2");
        }

        private void OnSwapMaxButtonClicked()
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (!_isWalletConnected) return;

            string selectedAsset = _swapInputAsset.SelectedItem?.ToString() ?? "SOL";
            decimal bal = selectedAsset == "SOL" ? _phantomSolBalance : _phantomUsdcBalance;

            if (selectedAsset == "SOL")
            {
                // Leave 0.01 SOL for transaction fees/rent
                bal -= 0.01m;
                if (bal < 0) bal = 0;
                _swapInputAmount.Text = bal.ToString("F4");
            }
            else
            {
                _swapInputAmount.Text = bal.ToString("F6");
            }
        }

        private async Task OnSwapPreviewClicked()
        {
            if (!_isWalletConnected || string.IsNullOrEmpty(_connectedWalletAddress))
            {
                ShowMessage("Connect wallet first.");
                return;
            }

            string amountText = _swapInputAmount.Text?.Trim();
            if (string.IsNullOrEmpty(amountText) || !decimal.TryParse(amountText, out decimal amount) || amount <= 0)
            {
                ShowMessage("Enter a valid amount.");
                return;
            }

            string inputAsset = _swapInputAsset.SelectedItem?.ToString() ?? "SOL";
            string outputAsset = "LUDC";

            try
            {
                ShowMessage($"Calculating swap: {amount} {inputAsset}...");
                var result = await GlobalConstants.MatchMaker.PrepareAssetSwap(_connectedWalletAddress, inputAsset, outputAsset, amount, 100);
                
                if (result != null)
                {
                    string json = result.ToString();
                    var data = Newtonsoft.Json.Linq.JObject.Parse(json);

                    if ((bool?)data["success"] == true || (bool?)data["Success"] == true)
                    {
                        _preparedSwapTx = (string)(data["swapTransaction"] ?? data["SwapTransaction"] ?? data["swap_transaction"]);
                        _preparedSwapRequestId = (string)(data["requestId"] ?? data["RequestId"] ?? data["request_id"]);
                        _preparedSwapOutput = (decimal?)(data["outAmount"] ?? data["OutAmount"] ?? data["out_amount"]) ?? 0m;
                        
                        // If it's a raw ulong from Jupiter, we need to scale it by LUDC decimals (9)
                        if (_preparedSwapOutput > 1000000) 
                            _preparedSwapOutput /= 1_000_000_000m;

                        ShowMessage($"PREVIEW: Receive approx {_preparedSwapOutput:N2} LUDC. Ready to Swap.");
                        Console.WriteLine($"[Swap] Prepared Quote: ID={_preparedSwapRequestId}, TxLen={_preparedSwapTx?.Length}");
                    }
                    else
                    {
                        string err = (string)(data["error"] ?? data["Error"]) ?? "Failed to get swap quote.";
                        ShowMessage($"Swap Error: {err}");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Preview failed: {ex.Message}");
            }
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
                if (string.IsNullOrEmpty(amountText) || !decimal.TryParse(amountText, out decimal amount) || amount <= 0)
                {
                    ShowMessage("Invalid amount.");
                    return;
                }

                if (!_isWalletConnected || string.IsNullOrEmpty(_connectedWalletAddress))
                {
                    ShowMessage("Connect wallet first.");
                    return;
                }

                if (amount > _phantomLudcBalance)
                {
                    ShowMessage($"Insufficient LUDC. Balance: {_phantomLudcBalance}");
                    return;
                }

                ShowMessage("Preparing LUDC Deposit...");

                // 1. Prepare Deposit (Fetch blueprint and auto-initialize internal wallet if needed)
                var prepResult = await GlobalConstants.MatchMaker.PrepareLudcDeposit(_connectedWalletAddress, amount);
                if (prepResult == null)
                {
                    ShowMessage("Server failed to prepare deposit.");
                    return;
                }

                var prep = Newtonsoft.Json.Linq.JObject.Parse(prepResult.ToString());
                T GetVal<T>(string key) => prep.Value<T>(key) ?? prep.Value<T>(char.ToLower(key[0]) + key.Substring(1));

                string blockhash = GetVal<string>("Blockhash");
                string senderAta = GetVal<string>("SenderAta");
                string destinationAta = GetVal<string>("DestinationAta");
                string mint = GetVal<string>("Mint");
                int decimals = GetVal<int>("Decimals") == 0 ? 9 : GetVal<int>("Decimals");
                string amountRawStr = GetVal<string>("AmountRaw");
                string destinationOwner = GetVal<string>("DestinationOwner");

                if (string.IsNullOrEmpty(blockhash) || string.IsNullOrEmpty(senderAta) || 
                    string.IsNullOrEmpty(destinationAta) || string.IsNullOrEmpty(mint) || 
                    string.IsNullOrEmpty(amountRawStr) || string.IsNullOrEmpty(destinationOwner))
                {
                    ShowMessage("Incomplete deposit data received from server.");
                    return;
                }

                // 2. Build Transaction locally
                var feePayer = new Solnet.Wallet.PublicKey(_connectedWalletAddress);
                var mintPub = new Solnet.Wallet.PublicKey(mint);
                var senderAtaPub = new Solnet.Wallet.PublicKey(senderAta);
                var destAtaPub = new Solnet.Wallet.PublicKey(destinationAta);


                var txBuilder = new Solnet.Rpc.Builders.TransactionBuilder()
                    .SetRecentBlockHash(blockhash)
                    .SetFeePayer(feePayer)
                    .AddInstruction(SolanaTokenService.CreateTransferCheckedInstruction(
                        senderAtaPub, mintPub, destAtaPub, feePayer, ulong.Parse(amountRawStr), (byte)decimals
                    ));

                ShowMessage("Awaiting Wallet Signature...");
                
                // 3. Sign via Wallet (MWA) - does not broadcast locally
                string signedTxBase64 = await ClientGlobalConstants.WalletConnection.SignTransaction(txBuilder);

                if (!string.IsNullOrEmpty(signedTxBase64))
                {
                    ShowMessage("Broadcasting via Server...");
                    
                    // 4. Broadcast via Server (Server handles the "Unable to parse json" and translates errors)
                    BlockchainResult result = await GlobalConstants.MatchMaker.BroadcastTransaction(signedTxBase64);

                    if (result != null)
                    {
                        if (result.Success && !string.IsNullOrEmpty(result.Signature))
                        {
                            ShowMessage($"Success! Sig: {result.Signature.Substring(0, 8)}...");
                            await Task.Delay(2000);
                            _ = LoadAllBalances();
                        }
                        else
                        {
                            // Display the user-friendly translated error (e.g. "Insufficient SOL for Rent")
                            string userError = result.Error ?? "Broadcast failed.";
                            ShowMessage(userError);
                            Console.WriteLine($"[AddCash] Deposit Failed: {userError}");
                        }
                    }
                    else
                    {
                        ShowMessage("Server failed to respond to broadcast.");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Error: {ex.Message}");
            }
        }

        private async Task ProcessWalletSwap()
        {
            try
            {
                if (string.IsNullOrEmpty(_preparedSwapTx) || string.IsNullOrEmpty(_preparedSwapRequestId))
                {
                    ShowMessage("Get a quote first.");
                    return;
                }

                if (!_isWalletConnected) return;

                ShowMessage("Awaiting Swap Signature...");
                var txBytes = Convert.FromBase64String(_preparedSwapTx);
                
                // Use the robust SignRawTransaction helper which handles authorization and disconnection
                string signedTxBase64 = await ClientGlobalConstants.WalletConnection.SignRawTransaction(txBytes);

                if (!string.IsNullOrEmpty(signedTxBase64))
                {
                    ShowMessage("Broadcasting Swap...");
                    
                    BlockchainResult result = await GlobalConstants.MatchMaker.ExecutePreparedSwap(_preparedSwapRequestId, signedTxBase64);
                    if (result != null)
                    {
                        if (result.Success)
                        {
                            string signature = result.Signature;
                            string sigDisplay = !string.IsNullOrEmpty(signature) 
                                ? signature.Substring(0, Math.Min(8, signature.Length)) 
                                : "Unknown";
                            ShowMessage("Swap Sent! Signature: " + sigDisplay + "...");
                            _preparedSwapTx = ""; 
                            _preparedSwapRequestId = "";
                            _ = LoadAllBalances(); 
                        }
                        else
                        {
                            string err = result.Error ?? "Swap execution failed.";
                            ShowMessage("Swap Failed: " + err);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Swap Error: " + ex.Message);
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
            if (_contentWallet != null && _contentWallet.Visibility == ViewStates.Visible) {
                if (_isWalletConnecting)
                    _infoAddressText.Text = "CONNECTING TO WALLET...";
                else if (_isWalletConnected)
                    UpdateSwapBalanceDisplay(); // 🔥 Show balances instead of generic "READY" message
                else
                    _infoAddressText.Text = "CONNECT WALLET TO START";
            }
        }

        private void OnBalanceChanged(decimal balance)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_coinsText != null)
                {
                    _coinsText.Text = ClientGlobalConstants.NormalizeCoins(balance);
                    _currentBalance = balance;
                }
            });
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

        private void SwitchTab(int tabIndex, bool playsound = true)
        {
            if(playsound)
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
                UpdateWalletUIState(); // 🔥 Update UI state immediately (shows "CONNECT" or "BALANCES")
                if (_isWalletConnected) {
                    _ = LoadAllBalances();
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

                    wallet.BalanceChanged += OnBalanceChanged;
                }
            }
        }

        public override void OnDestroyView()
        {
            if (UserInfo.Instance.player != null && UserInfo.Instance.player.Wallet != null)
                UserInfo.Instance.player.Wallet.BalanceChanged -= OnBalanceChanged;
            base.OnDestroyView();
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
