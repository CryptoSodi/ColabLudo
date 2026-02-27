using LudoServer.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;
using SignalR.Server.Services;

var builder = WebApplication.CreateBuilder(args);
//Add CORS policy
builder.Services.AddCors(o =>
{// Allow ANY origin (localhost, IP, external)
    o.AddPolicy("AllowAnyOrigin", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});
// Load secrets (local dev) and environment variables (for production)
builder.Configuration
.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddUserSecrets<Program>().AddEnvironmentVariables();
// Add SignalR services
builder.Services.AddSignalR(o => o.StatefulReconnectBufferSize = 100_000);

builder.Services.AddDbContextFactory<LudoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging(false));// Turn off verbose logging

builder.Services.AddHostedService<PlayerCleanupService>();
builder.Services.AddScoped<FriendsService>();
builder.Services.AddScoped<TournamentService>();
builder.Services.AddScoped<DailyBonusService>();
builder.Services.AddScoped<GoogleAuthService>();
builder.Services.AddScoped<UtilService>();

// 1) Register Data Protection so IDataProtectionProvider can be injected:
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\repos\LudoKeyRing")) 
    .SetApplicationName("LudoServer");

builder.Services.AddSingleton<DatabaseManager>(sp => {
    var hubContext = sp.GetRequiredService<IHubContext<LudoHub>>();
    var contextFactory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var crypto = sp.GetRequiredService<CryptoHelper>();
    var utilService = sp.GetRequiredService<UtilService>();
    var dm = new DatabaseManager(hubContext, contextFactory, crypto, utilService);
    // Call LoadData in background
    Task.Run(dm.LoadData);
    return dm;
});

// Replace your existing CryptoHelper registration with this:
builder.Services.AddSingleton<CryptoHelper>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    var factory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var protector = sp.GetRequiredService<IDataProtectionProvider>();
    // Use the factory to create a new DbContext instance
    const int masterUserId = 1; // your chosen ID

    try
    {
        return new CryptoHelper(
            factory,
            env,
            protector,
            masterUserId,
            network: "MainNetBeta",
            protectorKey: "CryptoHelper.WalletProtector"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine("Failed to create CryptoHelper: " + ex);
        throw;
    }
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