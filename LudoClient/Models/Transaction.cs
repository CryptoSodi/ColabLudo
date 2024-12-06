using System.Text.Json.Serialization;

namespace LudoClient.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public int PlayerId { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal WithdrawAmount { get; set; }
        public DateTime TimeStamp { get; set; }

        [JsonIgnore] // Prevents circular reference during serialization
        public Player Player { get; set; }
    }
}
