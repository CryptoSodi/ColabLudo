namespace LudoClient.Models
{
    public class Tournament
    {
        public string TournamentName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int JoiningFee { get; set; }
        public int PrizeAmount { get; set; }
    }
}
