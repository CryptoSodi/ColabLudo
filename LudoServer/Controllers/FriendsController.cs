using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;
using SharedCode;
namespace LudoServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FriendsController : ControllerBase
    {
        private readonly LudoDbContext _context;

        public FriendsController(LudoDbContext context)
        {
            _context = context;
        }

        // Get All Friends by Player Id
        [HttpGet]
        public IActionResult GetAllFriends(int playerId)
        {
            List<PlayerCard> result = new List<PlayerCard>();

            // First, get the last game players
            var lastGame = _context.MultiPlayers
                .Where(m => m.P1 == playerId || m.P2 == playerId || m.P3 == playerId || m.P4 == playerId)
                .OrderByDescending(m => m.MultiPlayerId)
                .FirstOrDefault();

            if (lastGame != null)
            {
                var playerIds = new List<int?> { lastGame.P1, lastGame.P2, lastGame.P3, lastGame.P4 }
                    .Where(id => id.HasValue && id.Value != playerId)
                    .Select(id => id.Value)
                    .ToList();

                if (playerIds.Any())
                {
                    var lastGamePlayers = _context.Players
                        .Where(p => playerIds.Contains(p.PlayerId))
                        .Select(p => new PlayerCard
                        {
                            playerID = p.PlayerId,
                            name = p.Name,
                            pictureUrl = p.PictureUrl,
                            rank = 31, // No rank info available, setting 0 or you can later compute
                            status = "",
                            lastGame = true
                        })
                        .ToList();

                    result.AddRange(lastGamePlayers);
                }
            }

            // Now, get friends (all statuses)
            var friends = _context.FriendsRequests
                .Where(fr => fr.SenderId == playerId || fr.ReceiverId == playerId)
                .Select(fr => new
                {
                    OtherPlayer = fr.SenderId == playerId ? fr.Receiver : fr.Sender,
                    Status = fr.Status
                })
                .ToList();
            foreach (var fr in friends)
            {
                // try to find an existing card (e.g. from lastGame)
                var existing = result
                    .FirstOrDefault(x => x.playerID == fr.OtherPlayer.PlayerId);

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
                        rank = 31,
                        status = fr.Status.ToString(),
                        lastGame = false
                    });
                }
            }
            foreach (var fr in result)
            {       // update the status (and sender/receiver flag) on the existing card
                if (fr.status.ToString() == "")
                {
                    fr.status = "UN FRIEND";
                }
            }
            if (!result.Any())
            {
                return NotFound(new { Message = "No friends or last game players found." });
            }

            return Ok(result);
        }



        // Get Friends by Player Id
        [HttpGet("acceptedPlayers/{playerId}")]
        public IActionResult GetAllAcceptedFriends(int playerId)
        {
            var friends = _context.FriendsRequests
                      .Where(fr => (fr.SenderId == playerId || fr.ReceiverId == playerId)
                                  && fr.Status == "Accepted")
                      .Select(fr => new
                      {
                          PlayerId = (fr.SenderId == playerId ? fr.Receiver.PlayerId : fr.Sender.PlayerId),
                          PlayerName = (fr.SenderId == playerId ? fr.Receiver.Name : fr.Sender.Name),
                          PlayerPicture = (fr.SenderId == playerId ? fr.Receiver.PictureUrl : fr.Sender.PictureUrl)
                      })
                      .ToList();

            if (friends == null || !friends.Any())
            {
                return NotFound(new { Message = "Friends not found." });
            }

            // Return all friends as a list
            return Ok(friends);
        }

        [HttpGet("lastgamePlayers/{playerId}")]
        public IActionResult GetLastGamePlayers(int playerId)
        {
            // Find the last game where this player participated
            var lastGame = _context.MultiPlayers
                .Where(m => m.P1 == playerId || m.P2 == playerId || m.P3 == playerId || m.P4 == playerId)
                .OrderByDescending(m => m.MultiPlayerId)
                .FirstOrDefault();

            if (lastGame == null)
            {
                return NotFound(new { Message = "No game found for this player." });
            }

            // Collect all player IDs from the game, excluding the current player
            var playerIds = new List<int?> { lastGame.P1, lastGame.P2, lastGame.P3, lastGame.P4 }
                .Where(id => id.HasValue && id.Value != playerId)
                .Select(id => id.Value)
                .ToList();

            if (!playerIds.Any())
            {
                return NotFound(new { Message = "No other players found in the last game." });
            }

            // Fetch players' details
            var players = _context.Players
                .Where(p => playerIds.Contains(p.PlayerId))
                .Select(p => new
                {
                    p.PlayerId,
                    p.Name,
                    p.PictureUrl
                })
                .ToList();

            return Ok(players);
        }
        [HttpGet("friendrequest")]
        public IActionResult SendFriendRequest(int SenderId, int ReceiverId, string status)
        {
            if (SenderId == ReceiverId)
                return BadRequest(new { Message = "Cannot send friend request to yourself." });

            var sender = _context.Players.Find(SenderId);
            var receiver = _context.Players.Find(ReceiverId);

            if (sender == null || receiver == null)
                return NotFound(new { Message = "Sender or Receiver not found." });

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
            request.SenderId = SenderId;
            request.ReceiverId = ReceiverId;

            _context.FriendsRequests.Add(request);
            _context.SaveChanges();            
            return Ok(status); 
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
        [HttpPost("accept")]
        public IActionResult AcceptFriendRequest(int friendRequestId)
        {
            var friendRequest = _context.FriendsRequests.Find(friendRequestId);

            if (friendRequest == null)
                return NotFound(new { Message = "Friend request not found." });

            if (friendRequest.Status == "Accepted")
                return Conflict(new { Message = "Friend request is already accepted." });

            friendRequest.Status = "Accepted";
            _context.FriendsRequests.Update(friendRequest);
            _context.SaveChanges();

            return Ok(new { Message = "Friend request accepted successfully." });
        }

        [HttpPost("reject")]
        public IActionResult RejectFriendRequest(int friendRequestId)
        {
            var friendRequest = _context.FriendsRequests.Find(friendRequestId);

            if (friendRequest == null)
                return NotFound(new { Message = "Friend request not found." });

            if (friendRequest.Status == "Rejected")
                return Conflict(new { Message = "Friend request is already rejected." });

            friendRequest.Status = "Rejected";
            _context.FriendsRequests.Update(friendRequest);
            _context.SaveChanges();

            return Ok(new { Message = "Friend request rejected successfully." });
        }

    }
}