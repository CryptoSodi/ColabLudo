using LudoServer.Data;
using LudoServer.Models;
using Microsoft.AspNetCore.Mvc;

namespace LudoServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TournamentController : ControllerBase
    {
        private readonly LudoDbContext _context;

        public TournamentController(LudoDbContext context)
        {
            _context = context;
        }

        // GET /api/tournament/type/{type}/player/{playerId}
        [HttpGet("type/{type}/player/{playerId:int}")]
        public IActionResult GetAllTournaments(string type, int playerId)
        {
            // 1) Load all tournaments (unfiltered)
            var tournaments = _context.Tournaments.ToList();

            // 2) Filter by status (Completed/Running/Upcoming)
            DateTime serverDateTime = DateTime.Now;
            switch (type)
            {
                case "Completed":
                    tournaments = tournaments
                        .Where(t => serverDateTime > t.EndDate)
                        .ToList();
                    break;
                case "Running":
                    tournaments = tournaments
                        .Where(t => serverDateTime >= t.StartDate && serverDateTime <= t.EndDate)
                        .ToList();
                    break;
                case "Upcoming":
                    tournaments = tournaments
                        .Where(t => serverDateTime < t.StartDate)
                        .ToList();
                    break;
            }

            if (tournaments == null || !tournaments.Any())
            {
                return NotFound(new { Message = "No tournaments found." });
            }

            // 3) Pull all TournamentChallengers for this player in one query
            var joinedIds = _context.TournamentChallengers
                .Where(tc => tc.PlayerId == playerId)
                .Select(tc => tc.TournamentId)
                .ToHashSet();

            // 4) Project each tournament, including an IsJoined flag
            var result = tournaments.Select(t => new
            {
                TournamentId = t.TournamentId,
                Name = t.Name,
                Winner1 = t.Winner1,
                Winner2 = t.Winner2,
                Winner3 = t.Winner3,
                Prize1 = t.Prize1,
                Prize2 = t.Prize2,
                Prize3 = t.Prize3,
                EntryFee = t.EntryFee,
                City = t.City,
                ServerDateTime = serverDateTime,
                StartDate = t.StartDate.Date,
                EndDate = t.EndDate.Date,

                // NEW: a Boolean telling the client if this player has already joined
                IsJoined = joinedIds.Contains(t.TournamentId)
            });

            return Ok(result);
        }

        // Get tournament by Id
        [HttpGet("{id:int}")]
        public IActionResult GetTournamentById(int id)
        {
            var t = _context.Tournaments.FirstOrDefault(t => t.TournamentId == id);
            DateTime serverDateTime = DateTime.Now;

            if (t != null)
            {
                return Ok(new
                {
                    TournamentId = t.TournamentId,
                    Name = t.Name,
                    Winner1 = t.Winner1,
                    Winner2 = t.Winner2,
                    Winner3 = t.Winner3,
                    Prize1 = t.Prize1,
                    Prize2 = t.Prize2,
                    Prize3 = t.Prize3,
                    EntryFee = t.EntryFee,
                    City = t.City,
                    ServerDateTime = serverDateTime,
                    StartDate = t.StartDate.Date,
                    EndDate = t.EndDate.Date
                });
            }

            return NotFound(new { Message = "Tournament not found." });
        }

        // Create new tournament
        [HttpPost("create")]
        public IActionResult CreateTournament([FromBody] TournamentDto tournamentDto)
        {
            if (tournamentDto == null)
            {
                return BadRequest(new { Message = "Invalid tournament data." });
            }

            // Map the DTO to the entity
            var tournament = new Tournament
            {
                Name = tournamentDto.Name,
                //TournamentWinner = tournamentDto.TournamentWinner,
                StartDate = tournamentDto.StartDate,
                EndDate = tournamentDto.EndDate,
                EntryFee = tournamentDto.EntryFee,
                Prize1 = tournamentDto.Prize1,
                Prize2 = tournamentDto.Prize2,
                Prize3 = tournamentDto.Prize3,
            };

            // Add the tournament to the database
            _context.Tournaments.Add(tournament);
            _context.SaveChanges();

            // Return the created tournament
            return CreatedAtAction(nameof(GetTournamentById), new { id = tournament.TournamentId }, tournament);
        }


        // Update tournament by Id
        [HttpPut("{id}")]
        public IActionResult UpdateTournament(int id, [FromBody] TournamentDto updatedTournamentDto)
        {
            var t = _context.Tournaments.FirstOrDefault(t => t.TournamentId == id);

            if (t == null)
            {
                return NotFound(new { Message = "Tournament not found." });
            }

            t.Name = updatedTournamentDto.Name;
            //tournament.TournamentWinner = updatedTournamentDto.TournamentWinner;
            t.StartDate = updatedTournamentDto.StartDate;
            t.EndDate = updatedTournamentDto.EndDate;
            t.EntryFee = updatedTournamentDto.EntryFee;
            t.Prize1 = updatedTournamentDto.Prize1;
            t.Prize2 = updatedTournamentDto.Prize2;
            t.Prize3 = updatedTournamentDto.Prize3;

            _context.SaveChanges();

            return Ok(t);
        }

        // Delete tournament by Id
        [HttpDelete("{id}")]
        public IActionResult DeleteTournament(int id)
        {
            var tournament = _context.Tournaments.FirstOrDefault(t => t.TournamentId == id);

            if (tournament == null)
            {
                return NotFound(new { Message = "Tournament not found." });
            }

            _context.Tournaments.Remove(tournament);
            _context.SaveChanges();

            return NoContent();
        }

        [HttpPost("join")]
        public IActionResult JoinTournament(int playerId, int tournamentId)
        {
            // 1) Check if the player already joined
            var existingChallenger = _context.TournamentChallengers
                .FirstOrDefault(tc => tc.TournamentId == tournamentId && tc.PlayerId == playerId);

            if (existingChallenger != null)
            {
                return Conflict(new { Message = "Player has already joined this tournament." });
            }

            // 2) Add the new challenger
            var tournamentChallenger = new TournamentChallenger
            {
                TournamentId = tournamentId,
                PlayerId = playerId,
                RetryCount = 1
            };
            _context.TournamentChallengers.Add(tournamentChallenger);
            _context.SaveChanges();

            // 3) Load the tournament entity that was just joined
            var t = _context.Tournaments
                .FirstOrDefault(x => x.TournamentId == tournamentId);

            if (t == null)
            {
                // (This should not happen, unless the TournamentId was invalid)
                return NotFound(new { Message = "Tournament not found." });
            }
            // 4) Pull all TournamentChallengers for this player in one query
            var joinedIds = _context.TournamentChallengers
                .Where(tc => tc.PlayerId == playerId)
                .Select(tc => tc.TournamentId)
                .ToHashSet();
            // 5) Return the tournament’s details (same shape as in GetAllTournaments)
            var serverDateTime = DateTime.Now;
            var result = new
            {
                TournamentId = t.TournamentId,
                Name = t.Name,
                Winner1 = t.Winner1,
                Winner2 = t.Winner2,
                Winner3 = t.Winner3,
                Prize1 = t.Prize1,
                Prize2 = t.Prize2,
                Prize3 = t.Prize3,
                EntryFee = t.EntryFee,
                City = t.City,
                ServerDateTime = serverDateTime,
                StartDate = t.StartDate.Date,
                EndDate = t.EndDate.Date,
                // NEW: a Boolean telling the client if this player has already joined
                IsJoined = joinedIds.Contains(t.TournamentId)
            };
            return Ok(result);
        }
    }
}