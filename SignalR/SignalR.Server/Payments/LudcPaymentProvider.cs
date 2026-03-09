using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Interfaces;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;
using Solnet.Wallet;
namespace SignalR.Server.Payments
{
    public class LudcPaymentProvider(IDbContextFactory<LudoDbContext> _contextFactory, IDataProtectionProvider dataProtectionProvider, SolPaymentProvider solPaymentProvider, int _masterUserId, bool debug, string purpose, string LUDC_MINT_ADDRESS) : IPaymentProvider
    {
        public CurrencyType Currency => CurrencyType.LUDC;
        private readonly IRpcClient _rpc = ClientFactory.GetClient(debug ? Cluster.DevNet : Cluster.MainNet);                 // Solana RPC client
        private readonly IDataProtector _protector = dataProtectionProvider.CreateProtector(purpose);                         // Data protector for encrypt/decrypt        

        private const int LUDC_DECIMALS = 9;
        private readonly PublicKey LUDC_MINT = new PublicKey(LUDC_MINT_ADDRESS);

        private readonly PublicKey TOKEN_2022_PROGRAM = new PublicKey("TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb");

        /* =========================================================
        * WALLET INITIALIZATION (SINGLE SOURCE OF TRUTH)
        * ========================================================= */
        public async Task<PlayerWallet> EnsurePlayerWalletExists(int playerId, string addressType = "LUDC")
        {
            return await solPaymentProvider.EnsurePlayerWalletExists(playerId, addressType);
        }
        // ================= WITHDRAW =================

        public async Task<string> WithdrawAsync(Player player, string destination, decimal amount, Guid operationId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            using var tx = await ctx.Database.BeginTransactionAsync();

            var wallet = await EnsurePlayerWalletExists(player.PlayerId);

            if (wallet.AvailableBalance < amount)
                return "INSUFFICIENT_BALANCE";

            wallet.AvailableBalance -= amount;

            ctx.Update(wallet);
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();

            var masterKey = await ctx.PlayerWalletKey.FirstAsync(x => x.PlayerId == _masterUserId);

            var sig = await SendLudcAsync(masterKey, destination, amount);

            return sig;
        }

        // ================= SWEEP =================

        public async Task<string> SweepAsync(int playerId, decimal amount)
        {
            using var ctx = _contextFactory.CreateDbContext();

            var playerKey = await ctx.PlayerWalletKey.FirstAsync(x => x.PlayerId == playerId);
            var masterKey = await ctx.PlayerWalletKey.FirstAsync(x => x.PlayerId == _masterUserId);
            return await SendLudcAsync(playerKey, masterKey.PublicKey, amount);
        }

        // ================= BALANCE =================

        public async Task<decimal> GetOnChainBalanceAsync(string walletAddress)
        {
            var owner = new PublicKey(walletAddress);
            var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(owner, LUDC_MINT);
            var balance = await _rpc.GetTokenAccountBalanceAsync(ata);
            if (!balance.WasSuccessful || balance.Result?.Value == null)
                return 0m;
            return decimal.Parse(balance.Result.Value.UiAmountString);
        }

        // ================= INTERNAL SEND =================

