using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LudoServer.Models
{
    public class CashWithdrawal
    {
        [Key]
        public int Id { get; set; }

        public int PlayerId { get; set; }

        [Column(TypeName = "decimal(18, 8)")]
        public decimal Amount { get; set; }

        public string PayoutMethod { get; set; } = "";

        public string DestinationDetails { get; set; } = "";

        public string Status { get; set; } = "Pending";

        public string? AdminNote { get; set; }

        public int? ProcessedByAdminId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedDate { get; set; }

        [ForeignKey("PlayerId")]
        public Player Player { get; set; }

        [ForeignKey("ProcessedByAdminId")]
        public Player? ProcessedByAdmin { get; set; }
    }
}
