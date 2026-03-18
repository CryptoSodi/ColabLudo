using System.Text.Json.Serialization;

namespace LudoServer.Models
{
    public class DailyBonus
    {
        public int DailyBonusId { get; set; }
        public int PlayerId { get; set; }
        public bool Day1 { get; set; }
        public bool Day2 { get; set; }
        public bool Day3 { get; set; }
        public bool Day4 { get; set; }
        public bool Day5 { get; set; }
        public bool Day6 { get; set; }
        public bool Day7 { get; set; }
        public int DayCounter { get; set; }
        public DateTime LastResetDate { get; set; }
        [JsonIgnore]
        public Player Player { get; set; }
    }
}