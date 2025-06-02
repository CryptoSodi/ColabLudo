namespace SharedCode
{
    public class StateInfo
    {
        public int GamesPlayed { get; set; } = 0;
        public int GamesWon { get; set; } = 0;
        public int GamesLost { get; set; } = 0;
        public decimal BestWin { get; set; } = 0;
        public decimal TotalWin { get; set; } = 0;
        public decimal TotalLost { get; set; } = 0;
        public int Score { get; set; } = 0;
        public string? PhoneNumber { get; internal set; }
    }
    public class DepositInfo
    {
        public string Address { get; set; } = "";
        public string SolBalance { get; set; } = "";
    }

    public class PlayerDto
    {
        public int PlayerId { get; set; }
        public string? PlayerName { get; set; }
        public string? PlayerPicture { get; set; }
        public string? PlayerColor { get; set; }
    }
    public class GameCommand
    {
        // Your command properties
        public string SendToClientFunctionName { get; set; }
        public string seatName { get; set; }
        public string diceValue { get; set; }
        public string piece1 { get; set; }
        public string piece2 { get; set; }
        // Index to uniquely identify the command order
        public int Index { get; set; }
        public int IndexServer { get; set; }
        public String Result { get; set; }
    }

    public class PlayerCard
    {
        public int playerID { get; set; }
        public String name { get; set; }
        public String pictureUrl { get; set; }
        public int rank { get; set; }
        public String status { get; set; }
        public bool lastGame { get; set; }
    }
    public class ChatMessages
    {
        public int Index { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string? SenderColor { get; set; }
        public string? SenderPicture { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
    }
}