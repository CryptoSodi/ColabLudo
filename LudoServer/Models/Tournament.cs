using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LudoServer.Models
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
        [Column(TypeName = "decimal(18,8)")]
        public decimal EntryFee { get; set; }
        [Column(TypeName = "decimal(18,8)")]
        public decimal Prize1 { get; set; }
        [Column(TypeName = "decimal(18,8)")]
        public decimal Prize2 { get; set; }
        [Column(TypeName = "decimal(18,8)")]
        public decimal Prize3 { get; set; }
        public bool IsRepeatable { get; set; } = false;
        public State TournamentState { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
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