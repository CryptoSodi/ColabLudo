namespace SignalR.Server
{
    public class GameRoom
    {
        public string RoomName { get; set; }
        public string GameType { get; set; }
        public decimal GameCost { get; set; }
        public List<User> Users { get; set; }
        public GameRoom(string roomName, string gameType, decimal gameCost)
        {
            RoomName = roomName;
            GameType = gameType;
            GameCost = gameCost;
            Users = new List<User>();
        }
    }
}