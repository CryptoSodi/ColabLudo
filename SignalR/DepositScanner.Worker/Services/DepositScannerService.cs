using System.Text.Json;
using LudoServer.Data;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Payments;

namespace DepositScanner.Worker.Services
{
    /// <summary>
    /// Polls recent LUDC deposits and credits matched player wallets.
    /// Runs independently from the web server so scanner failures do not affect SignalR availability.
    /// </summary>
    public class DepositScannerService(IServiceScopeFactory scopeFactory, CryptoHelper cryptoHelper) : BackgroundService
    {
        private readonly string _signatureFile = Path.Combine(AppContext.BaseDirectory, "last_signature.txt");
        private string? _lastProcessedSignature;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await SeedSignatureFileFromServerAsync();

            using var scope = scopeFactory.CreateScope();
            var ludcProvider = scope.ServiceProvider.GetRequiredService<LudcPaymentProvider>();
            _lastProcessedSignature = await LoadLastSignature();

            Console.WriteLine($"[DepositScanner] Signature file path: {_signatureFile}");
            Console.WriteLine($"[DepositScanner] Loaded signature: {_lastProcessedSignature ?? "NONE"}");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (string.IsNullOrEmpty(_lastProcessedSignature))
                    {
                        Console.WriteLine("[DepositScanner] Calling InitializeLatestSignature...");
                        var initialSig = await ludcProvider.InitializeLatestSignature();
                        if (!string.IsNullOrEmpty(initialSig))
                        {
                            await UpdateLastSignature(initialSig);
                            Console.WriteLine($"[DepositScanner] Initialized signature marker: {_lastProcessedSignature}");
                        }
                        else
                        {
                            Console.WriteLine("[DepositScanner] Network unavailable or no signatures found. Retrying in 30s...");
                        }
                    }

                    if (!string.IsNullOrEmpty(_lastProcessedSignature))
                        await ScanDeposits();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DepositScanner] Network or execution error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task ScanDeposits()
        {
            using var scope = scopeFactory.CreateScope();
            var ludcProvider = scope.ServiceProvider.GetRequiredService<LudcPaymentProvider>();
            using var ctx = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LudoDbContext>>().CreateDbContext();

            Console.WriteLine($"[DepositScanner] Calling GetRecentDeposits with marker: {_lastProcessedSignature}");
            var result = await ludcProvider.GetRecentDeposits(_lastProcessedSignature);
            Console.WriteLine($"[DepositScanner] Network response: deposits={result.Deposits.Count}, latestSeen={result.LatestSeenSignature ?? "NONE"}");

            if (result.Deposits.Count > 0)
            {
                foreach (var deposit in result.Deposits)
                {
                    Console.WriteLine(JsonSerializer.Serialize(deposit));

                    bool alreadyProcessed = await ctx.WalletTransaction.AnyAsync(x => x.txId == deposit.Signature);
                    Console.WriteLine($"[DepositScanner] Deposit signature {deposit.Signature} alreadyProcessed={alreadyProcessed}");
                    if (alreadyProcessed)
                        continue;

                    var wallet = await ctx.PlayerWallet.FirstOrDefaultAsync(w => w.WalletAddress == deposit.WalletAddress && w.AddressType == "LUDC");
                    Console.WriteLine($"[DepositScanner] Wallet lookup for {deposit.WalletAddress}: {(wallet == null ? "NOT_FOUND" : $"Player {wallet.PlayerId}")}");
                    if (wallet == null)
                        continue;

                    if (wallet.PlayerId == 1)
                    {
                        Console.WriteLine($"[DepositScanner] Deposit matched MASTER wallet. Signature={deposit.Signature}, Amount={deposit.Amount}");
                    }

                    await cryptoHelper.ApplyOffChainLedger(ctx, wallet, deposit.Amount, "Deposit", deposit.Signature, true, "");
                    await ctx.SaveChangesAsync();

                    Console.WriteLine($"[DepositScanner] Deposit detected: Player {wallet.PlayerId} + {deposit.Amount} LUDC");
                }
            }

            if (!string.IsNullOrEmpty(result.LatestSeenSignature))
            {
                await UpdateLastSignature(result.LatestSeenSignature);
                Console.WriteLine($"[DepositScanner] Marker advanced to: {_lastProcessedSignature}");
            }
        }

        private async Task<string?> LoadLastSignature()
        {
            if (!File.Exists(_signatureFile))
                return null;

            return (await File.ReadAllTextAsync(_signatureFile)).Trim();
        }

        private async Task UpdateLastSignature(string signature)
        {
            if (_lastProcessedSignature == signature)
                return;

            _lastProcessedSignature = signature;
            await File.WriteAllTextAsync(_signatureFile, signature);
            Console.WriteLine($"[DepositScanner] Signature updated: {_lastProcessedSignature}");
        }

        private async Task SeedSignatureFileFromServerAsync()
        {
            if (File.Exists(_signatureFile))
                return;

            var serverSignaturePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "SignalR.Server",
                "last_signature.txt"));

            if (!File.Exists(serverSignaturePath))
                return;

            var signature = (await File.ReadAllTextAsync(serverSignaturePath)).Trim();
            if (string.IsNullOrWhiteSpace(signature))
                return;

            await File.WriteAllTextAsync(_signatureFile, signature);
            Console.WriteLine($"[DepositScanner] Seeded worker signature file from: {serverSignaturePath}");
        }
    }
}
