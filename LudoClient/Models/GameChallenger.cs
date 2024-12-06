using System.Text.Json.Serialization;

namespace LudoClient.Models
{
    public class GameChallenger
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameId { get; set; }
        public decimal WinAmount { get; set; }
        public decimal LossAmount { get; set; }

        [JsonIgnore] // Prevents circular reference during serialization
        public Player Player { get; set; }
        [JsonIgnore] // Prevents circular reference during serialization
        public Game Game { get; set; }
    }
}
