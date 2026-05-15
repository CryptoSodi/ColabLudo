using Ludo.Api.Controllers;
using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;
using SignalR.Server;

namespace Ludo.Api.Hubs;

public abstract class LudoHubGameplayBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    IDbContextFactory<LudoDbContext> contextFactory) : LudoHubClockBase(playerContext, databaseManager, hubContext)
{
    private readonly IDbContextFactory<LudoDbContext> _contextFactory = contextFactory;

    public async Task JoinRoom(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            Console.WriteLine($"[LudoHub] JoinRoom ignored. ConnectionId={Context.ConnectionId}, Reason=EmptyRoomCode");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomCode);
        var token = GetAuthToken();
        Console.WriteLine($"[LudoHub] JoinRoom. ConnectionId={Context.ConnectionId}, RoomCode={roomCode}, HasAuthToken={!string.IsNullOrWhiteSpace(token)}");
    }

    public async Task LeaveRoom(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            Console.WriteLine($"[LudoHub] LeaveRoom ignored. ConnectionId={Context.ConnectionId}, Reason=EmptyRoomCode");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomCode);
        var token = GetAuthToken();
        Console.WriteLine($"[LudoHub] LeaveRoom. ConnectionId={Context.ConnectionId}, RoomCode={roomCode}, HasAuthToken={!string.IsNullOrWhiteSpace(token)}");
    }

    public async Task<GameCommand> Send(GameplaySendRequest request)
    {
        if (request.Command == null || string.IsNullOrWhiteSpace(request.CommandType) || string.IsNullOrWhiteSpace(request.RoomCode))
            return new GameCommand { Result = "Error: command, commandType, and roomCode are required." };

        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token))
            return new GameCommand { Result = "Error: Missing auth token." };

        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        var player = await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
        if (player == null)
        {
            Console.WriteLine($"[LudoHub] Send auth failed. ConnectionId={Context.ConnectionId}, CommandType={request.CommandType}");
            return new GameCommand { Result = "Error: Unauthorized." };
        }

        if (!DatabaseManager._gameRooms.TryGetValue(request.RoomCode, out var gameRoom))
            return new GameCommand { Result = "Error: Room not found." };

        if (gameRoom.engine == null)
            return new GameCommand { Result = "Error: Engine not initialized." };

        var authToken = player.AuthToken ?? string.Empty;
        if (string.IsNullOrWhiteSpace(authToken))
            return new GameCommand { Result = "Error: Missing player auth token." };

        Console.WriteLine($"[LudoHub] Send start. ConnectionId={Context.ConnectionId}, CommandType={request.CommandType}, RoomCode={request.RoomCode}");
        GameCommand? result;
        if (string.Equals(request.CommandType, "MovePiece", StringComparison.Ordinal))
        {
            result = await gameRoom.MovePieceAsync(authToken, request.Command);
        }
        else if (string.Equals(request.CommandType, "DiceRoll", StringComparison.Ordinal))
        {
            result = await gameRoom.SeatTurn(authToken, request.Command);
        }
        else
        {
            return new GameCommand { Result = "Error: Unsupported command type." };
        }

        return result ?? new GameCommand { Result = "Error: Command execution failed." };
    }

    public async Task<GameplayJoinResponse?> JoinLobby(SharedCode.GameDto game)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null || game == null)
            return null;

        try
        {
            var joined = await DatabaseManager.JoinGameLobby(player, game);
            if (joined == null)
                return null;

            return new GameplayJoinResponse(joined.RoomCode, joined.GameType, joined.BetAmount, joined.State);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameplayHub] JoinLobby failed. PlayerId={player.PlayerId}, Error={ex.Message}");
            return null;
        }
    }

    public async Task<GameplayReadyResponse?> Ready(string roomCode)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null || string.IsNullOrWhiteSpace(roomCode))
            return null;

        var (game, seats, rollsString) = await DatabaseManager.Ready(player.PlayerId, roomCode);
        if (game == null)
            return null;

        var seatAssignments = await BuildSeatAssignmentsAsync(game).ConfigureAwait(false);
        var started = seats != null && !string.IsNullOrWhiteSpace(rollsString) && string.Equals(game.State, "Playing", StringComparison.OrdinalIgnoreCase);
        var seatsJson = seats == null ? string.Empty : JsonConvert.SerializeObject(seats);

        return new GameplayReadyResponse(
            game.RoomCode,
            game.GameType,
            game.State,
            started,
            seatsJson,
            rollsString ?? string.Empty,
            seatAssignments);
    }

    public async Task<GameplayReadyResponse?> GetLobbyState(string roomCode)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null || string.IsNullOrWhiteSpace(roomCode))
            return null;

        using var ctx = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var game = await ctx.Games
            .Include(g => g.MultiPlayer)
            .FirstOrDefaultAsync(g =>
                g.RoomCode == roomCode &&
                (g.State == "Active" || g.State == "Playing") &&
                (g.MultiPlayer.P1 == player.PlayerId ||
                 g.MultiPlayer.P2 == player.PlayerId ||
                 g.MultiPlayer.P3 == player.PlayerId ||
                 g.MultiPlayer.P4 == player.PlayerId))
            .ConfigureAwait(false);
        if (game == null)
            return null;

        var seats = await BuildColoredSeatsAsync(game).ConfigureAwait(false);
        var seatAssignments = await BuildSeatAssignmentsAsync(game).ConfigureAwait(false);
        var seatsJson = JsonConvert.SerializeObject(seats);

        string rollsString = string.Empty;
        if (!string.IsNullOrWhiteSpace(game.RoomCode) &&
            DatabaseManager._gameRooms.TryGetValue(game.RoomCode, out var room) &&
            room.engine?.EngineHelper != null)
        {
            rollsString = room.engine.EngineHelper.rollsString ?? string.Empty;
        }

        var started = string.Equals(game.State, "Playing", StringComparison.OrdinalIgnoreCase)
                      && !string.IsNullOrWhiteSpace(rollsString);

        return new GameplayReadyResponse(
            game.RoomCode,
            game.GameType,
            game.State,
            started,
            seatsJson,
            rollsString,
            seatAssignments);
    }

    public Task<List<GameCommand>> PullCommands(string roomCode, int lastSeenIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
            return Task.FromResult(new List<GameCommand>());

        if (!DatabaseManager._gameRooms.TryGetValue(roomCode, out var gameRoom))
            return Task.FromResult(new List<GameCommand>());

        return gameRoom.PullCommands(lastSeenIndex);
    }

    public async Task<GameplayLeaveResponse?> LeaveLobby()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
            return null;

        var (game, user) = await DatabaseManager.LeaveGameLobby(player.PlayerId);
        if (game == null)
            return new GameplayLeaveResponse(false, string.Empty, "No active lobby found.");

        return new GameplayLeaveResponse(true, game.RoomCode, user == null ? "Left lobby." : "Left lobby and seat released.");
    }

    public async Task<List<ActiveGameListItem>> GetActivePublicGames()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
            return new List<ActiveGameListItem>();

        using var ctx = await _contextFactory.CreateDbContextAsync();
        return await ctx.Games
            .Where(g => g.State == "Active" && !g.IsPrivate && !g.IsPractice)
            .Select(g => new ActiveGameListItem
            {
                GameId = g.GameId,
                GameType = g.GameType ?? "",
                RoomCode = g.RoomCode ?? "",
                BetAmount = g.BetAmount,
                State = g.State ?? ""
            })
            .ToListAsync();
    }

    protected async Task<Player?> TryGetAuthenticatedPlayerAsync()
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token)) return null;
        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        return await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
    }

    private async Task<List<GameplaySeatInfo>> BuildSeatAssignmentsAsync(Game game)
    {
        using var ctx = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var loadedGame = await ctx.Games
            .Include(g => g.MultiPlayer)
            .FirstOrDefaultAsync(g => g.GameId == game.GameId)
            .ConfigureAwait(false);
        if (loadedGame?.MultiPlayer == null)
            return new List<GameplaySeatInfo>();

        var playerIds = new[]
        {
            loadedGame.MultiPlayer.P1,
            loadedGame.MultiPlayer.P2,
            loadedGame.MultiPlayer.P3,
            loadedGame.MultiPlayer.P4
        }.Where(x => x.HasValue).Select(x => x!.Value).ToList();

        var players = await ctx.Players
            .Where(p => playerIds.Contains(p.PlayerId))
            .ToDictionaryAsync(p => p.PlayerId)
            .ConfigureAwait(false);

        var result = new List<GameplaySeatInfo>();
        AddSeat(result, players, "P1", loadedGame.MultiPlayer.P1);
        AddSeat(result, players, "P2", loadedGame.MultiPlayer.P2);
        AddSeat(result, players, "P3", loadedGame.MultiPlayer.P3);
        AddSeat(result, players, "P4", loadedGame.MultiPlayer.P4);
        return result;
    }

    private async Task<List<SharedCode.PlayerDto>> BuildColoredSeatsAsync(Game game)
    {
        using var ctx = await _contextFactory.CreateDbContextAsync().ConfigureAwait(false);
        var loadedGame = await ctx.Games
            .Include(g => g.MultiPlayer)
            .FirstOrDefaultAsync(g => g.GameId == game.GameId)
            .ConfigureAwait(false);
        if (loadedGame?.MultiPlayer == null)
            return new List<SharedCode.PlayerDto>();

        var slots = new (int? PlayerId, string Color)[]
        {
            (loadedGame.MultiPlayer.P1, "Red"),
            (loadedGame.MultiPlayer.P2, loadedGame.GameType == "2" ? "Yellow" : "Green"),
            (loadedGame.MultiPlayer.P3, "Yellow"),
            (loadedGame.MultiPlayer.P4, "Blue")
        };

        var playerIds = slots.Where(s => s.PlayerId.HasValue).Select(s => s.PlayerId!.Value).ToList();
        var players = await ctx.Players
            .Where(p => playerIds.Contains(p.PlayerId))
            .ToDictionaryAsync(p => p.PlayerId)
            .ConfigureAwait(false);

        var result = new List<SharedCode.PlayerDto>();
        foreach (var slot in slots)
        {
            if (!slot.PlayerId.HasValue)
                continue;
            if (!players.TryGetValue(slot.PlayerId.Value, out var player))
                continue;

            result.Add(new SharedCode.PlayerDto
            {
                PlayerId = player.PlayerId,
                PlayerName = player.Name,
                PlayerPicture = player.PictureUrl,
                PlayerColor = slot.Color
            });
        }
        return result;
    }

    private static void AddSeat(List<GameplaySeatInfo> result, Dictionary<int, Player> players, string seatType, int? playerId)
    {
        if (!playerId.HasValue)
            return;
        if (!players.TryGetValue(playerId.Value, out var player))
            return;

        result.Add(new GameplaySeatInfo(seatType, player.PlayerId, player.Name ?? "Waiting", player.PictureUrl ?? "user.webp"));
    }
}
