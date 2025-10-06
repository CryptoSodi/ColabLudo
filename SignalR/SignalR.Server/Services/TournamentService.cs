using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;
using SharedCode;

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
        public async Task<TournamentResultDTO> GetResultsTournament(int tournamentId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var tournament = await ctx.Tournaments.FirstOrDefaultAsync(x => x.TournamentId == tournamentId);

            if (tournament == null)
                return null;

            var resultDto = new TournamentResultDTO
            {
                Seats = new List<SharedCode.PlayerDto>
                {
                    // TODO: Replace these with actual top players from your tournament logic!
                    new SharedCode.PlayerDto
                    {
                        PlayerId = 3,
                        PlayerColor = "Red",
                        PlayerName = "Syed Tassaduq",
                        PlayerPicture = "https://lh3.googleusercontent.com/a/ACg8ocLMYETsXNDf8wihXQej62uXHjuF67aNzDfoFgn7Tvp53eNu8Wux=s96-c"
                    },
                    new SharedCode.PlayerDto
                    {
                        PlayerId = 2,
                        PlayerColor = "Green",
                        PlayerName = "Sodi",
                        PlayerPicture = "https://yt3.ggpht.com/ytc/AIdro_nuNlfceTDiBSTQUhxQ56YDJFbBu1DjRfTpJMFP6ck9D0x3tsglom8eMUA2blBLpRVU8w=s108-c-k-c0x00ffffff-no-rj"
                    },
                    new SharedCode.PlayerDto
                    {
                        PlayerId = 4,
                        PlayerColor = "Yellow",
                        PlayerName = "Mazhar",
                        PlayerPicture = "https://lh3.googleusercontent.com/a/ACg8ocIbkj3BjuoGtaCnkdqwfXkk21UPGUuLLUZcCWlzwuIhCvsyKQ=s360-c-no"
                    }
                },
                Prize1 = tournament.Prize1,
                Prize2 = tournament.Prize2,
                Prize3 = tournament.Prize3,
                GameType = "3" // You can set this from tournament.GameType if you have it
            };

            return resultDto;
        }

        internal async Task<List<TournamentDTO>> GetAllTournaments(Player player, string type)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var nowUtc = DateTime.UtcNow;
            // 1) Begin queryable for efficiency
            IQueryable<Tournament> query = ctx.Tournaments.AsNoTracking();
            // 2) Apply tournament type filter
            if (!string.IsNullOrWhiteSpace(type))
            {
                query = type switch
                {
                    "Completed" => query.Where(t => nowUtc > t.EndDate),
                    "Running" => query.Where(t => nowUtc >= t.StartDate && nowUtc <= t.EndDate),
                    "Upcoming" => query.Where(t => nowUtc < t.StartDate),
                    _ => query // Return all if type is unknown
                };
            }
            var tournaments = query.ToList();
            // 3) If no tournaments found, return empty list
            if (tournaments.Count == 0)
                return new List<TournamentDTO>();

            // 3) Fetch all tournament IDs this player has joined
            var joinedIds = ctx.TournamentChallengers
                .Where(tc => tc.PlayerId == player.PlayerId)
                .Select(tc => tc.TournamentId)
                .ToHashSet();

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
                StartDate = t.StartDate.Date,
                EndDate = t.EndDate.Date,
                IsJoined = joinedIds.Contains(t.TournamentId)
            }).ToList();

            return result;
        }

        internal async Task<TournamentDTO> JoinTournament(Player player, int tournamentId, string NetworkFlag)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var tournament = await ctx.Tournaments.FirstOrDefaultAsync(x => x.TournamentId == tournamentId);
            if (tournament == null)
            {
                return await BuildTournamentDto(ctx, tournament, player.PlayerId, "NOTFOUND");
            }
            if (!_crypto.deductGameFee(player.PlayerId, tournament.TournamentId, "", true, tournament.EntryFee, NetworkFlag))
            {
                Console.WriteLine($"Game fee FAILED TO deduct for player {player.PlayerId}.");
                return await BuildTournamentDto(ctx, tournament, player.PlayerId, "INSUFFICIENT_BALANCE");
            }
            return await BuildTournamentDto(ctx, tournament, player.PlayerId, "JOINEND");
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
