using Ludo.Api.Services;
using LudoServer.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;
using SignalR.Server.Interfaces;
using SignalR.Server.Payments;
using SignalR.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8086);
    serverOptions.ListenAnyIP(8444, listenOptions =>
    {
        listenOptions.UseHttps();
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
    });
});

const int masterUserId = 1;
const bool debug = false;
const string ludcMintAddress = debug ? "8Abr4aSqHbqUNK1ubRVfcdnAhS3RjmYRPDf11dt7pcfW" : "JSXWEi4ZXJkrkqWQg4UjUPzpmpYYFxzLmBuADh5cyai";
var apiBasePath = AppContext.BaseDirectory;
var repoServerSettingsPath = Path.GetFullPath(Path.Combine(apiBasePath, "..", "..", "..", "..", "SignalR", "SignalR.Server", "appsettings.json"));

builder.Configuration
    .SetBasePath(apiBasePath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile(repoServerSettingsPath, optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

var dbstring = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection is not configured.");
var purpose = builder.Configuration.GetConnectionString("purpose")
    ?? throw new InvalidOperationException("purpose is not configured.");
var clientRpcUrl = builder.Configuration["Solana:ClientRpcUrl"] ?? string.Empty;

Console.WriteLine($"[Ludo.Api] BasePath: {apiBasePath}");
Console.WriteLine($"[Ludo.Api] Server settings path: {repoServerSettingsPath}");
Console.WriteLine($"[Ludo.Api] Loaded DefaultConnection: {!string.IsNullOrWhiteSpace(dbstring)}");
Console.WriteLine($"[Ludo.Api] Loaded purpose: {!string.IsNullOrWhiteSpace(purpose)}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAnyOrigin", policy => policy
        .SetIsOriginAllowed(_ => true)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
});

builder.Services.AddControllers()
    .ConfigureApplicationPartManager(manager =>
    {
        var apiAssemblyName = typeof(Ludo.Api.Program).Assembly.GetName().Name;
        var externalParts = manager.ApplicationParts
            .Where(part => part.Name != apiAssemblyName)
            .ToList();

        foreach (var part in externalParts)
            manager.ApplicationParts.Remove(part);
    });
builder.Services.AddSignalR();
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
builder.Services.AddScoped<DailyBonusService>();
builder.Services.AddScoped<ApiPlayerContext>();
builder.Services.AddScoped<FriendsService>();
builder.Services.AddScoped<GoogleAuthService>();
builder.Services.AddScoped<TournamentService>();
builder.Services.AddSingleton<UtilService>();
builder.Services.AddSingleton<PlayerPresenceTracker>();
builder.Services.AddHostedService<PlayerInactivityCleanupService>();
builder.Services.AddHttpClient<JupiterSwapService>();
builder.Services.AddSingleton<DatabaseManager>(sp =>
{
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var crypto = sp.GetRequiredService<CryptoHelper>();
    var utilService = sp.GetRequiredService<UtilService>();
    return new DatabaseManager(contextFactory, crypto, utilService);
});

var app = builder.Build();

app.UseCors("AllowAnyOrigin");
app.MapControllers();
app.MapHub<Ludo.Api.Hubs.LudoHub>("/hubs/ludohub");

app.Run();

namespace Ludo.Api
{
    public partial class Program { }
}
