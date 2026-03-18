namespace LudoServer.Models
{
    public class OtpRequest
    {
        public int playerId { get; set; }
        public string PhoneNumber { get; set; }
        public string countryCode { get; set; }
        public string country { get; set; }
        public string regionName { get; set; }
        public string city { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
