namespace LudoServer.Data
{
    public class ChatMessageDTO
    {
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string? SenderPicture { get; set; }
        public string? SenderColor { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string Message { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
