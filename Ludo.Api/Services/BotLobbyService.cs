using SignalR.Server;

namespace Ludo.Api.Services;

public sealed class BotLobbyService(DatabaseManager databaseManager, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = configuration.GetValue("BotLobby:Enabled", true);
        if (!enabled)
            return;

        var botCount = configuration.GetValue("BotLobby:BotCount", 16);
        var seedRoomCount = configuration.GetValue("BotLobby:SeedRoomCount", 4);
        var fillDelaySeconds = configuration.GetValue("BotLobby:FillDelaySeconds", 30);
        var pollSeconds = configuration.GetValue("BotLobby:PollSeconds", 15);
        var walletFloat = configuration.GetValue("BotLobby:WalletFloat", 1000m);
        var avatarBaseUrl = (configuration["BotLobby:AvatarBaseUrl"] ?? "https://www.ludocities.com/images/avatars").TrimEnd('/');
        var betAmounts = ParseDecimals(configuration["BotLobby:SeedBetAmounts"], [1m, 2m, 5m]);
        var gameTypes = ParseStrings(configuration["BotLobby:SeedGameTypes"], ["2"]);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await databaseManager.EnsureBotPlayersAsync(botCount, walletFloat, avatarBaseUrl);
                await databaseManager.SeedBotRoomsAsync(seedRoomCount, betAmounts, gameTypes);
                await databaseManager.FillExpiredRoomsWithBotsAsync(TimeSpan.FromSeconds(fillDelaySeconds));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BotLobbyService] Error={ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
        }
    }

    private static List<decimal> ParseDecimals(string? value, List<decimal> fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var parsed = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => decimal.TryParse(x, out var amount) ? amount : (decimal?)null)
            .Where(x => x.HasValue && x.Value >= 0)
            .Select(x => x!.Value)
            .ToList();

        return parsed.Count == 0 ? fallback : parsed;
    }

    private static List<string> ParseStrings(string? value, List<string> fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var parsed = value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return parsed.Count == 0 ? fallback : parsed;
    }
}
