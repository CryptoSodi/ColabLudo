using System.Text.Json.Serialization;

namespace LudoClient.Models
{
    public class TournamentChallenger
    {
        public int Id { get; set; }
        public int TournamentId { get; set; }
        public int PlayerId { get; set; }
        public int RetryCount { get; set; }

        [JsonIgnore] // Prevents circular reference during serialization
        public Tournament Tournament { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public Player Player { get; set; }
    }
}
