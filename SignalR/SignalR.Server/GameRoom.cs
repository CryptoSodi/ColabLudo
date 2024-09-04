namespace SignalR.Server
{
    public class GameRoom
    {
        public string RoomName { get; set; }
        public string GameType { get; set; }
        public int GameCost { get; set; }
        public List<User> Users { get; set; }
        public GameRoom(string roomName, string gameType, int gameCost)
        {
            RoomName = roomName;
            GameType = gameType;
            GameCost = gameCost;
            Users = new List<User>();
        }
    }
}