using Android.App;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using LudoClient.Constants;
using LudoClient.SolanaWallet;
using Newtonsoft.Json.Linq;
using SharedCode;
using SharedCode.Constants;

namespace LudoClient.Platforms.Android.Popups
{
    public class WithdrawDialogFragment : global::AndroidX.Fragment.App.DialogFragment
    {
        private TextView _coinsText;
        private EditText _addressEntry, _amountEntry, _walletAmountEntry, _walletSwapAmount, _bankWithdrawAmount, _bankAccountEntry;
        private TextView _footerTitle, _footerText, _phantomBtnText, _walletSwapTitle;
        private ImageView _phantomBtnBg;
        
        // Dynamic Footer Buttons
        private global::Android.Views.View _btnSolanaConfirm, _btnPhantomConnect, _btnSubmitManual;

        // Tabs
        private global::Android.Views.View _tabSOL, _tabWallet, _tabBank;
        private ImageView _tabSOLImg, _tabWalletImg, _tabBankImg;
        private TextView _tabSOLText, _tabWalletText, _tabBankText;

        // Content
        private global::Android.Views.View _contentSOL, _contentWallet, _contentBank;

        // Tab Helpers
        private global::Android.Views.View _btnPaste, _btnWalletSign, _btnWalletSwapView, _btnWalletSwapConfirm, _btnBankWithdrawView, _btnBankPaste;
        private TextView _btnWithdrawMax, _btnWalletMax, _btnSwapMax, _btnBankWithdrawMax;
        private Spinner _manualWithdrawMethod, _walletSwapOutputSpinner;

        private string _userWalletAddress = "";
        private decimal _solBalance = 0;
        private decimal _phantomSolBalance = 0;
        private decimal _phantomLudcBalance = 0;
        private decimal _phantomUsdcBalance = 0;
        private string _preparedSwapTx = "";
        private string _preparedSwapRequestId = "";
        private decimal _preparedSwapOutput = 0;
        private string _preparedSwapRouter = "";
        private string _selectedSwapOutputAsset = "USDC";
        private bool _isWalletConnected = false;
        private bool _isWalletConnecting = false;

        public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (Dialog != null && Dialog.Window != null)
            {
                Dialog.Window.SetBackgroundDrawable(new ColorDrawable(global::Android.Graphics.Color.Transparent));
                Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            }

            var view = inflater.Inflate(Resource.Layout.dialog_withdraw, container, false);
            
            // ... (rest of FindViewById calls unchanged) ...
            // ... (rest of the FindViewByID calls) ...

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

            // Tab Helper Elements
            _btnPaste = view.FindViewById<global::Android.Views.View>(Resource.Id.btnPaste);
            _btnWithdrawMax = view.FindViewById<TextView>(Resource.Id.btnWithdrawMax);
            
            // Wallet Tab Hub
            _walletAmountEntry = view.FindViewById<EditText>(Resource.Id.walletAmountEntry);
            _btnWalletMax = view.FindViewById<TextView>(Resource.Id.btnWalletMax);
            _btnWalletSign = view.FindViewById<global::Android.Views.View>(Resource.Id.btnWalletSign);
            _walletSwapAmount = view.FindViewById<EditText>(Resource.Id.walletSwapAmount);
            _walletSwapTitle = view.FindViewById<TextView>(Resource.Id.walletSwapTitle);
            _walletSwapOutputSpinner = view.FindViewById<Spinner>(Resource.Id.walletSwapOutputSpinner);
            _btnSwapMax = view.FindViewById<TextView>(Resource.Id.btnSwapMax);
            _btnWalletSwapView = view.FindViewById<global::Android.Views.View>(Resource.Id.btnWalletSwapView);
            _btnWalletSwapConfirm = view.FindViewById<global::Android.Views.View>(Resource.Id.btnWalletSwapConfirm);

            // Bank Tab Hub
            _manualWithdrawMethod = view.FindViewById<Spinner>(Resource.Id.manualWithdrawMethodSpinner);
            _bankWithdrawAmount = view.FindViewById<EditText>(Resource.Id.bankWithdrawAmount);
            _btnBankWithdrawMax = view.FindViewById<TextView>(Resource.Id.btnBankWithdrawMax);
            _btnBankWithdrawView = view.FindViewById<global::Android.Views.View>(Resource.Id.btnBankWithdrawView);
            _bankAccountEntry = view.FindViewById<EditText>(Resource.Id.bankAccountEntry);
            _btnBankPaste = view.FindViewById<global::Android.Views.View>(Resource.Id.btnBankPaste);

