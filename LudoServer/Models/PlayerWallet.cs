using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LudoServer.Models
{
    public class PlayerWallet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WalletId { get; set; }
        [Required]
        public int PlayerId { get; set; }
        [Required]
        public string AddressType { get; set; } = "LUDC";
        [Required]
        public string WalletAddress { get; set; } = null!;
        [Column(TypeName = "decimal(18,8)")]
        public decimal AvailableBalance { get; set; } = 0m;
        // Prevents double-withdraw / race conditions
        public bool IsWithdrawalLocked { get; set; } = false;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    }
    public class PlayerWalletKey
    {
        public int PlayerId { get; set; }
        public string PublicKey { get; set; } = null!;
        public string EncryptedPrivateKey { get; set; } = null!; 
        [Required]
        public string AddressType { get; set; } = "LUDC";
        public bool IsMaster { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class WalletTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionId { get; set; }
        [Required]
        public int PlayerId { get; set; }
        // Idempotency key (VERY IMPORTANT)
        [Required]
        public Guid OperationId { get; set; } = Guid.NewGuid();
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal Amount { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal BalanceAfter { get; set; }
        [Required]
        public TransactionType Type { get; set; }
        [Required]
        public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Pending;
        public bool IsOnChain { get; set; } = false;
        public string? Description { get; set; }
        public string? RoomCode { get; set; }
        public string txId { get; set; } = "";
        [Required]
        public string AddressType { get; set; } = "LUDC";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
    /// Enum to classify transaction types.
    public enum WalletTransactionStatus
    {
        Pending = 0,
        Completed = 1,
        Failed = 2
    }
    public enum TransactionType
    {
        Deposit = 1,
        Withdrawal = 2,
        GameWin = 3,
        GameLoss = 4,
        Sweep = 5,
        DailyBonus = 6,
        Fee = 7
    }
}