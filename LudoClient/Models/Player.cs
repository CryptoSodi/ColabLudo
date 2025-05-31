using System.Text.Json.Serialization;

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


        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<Wallet> Wallets { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<Transaction> Transactions { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<GameChallenger> GameChallengers { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<Analysis> Analyses { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<TournamentChallenger> TournamentChallengers { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<DailyBonus> DailyBonus { get; set; }

        // Self-referencing Many-to-Many Relationship for Friend Requests
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<FriendRequest> SentFriendRequests { get; set; }  // Player initiated requests
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<FriendRequest> ReceivedFriendRequests { get; set; }  // Requests received by the player
    }
}
