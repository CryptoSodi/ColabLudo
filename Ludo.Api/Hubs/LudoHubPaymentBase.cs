using Ludo.Api.Controllers;
using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode;
using SignalR.Server;
using SignalR.Server.Payments;
using SignalR.Server.Services;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace Ludo.Api.Hubs;

public abstract class LudoHubPaymentBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    DailyBonusService dailyBonusService,
    CryptoHelper cryptoHelper,
    UtilService utilService,
    PlayerPresenceTracker presenceTracker,
    IDbContextFactory<LudoDbContext> contextFactory,
    FriendsService friendsService,
    TournamentService tournamentService,
    LudcPaymentProvider ludcPaymentProvider,
    JupiterSwapService jupiterSwapService) : LudoHubWalletBase(playerContext, databaseManager, hubContext, dailyBonusService, cryptoHelper, utilService, presenceTracker, contextFactory, friendsService, tournamentService)
{
    private readonly IDbContextFactory<LudoDbContext> _contextFactory = contextFactory;
    private readonly LudcPaymentProvider _ludcPaymentProvider = ludcPaymentProvider;
    private readonly JupiterSwapService _jupiterSwapService = jupiterSwapService;
    private readonly IRpcClient _solanaRpc = ClientFactory.GetClient(Cluster.MainNet);

    private const string Token2022Program = "TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb";
    private const string StandardTokenProgram = "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA";
    private const string UsdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
    private const string SolMint = "So11111111111111111111111111111111111111112";
    private const int SolDecimals = 9;
    private const int UsdcDecimals = 6;
    private const int LudcDecimals = 9;
    private const int MaxSwapSlippageBps = 150;

    public async Task<object> GetWalletBalance(string walletAddress)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return new { Success = false, Error = "Unauthorized" };
        try
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var internalWallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            var phantomLudc = await GetTokenBalanceAsync(walletAddress, _ludcPaymentProvider.MintAddress, Token2022Program);
            return new { Success = true, PhantomLudc = phantomLudc, InternalLudc = internalWallet?.AvailableBalance ?? 0m };
        }
        catch (Exception ex)
        {
            return new { Success = false, Error = ex.Message };
        }
    }

    public async Task<object> GetSwapBalances(string walletAddress)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return new { Success = false, Error = "Unauthorized" };
        try
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var internalWallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            var solResult = await _solanaRpc.GetBalanceAsync(walletAddress, Commitment.Confirmed);
            var phantomSol = solResult.WasSuccessful && solResult.Result != null ? solResult.Result.Value / 1_000_000_000m : 0m;
            var phantomUsdc = await GetTokenBalanceAsync(walletAddress, UsdcMint, StandardTokenProgram);
            var phantomLudc = await GetTokenBalanceAsync(walletAddress, _ludcPaymentProvider.MintAddress, Token2022Program);
            return new { Success = true, PhantomSol = phantomSol, PhantomUsdc = phantomUsdc, PhantomLudc = phantomLudc, InternalLudc = internalWallet?.AvailableBalance ?? 0m };
        }
        catch (Exception ex)
        {
            return new { Success = false, Error = ex.Message };
        }
    }

    public async Task<object> PrepareLudcDeposit(PrepareLudcDepositDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return new { Success = false, Error = "Unauthorized" };
        if (string.IsNullOrWhiteSpace(request.WalletAddress) || request.Amount <= 0)
            return new { Success = false, Error = "Invalid request." };

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
        if (wallet == null || string.IsNullOrWhiteSpace(wallet.WalletAddress))
            return new { Success = false, Error = "Internal LUDC wallet not ready." };

        return await _ludcPaymentProvider.PrepareDepositFromExternalWalletAsync(request.WalletAddress, wallet.WalletAddress, request.Amount);
    }

    public async Task<BlockchainResult> BroadcastTransaction(BroadcastTransactionDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return new BlockchainResult { Success = false, Error = "Unauthorized" };
        if (string.IsNullOrWhiteSpace(request.TxBase64))
            return new BlockchainResult { Success = false, Error = "Empty transaction data." };

        try
        {
            var res = await _ludcPaymentProvider.BroadcastTransactionAsync(request.TxBase64);
            if (!res.WasSuccessful)
                return new BlockchainResult { Success = false, Error = res.Reason };

            var signature = res.Result;
            var confirmed = await _ludcPaymentProvider.ConfirmSignatureAsync(signature);
            return confirmed
                ? new BlockchainResult { Success = true, Signature = signature }
                : new BlockchainResult { Success = false, Error = "Transaction broadcasted but failed on-chain execution.", Signature = signature };
        }
        catch (Exception ex)
        {
            return new BlockchainResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<bool> ConfirmSolanaTransaction(ConfirmTransactionDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null || string.IsNullOrWhiteSpace(request.Signature))
            return false;
        return await _ludcPaymentProvider.ConfirmSignatureAsync(request.Signature);
    }

    public async Task<object> PrepareAssetSwap(PrepareAssetSwapDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return new { Success = false, Error = "Unauthorized" };
        try
        {
            if (string.IsNullOrWhiteSpace(request.WalletAddress) || request.Amount <= 0)
                return new { Success = false, Error = "Invalid swap request." };

            using var ctx = await _contextFactory.CreateDbContextAsync();
            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            if (wallet == null || string.IsNullOrWhiteSpace(wallet.WalletAddress))
                return new { Success = false, Error = "Internal LUDC wallet not ready." };

            var normalizedInput = (request.InputAsset ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedOutput = (request.OutputAsset ?? string.Empty).Trim().ToUpperInvariant();
            if (normalizedInput == normalizedOutput) return new { Success = false, Error = "Choose two different assets." };
            if (normalizedInput != "LUDC" && normalizedOutput != "LUDC")
                return new { Success = false, Error = "One side of the swap must be LUDC." };

            var inputConfig = GetSupportedAsset(normalizedInput, _ludcPaymentProvider.MintAddress);
            var outputConfig = GetSupportedAsset(normalizedOutput, _ludcPaymentProvider.MintAddress);
            if (inputConfig == null || outputConfig == null) return new { Success = false, Error = "Unsupported asset." };

            var slippageBps = request.SlippageBps is <= 0 or > MaxSwapSlippageBps ? 100 : request.SlippageBps;
            var scale = (decimal)Math.Pow(10, inputConfig.Value.Decimals);
            var amountRaw = Convert.ToUInt64(decimal.Round(request.Amount * scale, 0, MidpointRounding.AwayFromZero));
            if (amountRaw == 0) return new { Success = false, Error = "Swap amount is too small." };

            var receiver = normalizedOutput == "LUDC" ? wallet.WalletAddress : request.WalletAddress;
            using var order = await _jupiterSwapService.GetOrderAsync(
                inputConfig.Value.Mint,
                outputConfig.Value.Mint,
                amountRaw.ToString(),
                request.WalletAddress,
                receiver,
                slippageBps);

            var root = order.RootElement;
            var swapTx = GetJsonString(root, "transaction");
            if (string.IsNullOrEmpty(swapTx)) swapTx = GetJsonString(root, "swapTransaction");
            var error = GetJsonString(root, "error");
            var errorMessage = GetJsonString(root, "errorMessage");
            if (!string.IsNullOrWhiteSpace(error) || !string.IsNullOrWhiteSpace(errorMessage))
                return new { Success = false, Error = NormalizeSwapError(string.IsNullOrWhiteSpace(errorMessage) ? error : errorMessage, normalizedInput) };

            var outAmt = GetJsonString(root, "outAmount", "0");
            decimal? normalizedOutAmount = null;
            if (decimal.TryParse(outAmt, out var rawOut))
            {
                var outputScale = (decimal)Math.Pow(10, outputConfig.Value.Decimals);
                normalizedOutAmount = rawOut / outputScale;
            }

            return new
            {
                Success = true,
                InputAsset = normalizedInput,
                InputMint = inputConfig.Value.Mint,
                InputAmount = request.Amount,
                InputAmountRaw = GetJsonString(root, "inAmount", amountRaw.ToString()),
                OutputAsset = normalizedOutput,
                OutputMint = outputConfig.Value.Mint,
                SwapTransaction = swapTx,
                Transaction = swapTx,
                OutAmount = normalizedOutAmount,
                OutAmountRaw = outAmt,
                OutputAmountRaw = outAmt,
                Receiver = receiver,
                PriceImpactPct = GetJsonString(root, "priceImpactPct"),
                Router = GetJsonString(root, "router"),
                OutUsdValue = GetJsonString(root, "outUsdValue"),
                RequestId = GetJsonString(root, "requestId"),
                SlippageBps = slippageBps
            };
        }
        catch (Exception ex)
        {
            return new { Success = false, Error = ex.Message };
        }
    }

    public async Task<BlockchainResult> ExecutePreparedSwap(ExecutePreparedSwapDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return new BlockchainResult { Success = false, Error = "Unauthorized" };
        if (string.IsNullOrWhiteSpace(request.RequestId) || string.IsNullOrWhiteSpace(request.SignedTxBase64))
            return new BlockchainResult { Success = false, Error = "Invalid signed transaction." };

        try
        {
            using var result = await _jupiterSwapService.ExecuteOrderAsync(request.RequestId, request.SignedTxBase64);
            var root = result.RootElement;
            var signature = GetJsonString(root, "signature");
            var error = GetJsonString(root, "error");
            var message = GetJsonString(root, "message");
            if (!string.IsNullOrWhiteSpace(error) || !string.IsNullOrWhiteSpace(message))
                return new BlockchainResult { Success = false, Error = string.IsNullOrWhiteSpace(message) ? error : message };

            return new BlockchainResult { Success = true, Signature = signature, RequestId = request.RequestId };
        }
        catch (Exception ex)
        {
            return new BlockchainResult { Success = false, Error = ex.Message };
        }
    }

    public async Task<string> SubmitManualDeposit(ManualDepositDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return "Unauthorized";
        if (request.Amount <= 0) return "Invalid amount.";

        var normalizedMethod = (request.Method ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMethod)) return "Select a payment method.";
        var normalizedReference = (request.ReferenceNumber ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedReference)) return "Reference number is required.";
        var normalizedReceipt = (request.ReceiptUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedReceipt)) return "Receipt proof is required.";

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var duplicateExists = await ctx.CashDeposits.AnyAsync(d => d.ReferenceNumber == normalizedReference || d.ReceiptImageUrl == normalizedReceipt);
        if (duplicateExists) return "Duplicate receipt or reference number.";

        var deposit = new CashDeposit
        {
            PlayerId = player.PlayerId,
            Amount = request.Amount,
            ReferenceNumber = normalizedReference,
            PaymentMethod = normalizedMethod,
            ReceiptImageUrl = normalizedReceipt,
            Status = "Pending",
            CreatedDate = DateTime.UtcNow
        };
        ctx.CashDeposits.Add(deposit);
        await ctx.SaveChangesAsync();
        return "Success";
    }

    public async Task<string> InitiateWithdrawal(InitiateWithdrawalDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return "Unauthorized";
        if (request.Amount <= 0) return "Invalid amount.";

        var destination = (request.Destination ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(destination)) return "Invalid destination.";

        try
        {
            using var ctx = await _contextFactory.CreateDbContextAsync();
            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            if (wallet == null || wallet.AvailableBalance < request.Amount) return "Insufficient internal balance.";

            var result = await _ludcPaymentProvider.WithdrawAsync(player, destination, request.Amount, Guid.NewGuid());
            if (result != "ERROR" && !result.Contains("INSUFFICIENT", StringComparison.OrdinalIgnoreCase))
                return $"Success: {result}";
            return result;
        }
        catch (Exception ex)
        {
            return "Error: " + ex.Message;
        }
    }

    public async Task<string> SubmitManualWithdrawal(ManualWithdrawalDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null) return "Unauthorized";
        if (request.Amount <= 0) return "Invalid amount.";

        var normalizedMethod = (request.Method ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMethod) || normalizedMethod == "Select Payout Method")
            return "Select a payout method.";
        var normalizedDestination = (request.DestinationDetails ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedDestination)) return "Account details are required.";

        using var ctx = await _contextFactory.CreateDbContextAsync();
        var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
        if (wallet == null) return "Wallet Not Found";
        if (wallet.AvailableBalance < request.Amount) return "Insufficient internal balance.";

        var duplicatePending = await ctx.CashWithdrawals.AnyAsync(w =>
            w.PlayerId == player.PlayerId &&
            w.Status == "Pending" &&
            w.Amount == request.Amount &&
            w.PayoutMethod == normalizedMethod &&
            w.DestinationDetails == normalizedDestination);
        if (duplicatePending) return "A similar payout request is already pending.";

        var withdrawal = new CashWithdrawal
        {
            PlayerId = player.PlayerId,
            Amount = request.Amount,
            PayoutMethod = normalizedMethod,
            DestinationDetails = normalizedDestination,
            Status = "Pending",
            CreatedDate = DateTime.UtcNow
        };
        ctx.CashWithdrawals.Add(withdrawal);
        await ctx.SaveChangesAsync();
        return "Success";
    }

    private async Task<Player?> TryGetAuthenticatedPlayerAsync()
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token)) return null;
        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        return await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
    }

    private async Task<decimal> GetTokenBalanceAsync(string ownerAddress, string mintAddress, string tokenProgramAddress)
    {
        try
        {
            var owner = new PublicKey(ownerAddress);
            var mint = new PublicKey(mintAddress);
            var tokenProgram = new PublicKey(tokenProgramAddress);
            PublicKey.TryFindProgramAddress(new[] { owner.KeyBytes, tokenProgram.KeyBytes, mint.KeyBytes }, AssociatedTokenAccountProgram.ProgramIdKey, out var ata, out _);
            var balanceResult = await _solanaRpc.GetTokenAccountBalanceAsync(ata.Key, Commitment.Confirmed);
            if (balanceResult.WasSuccessful && balanceResult.Result?.Value != null)
                return decimal.TryParse(balanceResult.Result.Value.UiAmountString, out var parsed) ? parsed : 0m;
        }
        catch { }
        return 0m;
    }

    private static (string Mint, int Decimals, string? TokenProgram)? GetSupportedAsset(string assetCode, string ludcMintAddress)
    {
        return (assetCode ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "SOL" => (SolMint, SolDecimals, null),
            "USDC" => (UsdcMint, UsdcDecimals, StandardTokenProgram),
            "LUDC" => (ludcMintAddress, LudcDecimals, Token2022Program),
            _ => null
        };
    }

    private static string GetJsonString(System.Text.Json.JsonElement parent, string propertyName, string fallback = "")
    {
        if (!parent.TryGetProperty(propertyName, out var value)) return fallback;
        return value.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => value.GetString() ?? fallback,
            System.Text.Json.JsonValueKind.Number => value.GetRawText(),
            System.Text.Json.JsonValueKind.True => bool.TrueString,
            System.Text.Json.JsonValueKind.False => bool.FalseString,
            _ => fallback
        };
    }

    private static string NormalizeSwapError(string swapError, string normalizedInput)
    {
        if (string.Equals(swapError, "Insufficient funds", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedInput == "USDC"
                ? "Insufficient funds in Phantom wallet. You need enough USDC and a small SOL balance for fees."
                : $"Insufficient {normalizedInput} in Phantom wallet.";
        }
        if (string.Equals(swapError, "Failed to get quotes", StringComparison.OrdinalIgnoreCase))
            return "No swap route is available for this amount right now. Try a smaller amount.";
        return swapError;
    }
}
