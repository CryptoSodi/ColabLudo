using System.Diagnostics;

namespace LudoClient.Constants
{
    public static class GlobalConstants
    {
        static GlobalConstants()
        {
            #if WINDOWS
                Debug = false;
            #elif ANDROID
                Debug = true;
            #endif

            BaseUrl = Debug ? "https://192.168.1.21:7255/" : "https://localhost:7255/";

            httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl) // Set the base URL
            };
        }
        public static readonly HttpClient httpClient;
        public static readonly int initialEntry = 5;
        public static readonly bool Debug = false;
        public static readonly string BaseUrl;

        static readonly HttpClientHandler handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true; // Ignore SSL certificate errors for development
            }
        };
    }
}