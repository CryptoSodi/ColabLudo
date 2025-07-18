using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharedCode.Constants;
public class PlayerInfo
{
    public int PlayerId { get; set; }
    public string? GoogleId { get; set; }
    public string? AuthToken { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PictureUrl { get; set; }
    public string? PhoneNumber { get; set; } = "###########";
    public string? CountryCode { get; set; } = "###";
    public string? City { get; set; } = "###########";
    public string? Otp { get; set; }
    public DateTime RegisteredDate { get; set; }
    public DateTime LastLogin { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOnline { get; set; } = true;
    public int GamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int GamesLost { get; set; }
    [Column(TypeName = "decimal(18,8)")]
    public decimal BestWin { get; set; }
    [Column(TypeName = "decimal(18,8)")]
    public decimal TotalLost { get; set; }
    [Column(TypeName = "decimal(18,8)")]
    public decimal TotalWin { get; set; }
    public int Score { get; set; } = 0;
    public PlayerWallet? Wallet { get; set; }
}
public class PlayerWallet
{
    /// Primary key: corresponds to the user or sub-account ID.
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int WalletId { get; set; }
    public int PlayerId { get; set; }
    public string? AddressType { get; set; } = "";
    public string? WalletAddress { get; set; } = "";

    [Column(TypeName = "decimal(18,8)")]
    public decimal AvailableBalance { get; set; } = 0;

    [Column(TypeName = "decimal(18,8)")]
    public decimal UnUtilizedCoins { get; set; } = 0;
    [Column(TypeName = "decimal(18,8)")]
    public decimal ReferBonus { get; set; } = 0;
    [Column(TypeName = "decimal(18,8)")]
    public decimal SurpriseCoins { get; set; } = 0;
    [Column(TypeName = "decimal(18,8)")]
    public decimal SignupBonus { get; set; } = 0;
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
public class WalletTransaction
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int TransactionId { get; set; }
    public string? txId { get; set; }
    [Required]
    public int PlayerId { get; set; }
    public PlayerWallet? PlayerWallet { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(18,8)")]
    public decimal Amount { get; set; }
    [Column(TypeName = "decimal(18,8)")]
    public decimal BalanceAfter { get; set; }
    [Required]
    public TransactionType Type { get; set; }
    public string? Description { get; set; }
    public bool IsOnChain { get; set; }
    public string? RoomCode { get; set; }
}
/// Enum to classify transaction types.
public enum TransactionType
{
    Deposit = 1,
    Withdrawal = 2,
    GameWin = 3,
    GameLoss = 4,
    Sweep = 5
}