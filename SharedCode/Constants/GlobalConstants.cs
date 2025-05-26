
using SharedCode.Network;
using System.Net;
using System.Net.Sockets;
namespace SharedCode.Constants
{
    public static class GlobalConstants
    {
        public static int GameHistorySaveIndex = 0;
        public static readonly HttpClient httpClient;
        public static readonly double initialEntry = 0.1;
        public static readonly bool Debug = false;
        public static readonly string Url;
        public static readonly string BaseUrl;
        public static readonly string HubUrl;
        public static Client MatchMaker;
        public static bool online = true;
        public static Random rnd = new Random();

        static GlobalConstants()
        {
                Url = Debug ? "http://192.168.1.50" : "http://3.143.14.201";
#if WINDOWS
if(Debug){
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4 address
                {
                    Console.WriteLine( ip.ToString());
                    Url = "http://"+ip.ToString();
                }
            }
#endif
            BaseUrl = Url.Replace("http:", "https:") + ":7255/";
            HubUrl  = Url + ":8085/";
            
            HttpClientHandler handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl) // Set the base URL
            };
        }

        public static int lastSeenIndex = -1;
        public static string RoomCode { get; internal set; }
        public static double GameCost { get; internal set; }
    }
}