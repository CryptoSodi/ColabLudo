using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SharedCode;
using SignalR.Server;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/gameplay")]
public class GameplayController(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IDbContextFactory<LudoDbContext> contextFactory) : ControllerBase
{
    [HttpPost("lobbies/join")]
    public async Task<ActionResult<GameplayJoinResponse>> JoinLobby([FromBody] GameplayJoinRequest request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        if (request.Game == null)
            return BadRequest("Game payload is required.");

        try
        {
            var game = await databaseManager.JoinGameLobby(player, request.Game);
            if (game == null)
                return Conflict("Room is full");

            return new GameplayJoinResponse(game.RoomCode, game.GameType, game.BetAmount, game.State);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GameplayApi] JoinLobby failed. PlayerId={player.PlayerId}, Error={ex.Message}");
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("lobbies/ready")]
    public async Task<ActionResult<GameplayReadyResponse>> Ready([FromBody] GameplayReadyRequest request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();
        if (request == null || string.IsNullOrWhiteSpace(request.RoomCode))
            return BadRequest("roomCode is required.");

        var (game, seats, rollsString) = await databaseManager.Ready(player.PlayerId, request.RoomCode);
        if (game == null)
            return NotFound("No active lobby found for player.");

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

    [HttpGet("lobbies/state")]
    public async Task<ActionResult<GameplayReadyResponse>> GetLobbyState([FromQuery] string roomCode)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();
        if (string.IsNullOrWhiteSpace(roomCode))
            return BadRequest("roomCode is required.");

        using var ctx = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
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
            return NotFound("No active lobby found for player.");

        var seats = await BuildColoredSeatsAsync(game).ConfigureAwait(false);
        var seatAssignments = await BuildSeatAssignmentsAsync(game).ConfigureAwait(false);
        var seatsJson = JsonConvert.SerializeObject(seats);

        string rollsString = string.Empty;
        if (!string.IsNullOrWhiteSpace(game.RoomCode) &&
            databaseManager._gameRooms.TryGetValue(game.RoomCode, out var room) &&
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

    [HttpGet("commands/pull")]
    public ActionResult<List<GameCommand>> PullCommands([FromQuery] string roomCode, [FromQuery] int lastSeenIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
            return BadRequest("roomCode is required.");

        if (!databaseManager._gameRooms.TryGetValue(roomCode, out var gameRoom))
            return new List<GameCommand>();

        var commands = gameRoom.PullCommands(lastSeenIndex).GetAwaiter().GetResult();
        return commands;
    }

    [HttpPost("commands/send")]
    public async Task<ActionResult<GameCommand>> SendCommand([FromBody] GameplaySendRequest request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        if (request.Command == null || string.IsNullOrWhiteSpace(request.CommandType) || string.IsNullOrWhiteSpace(request.RoomCode))
            return BadRequest("command, commandType, and roomCode are required.");

        if (!databaseManager._gameRooms.TryGetValue(request.RoomCode, out var gameRoom))
            return NotFound(new GameCommand { Result = "Error: Room not found." });

        if (gameRoom.engine == null)
            return Conflict(new GameCommand { Result = "Error: Engine not initialized." });

        var authToken = player.AuthToken ?? string.Empty;
        if (string.IsNullOrWhiteSpace(authToken))
            return Unauthorized();

        GameCommand result;
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
            return BadRequest(new GameCommand { Result = "Error: Unsupported command type." });
        }

        return result ?? new GameCommand { Result = "Error: Command execution failed." };
    }

    [HttpPost("chat/send")]
    public async Task<ActionResult<List<ChatMessages>>> SendChat([FromBody] GameplayChatRequest request)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        if (request.Message == null)
            return BadRequest("message is required.");

        using var ctx = await contextFactory.CreateDbContextAsync();

        var isRoomChat = !string.IsNullOrWhiteSpace(request.RoomCode);
        if (!isRoomChat)
        {
            var isBlocked = await ctx.FriendsRequests.AnyAsync(fr =>
                ((fr.SenderId == player.PlayerId && fr.ReceiverId == request.Message.ReceiverId) ||
                 (fr.SenderId == request.Message.ReceiverId && fr.ReceiverId == player.PlayerId)) &&
                fr.Status == "BLOCK");
            if (isBlocked)
                return new List<ChatMessages>();
        }

        if (!string.IsNullOrWhiteSpace(request.Message.Message))
        {
            var entity = new ChatMessage
            {
                SenderId = player.PlayerId,
                SenderName = player.Name,
                SenderPicture = player.PictureUrl,
                SenderColor = request.Message.SenderColor,
                ReceiverId = request.Message.ReceiverId,
                ReceiverName = request.Message.ReceiverName != null ? request.Message.ReceiverName: "",
                Message = request.Message.Message,
                RoomCode = isRoomChat ? request.RoomCode : "",
                CreatedDate = DateTime.UtcNow
            };
            ctx.ChatMessages.Add(entity);
            await ctx.SaveChangesAsync();
        }

        IQueryable<ChatMessage> historyQuery;
        if (isRoomChat)
        {
            historyQuery = ctx.ChatMessages.Where(x => x.RoomCode == request.RoomCode);
        }
        else
        {
            historyQuery = ctx.ChatMessages.Where(x =>
                (x.RoomCode == null || x.RoomCode == "") &&
                ((x.SenderId == player.PlayerId && x.ReceiverId == request.Message.ReceiverId) ||
                 (x.SenderId == request.Message.ReceiverId && x.ReceiverId == player.PlayerId)));
        }

        var history = await historyQuery
            .OrderByDescending(x => x.Index)
            .Take(isRoomChat ? 200 : 30)
            .OrderBy(x => x.Index)
            .Select(x => new ChatMessages
            {
                Index = x.Index,
                SenderId = x.SenderId,
                SenderName = x.SenderName,
                SenderPicture = x.SenderPicture,
                SenderColor = x.SenderColor,
                ReceiverId = x.ReceiverId,
                ReceiverName = x.ReceiverName,
                Message = x.Message,
                RoomCode = x.RoomCode,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync();

        return history;
    }

    [HttpGet("chat/pull")]
    public async Task<ActionResult<List<ChatMessages>>> PullChat([FromQuery] string? roomCode = null, [FromQuery] int lastSeenIndex = 0)
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        var playerId = player.PlayerId;
        using var ctx = await contextFactory.CreateDbContextAsync();
        IQueryable<ChatMessage> query;
        if (!string.IsNullOrWhiteSpace(roomCode))
        {
            var isMember = await ctx.Games
                .Include(g => g.MultiPlayer)
                .AnyAsync(g =>
                    g.RoomCode == roomCode &&
                    (g.State == "Active" || g.State == "Playing") &&
                    (g.MultiPlayer.P1 == playerId ||
                     g.MultiPlayer.P2 == playerId ||
                     g.MultiPlayer.P3 == playerId ||
                     g.MultiPlayer.P4 == playerId));
            if (!isMember)
                return new List<ChatMessages>();

            query = ctx.ChatMessages.Where(x => x.RoomCode == roomCode && x.Index > lastSeenIndex);
        }
        else
        {
            query = ctx.ChatMessages.Where(x =>
                (x.RoomCode == null || x.RoomCode == "") &&
                x.ReceiverId == playerId &&
                x.Index > lastSeenIndex);
        }

        var updates = await query
            .OrderBy(x => x.Index)
            .Take(50)
            .Select(x => new ChatMessages
            {
                Index = x.Index,
                SenderId = x.SenderId,
                SenderName = x.SenderName,
                SenderPicture = x.SenderPicture,
                SenderColor = x.SenderColor,
                ReceiverId = x.ReceiverId,
                ReceiverName = x.ReceiverName,
                Message = x.Message,
                RoomCode = x.RoomCode,
                CreatedDate = x.CreatedDate
            })
            .ToListAsync();

        return updates;
    }

    [HttpPost("lobbies/leave")]
    public async Task<ActionResult<GameplayLeaveResponse>> LeaveLobby()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        var (game, user) = await databaseManager.LeaveGameLobby(player.PlayerId);
        if (game == null)
            return new GameplayLeaveResponse(false, string.Empty, "No active lobby found.");

        return new GameplayLeaveResponse(true, game.RoomCode, user == null ? "Left lobby." : "Left lobby and seat released.");
    }

