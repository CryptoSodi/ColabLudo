using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace SignalR.Server
{
    public class CryptoHelper
    {
        private readonly IRpcClient _rpc;
        private readonly string _storageFile;
        // userId → (privateBase58, publicBase58)
        private readonly Dictionary<string, WalletRecord> _wallets;

        private class WalletRecord
        {
            public string Priv { get; set; } = "";
            public string Pub { get; set; } = "";
        }

        /// <summary>
        /// env: used to locate a stable folder under ContentRootPath
        /// relativeStoragePath: e.g. "Data/wallets.json"
        /// </summary>
        public CryptoHelper(
            IHostEnvironment env,
            string network = "MainNetBeta",
            string relativeStoragePath = "wallets.json")
        {
            // 1) RPC client
            var cluster = network.Equals("DevNet", StringComparison.OrdinalIgnoreCase)
                ? Cluster.DevNet
                : Cluster.MainNet;
            _rpc = ClientFactory.GetClient(cluster);

            // 2) Compute absolute storage path
            var dataFolder = Path.Combine(env.ContentRootPath,
                                          Path.GetDirectoryName(relativeStoragePath) ?? "");
            Directory.CreateDirectory(dataFolder);

            _storageFile = Path.Combine(env.ContentRootPath,
                                        relativeStoragePath);

            // 3) Load existing wallets from disk (if any)
            if (File.Exists(_storageFile))
            {
                var json = File.ReadAllText(_storageFile);
                _wallets = JsonSerializer
                    .Deserialize<Dictionary<string, WalletRecord>>(json)
                          ?? new Dictionary<string, WalletRecord>();
            }
            else
            {
                _wallets = new Dictionary<string, WalletRecord>();
            }
        }


        /// <summary>
        /// Returns existing or new public deposit address for user.
        /// Persists both keys so the same pair is restored on restart.
        /// </summary>
        public async Task<string> GetOrCreateDepositAccountAsync(string userId)
        {
            if (_wallets.TryGetValue(userId, out var rec))
            {
                // Restore from Base58 strings
                var acc = new Account(rec.Priv, rec.Pub);
                return acc.PublicKey.Key;
            }

            // First-time: generate, persist, return
            var newAcc = new Account();
            var priv58 = new Base58Encoder().EncodeData(newAcc.PrivateKey);
            var pub58 = newAcc.PublicKey.Key;

            rec = new WalletRecord { Priv = priv58, Pub = pub58 };
            _wallets[userId] = rec;

            await File.WriteAllTextAsync(_storageFile,
                JsonSerializer.Serialize(_wallets,
                    new JsonSerializerOptions { WriteIndented = true }));

            return pub58;
        }

        /// <summary>Get balance in lamports (1 SOL = 1e9 lamports).</summary>
        public async Task<ulong> GetSolBalanceAsync(string pubAddr)
        {
            var resp = await _rpc.GetBalanceAsync(pubAddr);
            if (resp.WasSuccessful) return resp.Result.Value;
            throw new Exception($"RPC Error: {resp.Reason}");
        }

        /// <summary>
        /// Sends the specified amount of lamports from the user's stored wallet to a destination address.
        /// </summary>
        /// <param name="userId">The user ID whose stored account is the source.</param>
        /// <param name="destination">The base58-encoded public key to receive funds.</param>
        /// <param name="lamports">Amount in lamports (1 SOL = 1e9 lamports).</param>
        /// <returns>The transaction signature string.</returns>
        /// <summary>
        /// Sends the specified amount of lamports from the user's stored wallet to a destination address.
        /// </summary>
        
        public async Task<string> SendSolAsync(string userId, string destination, double solAmount)
        {
            // 1 SOL = 1_000_000_000 lamports
            var lamports = (ulong)(solAmount * 1_000_000_000d);
            // 1) Load the WalletRecord (must already exist)
            if (!_wallets.TryGetValue(userId, out var rec))
                throw new InvalidOperationException(
                    "User wallet not found. Call GetOrCreateDepositAccountAsync first.");

            // 2) Reconstruct the Account using the same Base58 strings you stored
            //    (matches your GetOrCreateDepositAccountAsync logic)
            var sender = new Account(rec.Priv, rec.Pub);

            // 3) Fetch a recent blockhash
            var blockHashResp = await _rpc.GetLatestBlockHashAsync();
            if (!blockHashResp.WasSuccessful)
                throw new Exception($"Failed to get recent blockhash: {blockHashResp.Reason}");
            var recentBlockHash = blockHashResp.Result.Value.Blockhash;

            // 4) Build and sign the transfer transaction
            // `tx` here is already a Base64 string
            var tx = new TransactionBuilder()
                .SetRecentBlockHash(recentBlockHash)
                .SetFeePayer(sender.PublicKey)
                .AddInstruction(
                    SystemProgram.Transfer(
                        fromPublicKey: sender.PublicKey,
                        toPublicKey: new PublicKey(destination),
                        lamports: lamports
                    )
                )
                .Build(sender); // returns Base64 string

            // 5) Send and confirm
            var sendResp = await _rpc.SendTransactionAsync(
                tx,                      // <— pass tx directly
                skipPreflight: false,
                commitment: Commitment.Confirmed
            );

            if (!sendResp.WasSuccessful)
                throw new Exception($"Transaction failed: {sendResp.Reason}");

            return sendResp.Result; // transaction signature
        }
    }
}
