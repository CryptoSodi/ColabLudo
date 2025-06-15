namespace SharedCode
{
    public class StateInfo
    {
        public int GamesPlayed { get; set; } = 0;
        public int GamesWon { get; set; } = 0;
        public int GamesLost { get; set; } = 0;
        public decimal BestWin { get; set; }
        public decimal TotalWin { get; set; }
        public decimal TotalLost { get; set; }
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
    public class GameDto
    {
        public string GameType { get; set; } = "2"; 
        public string RoomCode { get; set; } = ""; // Room code for joining games
        public decimal BetAmount { get; set; } = 0; // Default game cost
        public int PlayerCount { get; set; } = 2; // Default to 2 players
        public bool IsPracticeGame { get; set; } = false; // Flag for practice games
        public bool IsTournamentGame { get; set; } = false; // Flag for tournament games
        public bool IsPrivateGame { get; set; } = false; // Flag for private games
        public string playerColor { get; set; } = "";
    }
    public class TournamentDTO
    {
        public int TournamentId { get; set; }
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime ServerDateTime { get; set; }
        public decimal EntryFee { get; set; } = 0;
        public decimal Prize1 { get; set; } = 0;
        public decimal Prize2 { get; set; } = 0;
        public decimal Prize3 { get; set; } = 0;
        public string? Winner1 { get; set; }
        public string? Winner2 { get; set; }
        public string? Winner3 { get; set; }
        public bool IsJoined { get; set; } = false; // Flag to indicate if the player has joined the tournament
        public string StatusCode { get; set; }
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