    [HttpGet("games/active")]
    public async Task<ActionResult<List<ActiveGameListItem>>> GetActivePublicGames()
    {
        var player = await playerContext.GetAuthenticatedPlayerAsync(Request);
        if (player == null)
            return Unauthorized();

        using var ctx = await contextFactory.CreateDbContextAsync();
        var games = await ctx.Games
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

        return games;
    }

    private async Task<List<GameplaySeatInfo>> BuildSeatAssignmentsAsync(Game game)
    {
        using var ctx = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
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
        using var ctx = await contextFactory.CreateDbContextAsync().ConfigureAwait(false);
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

public record GameplayJoinRequest(SharedCode.GameDto Game);
public record GameplayReadyRequest(string RoomCode);
public record GameplayJoinResponse(string RoomCode, string GameType, decimal BetAmount, string State);
public record GameplayReadyResponse(string RoomCode, string GameType, string State, bool Started, string SeatsJson, string RollsString, List<GameplaySeatInfo> SeatAssignments);
public record GameplaySendRequest(string RoomCode, string CommandType, GameCommand Command);
public record GameplayChatRequest(string RoomCode, ChatMessages Message);
public record GameplayLeaveResponse(bool Success, string RoomCode, string Message);
public record GameplaySeatInfo(string PlayerType, int PlayerId, string UserName, string PictureUrl);
