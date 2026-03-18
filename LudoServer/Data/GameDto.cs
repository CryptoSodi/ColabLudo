namespace LudoServer.Data
{
    public class GameDto
    {
        public string GameType { get; set; }
        public int? MultiPlayerId { get; set; }
        public int? TournamentId { get; set; }
        public decimal BetAmount { get; set; }
        public int Winner1 { get; set; }
        public int Winner2 { get; set; }
        public string Recording { get; set; }
        public string RoomCode { get; set; }
        public int Owner { get; set; }
        public string State { get; set; }
    }
}
