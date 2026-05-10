using Microsoft.AspNetCore.SignalR;
using System.Linq;
using Ludo.Api.Controllers;
using Ludo.Api.Services;
using SignalR.Server;
using SharedCode;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Ludo.Api.Hubs;

public class LudoHub(ApiPlayerContext playerContext, DatabaseManager databaseManager) : Hub
{
    private string GetAuthToken()
    {
        return Context.GetHttpContext()?.Request.Headers["X-Auth-Token"].FirstOrDefault() ?? string.Empty;
    }

    public override async Task OnConnectedAsync()
    {
        var token = GetAuthToken();
        Console.WriteLine($"[LudoHub] Connected. ConnectionId={Context.ConnectionId}, HasAuthToken={!string.IsNullOrWhiteSpace(token)}");
        await base.OnConnectedAsync();
    }

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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[LudoHub] Disconnected. ConnectionId={Context.ConnectionId}, Error={exception?.Message ?? "none"}");
        await base.OnDisconnectedAsync(exception);
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
        var player = await playerContext.GetAuthenticatedPlayerAsync(authContext.Request);
        if (player == null)
        {
            Console.WriteLine($"[LudoHub] Send auth failed. ConnectionId={Context.ConnectionId}, CommandType={request.CommandType}");
            return new GameCommand { Result = "Error: Unauthorized." };
        }

        var validateSw = Stopwatch.StartNew();
        // Payload already validated above; keep this segment for apples-to-apples logs.
        validateSw.Stop();

        var roomLookupSw = Stopwatch.StartNew();
        if (!databaseManager._gameRooms.TryGetValue(request.RoomCode, out var gameRoom))
            return new GameCommand { Result = "Error: Room not found." };
        roomLookupSw.Stop();

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
}
