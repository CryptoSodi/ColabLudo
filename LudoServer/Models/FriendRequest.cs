using System.Text.Json.Serialization;

namespace LudoServer.Models
{
    public class FriendRequest
    {
        public int FriendRequestId { get; set; }
        public int SenderId { get; set; }   // The player who sends the friend request
        public int ReceiverId { get; set; } // The player who receives the friend request

        [JsonIgnore] // Prevents circular reference during serialization
        public Player Sender { get; set; }  // Navigation property for sender player
        [JsonIgnore] // Prevents circular reference during serialization
        public Player Receiver { get; set; }  // Navigation property for receiver player

        public String Status { get; set; }  // Status of the friend request
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
