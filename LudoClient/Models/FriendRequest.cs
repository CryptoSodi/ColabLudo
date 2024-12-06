using System.Text.Json.Serialization;

namespace LudoClient.Models
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

        public FriendRequestStatus Status { get; set; }  // Status of the friend request
        public DateTime RequestDate { get; set; }
    }

    public enum FriendRequestStatus
    {
        Pending,
        Accepted,
        Rejected
    }
}
