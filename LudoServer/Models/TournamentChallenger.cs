using System.Text.Json.Serialization;

namespace LudoServer.Models
{
    public class TournamentChallenger
    {
        public int Id { get; set; }
        public int? TournamentId { get; set; }
        public int PlayerId { get; set; }
        public int RetryCount { get; set; } = 0;
        public string Status { get; set; } = "JOINEND";
        public int Score { get; set; } = 0;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [JsonIgnore] // Prevents circular reference during serialization
        public Tournament Tournament { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public Player Player { get; set; }
    }
}