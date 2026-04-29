using SharedCode.Network;
namespace SharedCode.Constants
{
    public static class GlobalConstants
    {
        public static readonly decimal initialEntry = (decimal)1;
        public static readonly bool Debug = true;
        public static readonly string HubUrl = (Debug ? "http://192.168.1.8" + ":8085/" : "https://signal.ludocities.com/");
        public static readonly string ApiUrl = (Debug ? "http://192.168.1.13" + ":8086/" : "https://signal.ludocities.com/");
        public static Client MatchMaker;
        public static string RoomCode { get; internal set; } = "";
        public static double GameCost { get; internal set; }
    }
}
