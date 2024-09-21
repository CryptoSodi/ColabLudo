namespace LudoClient.Constants
{
    public static class GlobalConstants
    {
        public static readonly int initialEntry = 5;
        public static readonly bool Debug = false;

        public static readonly string BaseUrl = Debug
            ? "https://192.168.1.13:7255/" // Development URL
            : "https://localhost:7255/"; // Production URL

        static readonly HttpClientHandler handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true; // Ignore SSL certificate errors for development
            }
        };
        
        public static readonly HttpClient httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl) // Set the base URL
        };
    }
}