using LudoServer.Data;
using LudoServer.Models;
using Microsoft.EntityFrameworkCore;

namespace SignalR.Server.Services
{
    public class DailyBonusService
    {
        private readonly IDbContextFactory<LudoDbContext> _contextFactory;
        private readonly CryptoHelper _crypto;

        public DailyBonusService(IDbContextFactory<LudoDbContext> contextFactory, CryptoHelper crypto)
        {
            _contextFactory = contextFactory;
            _crypto = crypto;
        }
        public async Task<DailyBonusDto> GetDailyBonus(Player player)
        {
            using var ctx = _contextFactory.CreateDbContext();

            // Fetch the record (or null)
            var bonus = await ctx.DailyBonus.FirstOrDefaultAsync(x => x.PlayerId == player.PlayerId);
            var now = DateTime.UtcNow;
            var today = now.Date;
            var weekdayIndex = (int)now.DayOfWeek; // Sunday=0, Monday=1, …

            if (bonus == null)
            {
                // First‐time setup
                bonus = new DailyBonus
                {
                    PlayerId = player.PlayerId,
                    Day1 = false,
                    Day2 = false,
                    Day3 = false,
                    Day4 = false,
                    Day5 = false,
                    Day6 = false,
                    Day7 = false,
                    DayCounter = weekdayIndex,
                    LastResetDate = today.AddDays(-1)
                };
                ctx.DailyBonus.Add(bonus);
            }
            else if (bonus.LastResetDate < today && weekdayIndex == 1)
            {
                bonus.Day1 = bonus.Day2 = bonus.Day3 = bonus.Day4 =
                bonus.Day5 = bonus.Day6 = bonus.Day7 = false;

                // Reset your counter back to Monday (1)
                bonus.DayCounter = weekdayIndex;
            }
            // else: same day, nothing to reset
            try
            {
                await ctx.SaveChangesAsync();
            }
            catch (Exception)
            {
            }
            return new DailyBonusDto
            {
                DailyBonusId = bonus.DailyBonusId,
                PlayerId = bonus.PlayerId,
                Day1 = bonus.Day1,
                Day2 = bonus.Day2,
                Day3 = bonus.Day3,
                Day4 = bonus.Day4,
                Day5 = bonus.Day5,
                Day6 = bonus.Day6,
                Day7 = bonus.Day7,
                Bonus = 10,
                DayCounter = weekdayIndex
            };
        }
        // New function: Claim today's bonus and update LastResetDate
        public async Task<DailyBonusDto> ClaimTodayBonus(Player player)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var bonus = await ctx.DailyBonus.FirstOrDefaultAsync(x => x.PlayerId == player.PlayerId);
            var today = DateTime.UtcNow.Date;
            var weekdayIndex = (int)DateTime.UtcNow.DayOfWeek; // Sunday=0, Monday=1, …

            if (bonus == null)
            {
                // Initialize record if missing
                bonus = new DailyBonus
                {
                    PlayerId = player.PlayerId,
                    Day1 = false,
                    Day2 = false,
                    Day3 = false,
                    Day4 = false,
                    Day5 = false,
                    Day6 = false,
                    Day7 = false,
                    DayCounter = weekdayIndex,
                    LastResetDate = today
                };
                ctx.DailyBonus.Add(bonus);
            }

            bool alreadyClaimed = weekdayIndex switch
            {
                0 => bonus.Day1,
                1 => bonus.Day2,
                2 => bonus.Day3,
                3 => bonus.Day4,
                4 => bonus.Day5,
                5 => bonus.Day6,
                6 => bonus.Day7,
                _ => true
            };

            if (!alreadyClaimed)
            {
                // Mark today's day flag
                switch (weekdayIndex)
                {
                    case 0: bonus.Day1 = true; break;
                    case 1: bonus.Day2 = true; break;
                    case 2: bonus.Day3 = true; break;
                    case 3: bonus.Day4 = true; break;
                    case 4: bonus.Day5 = true; break;
                    case 5: bonus.Day6 = true; break;
                    case 6: bonus.Day7 = true; break;
                }

                // Update LastResetDate to today
                bonus.LastResetDate = today;
                bonus.DayCounter = weekdayIndex;

                await ctx.SaveChangesAsync();

                // Transfer bonus logic here
                int bonusAmount = 10;
                //await TransferBonusToPlayer(playerId, bonusAmount); // <- Your own logic/method
                bool credited = _crypto.OffChainTransaction(player.PlayerId, bonusAmount, "Daily Bonus", "", false, "");
            }

            return new DailyBonusDto
            {
                DailyBonusId = bonus.DailyBonusId,
                PlayerId = bonus.PlayerId,
                Day1 = bonus.Day1,
                Day2 = bonus.Day2,
                Day3 = bonus.Day3,
                Day4 = bonus.Day4,
                Day5 = bonus.Day5,
                Day6 = bonus.Day6,
                Day7 = bonus.Day7,
                Bonus = 10,
                DayCounter = weekdayIndex
            };
        }
    }
}
