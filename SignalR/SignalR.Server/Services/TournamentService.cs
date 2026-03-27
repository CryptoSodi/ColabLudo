using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;
using SharedCode;
using SignalR.Server.Payments;

namespace SignalR.Server.Services
{
    public class TournamentService
    {
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly CryptoHelper _crypto;
        public TournamentService(IDbContextFactory<LudoDbContext> contextFactory, CryptoHelper crypto)
        {
            _contextFactory = contextFactory;
            _crypto = crypto;
        }
        public async Task ProcessTournamentLifecycle()
        {
            using var ctx = _contextFactory.CreateDbContext();
            var now = DateTime.UtcNow;

            // 1. Find Active tournaments that have ended
            var completedTournaments = await ctx.Tournaments
                .Where(t => t.TournamentState == State.Active && t.EndDate < now)
                .Include(t => t.TournamentChallengers)
                .ThenInclude(tc => tc.Player)
                .ToListAsync();

            foreach (var tournament in completedTournaments)
            {
                // 2. Assign Winners
                var top3 = tournament.TournamentChallengers
                    .OrderByDescending(tc => tc.Score)
                    .Take(3)
                    .ToList();

                if (top3.Count > 0) tournament.Winner1 = top3[0].Player.Name;
                if (top3.Count > 1) tournament.Winner2 = top3[1].Player.Name;
                if (top3.Count > 2) tournament.Winner3 = top3[2].Player.Name;

                // 3. Close the tournament
                tournament.TournamentState = State.Completed;

                // 4. Generate the next tournament of this type
                var nextTournament = new Tournament
                {
                    Name = tournament.Name,
                    City = tournament.City,
                    EntryFee = tournament.EntryFee,
                    Prize1 = tournament.Prize1,
                    Prize2 = tournament.Prize2,
                    Prize3 = tournament.Prize3,
                    TournamentState = State.Active,
                    StartDate = tournament.EndDate, // Start exactly where the last one ended
                    EndDate = CalculateNextEndDate(tournament.Name, tournament.EndDate),
                    CreatedDate = now
                };

                ctx.Tournaments.Add(nextTournament);
            }

            await ctx.SaveChangesAsync();
        }

        private DateTime CalculateNextEndDate(string name, DateTime lastEndDate)
        {
            // Ensure we are working with the Date part only to align with midnight UTC
            var baseDate = lastEndDate.Date;

            if (name.Contains("Daily")) return baseDate.AddDays(2); // Ends at start of the day after tomorrow
            if (name.Contains("Weekly")) 
            {
                // Align to the next Monday or specific day if preferred, 
                // here we just add 7 clear days to the midnight of last end date
                return baseDate.AddDays(8); 
            }
            if (name.Contains("Monthly"))
            {
                // Align to the 1st of the month after next
                var nextMonth = baseDate.AddMonths(1);
                return new DateTime(nextMonth.Year, nextMonth.Month, 1).AddMonths(1);
            }
            
            return baseDate.AddDays(2);
        }

        public async Task<TournamentResultDTO> GetResultsTournament(int tournamentId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var tournament = await ctx.Tournaments
                .Include(t => t.TournamentChallengers)
                .ThenInclude(tc => tc.Player)
                .FirstOrDefaultAsync(x => x.TournamentId == tournamentId);

            if (tournament == null)
                return null;

            // Fetch actual top 3 (or all) players from this specific tournament
            var topChallengers = tournament.TournamentChallengers
                .OrderByDescending(tc => tc.Score)
                .Take(3) // Typically we only show top 3 in the results popup
                .ToList();

            var resultDto = new TournamentResultDTO
            {
                TournamentId = tournament.TournamentId,
                Seats = topChallengers.Select((tc, index) => new SharedCode.PlayerDto
                {
                    PlayerId = tc.PlayerId,
                    PlayerName = tc.Player.Name,
                    PlayerPicture = tc.Player.PictureUrl ?? "user.webp",
                    PlayerColor = index switch { 0 => "Red", 1 => "Green", 2 => "Yellow", _ => "Blue" }
                }).ToList(),
                Prize1 = tournament.Prize1,
                Prize2 = tournament.Prize2,
                Prize3 = tournament.Prize3,
                GameType = "3" // 3-player results UI common for podiums
            };

            return resultDto;
        }

