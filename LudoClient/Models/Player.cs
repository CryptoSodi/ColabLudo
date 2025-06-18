namespace LudoClient.Models
{
    public class Player
    {
        public int PlayerId { get; set; }
        public string GoogleId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PictureUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public decimal? PlayerLudoCoins { get; set; } = 0;
        public string? CountryCode { get; set; }
        public string? City { get; set; }
        public string? Otp { get; set; }
        public DateTime RegisteredDate { get; set; }
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; } = true;

        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        public decimal BestWin { get; set; }
        public decimal TotalLost { get; set; }
        public decimal TotalWin { get; set; }
        public int Score { get; set; }
    }
}