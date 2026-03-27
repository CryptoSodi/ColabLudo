using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;
using SharedCode;

namespace SignalR.Server.Services
{
    public class FriendsService
    {
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;

        public FriendsService(IDbContextFactory<LudoDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public Task<List<PlayerCard>> GetFriends(Player player, string Type = "All")
        {
            using var ctx = _contextFactory.CreateDbContext();
            List<PlayerCard> result = new List<PlayerCard>();
            // First, get the last game players
            var lastGame = ctx.MultiPlayers
                .Where(m => m.P1 == player.PlayerId || m.P2 == player.PlayerId || m.P3 == player.PlayerId || m.P4 == player.PlayerId)
                .OrderByDescending(m => m.MultiPlayerId)
                .FirstOrDefault();

            if (lastGame != null)
            {
                var playerIds = new List<int?> { lastGame.P1, lastGame.P2, lastGame.P3, lastGame.P4 }
                    .Where(id => id.HasValue && id.Value != player.PlayerId)
                    .Select(id => id.Value)
                    .ToList();

                if (playerIds.Any())
                {
                    var lastGamePlayers = ctx.Players
                        .Where(p => playerIds.Contains(p.PlayerId) && p.Role == "Player")
                        .Select(p => new PlayerCard
                        {
                            playerID = p.PlayerId,
                            name = p.Name,
                            pictureUrl = p.PictureUrl,
                            rank = ctx.Players.Count(other => other.GamesWon > p.GamesWon) + 1,
                            status = "",
                            lastGame = true,
                            gamesWon = p.GamesWon
                        })
                        .ToList();

                    result.AddRange(lastGamePlayers);
                }
            }
            // Now, get friends (all statuses) with Role "Player"
            var friends = ctx.FriendsRequests
                .Where(fr => fr.SenderId == player.PlayerId || fr.ReceiverId == player.PlayerId)
                .Select(fr => new
                {
                    OtherPlayer = fr.SenderId == player.PlayerId ? fr.Receiver : fr.Sender,
                    Status = fr.Status
                })
                .Where(x => x.OtherPlayer.Role == "Player")
                .ToList();


            foreach (var fr in friends)
            {
                // try to find an existing card (e.g. from lastGame)
                var existing = result.FirstOrDefault(x => x.playerID == fr.OtherPlayer.PlayerId);

                if (existing != null)
                {
                    // update the status (and sender/receiver flag) on the existing card
                    existing.status = fr.Status.ToString();
                }
                else
                {
                    // still need to add brand-new friends
                    result.Add(new PlayerCard
                    {
                        playerID = fr.OtherPlayer.PlayerId,
                        name = fr.OtherPlayer.Name,
                        pictureUrl = fr.OtherPlayer.PictureUrl,
                        rank = ctx.Players.Count(other => other.GamesWon > fr.OtherPlayer.GamesWon) + 1,
                        status = fr.Status.ToString(),
                        lastGame = false,
                        gamesWon = fr.OtherPlayer.GamesWon
                    });
                }
            }
            foreach (var fr in result)
            {       // update the status (and sender/receiver flag) on the existing card
                if (fr.status.ToString() == "")
                    fr.status = "UN FRIEND";
            }
            if (!result.Any())
                return Task.FromResult(new List<PlayerCard>());
            return Task.FromResult(result);
        }
        public async Task<string> SendFriendRequest(Player player, int ReceiverId, string status)
        {
            using var ctx = _contextFactory.CreateDbContext();

            if (player.PlayerId == ReceiverId)
                return "Cannot send friend request to yourself.";

            var receiver = ctx.Players.Find(ReceiverId);

            if (player == null || receiver == null)
                return "Sender or Receiver not found.";

            //var existingRequest = _context.FriendsRequests
            //    .FirstOrDefault(fr =>
            //        (fr.SenderId == SenderId && fr.ReceiverId == ReceiverId) ||
            //        (fr.SenderId == ReceiverId && fr.ReceiverId == SenderId));

            //if (existingRequest != null)
            //    return Conflict(new { Message = "Friend request already exists or is pending." });

            FriendRequest request = new FriendRequest();
            request.Status = status;
            request.CreatedDate = DateTime.UtcNow;

            // Make sure navigation properties are not set by client
            request.SenderId = player.PlayerId;
            request.ReceiverId = ReceiverId;

            ctx.FriendsRequests.Add(request);
            ctx.SaveChanges();
            return status;
            //switch (status)
            //{
            //    case "UN BLOCK":
            //        return Ok(new { Message = "UN BLOCK" });
            //        break;
            //    case "BLOCK":
            //        return Ok(new { Message = "BLOCK" });
            //        break;
            //    case "FIREND":
            //        return Ok(new { Message = "PENDING" });
            //        break;
            //    case "UN FRIEND":
            //        return Ok(new { Message = "UN FRIEND" });
            //        break;
            //}
        }
    }
}