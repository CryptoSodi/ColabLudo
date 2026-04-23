using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedCode;
using SignalR.Server.Payments;
using SignalR.Server.Services;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController(
    ApiPlayerContext playerContext,
    IDbContextFactory<LudoDbContext> contextFactory,
    LudcPaymentProvider ludcPaymentProvider,
    JupiterSwapService jupiterSwapService) : ControllerBase
{
    private readonly IRpcClient _solanaRpc = ClientFactory.GetClient(Cluster.MainNet);
    private const string Token2022Program = "TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb";
    private const string StandardTokenProgram = "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA";
    private const string UsdcMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v";
    private const string SolMint = "So11111111111111111111111111111111111111112";
    private const int SolDecimals = 9;
    private const int UsdcDecimals = 6;
    private const int LudcDecimals = 9;
    private const int MaxSwapSlippageBps = 150;

    [HttpGet("wallet-balance")]
    public async Task<ActionResult<object>> GetWalletBalance([FromQuery] string walletAddress)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] GetWalletBalance unauthorized. Wallet={Short(walletAddress)}");
            return Unauthorized();
        }

        try
        {
            Console.WriteLine($"[PaymentApi] GetWalletBalance requested. PlayerId={player.PlayerId}, Wallet={Short(walletAddress)}");
            using var ctx = await contextFactory.CreateDbContextAsync();
            var internalWallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            var phantomLudc = await GetTokenBalanceAsync(walletAddress, ludcPaymentProvider.MintAddress, Token2022Program);

            Console.WriteLine($"[PaymentApi] GetWalletBalance completed. PlayerId={player.PlayerId}, Wallet={Short(walletAddress)}, PhantomLudc={phantomLudc}, InternalLudc={internalWallet?.AvailableBalance ?? 0m}");
            return new
            {
                Success = true,
                PhantomLudc = phantomLudc,
                InternalLudc = internalWallet?.AvailableBalance ?? 0m
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentApi] GetWalletBalance failed. PlayerId={player.PlayerId}, Wallet={Short(walletAddress)}, Error={ex.Message}");
            return new { Success = false, Error = ex.Message };
        }
    }

    [HttpGet("swap-balances")]
    public async Task<ActionResult<object>> GetSwapBalances([FromQuery] string walletAddress)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] GetSwapBalances unauthorized. Wallet={Short(walletAddress)}");
            return Unauthorized();
        }

        try
        {
            Console.WriteLine($"[PaymentApi] GetSwapBalances requested. PlayerId={player.PlayerId}, Wallet={Short(walletAddress)}");
            using var ctx = await contextFactory.CreateDbContextAsync();
            var internalWallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            var solResult = await _solanaRpc.GetBalanceAsync(walletAddress, Commitment.Confirmed);
            var phantomSol = solResult.WasSuccessful && solResult.Result != null
                ? solResult.Result.Value / 1_000_000_000m
                : 0m;

            var phantomUsdc = await GetTokenBalanceAsync(walletAddress, UsdcMint, StandardTokenProgram);
            var phantomLudc = await GetTokenBalanceAsync(walletAddress, ludcPaymentProvider.MintAddress, Token2022Program);
            Console.WriteLine($"[PaymentApi] GetSwapBalances completed. PlayerId={player.PlayerId}, Wallet={Short(walletAddress)}, PhantomSol={phantomSol}, PhantomUsdc={phantomUsdc}, PhantomLudc={phantomLudc}, InternalLudc={internalWallet?.AvailableBalance ?? 0m}");
            return new
            {
                Success = true,
                PhantomSol = phantomSol,
                PhantomUsdc = phantomUsdc,
                PhantomLudc = phantomLudc,
                InternalLudc = internalWallet?.AvailableBalance ?? 0m
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentApi] GetSwapBalances failed. PlayerId={player.PlayerId}, Wallet={Short(walletAddress)}, Error={ex.Message}");
            return new { Success = false, Error = ex.Message };
        }
    }

    [HttpPost("ludc-deposit/prepare")]
    public async Task<ActionResult<object>> PrepareLudcDeposit([FromBody] PrepareLudcDepositDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] PrepareLudcDeposit unauthorized. Wallet={Short(request.WalletAddress)}, Amount={request.Amount}");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.WalletAddress) || request.Amount <= 0)
        {
            Console.WriteLine($"[PaymentApi] PrepareLudcDeposit rejected. PlayerId={player.PlayerId}, Wallet={Short(request.WalletAddress)}, Amount={request.Amount}");
            return BadRequest();
        }

        Console.WriteLine($"[PaymentApi] PrepareLudcDeposit requested. PlayerId={player.PlayerId}, Wallet={Short(request.WalletAddress)}, Amount={request.Amount}");
        using var ctx = await contextFactory.CreateDbContextAsync();
        var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
        if (wallet == null || string.IsNullOrWhiteSpace(wallet.WalletAddress))
        {
            Console.WriteLine($"[PaymentApi] PrepareLudcDeposit wallet not found. PlayerId={player.PlayerId}");
            return NotFound();
        }

        var result = await ludcPaymentProvider.PrepareDepositFromExternalWalletAsync(request.WalletAddress, wallet.WalletAddress, request.Amount);
        Console.WriteLine($"[PaymentApi] PrepareLudcDeposit completed. PlayerId={player.PlayerId}, SourceWallet={Short(request.WalletAddress)}, DestinationWallet={Short(wallet.WalletAddress)}, Amount={request.Amount}");
        return result;
    }

    [HttpPost("transactions/broadcast")]
    public async Task<ActionResult<BlockchainResult>> BroadcastTransaction([FromBody] BroadcastTransactionDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine("[PaymentApi] BroadcastTransaction unauthorized.");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.TxBase64))
        {
            Console.WriteLine($"[PaymentApi] BroadcastTransaction rejected. PlayerId={player.PlayerId}, Reason=EmptyTransaction");
            return new BlockchainResult { Success = false, Error = "Empty transaction data." };
        }

        try
        {
            Console.WriteLine($"[PaymentApi] BroadcastTransaction requested. PlayerId={player.PlayerId}, TxBytesBase64Length={request.TxBase64.Length}");
            var res = await ludcPaymentProvider.BroadcastTransactionAsync(request.TxBase64);
            if (!res.WasSuccessful)
            {
                Console.WriteLine($"[PaymentApi] BroadcastTransaction broadcast failed. PlayerId={player.PlayerId}, Reason={res.Reason}");
                return new BlockchainResult { Success = false, Error = res.Reason };
            }

            var signature = res.Result;
            var confirmed = await ludcPaymentProvider.ConfirmSignatureAsync(signature);
            Console.WriteLine($"[PaymentApi] BroadcastTransaction completed. PlayerId={player.PlayerId}, Signature={Short(signature)}, Confirmed={confirmed}");
            return confirmed
                ? new BlockchainResult { Success = true, Signature = signature }
                : new BlockchainResult { Success = false, Error = "Transaction broadcasted but failed on-chain execution.", Signature = signature };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentApi] BroadcastTransaction failed. PlayerId={player.PlayerId}, Error={ex.Message}");
            return new BlockchainResult { Success = false, Error = ex.Message };
        }
    }

    [HttpPost("transactions/confirm")]
    public async Task<ActionResult<bool>> ConfirmSolanaTransaction([FromBody] ConfirmTransactionDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] ConfirmTransaction unauthorized. Signature={Short(request.Signature)}");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Signature))
        {
            Console.WriteLine($"[PaymentApi] ConfirmTransaction rejected. PlayerId={player.PlayerId}, Reason=EmptySignature");
            return false;
        }

        Console.WriteLine($"[PaymentApi] ConfirmTransaction requested. PlayerId={player.PlayerId}, Signature={Short(request.Signature)}");
        var confirmed = await ludcPaymentProvider.ConfirmSignatureAsync(request.Signature);
        Console.WriteLine($"[PaymentApi] ConfirmTransaction completed. PlayerId={player.PlayerId}, Signature={Short(request.Signature)}, Confirmed={confirmed}");
        return confirmed;
    }

    [HttpPost("swap/prepare")]
    public async Task<ActionResult<object>> PrepareAssetSwap([FromBody] PrepareAssetSwapDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] PrepareAssetSwap unauthorized. Wallet={Short(request.WalletAddress)}, Input={request.InputAsset}, Output={request.OutputAsset}, Amount={request.Amount}");
            return Unauthorized();
        }

        try
        {
            Console.WriteLine($"[PaymentApi] PrepareAssetSwap requested. PlayerId={player.PlayerId}, Wallet={Short(request.WalletAddress)}, Input={request.InputAsset}, Output={request.OutputAsset}, Amount={request.Amount}, SlippageBps={request.SlippageBps}");
            if (string.IsNullOrWhiteSpace(request.WalletAddress) || request.Amount <= 0)
            {
                Console.WriteLine($"[PaymentApi] PrepareAssetSwap rejected. PlayerId={player.PlayerId}, Reason=InvalidRequest");
                return new { Success = false, Error = "Invalid swap request." };
            }

            using var ctx = await contextFactory.CreateDbContextAsync();
            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            if (wallet == null || string.IsNullOrWhiteSpace(wallet.WalletAddress))
            {
                Console.WriteLine($"[PaymentApi] PrepareAssetSwap rejected. PlayerId={player.PlayerId}, Reason=InternalWalletNotReady");
                return new { Success = false, Error = "Internal LUDC wallet not ready." };
            }

            var normalizedInput = (request.InputAsset ?? string.Empty).Trim().ToUpperInvariant();
            var normalizedOutput = (request.OutputAsset ?? string.Empty).Trim().ToUpperInvariant();

            if (normalizedInput == normalizedOutput)
            {
                Console.WriteLine($"[PaymentApi] PrepareAssetSwap rejected. PlayerId={player.PlayerId}, Reason=SameAsset");
                return new { Success = false, Error = "Choose two different assets." };
            }

            if (normalizedInput != "LUDC" && normalizedOutput != "LUDC")
            {
                Console.WriteLine($"[PaymentApi] PrepareAssetSwap rejected. PlayerId={player.PlayerId}, Reason=MissingLudcSide");
                return new { Success = false, Error = "One side of the swap must be LUDC." };
            }

            var inputConfig = GetSupportedAsset(normalizedInput, ludcPaymentProvider.MintAddress);
            var outputConfig = GetSupportedAsset(normalizedOutput, ludcPaymentProvider.MintAddress);
            if (inputConfig == null || outputConfig == null)
            {
                Console.WriteLine($"[PaymentApi] PrepareAssetSwap rejected. PlayerId={player.PlayerId}, Reason=UnsupportedAsset, Input={normalizedInput}, Output={normalizedOutput}");
                return new { Success = false, Error = "Unsupported asset." };
            }

            var slippageBps = request.SlippageBps is <= 0 or > MaxSwapSlippageBps ? 100 : request.SlippageBps;
            var scale = (decimal)Math.Pow(10, inputConfig.Value.Decimals);
            var amountRaw = Convert.ToUInt64(decimal.Round(request.Amount * scale, 0, MidpointRounding.AwayFromZero));
            if (amountRaw == 0)
            {
                Console.WriteLine($"[PaymentApi] PrepareAssetSwap rejected. PlayerId={player.PlayerId}, Reason=AmountTooSmall");
                return new { Success = false, Error = "Swap amount is too small." };
            }

            var receiver = normalizedOutput == "LUDC" ? wallet.WalletAddress : request.WalletAddress;
            using var order = await jupiterSwapService.GetOrderAsync(
                inputConfig.Value.Mint,
                outputConfig.Value.Mint,
                amountRaw.ToString(),
                request.WalletAddress,
                receiver,
                slippageBps);

            var root = order.RootElement;
            var swapTx = GetJsonString(root, "transaction");
            if (string.IsNullOrEmpty(swapTx))
                swapTx = GetJsonString(root, "swapTransaction");

            var error = GetJsonString(root, "error");
            var errorMessage = GetJsonString(root, "errorMessage");
            if (!string.IsNullOrWhiteSpace(error) || !string.IsNullOrWhiteSpace(errorMessage))
            {
                Console.WriteLine($"[PaymentApi] PrepareAssetSwap quote failed. PlayerId={player.PlayerId}, Error={(string.IsNullOrWhiteSpace(errorMessage) ? error : errorMessage)}");
                return new { Success = false, Error = NormalizeSwapError(string.IsNullOrWhiteSpace(errorMessage) ? error : errorMessage, normalizedInput) };
            }

            var outAmt = GetJsonString(root, "outAmount", "0");
            decimal? normalizedOutAmount = null;
            if (decimal.TryParse(outAmt, out var rawOut))
            {
                var outputScale = (decimal)Math.Pow(10, outputConfig.Value.Decimals);
                normalizedOutAmount = rawOut / outputScale;
            }

            Console.WriteLine($"[PaymentApi] PrepareAssetSwap completed. PlayerId={player.PlayerId}, RequestId={Short(GetJsonString(root, "requestId"))}, Input={normalizedInput}, Output={normalizedOutput}, Amount={request.Amount}, OutAmountRaw={outAmt}");
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
            Console.WriteLine($"[PaymentApi] PrepareAssetSwap failed. PlayerId={player.PlayerId}, Error={ex.Message}");
            return new { Success = false, Error = ex.Message };
        }
    }

    [HttpPost("swap/execute")]
    public async Task<ActionResult<BlockchainResult>> ExecutePreparedSwap([FromBody] ExecutePreparedSwapDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] ExecutePreparedSwap unauthorized. RequestId={Short(request.RequestId)}");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.RequestId) || string.IsNullOrWhiteSpace(request.SignedTxBase64))
        {
            Console.WriteLine($"[PaymentApi] ExecutePreparedSwap rejected. PlayerId={player.PlayerId}, RequestId={Short(request.RequestId)}");
            return new BlockchainResult { Success = false, Error = "Invalid signed transaction." };
        }

        try
        {
            Console.WriteLine($"[PaymentApi] ExecutePreparedSwap requested. PlayerId={player.PlayerId}, RequestId={Short(request.RequestId)}, SignedTxLength={request.SignedTxBase64.Length}");
            using var result = await jupiterSwapService.ExecuteOrderAsync(request.RequestId, request.SignedTxBase64);
            var root = result.RootElement;
            var signature = GetJsonString(root, "signature");
            var error = GetJsonString(root, "error");
            var message = GetJsonString(root, "message");

            if (!string.IsNullOrWhiteSpace(error) || !string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine($"[PaymentApi] ExecutePreparedSwap failed by provider. PlayerId={player.PlayerId}, RequestId={Short(request.RequestId)}, Error={(string.IsNullOrWhiteSpace(message) ? error : message)}");
                return new BlockchainResult { Success = false, Error = string.IsNullOrWhiteSpace(message) ? error : message };
            }

            Console.WriteLine($"[PaymentApi] ExecutePreparedSwap completed. PlayerId={player.PlayerId}, RequestId={Short(request.RequestId)}, Signature={Short(signature)}");
            return new BlockchainResult
            {
                Success = true,
                Signature = signature,
                RequestId = request.RequestId
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentApi] ExecutePreparedSwap failed. PlayerId={player.PlayerId}, RequestId={Short(request.RequestId)}, Error={ex.Message}");
            return new BlockchainResult { Success = false, Error = ex.Message };
        }
    }

    [HttpPost("manual-deposits")]
    public async Task<ActionResult<string>> SubmitManualDeposit([FromBody] ManualDepositDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] ManualDeposit unauthorized. PayloadPlayerId={request.PlayerId}, Amount={request.Amount}, Method={request.Method}, Reference={request.ReferenceNumber}");
            return Unauthorized();
        }

        if (request.Amount <= 0)
        {
            Console.WriteLine($"[PaymentApi] ManualDeposit rejected. PlayerId={player.PlayerId}, Amount={request.Amount}, Reason=InvalidAmount");
            return "Invalid amount.";
        }

        var normalizedMethod = (request.Method ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMethod))
        {
            Console.WriteLine($"[PaymentApi] ManualDeposit rejected. PlayerId={player.PlayerId}, Reason=MissingMethod");
            return "Select a payment method.";
        }

        var normalizedReference = (request.ReferenceNumber ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedReference))
        {
            Console.WriteLine($"[PaymentApi] ManualDeposit rejected. PlayerId={player.PlayerId}, Method={normalizedMethod}, Reason=MissingReference");
            return "Reference number is required.";
        }

        var normalizedReceipt = (request.ReceiptUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedReceipt))
        {
            Console.WriteLine($"[PaymentApi] ManualDeposit rejected. PlayerId={player.PlayerId}, Method={normalizedMethod}, Reference={normalizedReference}, Reason=MissingReceipt");
            return "Receipt proof is required.";
        }

        Console.WriteLine($"[PaymentApi] ManualDeposit requested. PlayerId={player.PlayerId}, PayloadPlayerId={request.PlayerId}, Amount={request.Amount}, Method={normalizedMethod}, Reference={normalizedReference}, ReceiptLength={normalizedReceipt.Length}");
        using var ctx = await contextFactory.CreateDbContextAsync();
        var duplicateExists = await ctx.CashDeposits.AnyAsync(d =>
            d.ReferenceNumber == normalizedReference ||
            d.ReceiptImageUrl == normalizedReceipt);

        if (duplicateExists)
        {
            Console.WriteLine($"[PaymentApi] ManualDeposit duplicate. PlayerId={player.PlayerId}, Reference={normalizedReference}");
            return "Duplicate receipt or reference number.";
        }

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
        Console.WriteLine($"[PaymentApi] ManualDeposit completed. PlayerId={player.PlayerId}, DepositId={deposit.Id}, Amount={deposit.Amount}, Method={deposit.PaymentMethod}, Reference={deposit.ReferenceNumber}");
        return "Success";
    }

    [HttpPost("withdrawals/initiate")]
    public async Task<ActionResult<string>> InitiateWithdrawal([FromBody] InitiateWithdrawalDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] InitiateWithdrawal unauthorized. Destination={Short(request.Destination)}, Amount={request.Amount}");
            return Unauthorized();
        }

        if (request.Amount <= 0)
        {
            Console.WriteLine($"[PaymentApi] InitiateWithdrawal rejected. PlayerId={player.PlayerId}, Destination={Short(request.Destination)}, Amount={request.Amount}, Reason=InvalidAmount");
            return "Invalid amount.";
        }

        var destination = (request.Destination ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(destination))
        {
            Console.WriteLine($"[PaymentApi] InitiateWithdrawal rejected. PlayerId={player.PlayerId}, Reason=InvalidDestination");
            return "Invalid destination.";
        }

        try
        {
            Console.WriteLine($"[PaymentApi] InitiateWithdrawal requested. PlayerId={player.PlayerId}, Destination={Short(destination)}, Amount={request.Amount}");
            using var ctx = await contextFactory.CreateDbContextAsync();
            var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
            if (wallet == null || wallet.AvailableBalance < request.Amount)
            {
                Console.WriteLine($"[PaymentApi] InitiateWithdrawal rejected. PlayerId={player.PlayerId}, Destination={Short(destination)}, Amount={request.Amount}, Balance={wallet?.AvailableBalance ?? 0m}, Reason=InsufficientOrMissingWallet");
                return "Insufficient internal balance.";
            }

            var result = await ludcPaymentProvider.WithdrawAsync(player, destination, request.Amount, Guid.NewGuid());
            if (result != "ERROR" && !result.Contains("INSUFFICIENT", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[PaymentApi] InitiateWithdrawal completed. PlayerId={player.PlayerId}, Destination={Short(destination)}, Amount={request.Amount}, Result={Short(result)}");
                return $"Success: {result}";
            }

            Console.WriteLine($"[PaymentApi] InitiateWithdrawal provider rejected. PlayerId={player.PlayerId}, Destination={Short(destination)}, Amount={request.Amount}, Result={result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PaymentApi] InitiateWithdrawal failed. PlayerId={player.PlayerId}, Destination={Short(destination)}, Amount={request.Amount}, Error={ex.Message}");
            return "Error: " + ex.Message;
        }
    }

    [HttpPost("manual-withdrawals")]
    public async Task<ActionResult<string>> SubmitManualWithdrawal([FromBody] ManualWithdrawalDto request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
        {
            Console.WriteLine($"[PaymentApi] ManualWithdrawal unauthorized. Amount={request.Amount}, Method={request.Method}");
            return Unauthorized();
        }

        if (request.Amount <= 0)
        {
            Console.WriteLine($"[PaymentApi] ManualWithdrawal rejected. PlayerId={player.PlayerId}, Amount={request.Amount}, Reason=InvalidAmount");
            return "Invalid amount.";
        }

        var normalizedMethod = (request.Method ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedMethod) || normalizedMethod == "Select Payout Method")
        {
            Console.WriteLine($"[PaymentApi] ManualWithdrawal rejected. PlayerId={player.PlayerId}, Amount={request.Amount}, Reason=InvalidMethod");
            return "Select a payout method.";
        }

        var normalizedDestination = (request.DestinationDetails ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedDestination))
        {
            Console.WriteLine($"[PaymentApi] ManualWithdrawal rejected. PlayerId={player.PlayerId}, Amount={request.Amount}, Method={normalizedMethod}, Reason=MissingDestination");
            return "Account details are required.";
        }

        Console.WriteLine($"[PaymentApi] ManualWithdrawal requested. PlayerId={player.PlayerId}, Amount={request.Amount}, Method={normalizedMethod}, DestinationLength={normalizedDestination.Length}");
        using var ctx = await contextFactory.CreateDbContextAsync();
        var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.PlayerId == player.PlayerId && w.AddressType == "LUDC");
        if (wallet == null)
        {
            Console.WriteLine($"[PaymentApi] ManualWithdrawal rejected. PlayerId={player.PlayerId}, Reason=WalletNotFound");
            return "Wallet Not Found";
        }

        if (wallet.AvailableBalance < request.Amount)
        {
            Console.WriteLine($"[PaymentApi] ManualWithdrawal rejected. PlayerId={player.PlayerId}, Amount={request.Amount}, Balance={wallet.AvailableBalance}, Reason=InsufficientBalance");
            return "Insufficient internal balance.";
        }

        var duplicatePending = await ctx.CashWithdrawals.AnyAsync(w =>
            w.PlayerId == player.PlayerId &&
            w.Status == "Pending" &&
            w.Amount == request.Amount &&
            w.PayoutMethod == normalizedMethod &&
            w.DestinationDetails == normalizedDestination);

        if (duplicatePending)
        {
            Console.WriteLine($"[PaymentApi] ManualWithdrawal duplicate. PlayerId={player.PlayerId}, Amount={request.Amount}, Method={normalizedMethod}");
            return "A similar payout request is already pending.";
        }

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
        Console.WriteLine($"[PaymentApi] ManualWithdrawal completed. PlayerId={player.PlayerId}, WithdrawalId={withdrawal.Id}, Amount={withdrawal.Amount}, Method={withdrawal.PayoutMethod}");
        return "Success";
    }

    private static string Short(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var trimmed = value.Trim();
        return trimmed.Length <= 12 ? trimmed : $"{trimmed[..6]}...{trimmed[^6..]}";
    }

    private async Task<decimal> GetTokenBalanceAsync(string ownerAddress, string mintAddress, string tokenProgramAddress)
    {
        try
        {
            var owner = new PublicKey(ownerAddress);
            var mint = new PublicKey(mintAddress);
            var tokenProgram = new PublicKey(tokenProgramAddress);
            PublicKey.TryFindProgramAddress(
                new[] { owner.KeyBytes, tokenProgram.KeyBytes, mint.KeyBytes },
                AssociatedTokenAccountProgram.ProgramIdKey,
                out var ata,
                out _);

            var balanceResult = await _solanaRpc.GetTokenAccountBalanceAsync(ata.Key, Commitment.Confirmed);
            if (balanceResult.WasSuccessful && balanceResult.Result?.Value != null)
                return decimal.TryParse(balanceResult.Result.Value.UiAmountString, out var parsed) ? parsed : 0m;
        }
        catch
        {
        }

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
        if (!parent.TryGetProperty(propertyName, out var value))
            return fallback;

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

public record PrepareLudcDepositDto(string WalletAddress, decimal Amount);
public record BroadcastTransactionDto(string TxBase64);
public record ConfirmTransactionDto(string Signature);
public record PrepareAssetSwapDto(string WalletAddress, string InputAsset, string OutputAsset, decimal Amount, int SlippageBps);
public record ExecutePreparedSwapDto(string RequestId, string SignedTxBase64);
public record ManualDepositDto(int PlayerId, decimal Amount, string Method, string ReferenceNumber, string ReceiptUrl);
public record InitiateWithdrawalDto(string Destination, decimal Amount);
public record ManualWithdrawalDto(decimal Amount, string Method, string DestinationDetails);