        private async Task<string> SendLudcAsync(PlayerWalletKey senderKey, string destination, decimal amount)
        {
            var rawPriv = _protector.Unprotect(senderKey.EncryptedPrivateKey);
            var senderAccount = new Account(rawPriv, senderKey.PublicKey);
            var receiverPub = new PublicKey(destination);

            // Correct ATA derivation for Token-2022
            PublicKey.TryFindProgramAddress(new[]{
                    senderAccount.PublicKey.KeyBytes,
                    TOKEN_2022_PROGRAM.KeyBytes,
                    LUDC_MINT.KeyBytes
                }, AssociatedTokenAccountProgram.ProgramIdKey, out var senderAta, out _);

            PublicKey.TryFindProgramAddress(new[]{
                    receiverPub.KeyBytes,
                    TOKEN_2022_PROGRAM.KeyBytes,
                    LUDC_MINT.KeyBytes
                }, AssociatedTokenAccountProgram.ProgramIdKey, out var receiverAta, out _);

            ulong tokenAmount = Convert.ToUInt64(decimal.Round(amount * 1_000_000_000m, 0, MidpointRounding.AwayFromZero));

            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;

            var data = new List<byte> { 12 };

            var amountBytes = new byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(amountBytes, tokenAmount);
            data.AddRange(amountBytes);

            data.Add((byte)9);

            var instruction = new TransactionInstruction
            {
                ProgramId = TOKEN_2022_PROGRAM,
                Keys = new List<AccountMeta>
                {
                    AccountMeta.Writable(senderAta, false),
                    AccountMeta.ReadOnly(LUDC_MINT, false),
                    AccountMeta.Writable(receiverAta, false),
                    AccountMeta.ReadOnly(senderAccount.PublicKey, true)
                },
                Data = data.ToArray()
            };

            var tx = new TransactionBuilder()
                .SetRecentBlockHash(blockhash)
                .SetFeePayer(senderAccount.PublicKey)
                .AddInstruction(instruction)
                .Build(senderAccount);

            var res = await _rpc.SendTransactionAsync(tx, false, Commitment.Confirmed);

            if (!res.WasSuccessful)
                throw new Exception(res.Reason);

            return res.Result;
        }
        public async Task<List<TokenDeposit>> GetRecentDeposits(string? beforeSignature = null)
        {
            var deposits = new List<TokenDeposit>();
            // Fetch recent transactions involving the LUDC mint
            var sigs = await _rpc.GetSignaturesForAddressAsync(LUDC_MINT, before: beforeSignature, limit: 50);

            if (!sigs.WasSuccessful || sigs.Result == null)
                return deposits;

            foreach (var sig in sigs.Result)
            {
                var tx = await _rpc.GetTransactionAsync(
                    sig.Signature,
                    Commitment.Confirmed);

                if (!tx.WasSuccessful || tx.Result == null)
                    continue;

                var meta = tx.Result.Meta;

                if (meta?.PostTokenBalances == null)
                    continue;

                foreach (var balance in meta.PostTokenBalances)
                {
                    // Only track LUDC token
                    if (balance.Mint != LUDC_MINT.Key)
                        continue;

                    var accountIndex = balance.AccountIndex;

                    // This is the token account (ATA)
                    var tokenAccountAddress =
                        tx.Result.Transaction.Message.AccountKeys[accountIndex];

                    var pre = meta.PreTokenBalances?
                        .FirstOrDefault(x => x.AccountIndex == accountIndex);

                    decimal postAmount =
                        decimal.Parse(balance.UiTokenAmount.UiAmountString);

                    decimal preAmount = 0;

                    if (pre != null)
                        preAmount = decimal.Parse(pre.UiTokenAmount.UiAmountString);

                    decimal delta = postAmount - preAmount;

                    // Only detect deposits
                    if (delta <= 0)
                        continue;

                    // Get ATA account info
                    var accountInfo = await _rpc.GetAccountInfoAsync(tokenAccountAddress);

                    if (!accountInfo.WasSuccessful || accountInfo.Result?.Value == null)
                        continue;

                    // Decode base64 account data
                    var data = Convert.FromBase64String(accountInfo.Result.Value.Data[0]);

                    // SPL token layout:
                    // 0..32   = mint
                    // 32..64  = owner wallet
                    var ownerBytes = data.Skip(32).Take(32).ToArray();

                    var owner = new PublicKey(ownerBytes).Key;

                    deposits.Add(new TokenDeposit
                    {
                        Signature = sig.Signature,
                        WalletAddress = owner, // ATA address
                        Amount = delta
                    });
                }
            }
            return deposits;
        }
    }
    public class TokenDeposit
    {
        public string Signature { get; set; } = "";
        public string WalletAddress { get; set; } = "";
        public decimal Amount { get; set; }
    }
}