using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SignalR.Server
{
    /// <summary>
    /// Represents metadata for an on-chain wallet, either master or sub-account.
    /// </summary>
    public class Wallet
    {
        /// <summary>User or manager identifier.</summary>
        public int PlayerId { get; set; }

        /// <summary>Encrypted Base58 private key for signing transactions.</summary>
        public string EncryptedPrivateKey { get; set; }

        /// <summary>Base58-encoded public key for deposit/withdrawal.</summary>
        public string PublicKey { get; set; }

        /// <summary>Flag indicating if this wallet is the master (hot) wallet.</summary>
        public bool IsMaster { get; set; }
    }

    /// <summary>
    /// Manages all on-chain & off-chain wallet operations: account creation,
    /// balance checks, transfers, and periodic sweeping of sub-accounts.
    /// </summary>
    public class CryptoHelper
    {
        // Number of lamports in one SOL.
        private const ulong LamportsPerSol = 1_000_000_000;

        private readonly IRpcClient _rpc;                       // Solana RPC client
        private readonly string _storageFile;                   // File path for JSON wallet store
        private readonly IDbContextFactory<LudoDbContext> _contextFactory; // EF factory for ledger DB
        private readonly IDataProtector _protector;             // Data protector for encrypt/decrypt
        private readonly Dictionary<int, Wallet> _wallets;   // In-memory wallet cache
        private readonly int _masterUserId;                  // Identifier for the master wallet

        /// Constructor sets up RPC client, loads or creates wallets, and ensures
        /// master wallet is present in the JSON store.
        public CryptoHelper(IDbContextFactory<LudoDbContext> contextFactory, IHostEnvironment env, IDataProtectionProvider dataProtectionProvider,
            int masterUserId,
            string network = "MainNetBeta",
            string relativeStoragePath = "wallets.json",
            string protectorKey = "CryptoHelper.WalletProtector")
        {
            Key = SHA256.HashData(Encoding.UTF8.GetBytes(protectorKey));
            IV = new byte[16]; // 16 bytes IV for AES

            _contextFactory = contextFactory;
            // Create a protector instance scoped to this class
            _protector = dataProtectionProvider.CreateProtector(protectorKey);
            _masterUserId = masterUserId;

            // Initialize Solana RPC for MainNet or DevNet
            var cluster = network.Equals("DevNet", StringComparison.OrdinalIgnoreCase)
                ? Cluster.DevNet
                : Cluster.MainNet;
            _rpc = ClientFactory.GetClient(cluster);

            // Ensure the folder for storing JSON exists
            var dataFolder = Path.Combine(env.ContentRootPath, Path.GetDirectoryName(relativeStoragePath) ?? string.Empty);
            Directory.CreateDirectory(dataFolder);
            _storageFile = Path.Combine(env.ContentRootPath, relativeStoragePath);

            // Load existing wallets or initialize empty dictionary
            if (File.Exists(_storageFile))
                _wallets = JsonSerializer.Deserialize<Dictionary<int, Wallet>>(File.ReadAllText(_storageFile)) ?? new Dictionary<int, Wallet>();
            else
                _wallets = new Dictionary<int, Wallet>();

            // Ensure the master (hot) wallet exists; if not, generate and persist it.
            if (!_wallets.ContainsKey(_masterUserId))
            {
                using var ctx = _contextFactory.CreateDbContext();
                
                bool playerExists = ctx.Players.Any(p => p.PlayerId == _masterUserId);
                if (!playerExists)
                {
                    Player admin = new Player
                    {
                        GoogleId = "",
                        Name = "Admin",
                        Email = "Admin@LudoNFT.com",
                        PictureUrl = "",
                        City = "Global",
                        CountryCode = "+1"
                    };
                    ctx.Players.Add(admin);
                    // Save changes to the database
                    ctx.SaveChangesAsync();
                }
                GetOrCreateAccount(_masterUserId, true, true).GetAwaiter().GetResult();
            }
        }
        /// <summary>
        /// Returns an existing sub-account public key, or generates one if missing.
        /// </summary>
        public async Task<string> GetOrCreateAccount(int playerId, bool isMaster = false, bool save = true, CancellationToken cancellationToken = default)
        {
            // If already exists, return the public key
            if (_wallets.TryGetValue(playerId, out var w) && w.PublicKey != null)
                return await Task.FromResult(w.PublicKey);

            var account = new Account();
            // Encode and encrypt the private key
            var rawPriv = new Base58Encoder().EncodeData(account.PrivateKey);
            var cipherPriv = _protector.Protect(rawPriv);
            var pub = account.PublicKey.Key;

            _wallets[playerId] = new Wallet
            {
                PlayerId = playerId,
                EncryptedPrivateKey = cipherPriv,
                PublicKey = pub,
                IsMaster = isMaster
            };
            await EnsurePlayerWalletExists(playerId, pub);
            if(save)
                PersistWallets(); // Persist updated store
            Console.WriteLine($"Sub-account created: {playerId} -> {pub}");
            return await Task.FromResult(pub);
        }
        /// Moves SOL off-chain from master ledger to a sub-account ledger.
        public async Task<bool> OffChainTransaction(int playerId, decimal solAmount, String description, String txId = "", bool IsOnChain = false, String RoomCode="")
        {
            using var ctx = _contextFactory.CreateDbContext();
            var sub = await EnsurePlayerWalletExists(playerId);
            sub.AvailableBalance += solAmount;

            ctx.WalletTransaction.Add(new WalletTransaction
            {
                PlayerId = playerId,
                Amount = solAmount,
                BalanceAfter = sub.AvailableBalance,
                Type = TransactionType.Sweep,
                Description = description,
                RoomCode = RoomCode,
                IsOnChain = IsOnChain,
                txId = txId,
                CreatedDate = DateTime.UtcNow
            });
            ctx.PlayerWallet.Update(sub);
            await ctx.SaveChangesAsync();
            return true;
        }
        /// Queries the on-chain SOL balance (in lamports) for a given public key.
        public async Task<ulong> GetOnChainBalanceAsync(string pubKey)
        {
            var r = await _rpc.GetBalanceAsync(pubKey);
            if (r.WasSuccessful)
                return r.Result.Value;
            throw new Exception(r.Reason);
        }

        /// <summary>
        /// Builds, signs, and sends a SOL transfer transaction on-chain.
        /// </summary>
        public async Task<string> SendOnChainAsync(int fromPlayerId, string toPubKey, decimal solAmount)
        {
            if (!_wallets.TryGetValue(fromPlayerId, out var w))
                throw new InvalidOperationException("Wallet not found");

            // Decrypt (unprotect) the stored private key
            var rawPriv = _protector.Unprotect(w.EncryptedPrivateKey);
            var acct = new Account(rawPriv, w.PublicKey);

            // Convert SOL to lamports and fetch latest blockhash
            
            var lamports = (ulong)(solAmount * LamportsPerSol);
            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;

            var balance = (await _rpc.GetBalanceAsync(acct.PublicKey)).Result.Value;

            // Optional: Fetch required minimum lamports for a system account
            var rentExemptMin = (await _rpc.GetMinimumBalanceForRentExemptionAsync(0)).Result;

            // Add this to the transfer amount if you're sending to a fresh public key
            ulong lamportsToSend = (ulong)(solAmount * LamportsPerSol);
          
            ulong feeBuffer = await getFeeBuffer();

            if (lamportsToSend + feeBuffer > balance)
                throw new InvalidOperationException("Not enough funds to cover fee buffer.");

            var tx = new TransactionBuilder().SetRecentBlockHash(blockhash).SetFeePayer(acct.PublicKey).AddInstruction(SystemProgram.Transfer(acct.PublicKey, new PublicKey(toPubKey), lamportsToSend)).Build(acct);

            var s = await _rpc.SendTransactionAsync(tx, false, Commitment.Confirmed);
            Console.WriteLine($"Tx failed: {s.Reason}");
            if (!s.WasSuccessful)
            {
                var errorJson = JsonSerializer.Serialize(s);
                Console.WriteLine($"Tx failed: {errorJson}");
                throw new Exception("Transaction failed.");
            }
            return s.Result;
        }
        /// Sends SOL on-chain directly from the master account to an external address.
        /// Useful for withdrawals if you prefer using the main hot wallet.
        public async Task<string> SendFromMasterAsync(string toPubKey, decimal solAmount)
        {
            if (!_wallets.TryGetValue(_masterUserId, out var master))
                throw new InvalidOperationException("Master wallet not found");

            // Unprotect and reconstruct master account
            var rawPriv = _protector.Unprotect(master.EncryptedPrivateKey);
            var acct = new Account(rawPriv, master.PublicKey);

            // Prepare and send transaction similarly
            var lamports = (ulong)(solAmount * LamportsPerSol);
            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;

            var tx = new TransactionBuilder()
                .SetRecentBlockHash(blockhash)
                .SetFeePayer(acct.PublicKey)
                .AddInstruction(
                    SystemProgram.Transfer(
                        acct.PublicKey,
                        new PublicKey(toPubKey),
                        lamports))
                .Build(acct);

            var resp = await _rpc.SendTransactionAsync(tx, skipPreflight: false, commitment: Commitment.Confirmed);
            if (resp.WasSuccessful)
            {
                Console.WriteLine($"Master sent {solAmount} SOL to {toPubKey}. Tx: {resp.Result}");
                return resp.Result;
            }
            Console.WriteLine($"Master send failed: {resp.Reason}");
            throw new Exception(resp.Reason);
        }
        /// Combines on-chain deposits and off-chain ledger balances for a user.
        public async Task<decimal> GetTotalBalanceAsync(int playerId)
        {
            // Get or create sub-account, sum on-chain and off-chain balances
            var pub = await GetOrCreateAccount(playerId);
            // 1) Query current fee schedule

            ulong feeBuffer = await getFeeBuffer();

            var onChain = await GetOnChainBalanceAsync(pub);
            decimal onSol;
            if (onChain > feeBuffer)
                onSol = (onChain - feeBuffer) / (decimal)LamportsPerSol;
            else
                onSol = (onChain) / (decimal)LamportsPerSol;

            using var ctx = _contextFactory.CreateDbContext();
            var off = await ctx.PlayerWallet.FindAsync(playerId);
            var offSol = off?.AvailableBalance ?? 0m;

            return onSol + offSol;
        }
        public async Task<decimal> GetOffChainBalanceAsync(int playerId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var off = await ctx.PlayerWallet.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (off == null)
            {

            }
            return off?.AvailableBalance ?? 0m;
        }
        private async Task<ulong> getFeeBuffer()
        {
            // 1) Optional: dynamically get accurate fee estimate
            /*
            var message = new TransactionBuilder()
                .SetFeePayer(new PublicKey(pub))
                .AddInstruction(SystemProgram.Transfer(...))
                .BuildMessage();
            var feeEstimate = await _rpc.GetFeeForMessageAsync(Convert.ToBase64String(message));
            if (feeEstimate.Value > feeBuffer)
                feeBuffer = feeEstimate.Value;
            */
            Console.WriteLine("Fetching fee buffer..."+ (await _rpc.GetMinimumBalanceForRentExemptionAsync(0)).Result * 2);
            return (await _rpc.GetMinimumBalanceForRentExemptionAsync(0)).Result*2;
        }
        public async Task<PlayerWallet?> EnsurePlayerWalletExists(int playerId, String WalletAddress = "none")
        {
            using var ctx = _contextFactory.CreateDbContext();
            var exists = ctx.PlayerWallet.Any(p => p.PlayerId == playerId);
            if (!exists)
            {
                if (WalletAddress == "none")
                {
                    ctx.PlayerWallet.Add(new PlayerWallet
                    {
                        PlayerId = playerId,
                        AddressType = "SOL",
                        WalletAddress = await GetOrCreateAccount(playerId, false, true),
                        AvailableBalance = 0m  // 0m is C# syntax for a decimal literal with value zero
                    });
                    await ctx.SaveChangesAsync();
                    await OffChainTransaction(playerId, 10.0m, "Signup Bonus", "", false, "");
                }   
            }
            //if ()ctx.PlayerWallet.transactions == null)
            //var sub = ctx.PlayerWallet.Include(p => p.Transactions).FirstOrDefaultAsync(p => p.PlayerId == playerId);
            var sub = await ctx.PlayerWallet.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            return sub;
        }
        /// Serializes in-memory wallet records back to the JSON storage file.
        private void PersistWallets()
        {
            var json = JsonSerializer.Serialize(_wallets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storageFile, json);
        }
        /// Sweeps any positive on-chain balances from sub-account addresses back to master.
        public async Task SweepAllSubAccountsAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();
            var masterPub = await GetOrCreateAccount(_masterUserId, true, true);

            foreach (var kv in _wallets.Where(w => !w.Value.IsMaster))
            {
                var playerId = kv.Key;
                var pub = kv.Value.PublicKey;
                ulong lamports = await GetOnChainBalanceAsync(pub);

                // 2) Apply a safety multiplier (e.g. ×2) for headroom

                ulong feeBuffer = await getFeeBuffer();

                if (lamports > feeBuffer)
                {
                    decimal sol = (lamports - feeBuffer) / (decimal)LamportsPerSol;

                    var txId = await SendOnChainAsync(playerId, masterPub, sol);
                    Console.WriteLine($"On-chain sweep successful: {sol} SOL, tx {txId}");

                    var sub = await EnsurePlayerWalletExists(playerId);                    

                    // Debit sub-account and credit master in DB
                    sub.AvailableBalance += sol;
                    // Record the sweep transaction
                    ctx.WalletTransaction.Add(new WalletTransaction
                    {
                        PlayerId = playerId,
                        Amount = sol,
                        BalanceAfter = sub.AvailableBalance,
                        Type = TransactionType.Sweep,
                        Description = $"On-chain sweep",
                        IsOnChain = true,
                        RoomCode = "",
                        txId = txId,
                        CreatedDate = DateTime.UtcNow
                    });

                    ctx.PlayerWallet.Update(sub);
                    await ctx.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine($"Skipped sweep, balance {lamports} ≤ fee buffer");
                }
            }
        }

        /// AES encryption helper methods for securely storing PlayerIDs.
        private readonly byte[] Key;
        private readonly byte[] IV;
        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
        public async Task<bool> deductGameFee(int playerId, int? tournamentId, string roomCode, bool isTournamentGame, decimal betAmount)
        {
            bool debited = false;
            var balance = await GetOffChainBalanceAsync(playerId);
            if ((betAmount > 0 && balance < betAmount) || betAmount < 0)
                return debited; // Not enough balance to deduct the game fee   
            if (isTournamentGame)
            {
                using var ctx = _contextFactory.CreateDbContext();

                var existingChallenger = await ctx.TournamentChallengers.FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.PlayerId == playerId);
                if (existingChallenger == null)
                {
                    ctx.TournamentChallengers.Add(new TournamentChallenger
                    {
                        PlayerId = playerId,
                        TournamentId = tournamentId
                    });
                    ctx.SaveChanges();
                    debited = await OffChainTransaction(playerId, -betAmount, "Tournament Fee", tournamentId.ToString(), false, roomCode);
                }
                else if (existingChallenger.Status == "FAILED")
                {
                    existingChallenger.RetryCount++;
                    existingChallenger.Status = "JOINEND";
                    ctx.SaveChanges();
                    debited = await OffChainTransaction(playerId, -betAmount, "Tournament Fee", tournamentId.ToString(), false, roomCode);
                }
                else
                {
                    debited = true;
                }
            }
            else
            {
                debited = await OffChainTransaction(playerId, -betAmount, "Game Fee", "", false, roomCode);
            }
            return debited;
        }

        internal async Task<string> SendSolToExternalWallet(Player player, string destination, decimal amountInSol)
        {
            try
            {
                var txSignature = await SendFromMasterAsync(destination, amountInSol);

                // 0) Check total balance (on-chain + off-chain)
                var totalBalance = await GetTotalBalanceAsync(player.PlayerId);
                if (totalBalance < amountInSol)
                {
                    Console.WriteLine($"Withdrawal failed: insufficient total balance for {player.PlayerId}. Have {totalBalance} SOL, tried {amountInSol} SOL.");
                    return "INSUFFICIENT_FUNDS";
                }

                // 1) Debit from off-chain ledger (credit master balance)
                var debited = await OffChainTransaction(player.PlayerId, -amountInSol, "Withdraw", txSignature, true);
                if (!debited)
                {
                    Console.WriteLine($"Withdrawal failed: insufficient off-chain funds for {player.PlayerId}");
                    return "INSUFFICIENT_OFFCHAIN";
                }

                // 2) Send on-chain using master wallet
                Console.WriteLine($"Withdrawal of {amountInSol} SOL for {player.PlayerId} sent from master. Tx: {txSignature}");
                return txSignature;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during withdrawal for {player.PlayerId}: {ex.Message}");
                return "ERROR";
            }
        }
    }
}