using LudoServer.Data;
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

// Replace your existing CryptoHelper registration with this:
builder.Services.AddSingleton<CryptoHelper>(sp =>
{
    var env = sp.GetRequiredService<IHostEnvironment>();
    // "Data/wallets.json" is relative to ContentRoot, i.e. your project folder
    return new CryptoHelper(
        env,
        network: "DevNet",
        relativeStoragePath: "Data/wallets.json"
    );
});
// Build the app
var app = builder.Build();


// Use CORS policy
app.UseCors("AllowAnyOrigin");

// Map SignalR hubs
app.MapHub<LudoHub>("/LudoHub");
app.MapHub<LudoHub>("/TournamentHub");
app.MapHub<AdvancedChatHub>("/advanced");

// Run the app
app.Run();
