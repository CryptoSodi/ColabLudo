using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace LudoClient.Models
{
    public class MultiPlayer
    {
        [Key] // Ensures this is the primary key
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Makes it auto-increment
        public int Id { get; set; }
        public int MultiPlayerId { get; set; }
        public int? P1 { get; set; }  // Nullable to allow for less than 4 players
        public int? P2 { get; set; }
        public int? P3 { get; set; }
        public int? P4 { get; set; }
    }
}
