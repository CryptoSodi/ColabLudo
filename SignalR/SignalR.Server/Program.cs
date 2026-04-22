using LudoServer.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;
using SignalR.Server.Interfaces;
using SignalR.Server.Payments;
using SignalR.Server.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("System.Net.Http.HttpClient.JupiterSwapService", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.JupiterSwapService.LogicalHandler", LogLevel.Warning);
builder.Logging.AddFilter("System.Net.Http.HttpClient.JupiterSwapService.ClientHandler", LogLevel.Warning);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(8085); // HTTP
    serverOptions.ListenAnyIP(8443, listenOptions => listenOptions.UseHttps()); // HTTPS
});

const int masterUserId = 1;
const bool debug = false; // 🚀 SWITCHED TO FALSE FOR PRODUCTION/MAINNET
const string LUDC_MINT_ADDRESS = debug ? "8Abr4aSqHbqUNK1ubRVfcdnAhS3RjmYRPDf11dt7pcfW" : "JSXWEi4ZXJkrkqWQg4UjUPzpmpYYFxzLmBuADh5cyai" ;
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddUserSecrets<Program>().AddEnvironmentVariables();

string dbstring = builder.Configuration.GetConnectionString("DefaultConnection");
string purpose = builder.Configuration.GetConnectionString("purpose");
string clientRpcUrl = builder.Configuration["Solana:ClientRpcUrl"] ?? string.Empty;

//Add CORS policy
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAnyOrigin", p => p
        .SetIsOriginAllowed(origin => true) // 🚀 Allows Web, Mobile, and Local instantly
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()); // Required for SignalR sessions
});
// Load secrets (local dev) and environment variables (for production)

// Add SignalR services
builder.Services.AddSignalR(o =>
{
    o.StatefulReconnectBufferSize = 100_000;
    // Manual deposit receipts are sent as base64 data URIs, which can exceed the default 32 KB limit.
    o.MaximumReceiveMessageSize = 5 * 1024 * 1024;
});

builder.Services.AddDbContextFactory<LudoDbContext>(options => options.UseSqlServer(dbstring).EnableSensitiveDataLogging(false));// Turn off verbose logging

builder.Services.AddScoped<FriendsService>();
builder.Services.AddTransient<TournamentService>();
builder.Services.AddScoped<DailyBonusService>();
builder.Services.AddScoped<GoogleAuthService>();
builder.Services.AddScoped<UtilService>();
builder.Services.AddHttpClient<JupiterSwapService>();

builder.Services.AddSingleton<SolPaymentProvider>(sp => {
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    return new SolPaymentProvider(contextFactory, sp.GetDataProtectionProvider(), masterUserId, debug, purpose);
});
builder.Services.AddSingleton<LudcPaymentProvider>(sp => {
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    return new LudcPaymentProvider(contextFactory, sp.GetDataProtectionProvider(), sp.GetRequiredService<SolPaymentProvider>(), masterUserId, debug, purpose, LUDC_MINT_ADDRESS, clientRpcUrl);
});

builder.Services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<SolPaymentProvider>());
builder.Services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<LudcPaymentProvider>());
builder.Services.AddSingleton<PaymentProviderFactory>();

builder.Services.AddHostedService<DepositScannerService>();
builder.Services.AddHostedService<PlayerCleanupService>();
builder.Services.AddHostedService<TournamentBackgroundWorker>();

builder.Services.AddScoped<DashboardHub>(sp => {
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var googleAuth = sp.GetRequiredService<GoogleAuthService>();
    var util = sp.GetRequiredService<UtilService>();
    var dbManager = sp.GetRequiredService<DatabaseManager>();
    var crypto = sp.GetRequiredService<CryptoHelper>();
    var ludc = sp.GetRequiredService<LudcPaymentProvider>();
    var jupiter = sp.GetRequiredService<JupiterSwapService>();
    return new DashboardHub(contextFactory, googleAuth, util, dbManager, crypto, ludc, jupiter, clientRpcUrl);
});

// 1) Register Data Protection so IDataProtectionProvider can be injected:
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(@"C:\repos\LudoKeyRing")).SetApplicationName("LudoServer");

builder.Services.AddSingleton<DatabaseManager>(sp => {
    var hubContext = sp.GetRequiredService<IHubContext<LudoHub>>();
    var dashboardHubContext = sp.GetRequiredService<IHubContext<DashboardHub>>();
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var crypto = sp.GetRequiredService<CryptoHelper>();
    var utilService = sp.GetRequiredService<UtilService>();
    var dm = new DatabaseManager(hubContext, dashboardHubContext, contextFactory, crypto, utilService);
    return dm;
});
// Replace your existing CryptoHelper registration with this:
builder.Services.AddSingleton<CryptoHelper>(sp =>
{
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var factory = sp.GetRequiredService<PaymentProviderFactory>();
    
    CryptoHelper ch = new CryptoHelper(contextFactory, factory);
    ch.EnsurePlayerWalletExists(masterUserId, CurrencyType.LUDC).GetAwaiter().GetResult();
    return ch;
});
// Build the app
var app = builder.Build();
// Use CORS policy
app.UseCors("AllowAnyOrigin");
// Map SignalR hubs
app.MapHub<LudoHub>("/LudoHub", options => { options.AllowStatefulReconnects = true; }); // Enable stateful reconnect
// Map SignalR hubs
app.MapHub<AdminHub>("/AdminHub");
app.MapHub<DashboardHub>("/DashboardHub");
// Run the app
try {
    app.Run();
}catch (Exception ex)
{
    Console.WriteLine($"Critical error: {ex}");
}

namespace SignalR.Server
{
    public partial class Program { }
}
