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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanDeposits();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DepositScanner error: {ex.Message}");
                }

                // scan every 10 seconds
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

            // Ask the provider for new deposits
            var deposits = await ludcProvider.GetRecentDeposits(_lastProcessedSignature);

            if (deposits.Count == 0)
                return;
            foreach (var deposit in deposits)
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
            // update last processed signature
            _lastProcessedSignature = deposits.Last().Signature;
        }
    }
}