            // Find Footer Buttons
            _footerTitle = view.FindViewById<TextView>(Resource.Id.footerTitle);
            _footerText = view.FindViewById<TextView>(Resource.Id.footerText);
            _btnSolanaConfirm = view.FindViewById<global::Android.Views.View>(Resource.Id.btnSolanaConfirm);
            _btnPhantomConnect = view.FindViewById<global::Android.Views.View>(Resource.Id.btnPhantomConnect);
            _phantomBtnBg = (ImageView)((ViewGroup)_btnPhantomConnect).GetChildAt(0);
            _phantomBtnText = view.FindViewById<TextView>(Resource.Id.phantomBtnText);
            _btnSubmitManual = view.FindViewById<global::Android.Views.View>(Resource.Id.btnSubmitManual);

            // Global Click Handlers
            _tabSOL.Click += (s, e) => SwitchTab(1);
            _tabWallet.Click += (s, e) => SwitchTab(2);
            _tabBank.Click += (s, e) => SwitchTab(3);

            // Solana Tab Logic
            _btnPaste.Click += OnPasteButtonClicked;
            _btnWithdrawMax.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _amountEntry.Text = _solBalance.ToString("F2");
            };
            _btnSolanaConfirm.Click += async (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await ProcessSolanaWithdraw();
            };

            // Wallet Tab Logic
            _btnWalletMax.Click += (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _walletAmountEntry.Text = _solBalance.ToString("F2");
            };
            _btnSwapMax.Click += (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _walletSwapAmount.Text = _phantomLudcBalance.ToString("F2");
            };
            _btnWalletSign.Click += async (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await ProcessWalletWithdraw();
            };
            _btnWalletSwapView.Click += (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _ = PreviewWalletSwap();
            };
            _btnWalletSwapConfirm.Click += async (s, e) => {
                if (!_isWalletConnected) return;
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await ProcessWalletSwap();
            };
            _btnPhantomConnect.Click += async (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                await ToggleWalletConnection();
            };

