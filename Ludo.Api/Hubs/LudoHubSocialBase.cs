using Ludo.Api.Controllers;
using Ludo.Api.Services;
using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedCode;
using SignalR.Server;
using SignalR.Server.Payments;
using SignalR.Server.Services;

namespace Ludo.Api.Hubs;

public abstract class LudoHubSocialBase(
    ApiPlayerContext playerContext,
    DatabaseManager databaseManager,
    IHubContext<LudoHub> hubContext,
    DailyBonusService dailyBonusService,
    CryptoHelper cryptoHelper,
    UtilService utilService,
    PlayerPresenceTracker presenceTracker,
    IDbContextFactory<LudoDbContext> contextFactory,
    FriendsService friendsService) : LudoHubProfileBase(playerContext, databaseManager, hubContext, dailyBonusService, cryptoHelper, utilService, presenceTracker, contextFactory)
{
    private readonly IDbContextFactory<LudoDbContext> _contextFactory = contextFactory;
    private readonly FriendsService _friendsService = friendsService;

    public async Task<List<PlayerCard>?> GetFriends(string type = "All")
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine($"[SocialHub] GetFriends unauthorized. Type={type}");
            return null;
        }

        Console.WriteLine($"[SocialHub] GetFriends requested. PlayerId={player.PlayerId}, Type={type}");
        var friends = await _friendsService.GetFriends(player, type);
        Console.WriteLine($"[SocialHub] GetFriends completed. PlayerId={player.PlayerId}, Type={type}, Count={friends.Count}");
        return friends;
    }

    public async Task<string> SendFriendRequest(FriendRequestDto request)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine($"[SocialHub] SendFriendRequest unauthorized. ReceiverId={request.ReceiverId}, Status={request.Status}");
            return "Unauthorized";
        }

        if (request.ReceiverId <= 0 || string.IsNullOrWhiteSpace(request.Status))
        {
            Console.WriteLine($"[SocialHub] SendFriendRequest rejected. PlayerId={player.PlayerId}, ReceiverId={request.ReceiverId}, Status={request.Status}");
            return "Invalid friend request.";
        }

        Console.WriteLine($"[SocialHub] SendFriendRequest requested. PlayerId={player.PlayerId}, ReceiverId={request.ReceiverId}, Status={request.Status}");
        var result = await _friendsService.SendFriendRequest(player, request.ReceiverId, request.Status);
        Console.WriteLine($"[SocialHub] SendFriendRequest completed. PlayerId={player.PlayerId}, ReceiverId={request.ReceiverId}, Result={result}");
        return result;
    }

    public async Task<PlayerCard?> GetPlayerById(int playerId)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine($"[SocialHub] GetPlayerCard unauthorized. TargetPlayerId={playerId}");
            return null;
        }

        Console.WriteLine($"[SocialHub] GetPlayerCard requested. PlayerId={player.PlayerId}, TargetPlayerId={playerId}");
        using var ctx = await _contextFactory.CreateDbContextAsync();
        var p = await ctx.Players.FirstOrDefaultAsync(x => x.PlayerId == playerId);
        if (p == null)
        {
            Console.WriteLine($"[SocialHub] GetPlayerCard not found. PlayerId={player.PlayerId}, TargetPlayerId={playerId}");
            return null;
        }

        var card = new PlayerCard
        {
            playerID = p.PlayerId,
            name = p.Name,
            pictureUrl = p.PictureUrl,
            rank = ctx.Players.Count(other => other.GamesWon > p.GamesWon) + 1,
            status = "",
            lastGame = false,
            gamesWon = p.GamesWon
        };
        Console.WriteLine($"[SocialHub] GetPlayerCard completed. PlayerId={player.PlayerId}, TargetPlayerId={playerId}");
        return card;
    }

    public async Task<List<PlayerCard>?> GetLeaderboard()
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine("[SocialHub] GetLeaderboard unauthorized.");
            return null;
        }

        Console.WriteLine($"[SocialHub] GetLeaderboard requested. PlayerId={player.PlayerId}");
        using var ctx = await _contextFactory.CreateDbContextAsync();
        var topPlayers = await ctx.Players
            .Where(p => p.Role == "Player" && p.GamesWon > 0)
            .OrderByDescending(p => p.GamesWon)
            .Select(p => new PlayerCard
            {
                playerID = p.PlayerId,
                name = p.Name,
                pictureUrl = p.PictureUrl,
                rank = 0,
                status = "",
                lastGame = false,
                gamesWon = p.GamesWon
            })
            .ToListAsync();

        for (var i = 0; i < topPlayers.Count; i++)
            topPlayers[i].rank = i + 1;

        Console.WriteLine($"[SocialHub] GetLeaderboard completed. PlayerId={player.PlayerId}, Count={topPlayers.Count}");
        return topPlayers;
    }

    public async Task<List<PlayerCard>?> GetTournamentLeaderboard(string tournamentType)
    {
        var player = await TryGetAuthenticatedPlayerAsync();
        if (player == null)
        {
            Console.WriteLine($"[SocialHub] GetTournamentLeaderboard unauthorized. TournamentType={tournamentType}");
            return null;
        }

        Console.WriteLine($"[SocialHub] GetTournamentLeaderboard requested. PlayerId={player.PlayerId}, TournamentType={tournamentType}");
        using var ctx = await _contextFactory.CreateDbContextAsync();
        var tournament = await ctx.Tournaments
            .Where(t => t.Name.Contains(tournamentType) && t.TournamentState == State.Active)
            .OrderByDescending(t => t.TournamentId)
            .FirstOrDefaultAsync();

        if (tournament == null)
        {
            Console.WriteLine($"[SocialHub] GetTournamentLeaderboard no active tournament. PlayerId={player.PlayerId}, TournamentType={tournamentType}");
            return new List<PlayerCard>();
        }

        var challengers = await ctx.TournamentChallengers
            .Where(tc => tc.TournamentId == tournament.TournamentId && tc.Score > 0)
            .Include(tc => tc.Player)
            .OrderByDescending(tc => tc.Score)
            .Select(tc => new PlayerCard
            {
                playerID = tc.PlayerId,
                name = tc.Player.Name,
                pictureUrl = tc.Player.PictureUrl,
                rank = 0,
                status = "",
                lastGame = false,
                gamesWon = tc.Score
            })
            .ToListAsync();

        var uniqueChallengers = challengers
            .GroupBy(c => c.playerID)
            .Select(g => g.First())
            .ToList();

        for (var i = 0; i < uniqueChallengers.Count; i++)
            uniqueChallengers[i].rank = i + 1;

        Console.WriteLine($"[SocialHub] GetTournamentLeaderboard completed. PlayerId={player.PlayerId}, TournamentType={tournamentType}, TournamentId={tournament.TournamentId}, Count={uniqueChallengers.Count}");
        return uniqueChallengers;
    }

    private async Task<Player?> TryGetAuthenticatedPlayerAsync()
    {
        var token = GetAuthToken();
        if (string.IsNullOrWhiteSpace(token)) return null;
        var authContext = new DefaultHttpContext();
        authContext.Request.Headers["X-Auth-Token"] = token;
        return await PlayerContext.GetAuthenticatedPlayerAsync(authContext.Request);
    }
}
