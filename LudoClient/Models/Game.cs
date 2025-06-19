namespace LudoClient.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public int PlayerCount { get; set; }
        public String GameType { get; set; }
        public string? RoomCode { get; set; }
        public int? MultiPlayerId { get; set; }
        public int? TournamentId { get; set; }
        public bool IsPrivate { get; set; } = false;
        public decimal BetAmount { get; set; }
        public int? Winner1 { get; set; }
        public int? Winner2 { get; set; }
        public int? Owner { get; set; }
        public string? State { get; set; }
        public string? Recording { get; set; }
    }
}