            // Bank Tab Logic
            _btnBankWithdrawMax.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                _bankWithdrawAmount.Text = _solBalance.ToString("F2");
            };
            _btnBankWithdrawView.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                if (!string.IsNullOrEmpty(_bankWithdrawAmount.Text)) ShowMessage($"Previewing {_bankWithdrawAmount.Text} LUDC payout...");
                else ShowMessage("Enter an amount first.");
            };
            _btnBankPaste.Click += OnBankPasteButtonClicked;
            _btnSubmitManual.Click += (s, e) => {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
                if (string.IsNullOrEmpty(_bankWithdrawAmount.Text) || string.IsNullOrEmpty(_bankAccountEntry.Text)) {
                    ShowMessage("Fill all bank details.");
                } else {
                    ShowMessage("Submitting Manual Payout Request...");
                }
            };

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
                            _userWalletAddress = "";
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
                        _userWalletAddress = ClientGlobalConstants.WalletConnection.MainAddressBase58;
                        _phantomBtnText.Text = "DISCONNECT";
                        _phantomBtnBg.SetImageResource(Resource.Drawable.btn_pink);
                        _ = LoadWalletBalances();
                        ShowMessage("Wallet Connected Successfully!");
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
                _userWalletAddress = "";
                _phantomSolBalance = 0;
                _phantomLudcBalance = 0;
                _phantomUsdcBalance = 0;
                _preparedSwapTx = "";
                _preparedSwapRequestId = "";
                _preparedSwapOutput = 0;
                await ClientGlobalConstants.WalletConnection.DisconnectAsync(true);
                _phantomBtnText.Text = "CONNECT";
                _phantomBtnBg.SetImageResource(Resource.Drawable.btn_verify_account);
            }

            UpdateWalletUIState();
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

        private void UpdateWalletUIState()
        {
            float alpha = _isWalletConnected ? 1.0f : 0.4f;
            if (_isWalletConnecting)
                alpha = 1.0f;
            bool enabled = _isWalletConnected;

            _btnWalletSign.Enabled = enabled; _btnWalletSign.Alpha = alpha;
            _btnWalletSwapView.Enabled = enabled; _btnWalletSwapView.Alpha = alpha;
            _btnWalletSwapConfirm.Enabled = enabled; _btnWalletSwapConfirm.Alpha = alpha;
            _btnWalletMax.Enabled = enabled; _btnWalletMax.Alpha = alpha;
            _btnSwapMax.Enabled = enabled; _btnSwapMax.Alpha = alpha;
            _walletAmountEntry.Enabled = enabled;
            _walletSwapAmount.Enabled = enabled;
            if (_walletSwapOutputSpinner != null)
            {
                _walletSwapOutputSpinner.Enabled = enabled;
                _walletSwapOutputSpinner.Alpha = alpha;
            }

            if (_contentWallet != null && _contentWallet.Visibility == ViewStates.Visible) {
                _footerText.Text = _isWalletConnecting ? "CONNECTING TO WALLET..." : (_isWalletConnected ? GetWalletBalanceSummary() : "CONNECT WALLET TO START");
            }
        }

        private void InitializeSpinners()
        {
            var payoutMethods = new List<string> { "Select Payout Method", "JazzCash", "PayTM", "EasyPaisa", "Bank Account", "Binance Pay" };
            var adapter = new ArrayAdapter<string>(Context, Resource.Layout.spinner_item_hud, payoutMethods);
            adapter.SetDropDownViewResource(Resource.Layout.spinner_dropdown_item_hud);
            _manualWithdrawMethod.Adapter = adapter;

            var swapOutputs = new List<string> { "USDC", "SOL" };
            var swapOutputAdapter = new ArrayAdapter<string>(Context, Resource.Layout.spinner_item_hud, swapOutputs);
            swapOutputAdapter.SetDropDownViewResource(Resource.Layout.spinner_dropdown_item_hud);
            _walletSwapOutputSpinner.Adapter = swapOutputAdapter;
            _walletSwapOutputSpinner.ItemSelected += (s, e) =>
            {
                _selectedSwapOutputAsset = swapOutputs[e.Position];
                ResetSwapPreview();
                UpdateSwapTitle();
            };
            UpdateSwapTitle();
        }

        private void SwitchTab(int tabIndex, bool playsound = true)
        {
            if (playsound)
            {
                ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            }
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
                _btnSolanaConfirm.Visibility = ViewStates.Visible;
                _btnPhantomConnect.Visibility = ViewStates.Gone;
                _btnSubmitManual.Visibility = ViewStates.Gone;
            }
            else if (tabIndex == 2)
            {
                _footerTitle.Text = "PHANTOM WITHDRAWAL";
                _footerText.Text = _isWalletConnected ? "WALLET READY FOR PAYOUTS" : "CONNECT WALLET TO START";
                _btnSolanaConfirm.Visibility = ViewStates.Gone;
                _btnPhantomConnect.Visibility = ViewStates.Visible;
                _btnSubmitManual.Visibility = ViewStates.Gone;
            }
            else
            {
                _footerTitle.Text = "BANK PAYOUT HUB";
                _footerText.Text = "REQUEST MANUAL TRANSFER";
                _btnSolanaConfirm.Visibility = ViewStates.Gone;
                _btnPhantomConnect.Visibility = ViewStates.Gone;
                _btnSubmitManual.Visibility = ViewStates.Visible;
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
                        _walletAmountEntry.Text = "";
                        _walletSwapAmount.Text = "";
                        _bankWithdrawAmount.Text = "";
                        _bankAccountEntry.Text = "";
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

        private async Task ProcessWalletWithdraw()
        {
            if (!_isWalletConnected) {
                ShowMessage("Connect Phantom first."); return;
            }

            string amountText = _walletAmountEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(amountText) || !decimal.TryParse(amountText, out decimal amount)) {
                ShowMessage("Enter a valid amount."); return;
            }
            if (_solBalance < amount) {
                ShowMessage("Insufficient internal balance."); return;
            }

            try
            {
                ShowMessage("Processing Payout...");
                // Call renamed InitiateWithdrawal on LudoHub
                string result = await GlobalConstants.MatchMaker.InitiateWithdrawal(_userWalletAddress, amount);
                
                if (result.StartsWith("Success")) {
                    ShowMessage("Withdrawal successful!");
                    _walletAmountEntry.Text = "";
                } else {
                    ShowMessage("Payout failed: " + result);
                }
            }
            catch (Exception ex) { ShowMessage("Hub Error: " + ex.Message); }
        }

        private async Task LoadWalletBalances()
        {
            if (string.IsNullOrEmpty(_userWalletAddress))
                _userWalletAddress = ClientGlobalConstants.WalletConnection.MainAddressBase58 ?? "";

            if (!_isWalletConnected || string.IsNullOrEmpty(_userWalletAddress))
                return;

            try
            {
                var result = await GlobalConstants.MatchMaker.GetSwapBalances(_userWalletAddress);
                if (result != null)
                {
                    var data = JObject.Parse(result.ToString());
                    if ((bool?)data["success"] == true || (bool?)data["Success"] == true)
                    {
                        _phantomSolBalance = (decimal?)(data["phantomSol"] ?? data["PhantomSol"]) ?? 0m;
                        _phantomLudcBalance = (decimal?)(data["phantomLudc"] ?? data["PhantomLudc"]) ?? 0m;
                        _phantomUsdcBalance = (decimal?)(data["phantomUsdc"] ?? data["PhantomUsdc"]) ?? 0m;
                        Console.WriteLine($"[Withdraw] Server swap balances. PhantomSol={_phantomSolBalance}, PhantomLudc={_phantomLudcBalance}, PhantomUsdc={_phantomUsdcBalance}");
                    }
                    else
                    {
                        Console.WriteLine($"[Withdraw] Server swap balances failed: {result}");
                    }
                }

                if (_phantomSolBalance == 0 || _phantomLudcBalance == 0 || _phantomUsdcBalance == 0)
                {
                    await ClientGlobalConstants.WalletConnection.RefreshBalances();
                    _phantomSolBalance = (decimal)ClientGlobalConstants.WalletConnection.SolBalance;
                    var ludc = ClientGlobalConstants.WalletConnection.TokenBalances.FirstOrDefault(t => t.Mint == SolanaTokenService.LUDC_MINT_MAINNET)
                            ?? ClientGlobalConstants.WalletConnection.TokenBalances.FirstOrDefault(t => t.Mint == SolanaTokenService.LUDC_MINT_DEVNET);
                    if (ludc != null) _phantomLudcBalance = ludc.Amount;

                    var usdc = ClientGlobalConstants.WalletConnection.TokenBalances.FirstOrDefault(t => t.Mint == SolanaTokenService.USDC_MINT_MAINNET)
                            ?? ClientGlobalConstants.WalletConnection.TokenBalances.FirstOrDefault(t => t.Mint == SolanaTokenService.USDC_MINT_DEVNET);
                    if (usdc != null) _phantomUsdcBalance = usdc.Amount;
                    Console.WriteLine($"[Withdraw] Local wallet balances. PhantomSol={_phantomSolBalance}, PhantomLudc={_phantomLudcBalance}, PhantomUsdc={_phantomUsdcBalance}");
                }

                MainThread.BeginInvokeOnMainThread(UpdateWalletUIState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Withdraw] Error loading wallet balances: {ex.Message}");
            }
        }

        private async Task PreviewWalletSwap()
        {
            if (!_isWalletConnected || string.IsNullOrEmpty(_userWalletAddress))
            {
                ShowMessage("Connect wallet first.");
                return;
            }

            string amountText = _walletSwapAmount.Text?.Trim();
            if (string.IsNullOrEmpty(amountText) || !decimal.TryParse(amountText, out decimal amount) || amount <= 0)
            {
                ShowMessage("Enter a valid amount.");
                return;
            }

            if (amount > _phantomLudcBalance)
            {
                ShowMessage($"Insufficient Phantom LUDC. Balance: {_phantomLudcBalance:N2}");
                return;
            }

            try
            {
                ShowMessage($"Calculating swap: {amount} LUDC to {_selectedSwapOutputAsset}...");
                var result = await GlobalConstants.MatchMaker.PrepareAssetSwap(_userWalletAddress, "LUDC", _selectedSwapOutputAsset, amount, 100);
                if (result == null)
                {
                    ResetSwapPreview();
                    ShowMessage("Failed to get swap quote.");
                    return;
                }

                var data = JObject.Parse(result.ToString());
                if ((bool?)data["success"] == true || (bool?)data["Success"] == true)
                {
                    _preparedSwapTx = (string)(data["transaction"] ?? data["Transaction"] ?? data["swapTransaction"] ?? data["SwapTransaction"] ?? data["swap_transaction"]);
                    _preparedSwapRequestId = (string)(data["requestId"] ?? data["RequestId"] ?? data["request_id"]);
                    _preparedSwapRouter = (string)(data["router"] ?? data["Router"]) ?? "";

                    decimal rawOutAmount = (decimal?)(data["outputAmountRaw"] ?? data["OutputAmountRaw"] ?? data["outAmountRaw"] ?? data["OutAmountRaw"]) ?? 0m;
                    if (rawOutAmount > 0)
                    {
                        _preparedSwapOutput = rawOutAmount / GetSwapOutputScale(_selectedSwapOutputAsset);
                    }
                    else
                    {
                        _preparedSwapOutput = (decimal?)(data["outAmount"] ?? data["OutAmount"] ?? data["outputAmount"] ?? data["OutputAmount"] ?? data["out_amount"]) ?? 0m;
                    }

                    Console.WriteLine($"[Withdraw] Prepared wallet swap. RequestId={_preparedSwapRequestId}, Output={_selectedSwapOutputAsset}, Router={_preparedSwapRouter}, EstimatedOutput={_preparedSwapOutput}");
                    string format = GetSwapOutputFormat(_selectedSwapOutputAsset);
                    ShowMessage(!string.IsNullOrWhiteSpace(_preparedSwapRouter)
                        ? $"Preview: receive about {_preparedSwapOutput.ToString(format)} {_selectedSwapOutputAsset} via {_preparedSwapRouter}."
                        : $"Preview: receive about {_preparedSwapOutput.ToString(format)} {_selectedSwapOutputAsset}.");
                }
                else
                {
                    ResetSwapPreview();
                    string err = (string)(data["error"] ?? data["Error"]) ?? "Failed to get swap quote.";
                    ShowMessage($"Swap Error: {err}");
                }
            }
            catch (Exception ex)
            {
                ResetSwapPreview();
                ShowMessage($"Preview failed: {ex.Message}");
            }
        }

        private async Task ProcessWalletSwap()
        {
            if (!_isWalletConnected || string.IsNullOrEmpty(_userWalletAddress))
            {
                ShowMessage("Connect wallet first.");
                return;
            }

            if (string.IsNullOrEmpty(_preparedSwapTx) || string.IsNullOrEmpty(_preparedSwapRequestId))
            {
                ShowMessage("Get a quote first.");
                return;
            }

            try
            {
                ShowMessage("Awaiting wallet signature...");
                var txBytes = Convert.FromBase64String(_preparedSwapTx);
                string signedTxBase64 = await ClientGlobalConstants.WalletConnection.SignRawTransaction(txBytes);

                if (string.IsNullOrEmpty(signedTxBase64))
                {
                    ShowMessage("Swap signature was declined.");
                    return;
                }

                ShowMessage("Broadcasting swap...");
                BlockchainResult result = await GlobalConstants.MatchMaker.ExecutePreparedSwap(_preparedSwapRequestId, signedTxBase64);
                if (result != null && result.Success)
                {
                    string sigDisplay = !string.IsNullOrEmpty(result.Signature)
                        ? result.Signature.Substring(0, Math.Min(8, result.Signature.Length))
                        : "unknown";
                    ShowMessage($"Swap sent: {sigDisplay}...");
                    _walletSwapAmount.Text = "";
                    ResetSwapPreview();
                    _ = LoadWalletBalances();
                }
                else
                {
                    ShowMessage("Swap Failed: " + (result?.Error ?? "Unknown error"));
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Swap Error: " + ex.Message);
            }
        }

        private void ResetSwapPreview()
        {
            _preparedSwapTx = "";
            _preparedSwapRequestId = "";
            _preparedSwapOutput = 0;
            _preparedSwapRouter = "";
        }

        private void UpdateSwapTitle()
        {
            if (_walletSwapTitle != null)
            {
                _walletSwapTitle.Text = $"LUDC TO {_selectedSwapOutputAsset} SWAP";
            }
        }

        private static decimal GetSwapOutputScale(string assetCode)
        {
            return string.Equals(assetCode, "SOL", StringComparison.OrdinalIgnoreCase)
                ? 1_000_000_000m
                : 1_000_000m;
        }

        private static string GetSwapOutputFormat(string assetCode)
        {
            return string.Equals(assetCode, "SOL", StringComparison.OrdinalIgnoreCase) ? "N9" : "N6";
        }

        private string GetWalletBalanceSummary()
        {
            return $"SOL: {_phantomSolBalance:N4} | LUDC: {_phantomLudcBalance:N2} | USDC: {_phantomUsdcBalance:N6}";
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
                    // Using LudoHub (MatchMaker) for Withdraw
                    string result = await GlobalConstants.MatchMaker.InitiateWithdrawal(recAddress, amount);
                    if (result == "ERROR" || result.StartsWith("Error")) ShowMessage("Error: " + result);
                    else ShowMessage("Transaction Successful!");
                }
            }
            catch (Exception ex) { ShowMessage("Error: " + ex.Message); }
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

        private async void OnBankPasteButtonClicked(object sender, EventArgs e)
        {
            ClientGlobalConstants.hepticEngine?.PlayHapticFeedback("click");
            if (Clipboard.Default.HasText)
            {
                string clipboardText = await Clipboard.Default.GetTextAsync();
                if (!string.IsNullOrEmpty(clipboardText))
                {
                    _bankAccountEntry.Text = clipboardText.Trim();
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
