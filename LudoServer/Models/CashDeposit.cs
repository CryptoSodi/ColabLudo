using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LudoServer.Models
{
    public class CashDeposit
    {
        [Key]
        public int Id { get; set; }

        public int PlayerId { get; set; }
        
        [Column(TypeName = "decimal(18, 8)")]
        public decimal Amount { get; set; }

        public string ReferenceNumber { get; set; } = "";

        public string PaymentMethod { get; set; } = ""; // JazzCash, PayTM, etc.
        
        public string ReceiptImageUrl { get; set; } = "";
        
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        
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
