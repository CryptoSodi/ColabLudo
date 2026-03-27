using LudoServer.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;
using SignalR.Server.Interfaces;
using SignalR.Server.Payments;
using SignalR.Server.Services;

var builder = WebApplication.CreateBuilder(args);
const int masterUserId = 1;
const bool debug = true;
const string LUDC_MINT_ADDRESS = debug ? "8Abr4aSqHbqUNK1ubRVfcdnAhS3RjmYRPDf11dt7pcfW" : "JSXWEi4ZXJkrkqWQg4UjUPzpmpYYFxzLmBuADh5cyai" ;
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddUserSecrets<Program>().AddEnvironmentVariables();

string dbstring = builder.Configuration.GetConnectionString("DefaultConnection");
string purpose = builder.Configuration.GetConnectionString("purpose");

//Add CORS policy
builder.Services.AddCors(o =>
{// Allow ANY origin (localhost, IP, external)
    o.AddPolicy("AllowAnyOrigin", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
// Load secrets (local dev) and environment variables (for production)

// Add SignalR services
builder.Services.AddSignalR(o => o.StatefulReconnectBufferSize = 100_000);

builder.Services.AddDbContextFactory<LudoDbContext>(options => options.UseSqlServer(dbstring).EnableSensitiveDataLogging(false));// Turn off verbose logging

builder.Services.AddScoped<FriendsService>();
builder.Services.AddTransient<TournamentService>();
builder.Services.AddScoped<DailyBonusService>();
builder.Services.AddScoped<GoogleAuthService>();
builder.Services.AddScoped<UtilService>();

builder.Services.AddSingleton<SolPaymentProvider>(sp => {
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    return new SolPaymentProvider(contextFactory, sp.GetDataProtectionProvider(), masterUserId, debug, purpose);
});
builder.Services.AddSingleton<LudcPaymentProvider>(sp => {
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    return new LudcPaymentProvider(contextFactory, sp.GetDataProtectionProvider(), sp.GetRequiredService<SolPaymentProvider>(), masterUserId, debug, purpose, LUDC_MINT_ADDRESS);
});

builder.Services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<SolPaymentProvider>());
builder.Services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<LudcPaymentProvider>());
builder.Services.AddSingleton<PaymentProviderFactory>();

builder.Services.AddHostedService<DepositScannerService>();
builder.Services.AddHostedService<PlayerCleanupService>();
builder.Services.AddHostedService<TournamentBackgroundWorker>();

// 1) Register Data Protection so IDataProtectionProvider can be injected:
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(@"C:\repos\LudoKeyRing")).SetApplicationName("LudoServer");

builder.Services.AddSingleton<DatabaseManager>(sp => {
    var hubContext = sp.GetRequiredService<IHubContext<LudoHub>>();
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var crypto = sp.GetRequiredService<CryptoHelper>();
    var utilService = sp.GetRequiredService<UtilService>();
    var dm = new DatabaseManager(hubContext, contextFactory, crypto, utilService);
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