using System.Text.Json.Serialization;

namespace LudoClient.Models
{
    public class DailyBonus
    {
        public int DailyBonusId { get; set; }
        public int PlayerId { get; set; }
        public decimal Day1 { get; set; }
        public decimal Day2 { get; set; }
        public decimal Day3 { get; set; }
        public decimal Day4 { get; set; }
        public decimal Day5 { get; set; }
        public decimal Day6 { get; set; }
        public decimal Day7 { get; set; }
        public int DayCounter { get; set; }
        public bool DayState { get; set; }
        public DateTime DayStateChangeTime { get; set; }

        [JsonIgnore] // Prevents circular reference during serialization
        public Player Player { get; set; }
    }
}
