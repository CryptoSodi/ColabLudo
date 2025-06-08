using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SignalR.Server
{
    /// <summary>
    /// Represents metadata for an on-chain wallet, either master or sub-account.
    /// </summary>
    public class Wallet
    {
        /// <summary>User or manager identifier.</summary>
        public string UserId { get; set; }

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
        private readonly IDbContextFactory<LudoDbContext> _dbFactory; // EF factory for ledger DB
        private readonly IDataProtector _protector;             // Data protector for encrypt/decrypt
        private readonly Dictionary<string, Wallet> _wallets;   // In-memory wallet cache
        private readonly string _masterUserId;                  // Identifier for the master wallet

        /// <summary>
        /// Constructor sets up RPC client, loads or creates wallets, and ensures
        /// master wallet is present in the JSON store.
        /// </summary>
        public CryptoHelper(
            IDbContextFactory<LudoDbContext> dbFactory,
            IHostEnvironment env,
            IDataProtectionProvider dataProtectionProvider,
            string masterUserId,
            string network = "MainNetBeta",
            string relativeStoragePath = "wallets.json")
        {
            _dbFactory = dbFactory;
            // Create a protector instance scoped to this class
            _protector = dataProtectionProvider.CreateProtector("CryptoHelper.WalletProtector");
            _masterUserId = masterUserId;

            // Initialize Solana RPC for MainNet or DevNet
            var cluster = network.Equals("DevNet", StringComparison.OrdinalIgnoreCase)
                ? Cluster.DevNet
                : Cluster.MainNet;
            _rpc = ClientFactory.GetClient(cluster);

            // Ensure the folder for storing JSON exists
            var dataFolder = Path.Combine(env.ContentRootPath,
                Path.GetDirectoryName(relativeStoragePath) ?? string.Empty);
            Directory.CreateDirectory(dataFolder);
            _storageFile = Path.Combine(env.ContentRootPath, relativeStoragePath);

            // Load existing wallets or initialize empty dictionary
            if (File.Exists(_storageFile))
            {
                var json = File.ReadAllText(_storageFile);
                _wallets = JsonSerializer.Deserialize<Dictionary<string, Wallet>>(json)
                          ?? new Dictionary<string, Wallet>();
            }
            else
            {
                _wallets = new Dictionary<string, Wallet>();
            }

            // Ensure the master (hot) wallet exists; if not, generate and persist it.
            if (!_wallets.ContainsKey(_masterUserId))
            {
                var account = new Account();
                // Base58-encode the raw private key
                var rawPriv = new Base58Encoder().EncodeData(account.PrivateKey);
                // Protect (encrypt) the private key before storing
                var cipherPriv = _protector.Protect(rawPriv);

                _wallets[_masterUserId] = new Wallet
                {
                    UserId = _masterUserId,
                    EncryptedPrivateKey = cipherPriv,            // store only encrypted blob
                    PublicKey = account.PublicKey.Key,
                    IsMaster = true
                };
                PersistWallets();                              // Save JSON to disk
                Console.WriteLine($"Master wallet created: {account.PublicKey.Key}");
            }
        }

        /// <summary>
        /// Returns the master wallet's public key (for deposits).
        /// </summary>
        public Task<string> GetOrCreateMasterAccountAsync(CancellationToken cancellationToken = default)
        {
            if (_wallets.TryGetValue(_masterUserId, out var w))
                return Task.FromResult(w.PublicKey);
            throw new InvalidOperationException("Master wallet missing");
        }

        /// <summary>
        /// Returns an existing sub-account public key, or generates one if missing.
        /// </summary>
        public Task<string> GetOrCreateSubAccountAsync(string userId, CancellationToken cancellationToken = default)
        {
            // If already exists, return the public key
            if (_wallets.TryGetValue(userId, out var w) && w.PublicKey != null)
                return Task.FromResult(w.PublicKey);

            var account = new Account();
            // Encode and encrypt the private key
            var rawPriv = new Base58Encoder().EncodeData(account.PrivateKey);
            var cipherPriv = _protector.Protect(rawPriv);
            var pub = account.PublicKey.Key;

            _wallets[userId] = new Wallet
            {
                UserId = userId,
                EncryptedPrivateKey = cipherPriv,
                PublicKey = pub,
                IsMaster = false
            };

            PersistWallets(); // Persist updated store
            Console.WriteLine($"Sub-account created: {userId} -> {pub}");
            return Task.FromResult(pub);
        }

        /// <summary>
        /// Moves SOL off-chain from master ledger to a sub-account ledger.
        /// </summary>
        public async Task<bool> AllocateOffChainAsync(string subUserId, decimal solAmount)
        {
            using var ctx = _dbFactory.CreateDbContext();
            var master = await ctx.PlayerWallets.FindAsync(_masterUserId);
            if (master == null || master.AvailableBalance < solAmount)
                return false;

            // Debit master and credit sub-account in DB
            master.AvailableBalance -= solAmount;
            var sub = await ctx.PlayerWallets.FindAsync(subUserId);
            if (sub == null)
                ctx.PlayerWallets.Add(new PlayerWallet
                {
                    PlayerId = subUserId,
                    AvailableBalance = solAmount
                });
            else
                sub.AvailableBalance += solAmount;

            ctx.PlayerWallets.Update(master);
            await ctx.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Returns SOL off-chain from sub-account ledger back to master ledger.
        /// </summary>
        public async Task<bool> RefundOffChainAsync(string subUserId, decimal solAmount)
        {
            using var ctx = _dbFactory.CreateDbContext();
            var sub = await ctx.PlayerWallets.FindAsync(subUserId);
            var master = await ctx.PlayerWallets.FindAsync(_masterUserId);
            if (sub == null || master == null || sub.AvailableBalance < solAmount)
                return false;

            // Debit sub-account and credit master in DB
            sub.AvailableBalance -= solAmount;
            master.AvailableBalance += solAmount;
            ctx.PlayerWallets.Update(sub);
            ctx.PlayerWallets.Update(master);
            await ctx.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Queries the on-chain SOL balance (in lamports) for a given public key.
        /// </summary>
        public async Task<ulong> GetOnChainBalanceAsync(string pubKey, CancellationToken cancellationToken = default)
        {
            var r = await _rpc.GetBalanceAsync(pubKey);
            if (r.WasSuccessful)
                return r.Result.Value;
            throw new Exception(r.Reason);
        }

        /// <summary>
        /// Builds, signs, and sends a SOL transfer transaction on-chain.
        /// </summary>
        public async Task<string> SendOnChainAsync(string fromUserId, string toPubKey, decimal solAmount)
        {
            if (!_wallets.TryGetValue(fromUserId, out var w))
                throw new InvalidOperationException("Wallet not found");

            // Decrypt (unprotect) the stored private key
            var rawPriv = _protector.Unprotect(w.EncryptedPrivateKey);
            var acct = new Account(rawPriv, w.PublicKey);

            // Convert SOL to lamports and fetch latest blockhash
            var lamports = (ulong)(solAmount * LamportsPerSol);
            var blockhash = (await _rpc.GetLatestBlockHashAsync()).Result.Value.Blockhash;

            // Build, sign, and send transaction
            var tx = new TransactionBuilder()
                .SetRecentBlockHash(blockhash)
                .SetFeePayer(acct.PublicKey)
                .AddInstruction(
                    SystemProgram.Transfer(
                        acct.PublicKey,
                        new PublicKey(toPubKey),
                        lamports))
                .Build(acct);

            var s = await _rpc.SendTransactionAsync(tx, false, Commitment.Confirmed);
            if (s.WasSuccessful)
                return s.Result;
            throw new Exception(s.Reason);
        }

        /// <summary>
        /// Sends SOL on-chain directly from the master account to an external address.
        /// Useful for withdrawals if you prefer using the main hot wallet.
        /// </summary>
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

        /// <summary>
        /// Combines on-chain deposits and off-chain ledger balances for a user.
        /// </summary>
        public async Task<decimal> GetTotalBalanceAsync(string userId, CancellationToken cancellationToken = default)
        {
            // Get or create sub-account, sum on-chain and off-chain balances
            var pub = await GetOrCreateSubAccountAsync(userId, cancellationToken);
            var onChain = await GetOnChainBalanceAsync(pub, cancellationToken);
            var onSol = onChain / (decimal)LamportsPerSol;

            using var ctx = _dbFactory.CreateDbContext();
            var off = await ctx.PlayerWallets.FindAsync(userId);
            var offSol = off?.AvailableBalance ?? 0m;

            return onSol + offSol;
        }

        /// <summary>
        /// Serializes in-memory wallet records back to the JSON storage file.
        /// </summary>
        private void PersistWallets()
        {
            var json = JsonSerializer.Serialize(_wallets, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_storageFile, json);
        }

        /// <summary>
        /// Sweeps any positive on-chain balances from sub-account addresses back to master.
        /// </summary>
        public async Task SweepAllSubAccountsAsync()
        {
            var masterPub = await GetOrCreateMasterAccountAsync();
            foreach (var kv in _wallets)
            {
                if (kv.Key == _masterUserId) continue;
                var userId = kv.Key;
                var pub = kv.Value.PublicKey;
                var bal = await GetOnChainBalanceAsync(pub);
                if (bal > 0)
                {
                    var sol = bal / (decimal)LamportsPerSol;
                    await SendOnChainAsync(userId, masterPub, sol);
                    Console.WriteLine($"Swept {sol} SOL from {userId} to master");
                }
            }
        }
    }

    /// <summary>
    /// Background worker that calls the sweeper method on a fixed interval.
    /// </summary>
    public class SweeperService : BackgroundService
    {
        private readonly CryptoHelper _cryptoHelper;

        /// <summary>
        /// ctor for the background sweeper service.
        /// </summary>
        public SweeperService(CryptoHelper cryptoHelper)
        {
            _cryptoHelper = cryptoHelper;
        }

        /// <summary>
        /// Executes SweepAllSubAccountsAsync every 5 minutes until cancellation.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("SweeperService starting...");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _cryptoHelper.SweepAllSubAccountsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sweeping sub-accounts: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
