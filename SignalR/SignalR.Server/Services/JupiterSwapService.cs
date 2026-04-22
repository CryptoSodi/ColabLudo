using System.Net.Http.Headers;
using System.Text.Json;

namespace SignalR.Server.Services
{
    public class JupiterSwapService(HttpClient httpClient, IConfiguration configuration)
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = configuration["Jupiter:ApiKey"] ?? string.Empty;
        private readonly string _baseUrl = configuration["Jupiter:BaseUrl"] ?? "https://api.jup.ag";

        private HttpRequestMessage CreateRequest(HttpMethod method, string pathAndQuery)
        {
            var request = new HttpRequestMessage(method, $"{_baseUrl.TrimEnd('/')}{pathAndQuery}");
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.Add("x-api-key", _apiKey);
            }
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return request;
        }

        public async Task<JsonDocument> GetOrderAsync(string inputMint, string outputMint, string amountRaw, string taker, string? receiver, int slippageBps)
        {
            var query =
                $"/swap/v2/order?inputMint={Uri.EscapeDataString(inputMint)}" +
                $"&outputMint={Uri.EscapeDataString(outputMint)}" +
                $"&amount={Uri.EscapeDataString(amountRaw)}" +
                $"&slippageBps={slippageBps}" +
                $"&taker={Uri.EscapeDataString(taker)}";

            if (!string.IsNullOrWhiteSpace(receiver) &&
                !string.Equals(receiver, taker, StringComparison.Ordinal))
            {
                query += $"&receiver={Uri.EscapeDataString(receiver)}";
            }

            using var request = CreateRequest(HttpMethod.Get, query);
            using var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Jupiter order failed: {body}");
            }

            return JsonDocument.Parse(body);
        }

        public async Task<JsonDocument> ExecuteOrderAsync(string requestId, string signedTransactionBase64)
        {
            using var request = CreateRequest(HttpMethod.Post, "/swap/v2/execute");
            request.Content = JsonContent.Create(new
            {
                requestId,
                signedTransaction = signedTransactionBase64
            });

            using var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Jupiter execute failed: {body}");
            }

            return JsonDocument.Parse(body);
        }
    }
}
