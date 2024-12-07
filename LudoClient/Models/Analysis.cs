using System.Text.Json.Serialization;

namespace LudoClient.Models
{
    public class Analysis
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public decimal TotalLoss { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal TotalWithdraw { get; set; }
        public decimal TotalDeposit { get; set; }

        [JsonIgnore] // Prevents circular reference during serialization
        public Player Player { get; set; }
    }
}
