namespace LudoClient.Models
{
    public class Game
    {
        public int GameId { get; set; }
        public string Type { get; set; }
        public string? RoomCode { get; set; }
        public int? MultiPlayerId { get; set; }
        public int? TournamentId { get; set; }
        public decimal BetAmount { get; set; }
        public string? Winner { get; set; }
        public string? Owner { get; set; }
        public string? State { get; set; }
        public string? Recording { get; set; }
    }
}
