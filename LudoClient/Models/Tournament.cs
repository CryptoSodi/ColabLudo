namespace LudoClient.Models
{
    public class Tournament
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; }
        public string TournamentWinner { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal PrizeAmount { get; set; }
    }
}
