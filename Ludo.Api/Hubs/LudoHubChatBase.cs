using Ludo.Api.Controllers;
using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode;
using SignalR.Server;

namespace Ludo.Api.Hubs;

public abstract class LudoHubChatBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    IDbContextFactory<LudoDbContext> contextFactory) : LudoHubGameplayBase(playerContext, databaseManager, hubContext, contextFactory)
{
    private readonly IDbContextFactory<LudoDbContext> _contextFactory = contextFactory;

    public async Task<List<ChatMessages>> SendChat(GameplayChatRequest request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
            throw new HubException("Unauthorized");

        if (request.Message == null)
            throw new HubException("message is required.");

        using var ctx = await _contextFactory.CreateDbContextAsync();

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
                ReceiverName = request.Message.ReceiverName != null ? request.Message.ReceiverName : "",
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

    public async Task<List<ChatMessages>> PullChat(string? roomCode = null, int lastSeenIndex = 0)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
            throw new HubException("Unauthorized");

        var playerId = player.PlayerId;
        using var ctx = await _contextFactory.CreateDbContextAsync();
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
                throw new HubException("Player is not in the requested room.");

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

}