        internal async Task<List<TournamentDTO>> GetAllTournaments(Player player, string type)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var nowUtc = DateTime.UtcNow;
            
            // 1) Fetch tournaments that are Global OR match the player's city
            IQueryable<Tournament> query = ctx.Tournaments
                .AsNoTracking()
                .Where(t => t.City == "Global" || t.City == player.City);

            // 2) Apply status filter (Running, Upcoming, etc.)
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = type switch
                {
                    "Completed" => query.Where(t => nowUtc > t.EndDate),
                    "Running" => query.Where(t => nowUtc >= t.StartDate && nowUtc <= t.EndDate),
                    "Upcoming" => query.Where(t => nowUtc < t.StartDate),
                    _ => query
                };
            }

            var tournaments = await query.ToListAsync();

            // 3) Only mark as joined if a Challenger record exists for THIS player
            var joinedTournamentIds = await ctx.TournamentChallengers
                .Where(tc => tc.PlayerId == player.PlayerId)
                .Select(tc => tc.TournamentId)
                .ToListAsync();

            // 4) Build DTOs
            var result = tournaments.Select(t => new TournamentDTO
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
                ServerDateTime = nowUtc,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                IsJoined = joinedTournamentIds.Contains(t.TournamentId)
            }).ToList();

            return result;
        }

        internal async Task<TournamentDTO> JoinTournament(Player player, int tournamentId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var tournament = await ctx.Tournaments.FirstOrDefaultAsync(x => x.TournamentId == tournamentId);
            
            if (tournament == null)
                return null;

            // Check if already joined
            var existingChallenger = await ctx.TournamentChallengers
                .FirstOrDefaultAsync(tc => tc.TournamentId == tournamentId && tc.PlayerId == player.PlayerId);
            
            if (existingChallenger != null)
                return await BuildTournamentDto(ctx, tournament, player.PlayerId, "ALREADY_JOINED");

            // Deduct Fee
            if (!await _crypto.deductGameFee(player.PlayerId, tournament.TournamentId, "Tournament Entry", true, tournament.EntryFee))
            {
                return await BuildTournamentDto(ctx, tournament, player.PlayerId, "INSUFFICIENT_BALANCE");
            }

            // Create Challenger Record
            var challenger = new TournamentChallenger
            {
                TournamentId = tournamentId,
                PlayerId = player.PlayerId,
                Status = "JOINED",
                Score = 0,
                CreatedDate = DateTime.UtcNow
            };

            ctx.TournamentChallengers.Add(challenger);
            await ctx.SaveChangesAsync();

            return await BuildTournamentDto(ctx, tournament, player.PlayerId, "SUCCESS");
        }
        private async Task<TournamentDTO> BuildTournamentDto(LudoDbContext ctx, Tournament tournament, int playerId, String StatusCode = "SUCCESS")
        {
            var joinedIds = await ctx.TournamentChallengers
                .Where(tc => tc.PlayerId == playerId)
                .Select(tc => tc.TournamentId)
                .ToHashSetAsync();

            return new TournamentDTO
            {
                TournamentId = tournament.TournamentId,
                Name = tournament.Name,
                Winner1 = tournament.Winner1,
                Winner2 = tournament.Winner2,
                Winner3 = tournament.Winner3,
                Prize1 = tournament.Prize1,
                Prize2 = tournament.Prize2,
                Prize3 = tournament.Prize3,
                EntryFee = tournament.EntryFee,
                City = tournament.City,
                ServerDateTime = DateTime.UtcNow,
                StartDate = tournament.StartDate.Date,
                EndDate = tournament.EndDate.Date,
                IsJoined = joinedIds.Contains(tournament.TournamentId),
                StatusCode = StatusCode
            };
        }

    }
}
