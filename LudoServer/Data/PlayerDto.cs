namespace LudoServer.Data
{
    public class PlayerDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Otp { get; set; }
        public string? PictureUrl { get; set; }
        public decimal? PlayerLudoCoins { get; set; }        
    }
}