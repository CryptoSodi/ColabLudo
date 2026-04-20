using LudoServer.Data;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Payments;
using System.Text.Json;

namespace SignalR.Server.Services
{    /// <summary>
     /// Background service that scans the blockchain for deposits
     /// of the LUDC token and credits player balances.
     ///
     /// This service works by:
     /// 1. Fetching transactions involving the LUDC mint
     /// 2. Parsing token transfers
     /// 3. Detecting transfers to player wallets
     /// 4. Crediting the off-chain ledger
     ///
     /// This avoids scanning thousands of wallets individually
     /// and keeps RPC usage extremely low.
     /// </summary>
    public class DepositScannerService(IServiceScopeFactory _scopeFactory, CryptoHelper _cryptoHelper) : BackgroundService
    {
        // Last processed transaction signature
        private string? _lastProcessedSignature;
        /// <summary>
        /// Main background loop
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var ludcProvider = scope.ServiceProvider.GetRequiredService<LudcPaymentProvider>();
            _lastProcessedSignature = await LoadLastSignature();

            Console.WriteLine($"[DepositScanner] Loaded Signature from file: {_lastProcessedSignature ?? "NONE"}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Safe Initialization Loop
                    // If network is down on startup, this will safely retry every 30s instead of crashing
                    if (string.IsNullOrEmpty(_lastProcessedSignature))
                    {
                        var initialSig = await ludcProvider.InitializeLatestSignature();
                        if (!string.IsNullOrEmpty(initialSig))
                        {
                            await UpdateLastSignature(initialSig);
                            Console.WriteLine($"[DepositScanner] Network Active. INITIALIZED SIGNATURE : {_lastProcessedSignature}");
                        }
                        else
                        {
                            Console.WriteLine("[DepositScanner] Network unavailable or no signatures found. Retrying in 30s...");
                        }
                    }

                    // 2. Safe Scanning Loop
                    if (!string.IsNullOrEmpty(_lastProcessedSignature))
                    {
                        await ScanDeposits();
                    }
                }
                catch (Exception ex)
                {
                    // Catches all network timeouts, DNS failures, and database locks
                    // Ensures the background service NEVER dies
                    Console.WriteLine($"[DepositScanner] Network or Execution error: {ex.Message}");
                }

                // Delay before the next poll (30 seconds)
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        /// <summary>
        /// Scan blockchain for new deposits
        /// </summary>
        private async Task ScanDeposits()
        {
            using var scope = _scopeFactory.CreateScope();
            var ludcProvider = scope.ServiceProvider.GetRequiredService<LudcPaymentProvider>();
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LudoDbContext>>().CreateDbContext();

            // Ask the provider for new deposits (result includes the latest seen signature for progression)
            var result = await ludcProvider.GetRecentDeposits(_lastProcessedSignature);

            if (result.Deposits.Count > 0)
            {
                foreach (var deposit in result.Deposits)
                {
                    Console.WriteLine(JsonSerializer.Serialize(deposit));
                    // Prevent double-credit exploit
                    bool alreadyProcessed = await ctx.WalletTransaction.AnyAsync(x => x.txId == deposit.Signature);

                    if (alreadyProcessed)
                        continue;

                    // Find the player wallet
                    var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.WalletAddress == deposit.WalletAddress && w.AddressType == "LUDC");

                    if (wallet == null)
                        continue;

                    // Credit off-chain balance
                    await _cryptoHelper.ApplyOffChainLedger(ctx, wallet, deposit.Amount, "Deposit", deposit.Signature, true, "");
                    await ctx.SaveChangesAsync();

                    Console.WriteLine($"Deposit detected: Player {wallet.PlayerId} + {deposit.Amount} LUDC");
                }
            }

            // --- PERMANENT PROGRESSION FIX ---
            // Update last processed signature marker if we saw ANY signatures on the blockchain
            // This ensures we don't scan the same "junk" transactions every 30 seconds
            if (!string.IsNullOrEmpty(result.LatestSeenSignature))
            {
                await UpdateLastSignature(result.LatestSeenSignature);
                Console.WriteLine($"[DepositScanner] Marker advanced to: {_lastProcessedSignature}");
            }
        }
        private const string SignatureFile = "last_signature.txt";
        private async Task<string?> LoadLastSignature()
        {
            if (!File.Exists(SignatureFile))
                return null;

            return (await File.ReadAllTextAsync(SignatureFile)).Trim();
        }
        private async Task UpdateLastSignature(string signature)
        {
            if (_lastProcessedSignature == signature)
                return;

            _lastProcessedSignature = signature;

            await File.WriteAllTextAsync(SignatureFile, signature);

            Console.WriteLine($"SIGNATURE UPDATED : {_lastProcessedSignature}");
        }
    }
}
