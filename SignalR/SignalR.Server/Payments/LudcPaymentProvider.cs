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
using System.Text.Json;
namespace SignalR.Server.Payments
{
    public class LudcPaymentProvider(IDbContextFactory<LudoDbContext> _contextFactory, IDataProtectionProvider dataProtectionProvider, SolPaymentProvider solPaymentProvider, int _masterUserId, bool debug, string purpose, string LUDC_MINT_ADDRESS) : IPaymentProvider
    {
        public CurrencyType Currency => CurrencyType.LUDC;
        public string MintAddress => LUDC_MINT.Key;
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
            var masterKey = await ctx.PlayerWalletKey.FirstAsync(x => x.PlayerId == _masterUserId);
            var sig = await SendLudcAsync(masterKey, destination, amount);
            if(sig != "ERROR")
            {
                wallet.AvailableBalance -= amount;
                ctx.Update(wallet);
                await ctx.SaveChangesAsync();
                await tx.CommitAsync();
            }
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
        public async Task<object> PrepareDepositFromExternalWalletAsync(string senderWalletAddress, string destinationOwnerAddress, decimal amount)
        {
            var senderPub = new PublicKey(senderWalletAddress);
            var receiverPub = new PublicKey(destinationOwnerAddress);

            PublicKey.TryFindProgramAddress(
                new[] { senderPub.KeyBytes, TOKEN_2022_PROGRAM.KeyBytes, LUDC_MINT.KeyBytes },
                AssociatedTokenAccountProgram.ProgramIdKey,
                out var senderAta,
                out _);

            PublicKey.TryFindProgramAddress(
                new[] { receiverPub.KeyBytes, TOKEN_2022_PROGRAM.KeyBytes, LUDC_MINT.KeyBytes },
                AssociatedTokenAccountProgram.ProgramIdKey,
                out var receiverAta,
                out _);

            var senderAtaInfo = await _rpc.GetAccountInfoAsync(senderAta);
            var receiverAtaInfo = await _rpc.GetAccountInfoAsync(receiverAta);
            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;
            ulong tokenAmount = Convert.ToUInt64(decimal.Round(amount * 1_000_000_000m, 0, MidpointRounding.AwayFromZero));

            return new
            {
                SenderOwner = senderWalletAddress,
                DestinationOwner = destinationOwnerAddress,
                SenderAta = senderAta.Key,
                DestinationAta = receiverAta.Key,
                SenderAtaExists = senderAtaInfo.WasSuccessful && senderAtaInfo.Result?.Value != null,
                DestinationAtaExists = receiverAtaInfo.WasSuccessful && receiverAtaInfo.Result?.Value != null,
                Blockhash = blockhash,
                Mint = LUDC_MINT.Key,
                TokenProgram = TOKEN_2022_PROGRAM.Key,
                AssociatedTokenProgram = AssociatedTokenAccountProgram.ProgramIdKey.Key,
                SystemProgram = SystemProgram.ProgramIdKey.Key,
                Decimals = LUDC_DECIMALS,
                AmountRaw = tokenAmount.ToString()
            };
        }
        public async Task<bool> ConfirmSignatureAsync(string signature)
        {
            var tx = await _rpc.GetTransactionAsync(signature, Commitment.Confirmed);
            return tx.WasSuccessful && tx.Result != null;
        }
        // ================= INTERNAL SEND =================
        private async Task<string> SendLudcAsync(PlayerWalletKey senderKey, string destination, decimal amount)
        {
            var rawPriv = _protector.Unprotect(senderKey.EncryptedPrivateKey);
            var senderAccount = new Account(rawPriv, senderKey.PublicKey);
            var receiverPub = new PublicKey(destination);
            // ================= DERIVE ATA =================
            PublicKey.TryFindProgramAddress(new[]{senderAccount.PublicKey.KeyBytes,TOKEN_2022_PROGRAM.KeyBytes,LUDC_MINT.KeyBytes},
                AssociatedTokenAccountProgram.ProgramIdKey,out var senderAta,out _);
            PublicKey.TryFindProgramAddress(new[]{receiverPub.KeyBytes,TOKEN_2022_PROGRAM.KeyBytes,LUDC_MINT.KeyBytes},
                AssociatedTokenAccountProgram.ProgramIdKey,out var receiverAta,out _);
            // ================= CHECK RECEIVER ATA =================
            var receiverAtaInfo = await _rpc.GetAccountInfoAsync(receiverAta);
            bool receiverAtaExists = receiverAtaInfo.WasSuccessful && receiverAtaInfo.Result?.Value != null;
            // ================= TOKEN AMOUNT =================
            ulong tokenAmount = Convert.ToUInt64(decimal.Round(amount * 1_000_000_000m,0,MidpointRounding.AwayFromZero));
            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;
            // ================= BUILD TRANSACTION =================
            var builder = new TransactionBuilder().SetRecentBlockHash(blockhash).SetFeePayer(senderAccount.PublicKey);
            // ================= CREATE ATA IF MISSING =================
            if (!receiverAtaExists)
            {
                var createAtaInstruction = new TransactionInstruction
                {
                    ProgramId = AssociatedTokenAccountProgram.ProgramIdKey, Keys = new List<AccountMeta>
                    {
                        AccountMeta.Writable(senderAccount.PublicKey, true),     // payer        
                        AccountMeta.Writable(receiverAta, false),                // ATA        
                        AccountMeta.ReadOnly(receiverPub, false),                // owner        
                        AccountMeta.ReadOnly(LUDC_MINT, false),                  // mint        
                        AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false), // system program        
                        AccountMeta.ReadOnly(TOKEN_2022_PROGRAM, false)          // token program    
                    },
                    Data = Array.Empty<byte>() // ATA create has empty data
                };
                builder.AddInstruction(createAtaInstruction);
            }
            // ================= TRANSFER INSTRUCTION =================
            var data = new List<byte> {12};
            var amountBytes = new byte[8];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(amountBytes, tokenAmount);
            data.AddRange(amountBytes);
            data.Add((byte)LUDC_DECIMALS);
            var transferInstruction = new TransactionInstruction
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
            builder.AddInstruction(transferInstruction);
            // ================= BUILD TX =================
            var tx = builder.Build(senderAccount);
            // ================= SEND TX =================
            var res = await _rpc.SendTransactionAsync(tx, false, Commitment.Confirmed);
            if (!res.WasSuccessful)
            {
                Console.WriteLine($"SEND FAILED : {res.Reason}");
                return "ERROR";
            }
            else
                Console.WriteLine($"SEND SUCCESS : {res.Result}");
                return "SUCCESS";
        }
        public async Task<List<TokenDeposit>> GetRecentDeposits(string? lastProcessedSignature = null)
        {
            var deposits = new List<TokenDeposit>();

            // Fetch newest signatures
            var sigs = await _rpc.GetSignaturesForAddressAsync(LUDC_MINT,until: lastProcessedSignature,limit: 100);

            if (!sigs.WasSuccessful || sigs.Result == null || sigs.Result.Count == 0)
                return deposits;
            
            sigs.Result.Reverse();

            foreach (var sig in sigs.Result)
            {
                var tx = await _rpc.GetTransactionAsync(sig.Signature,Commitment.Confirmed);

                if (tx == null)
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

                    var tokenAccountAddress =
                        tx.Result.Transaction.Message.AccountKeys[accountIndex];

                    var pre = meta.PreTokenBalances?
                        .FirstOrDefault(x => x.AccountIndex == accountIndex);

                    // Ignore mint events (no previous balance)
                    if (pre == null)
                        continue;

                    decimal postAmount =
                        decimal.Parse(balance.UiTokenAmount.UiAmountString);

                    decimal preAmount =
                        decimal.Parse(pre.UiTokenAmount.UiAmountString);

                    decimal delta = postAmount - preAmount;

                    // Only detect deposits
                    if (delta <= 0)
                        continue;

                    // Fetch ATA account info
                    var accountInfo = await _rpc.GetAccountInfoAsync(tokenAccountAddress);

                    if (!accountInfo.WasSuccessful || accountInfo.Result?.Value == null)
                        continue;

                    var data = Convert.FromBase64String(accountInfo.Result.Value.Data[0]);

                    // SPL token account layout
                    var ownerBytes = data.Skip(32).Take(32).ToArray();

                    var owner = new PublicKey(ownerBytes).Key;

                    deposits.Add(new TokenDeposit
                    {
                        Signature = sig.Signature,
                        WalletAddress = owner,
                        Amount = delta
                    });
                }
            }        
            return deposits;
        }
        private static readonly HttpClient _http = new HttpClient();
        private async Task<JsonDocument?> GetTransactionRaw(string signature)
        {
            var request = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "getTransaction",
                @params = new object[]
                {
            signature,
            new
            {
                encoding = "jsonParsed",
                commitment = "confirmed",
                maxSupportedTransactionVersion = 0
            }
                }
            };

            var response = await _http.PostAsJsonAsync(
                "https://api.mainnet-beta.solana.com",
                request);

            var json = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null)
                return null;

            return doc;
        }
        public async Task<string> InitializeLatestSignature()
        {
            var sigs = await _rpc.GetSignaturesForAddressAsync(LUDC_MINT, limit: 1);

            if (sigs.WasSuccessful && sigs.Result?.Count > 0)
            {
               string _lastProcessedSignature = sigs.Result[0].Signature;

                Console.WriteLine($"Deposit scanner starting from: {_lastProcessedSignature}");
                return _lastProcessedSignature;
            }
            return null;
        }
    }
    public class TokenDeposit
    {
        public string Signature { get; set; } = "";
        public string WalletAddress { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
