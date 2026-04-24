namespace SharedCode.Constants;

public class WalletBonusHistoryItem
{
    public int Id { get; set; }
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string Description { get; set; } = "";
    public string TransactionReference { get; set; } = "";
    public DateTime CreatedDate { get; set; }
}

public class WalletDepositHistoryItem
{
    public int Id { get; set; }
    public bool IsManual { get; set; }
    public string Source { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string ReferenceNumber { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string Description { get; set; } = "";
    public string ReceiptImageUrl { get; set; } = "";
    public string TransactionReference { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? AdminNote { get; set; }
}

public class WalletWithdrawalHistoryItem
{
    public int Id { get; set; }
    public bool IsManual { get; set; }
    public string Destination { get; set; } = "";
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string Method { get; set; } = "";
    public string Description { get; set; } = "";
    public string TransactionReference { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string? AdminNote { get; set; }
}

public class WalletGameHistoryItem
{
    public int Id { get; set; }
    public string RoomCode { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Result { get; set; } = "";
    public decimal BetAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public int? TournamentId { get; set; }
    public string TournamentName { get; set; } = "";
    public List<string> Players { get; set; } = new();
    public List<string> Winners { get; set; } = new();
    public DateTime CreatedDate { get; set; }
}
