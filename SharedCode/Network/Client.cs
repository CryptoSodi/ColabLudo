using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using SharedCode.Constants;
using System.ComponentModel;
using System.Net.Http.Json;

namespace SharedCode.Network
{
    public class Client
    {
        private bool _connected;
        private readonly HttpClient _apiClient;
        public HubConnection _hubConnection { get; set; }

        // Event Definitions using standard .NET event patterns
        public event EventHandler<(string GameType, string seatsData, string rollsString)> GameStarted;
        public event EventHandler<(string GameType, double GameCost, string RoomCode)> RoomJoined;
        public event EventHandler<(string PlayerType, int PlayerId, string UserName, string PictureUrl)> PlayerSeated;
        public event EventHandler<(string seats, string GameType, string GameCost)> ShowResults;
        public event EventHandler<List<ChatMessages>> ReceiveChatMessage;
        public event EventHandler<NotificationDTO> ReceiveNotification;
        public event EventHandler<PlayerInfo> PlayerInfoUpdate;
        public event PropertyChangedEventHandler PropertyChanged;

        private CancellationTokenSource _pingCts;
        public bool Connected
        {
            get => _connected;
            set
            {
                if (_connected == value) return;
                _connected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Connected)));
            }
        }
        public Client()
        {
            _apiClient = CreateApiClient();
            Connected = false;
            _ = ConnectAsync();
        }

        private static HttpClient CreateApiClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(GlobalConstants.ApiUrl)
            };
        }

        private HttpRequestMessage CreateApiRequest(HttpMethod method, string path)
        {
            var request = new HttpRequestMessage(method, path);
            var authToken = getAuthToken();
            if (!string.IsNullOrWhiteSpace(authToken))
                request.Headers.Add("X-Auth-Token", authToken);
            return request;
        }
        private async Task StartHeartbeat()
        {
            _pingCts?.Cancel();
            _pingCts = new CancellationTokenSource();

            var token = _pingCts.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_hubConnection.State == HubConnectionState.Connected)
                        await _hubConnection.SendAsync("Ping", token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Heartbeat error: {ex}");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), token);
            }
        }
        private void RegisterHubEvents()
        {
            _hubConnection.Reconnected += async connectionId =>
            {
                Connected = true;
                Console.WriteLine("Connection lost. Reconnecting...");
                try
                {
                    await UserConnectedSetID();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Reconnect sync error: {ex}");
                }
            };
            _hubConnection.Reconnecting += error =>
            {
                Connected = false;
                Console.WriteLine("Connection lost. Reconnecting...");
                if (error != null)
                {
                    Console.WriteLine($"Reconnecting due to: {error.Message}");
                }
                return Task.CompletedTask;
            };
            _hubConnection.Closed += async error =>
            {
                Connected = false;
                Console.WriteLine("Connection closed.");
                if (error != null)
                {
                    Console.WriteLine($"Connection closed due to error: {error.Message}");
                }
            };
            // Player ReceiveMessage event
            _hubConnection.On<List<ChatMessages>>("ReceiveChatMessage", msgs =>
            {
                ReceiveChatMessage?.Invoke(this, msgs);
            });
            _hubConnection.On<NotificationDTO>("ReceiveNotification", notification =>
            {
                ReceiveNotification?.Invoke(this, notification);
            });
            _hubConnection.On<PlayerInfo>("PlayerInfoUpdate", playerInfo =>
            {
                // This lambda runs on a non-UI thread:
                PlayerInfoUpdate?.Invoke(this, (playerInfo));
            });
            // Player seat event
            _hubConnection.On<string, int, string, string>("PlayerSeat", (playerType, playerId, userName, pictureUrl) =>
            {
                Console.WriteLine($"{playerType}: {userName} has joined.");
                PlayerSeated?.Invoke(this, (playerType, playerId, userName, pictureUrl));
            });
            // Game start event
            _hubConnection.On<string, string, string>("GameStarted", (GameType, seatsData, rollsString) =>
            {
                //Game(GameType, playerCount, PlayerColor)
                GameStarted?.Invoke(this, (GameType, seatsData, rollsString));
                Console.WriteLine("Starting Game : " + DateTime.Now, GameType, seatsData);
            });
            _hubConnection.On<string, string, string>("ShowResults", (seats, GameType, GameCost) =>
            {
                Console.WriteLine("ShowResults : " + DateTime.Now, seats, GameType, GameCost);
                //Game(GameType, playerCount, PlayerColor)
                ShowResults?.Invoke(this, (seats, GameType, GameCost));
            });
            // Message event
            _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
            {   
                Console.WriteLine($"{user} says {message}");
            });
        }
        /// Establish the connection to the server asynchronously.
        public async Task ConnectAsync()
        {
            if (_hubConnection == null)
            {
                // Build connection with automatic reconnect
                _hubConnection = new HubConnectionBuilder().WithUrl(GlobalConstants.HubUrl + "LudoHub", options =>
                {
                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                            clientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        return handler;
                    };
                    options.CloseTimeout = TimeSpan.FromSeconds(30);
                }).WithAutomaticReconnect().WithStatefulReconnect().ConfigureLogging(logging => logging.AddDebug().SetMinimumLevel(LogLevel.Debug))
                .Build();
            }
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                Connected = true;
                await UserConnectedSetID();
                Console.WriteLine("Already connected.");
                return;
            }
            try
            {
                RegisterHubEvents();
                await _hubConnection.StartAsync();//.ConfigureAwait(false);
                Connected = true;
                await UserConnectedSetID();
                _ = StartHeartbeat();
                Console.WriteLine("Connection started. Waiting for messages from the server...");
            }
            catch (Exception ex)
            {
                Connected = false;
                Console.WriteLine($"Failed to start the connection: {ex.Message}");
                // Consider retry logic here if desired
            }
        }
        /// Disconnect from the server.
        public async Task DisconnectAsync()
        {
            if (_hubConnection == null) return;
            if (_hubConnection.State == HubConnectionState.Disconnected) return;

            try
            {
                await _hubConnection.StopAsync().ConfigureAwait(false);
                Console.WriteLine("Disconnected from the server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while disconnecting: {ex.Message}");
            }
        }
        public async Task CreateJoinLobbyAsync(GameDto gameDto)//string gameType, double gameCost, string roomCode
        {
            try
            {
                String roomCode = await _hubConnection.InvokeAsync<string>("CreateJoinLobby", gameDto).ConfigureAwait(false);
                Console.WriteLine($"Joined room: {roomCode}");
                RoomJoined?.Invoke(this, (gameDto.GameType, (double)gameDto.BetAmount, roomCode));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateJoinLobbyAsync: {ex.Message}");
            }
        }
        public async Task<GameCommand> SendMessageAsync(GameCommand commandValue, string command)
        {
            String AuthToken = getAuthToken();
            if (AuthToken == "")
                return null;
            try
            {
                GameCommand result = await _hubConnection.InvokeAsync<GameCommand>("Send", AuthToken, commandValue, command, GlobalConstants.RoomCode).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                return null;
            }
        }
        public async Task ReadyAsync()
        {
            try
            {
                string result = await _hubConnection.InvokeAsync<string>("Ready").ConfigureAwait(false);
                Console.WriteLine($"Ready acknowledged for room: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReadyAsync: {ex.Message}");
            }
        }
        public void LeaveCloseLobby()
        {
            String AuthToken = getAuthToken();
            if (AuthToken == "")
                return;
            if (GlobalConstants.RoomCode != "")
            {
                _ = _hubConnection.InvokeAsync("LeaveCloseLobby").ConfigureAwait(false);
                GlobalConstants.RoomCode = "";
            }
        }
        public async Task<List<GameCommand>> PullCommands(int lastSeenIndex, string RoomCode)
        {
            return await _hubConnection.InvokeAsync<List<GameCommand>>("PullCommands", lastSeenIndex, RoomCode).ConfigureAwait(false);
        }
        public async Task<List<ChatMessages>> SendChatMessageAsync(ChatMessages CM, string roomCode)
        {
            try
            {
                List<ChatMessages> result = await _hubConnection.InvokeAsync<List<ChatMessages>>("SendChatMessage", CM, roomCode).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                return null;
            }
        }
        public async Task<PlayerInfo> UserConnectedSetID()
        {
            String AuthToken = getAuthToken();
            if (AuthToken == "")
                return null;
            return await _hubConnection.InvokeAsync<PlayerInfo>("UserConnectedSetID", AuthToken).ConfigureAwait(false);
        }
        public async Task<string> InitiateWithdrawal(string destination, decimal amount)
        {
             try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/withdrawals/initiate");
                request.Content = JsonContent.Create(new { Destination = destination, Amount = amount });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return "Request failed";

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (result.StartsWith("Success:", StringComparison.OrdinalIgnoreCase))
                    await RefreshPlayerInfoFromApi();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] InitiateWithdrawal Error: {ex.Message}");
                return "Error";
            }
        }
        internal async Task<List<TournamentDTO>> GetAllTournaments(string type)
        {
            return await _hubConnection.InvokeAsync<List<TournamentDTO>>("GetAllTournaments", type).ConfigureAwait(false);
        }
        internal async Task<TournamentDTO> JoinTournament(int TournamentID)
        {
            return await _hubConnection.InvokeAsync<TournamentDTO>("JoinTournament", TournamentID).ConfigureAwait(false);
        }
        internal async Task<TournamentResultDTO> GetResultsTournament(int TournamentID)
        {
            return await _hubConnection.InvokeAsync<TournamentResultDTO>("GetResultsTournament", TournamentID).ConfigureAwait(false);
        }
        public string getAuthToken()
        {
            return Preferences.Get("AuthToken", "");
        }

        public async Task<T?> GetDailyBonus<T>()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/daily-bonus");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return default;

                return await response.Content.ReadFromJsonAsync<T>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetDailyBonus Error: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> ClaimTodayBonus<T>()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/daily-bonus/claim");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return default;

                return await response.Content.ReadFromJsonAsync<T>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] ClaimTodayBonus Error: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> GetProfile<T>()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/profile");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return default;

                return await response.Content.ReadFromJsonAsync<T>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetProfile Error: {ex.Message}");
                return default;
            }
        }

        public async Task<T?> GetWallet<T>()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/wallet");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return default;

                return await response.Content.ReadFromJsonAsync<T>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetWallet Error: {ex.Message}");
                return default;
            }
        }

        private async Task RefreshPlayerInfoFromApi()
        {
            var playerInfo = await GetProfile<PlayerInfo>().ConfigureAwait(false);
            if (playerInfo == null)
                return;

            UserInfo.Instance.player = playerInfo;
            PlayerInfoUpdate?.Invoke(this, playerInfo);
        }

        public async Task<List<PlayerCard>> GetFriends(string type = "All")
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, $"api/friends?type={Uri.EscapeDataString(type)}");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<PlayerCard>();

                return await response.Content.ReadFromJsonAsync<List<PlayerCard>>().ConfigureAwait(false) ?? new List<PlayerCard>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetFriends Error: {ex.Message}");
                return new List<PlayerCard>();
            }
        }

        public async Task<string> SendFriendRequest(int receiverId, string status)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/friends/request");
                request.Content = JsonContent.Create(new { ReceiverId = receiverId, Status = status });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return "Error";

                return await response.Content.ReadFromJsonAsync<string>().ConfigureAwait(false) ?? "Error";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] SendFriendRequest Error: {ex.Message}");
                return "Error";
            }
        }

        public async Task<PlayerCard> GetPlayerById(int playerId)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, $"api/players/{playerId}/card");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<PlayerCard>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetPlayerById Error: {ex.Message}");
                return null;
            }
        }

        public async Task<List<PlayerCard>> GetLeaderboard()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/leaderboard");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<PlayerCard>();

                return await response.Content.ReadFromJsonAsync<List<PlayerCard>>().ConfigureAwait(false) ?? new List<PlayerCard>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetLeaderboard Error: {ex.Message}");
                return new List<PlayerCard>();
            }
        }

        public async Task<List<PlayerCard>> GetTournamentLeaderboard(string tournamentType)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, $"api/leaderboard/tournament/{Uri.EscapeDataString(tournamentType)}");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<PlayerCard>();

                return await response.Content.ReadFromJsonAsync<List<PlayerCard>>().ConfigureAwait(false) ?? new List<PlayerCard>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetTournamentLeaderboard Error: {ex.Message}");
                return new List<PlayerCard>();
            }
        }

        public async Task<string> MintNFT(int amount)
        {
            return await _hubConnection.InvokeAsync<string>("MintNFT", amount).ConfigureAwait(false);
        }
        public async Task<object> GetWalletBalance(string walletAddress)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, $"api/payments/wallet-balance?walletAddress={Uri.EscapeDataString(walletAddress)}");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new { Success = false, Error = "Request failed" };

                return await response.Content.ReadFromJsonAsync<object>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetWalletBalance Error: {ex.Message}");
                return new { Success = false, Error = ex.Message };
            }
        }
        public async Task<object> GetSwapBalances(string walletAddress)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, $"api/payments/swap-balances?walletAddress={Uri.EscapeDataString(walletAddress)}");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new { Success = false, Error = "Request failed" };

                return await response.Content.ReadFromJsonAsync<object>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetSwapBalances Error: {ex.Message}");
                return new { Success = false, Error = ex.Message };
            }
        }

        public async Task<BlockchainResult> BroadcastTransaction(string txBase64)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/transactions/broadcast");
                request.Content = JsonContent.Create(new { TxBase64 = txBase64 });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new BlockchainResult { Success = false, Error = "Request failed" };

                return await response.Content.ReadFromJsonAsync<BlockchainResult>().ConfigureAwait(false)
                    ?? new BlockchainResult { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] BroadcastTransaction Error: {ex.Message}");
                return new BlockchainResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<BlockchainResult> ExecutePreparedSwap(string requestId, string signedTxBase64)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/swap/execute");
                request.Content = JsonContent.Create(new { RequestId = requestId, SignedTxBase64 = signedTxBase64 });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new BlockchainResult { Success = false, Error = "Request failed" };

                return await response.Content.ReadFromJsonAsync<BlockchainResult>().ConfigureAwait(false)
                    ?? new BlockchainResult { Success = false, Error = "Empty response" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] ExecutePreparedSwap Error: {ex.Message}");
                return new BlockchainResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<bool> ConfirmSolanaTransaction(string signature)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/transactions/confirm");
                request.Content = JsonContent.Create(new { Signature = signature });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return false;

                return await response.Content.ReadFromJsonAsync<bool>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] ConfirmSolanaTransaction Error: {ex.Message}");
                return false;
            }
        }

        public async Task<object> PrepareAssetSwap(string walletAddress, string inputAsset, string outputAsset, decimal amount, int slippageBps)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/swap/prepare");
                request.Content = JsonContent.Create(new
                {
                    WalletAddress = walletAddress,
                    InputAsset = inputAsset,
                    OutputAsset = outputAsset,
                    Amount = amount,
                    SlippageBps = slippageBps
                });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new { Success = false, Error = "Request failed" };

                return await response.Content.ReadFromJsonAsync<object>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] PrepareAssetSwap Error: {ex.Message}");
                return new { Success = false, Error = ex.Message };
            }
        }

        public async Task<object> PrepareLudcDeposit(string walletAddress, decimal amount)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/ludc-deposit/prepare");
                request.Content = JsonContent.Create(new { WalletAddress = walletAddress, Amount = amount });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<object>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] PrepareLudcDeposit Error: {ex.Message}");
                return null;
            }
        }

        public async Task<string> SubmitManualDeposit(int playerId, decimal amount, string method, string referenceNumber, string receiptUrl)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/manual-deposits");
                request.Content = JsonContent.Create(new
                {
                    PlayerId = playerId,
                    Amount = amount,
                    Method = method,
                    ReferenceNumber = referenceNumber,
                    ReceiptUrl = receiptUrl
                });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return "Request failed";

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] SubmitManualDeposit Error: {ex.Message}");
                return "Error";
            }
        }

        public async Task<string> SubmitManualWithdrawal(decimal amount, string method, string destinationDetails)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/manual-withdrawals");
                request.Content = JsonContent.Create(new
                {
                    Amount = amount,
                    Method = method,
                    DestinationDetails = destinationDetails
                });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return "Request failed";

                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] SubmitManualWithdrawal Error: {ex.Message}");
                return "Error";
            }
        }
    }
}
