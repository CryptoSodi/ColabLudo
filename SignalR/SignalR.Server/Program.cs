using LudoServer.Data;
using Microsoft.EntityFrameworkCore;
using SignalR.Server;
using SignalR.Server.Data;

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

// Configure DbContext with SQL Server
builder.Services.AddDbContext<LudoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Build the app
var app = builder.Build();

// Use CORS policy
app.UseCors("AllowAnyOrigin");

// Map SignalR hubs
app.MapHub<LudoHub>("/LudoHub");
app.MapHub<SimpleChatHub>("/simple");
app.MapHub<AdvancedChatHub>("/advanced");

// Run the app
app.Run();
