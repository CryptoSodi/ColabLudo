using SharedCode.Network;
namespace SharedCode.Constants
{
    public static class GlobalConstants
    {
        public static readonly decimal initialEntry = (decimal)1;
        public static readonly bool Debug = false;
        public static readonly string HubUrl = (Debug ? "http://192.168.1.13" : "http://13.202.76.246") + ":8085/";
        public static Client MatchMaker;        
        public static string RoomCode { get; internal set; }
        public static double GameCost { get; internal set; }
    }
}