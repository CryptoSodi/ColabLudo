using LudoServer.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;

var builder = WebApplication.CreateBuilder(args);

// Add CORS policy
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAnyOrigin", p => p
        .WithOrigins("null") // Origin of an HTML file opened in a browser
        .AllowAnyHeader()
        .AllowCredentials());
});

// Add SignalR services
builder.Services.AddSignalR();

builder.Services.AddDbContextFactory<LudoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
           .EnableSensitiveDataLogging(false) );// Turn off verbose logging

builder.Services.AddHostedService<SweeperService>();
// 1) Register Data Protection so IDataProtectionProvider can be injected:
builder.Services.AddDataProtection();
// Replace your existing CryptoHelper registration with this:
builder.Services.AddSingleton<CryptoHelper>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    
    var factory = sp.GetRequiredService<IDbContextFactory<LudoDbContext>>();
    var protector = sp.GetRequiredService<IDataProtectionProvider>();
    // Use the factory to create a new DbContext instance
    const string masterUserId = "MASTER_ACCOUNT"; // your chosen ID

    try
    {
        return new CryptoHelper(
            factory,
            env,
            protector,
            masterUserId,
            network: "DevNet",
            relativeStoragePath: "Data/wallets.json",
            protectorKey : "CryptoHelper.WalletProtector"
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
app.MapHub<LudoHub>("/LudoHub");
app.MapHub<AdvancedChatHub>("/advanced");

// Run the app
app.Run();
