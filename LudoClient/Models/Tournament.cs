using System.Text.Json.Serialization;

namespace LudoClient.Models
{
    public class Tournament
    {
        public int TournamentId { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string? Winner1 { get; set; }
        public string? Winner2 { get; set; }
        public string? Winner3 { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal EntryFee { get; set; }
        public decimal Prize1 { get; set; }
        public decimal Prize2 { get; set; }
        public decimal Prize3 { get; set; }
        public bool IsJoined { get; set; }
        public DateTime ServerDateTime { get; set; }
        public State TournamentState { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<Game> Games { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public ICollection<TournamentChallenger> TournamentChallengers { get; set; }
    }

    public enum State
    {
        Active,
        Inactive,
        Completed,
        Closed
    }
}
