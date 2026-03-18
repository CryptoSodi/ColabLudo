using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace LudoServer.Models
{
    public class MultiPlayer
    {                
        public int MultiPlayerId { get; set; }
        public int? RoomCode { get; set; }
        public int? P1 { get; set; }  // Nullable to allow for less than 4 players
        public int? P2 { get; set; }
        public int? P3 { get; set; }
        public int? P4 { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
