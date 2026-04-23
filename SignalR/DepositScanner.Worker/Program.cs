using LudoServer.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SignalR.Server.Interfaces;
using SignalR.Server.Payments;
using DepositScanner.Worker.Services;
using Microsoft.Extensions.Configuration;

const int masterUserId = 1;
const bool debug = false;
const string ludcMintAddress = debug ? "8Abr4aSqHbqUNK1ubRVfcdnAhS3RjmYRPDf11dt7pcfW" : "JSXWEi4ZXJkrkqWQg4UjUPzpmpYYFxzLmBuADh5cyai";

var builder = Host.CreateApplicationBuilder(args);
var workerBasePath = AppContext.BaseDirectory;
var repoServerSettingsPath = Path.GetFullPath(Path.Combine(workerBasePath, "..", "..", "..", "..", "SignalR.Server", "appsettings.json"));

builder.Configuration
    .SetBasePath(workerBasePath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile(repoServerSettingsPath, optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

string dbstring = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");
string purpose = builder.Configuration.GetConnectionString("purpose")
    ?? throw new InvalidOperationException("purpose is not configured.");
string clientRpcUrl = builder.Configuration["Solana:ClientRpcUrl"] ?? string.Empty;

Console.WriteLine($"[DepositScanner.Worker] BasePath: {workerBasePath}");
Console.WriteLine($"[DepositScanner.Worker] Server settings path: {repoServerSettingsPath}");
Console.WriteLine($"[DepositScanner.Worker] Loaded DefaultConnection: {!string.IsNullOrWhiteSpace(dbstring)}");
Console.WriteLine($"[DepositScanner.Worker] Loaded purpose: {!string.IsNullOrWhiteSpace(purpose)}");

builder.Services.AddDbContextFactory<LudoDbContext>(options =>
    options.UseSqlServer(dbstring).EnableSensitiveDataLogging(false));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\repos\LudoKeyRing"))
    .SetApplicationName("LudoServer");

builder.Services.AddSingleton<SolPaymentProvider>(sp =>
{
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    return new SolPaymentProvider(contextFactory, sp.GetRequiredService<IDataProtectionProvider>(), masterUserId, debug, purpose);
});

builder.Services.AddSingleton<LudcPaymentProvider>(sp =>
{
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    return new LudcPaymentProvider(
        contextFactory,
        sp.GetRequiredService<IDataProtectionProvider>(),
        sp.GetRequiredService<SolPaymentProvider>(),
        masterUserId,
        debug,
        purpose,
        ludcMintAddress,
        clientRpcUrl);
});

builder.Services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<SolPaymentProvider>());
builder.Services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<LudcPaymentProvider>());
builder.Services.AddSingleton<PaymentProviderFactory>();
builder.Services.AddSingleton<CryptoHelper>(sp =>
{
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var factory = sp.GetRequiredService<PaymentProviderFactory>();

    var crypto = new CryptoHelper(contextFactory, factory);
    crypto.EnsurePlayerWalletExists(masterUserId, CurrencyType.LUDC).GetAwaiter().GetResult();
    return crypto;
});

builder.Services.AddHostedService<DepositScannerService>();

var host = builder.Build();
await host.RunAsync();
