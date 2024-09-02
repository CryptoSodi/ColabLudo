namespace SignalR.Server
{
    public class GameRoom
    {
        public string RoomName { get; set; }
        public int GameType { get; set; }
        public int GameCost { get; set; }
        public List<User> Users { get; set; }

        public GameRoom(string roomName, int gameType, int gameCost)
        {
            RoomName = roomName;
            GameType = gameType;
            GameCost = gameCost;
            Users = new List<User>();
        }
    }
}
