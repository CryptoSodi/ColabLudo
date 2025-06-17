namespace LudoClient.Models
{
    public class Player
    {
        public int PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Otp { get; set; }
        public string? PlayerPicture { get; set; }  
        public decimal? PlayerLudoCoins { get; set; }
        public decimal? PlayerCryptoCoins { get; set; }
        public string? Country { get; set; }
        public DateTime RegisteredDate { get; set; }
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
    }
}