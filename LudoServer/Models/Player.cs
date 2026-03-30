using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LudoServer.Models
{
    public class Player
    {
        public int PlayerId { get; set; }
        public string? GoogleId { get; set; }
        public string? AuthToken { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PictureUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CountryCode { get; set; }
        public string? City { get; set; }
        public string? Otp { get; set; }
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public bool IsOnline { get; set; } = true;
        public int GamesPlayed { get; set; }
        public int GamesWon { get; set; }
        public int GamesLost { get; set; }
        [Column(TypeName = "decimal(18,8)")]
        public decimal BestWin { get; set; }
        [Column(TypeName = "decimal(18,8)")]
        public decimal TotalLost { get; set; }
        [Column(TypeName = "decimal(18,8)")]
        public decimal TotalWin { get; set; }
        public int Score { get; set; }
        public string? Role { get; set; } = "Player";// e.g., "Player", "Admin", etc.
        public bool IsBlocked { get; set; } = false;
        public DateTime CreatedDate { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<PlayerWallet> Wallets { get; set; }

        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<TournamentChallenger> TournamentChallengers { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<DailyBonus> DailyBonus { get; set; }
        // Self-referencing Many-to-Many Relationship for Friend Requests
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<FriendRequest> SentFriendRequests { get; set; }  // Player initiated requests
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<FriendRequest> ReceivedFriendRequests { get; set; }  // Requests received by the player
        [JsonIgnore] // Prevents circular reference during serialization
        [NotMapped]
        public DateTime LastPingUtc { get; set; } = DateTime.UtcNow;
    }
}
