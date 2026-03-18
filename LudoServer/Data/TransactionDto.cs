namespace LudoServer.Data
{
    public class TransactionDto
    {
        public int PlayerId { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal WithdrawAmount { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
