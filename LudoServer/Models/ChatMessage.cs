using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LudoServer.Models
{
    [Table("ChatMessages")]
    public class ChatMessage
    {
        [Key]
        public int Index { get; set; }

        public int SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderPicture { get; set; }
        public string? SenderColor { get; set; }

        public int ReceiverId { get; set; }
        public string? ReceiverName { get; set; }

        public string? Message { get; set; }
        public string? RoomCode { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

}