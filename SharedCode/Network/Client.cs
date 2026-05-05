using SharedCode.Constants;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;

namespace SharedCode.Network
{
    public class Client
    {
        private bool _connected;
        private static readonly HttpClient SharedApiClient = CreateApiClient();
        private readonly HttpClient _apiClient;
        private readonly Dictionary<string, int> _lastKnownLobbySeats = new(StringComparer.Ordinal);
        private string _startedRaisedForRoom = string.Empty;
        private CancellationTokenSource? _chatPollingCts;
        private Task? _chatPollingTask;
        private int _lastSeenRoomChatIndex;
        private int _lastSeenPrivateChatIndex;
        private string _lastPolledRoomCode = string.Empty;
        // Event Definitions using standard .NET event patterns
        public event EventHandler<(string GameType, string seatsData, string rollsString)> GameStarted;
        public event EventHandler<(string seats, string GameType, string GameCost)> ShowResults;
        public event EventHandler<(string PlayerType, int PlayerId, string UserName, string PictureUrl)> PlayerSeated;
        
        public event EventHandler<(string GameType, double GameCost, string RoomCode)> RoomJoined;
        public event EventHandler<List<ChatMessages>> ReceiveChatMessage;
        public event EventHandler<NotificationDTO> ReceiveNotification;
        public event EventHandler<PlayerInfo> PlayerInfoUpdate;
        public event PropertyChangedEventHandler PropertyChanged;

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
            _apiClient = SharedApiClient;
            Connected = false;
        }

        private static HttpClient CreateApiClient()
        {
            var handler = new SocketsHttpHandler
            {
                SslOptions =
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                },
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                PooledConnectionLifetime = TimeSpan.FromMinutes(30),
                EnableMultipleHttp2Connections = false
            };

            return new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri(GlobalConstants.ApiUrl),
                DefaultRequestVersion = HttpVersion.Version30,
                DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher
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
        /// Establish the connection to the server asynchronously.
        public async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(getAuthToken()))
            {
                Connected = false;
                return;
            }
            await RefreshSessionFromApi();
            Connected = true;
            StartChatPolling();
        }
        /// Disconnect from the server.
        public async Task DisconnectAsync()
        {
            StopChatPolling();
            Connected = false;
            await Task.CompletedTask;
        }
        public async Task CreateJoinLobbyAsync(GameDto gameDto)//string gameType, double gameCost, string roomCode
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/gameplay/lobbies/join");
                request.Content = JsonContent.Create(new { Game = gameDto });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return;

                var joined = await response.Content.ReadFromJsonAsync<GameplayJoinResponse>().ConfigureAwait(false);
                if (joined == null)
                    return;

                GlobalConstants.RoomCode = joined.RoomCode ?? string.Empty;
                _lastKnownLobbySeats.Clear();
                _startedRaisedForRoom = string.Empty;
                RoomJoined?.Invoke(this, (gameDto.GameType, (double)gameDto.BetAmount, joined.RoomCode));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] CreateJoinLobbyAsync Error: {ex.Message}");
            }
        }
        public async Task<GameCommand> SendMessageAsync(GameCommand commandValue, string command)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/gameplay/commands/send");
                request.Content = JsonContent.Create(new
                {
                    RoomCode = GlobalConstants.RoomCode,
                    CommandType = command,
                    Command = commandValue
                });

                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"[ApiClient] SendMessageAsync rejected. Status={(int)response.StatusCode}, CommandType={command}, Body={errorBody}");
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<GameCommand>().ConfigureAwait(false);
                if (result == null)
                    return null;

                // Server can return a GameCommand with Result="Error: ...".
                // Do not execute local engine steps with default/empty command fields.
                if (!string.IsNullOrWhiteSpace(result.Result) &&
                    result.Result.StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[ApiClient] SendMessageAsync server error command ignored. CommandType={command}, Result={result.Result}");
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] SendMessageAsync Error: {ex.Message}");
                return null;
            }
        }
        public async Task ReadyAsync()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/gameplay/lobbies/ready");
                request.Content = JsonContent.Create(new
                {
                    RoomCode = GlobalConstants.RoomCode
                });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return;

                var ready = await response.Content.ReadFromJsonAsync<GameplayReadyResponse>().ConfigureAwait(false);
                if (ready == null)
                    return;

                if (!string.IsNullOrWhiteSpace(ready.RoomCode))
                    GlobalConstants.RoomCode = ready.RoomCode;

                if (ready.SeatAssignments != null)
                {
                    foreach (var seat in ready.SeatAssignments)
                    {
                        if (seat == null || string.IsNullOrWhiteSpace(seat.PlayerType) || seat.PlayerId <= 0)
                            continue;

                        if (_lastKnownLobbySeats.TryGetValue(seat.PlayerType, out var existingId) && existingId == seat.PlayerId)
                            continue;

                        _lastKnownLobbySeats[seat.PlayerType] = seat.PlayerId;
                        PlayerSeated?.Invoke(this, (seat.PlayerType, seat.PlayerId, seat.UserName ?? "Waiting", seat.PictureUrl ?? "user.webp"));
                    }
                }

                if (ready.Started)
                {
                    _startedRaisedForRoom = ready.RoomCode ?? GlobalConstants.RoomCode;
                    GameStarted?.Invoke(this, (ready.GameType, ready.SeatsJson ?? string.Empty, ready.RollsString ?? string.Empty));
                }

                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] ReadyAsync Error: {ex.Message}");
            }
        }
        public async Task LeaveCloseLobby()
        {
            String AuthToken = getAuthToken();
            if (AuthToken == "")
                return;
            if (GlobalConstants.RoomCode != "")
            {
                GlobalConstants.RoomCode = "";
                try
                {
                    using var request = CreateApiRequest(HttpMethod.Post, "api/gameplay/lobbies/leave");
                    using var response = await _apiClient.SendAsync(request);
                    
                    if (!response.IsSuccessStatusCode)
                        return;

                    _lastKnownLobbySeats.Clear();
                    _startedRaisedForRoom = string.Empty;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ApiClient] LeaveCloseLobbyAsync Error: {ex.Message}");
                }
            }
        }
        public async Task<List<GameCommand>> PullCommands(int lastSeenIndex, string RoomCode)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, $"api/gameplay/commands/pull?roomCode={Uri.EscapeDataString(RoomCode)}&lastSeenIndex={lastSeenIndex}");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<GameCommand>();

                return await response.Content.ReadFromJsonAsync<List<GameCommand>>().ConfigureAwait(false) ?? new List<GameCommand>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] PullCommands Error: {ex.Message}");
                return new List<GameCommand>();
            }
        }
        public async Task GetLobbyAsync(string roomCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roomCode))
                    return;

                using var request = CreateApiRequest(HttpMethod.Get, $"api/gameplay/lobbies/state?roomCode={Uri.EscapeDataString(roomCode)}");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return;

                var lobby = await response.Content.ReadFromJsonAsync<GameplayReadyResponse>().ConfigureAwait(false);
                if (lobby == null)
                    return;

                if (!string.IsNullOrWhiteSpace(lobby.RoomCode))
                    GlobalConstants.RoomCode = lobby.RoomCode;

                if (lobby.SeatAssignments != null)
                {
                    foreach (var seat in lobby.SeatAssignments)
                    {
                        if (seat == null || string.IsNullOrWhiteSpace(seat.PlayerType) || seat.PlayerId <= 0)
                            continue;

                        if (_lastKnownLobbySeats.TryGetValue(seat.PlayerType, out var existingId) && existingId == seat.PlayerId)
                            continue;

                        _lastKnownLobbySeats[seat.PlayerType] = seat.PlayerId;
                        PlayerSeated?.Invoke(this, (seat.PlayerType, seat.PlayerId, seat.UserName ?? "Waiting", seat.PictureUrl ?? "user.webp"));
                    }
                }

                var startedRoom = lobby.RoomCode ?? GlobalConstants.RoomCode;
                if (lobby.Started && !string.Equals(_startedRaisedForRoom, startedRoom, StringComparison.Ordinal))
                {
                    _startedRaisedForRoom = startedRoom;
                    GameStarted?.Invoke(this, (lobby.GameType, lobby.SeatsJson ?? string.Empty, lobby.RollsString ?? string.Empty));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetLobbyAsync Error: {ex.Message}");
            }
        }
        public async Task<List<ActiveGameListItem>> GetActivePublicGamesAsync()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/gameplay/games/active");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<ActiveGameListItem>();

                return await response.Content.ReadFromJsonAsync<List<ActiveGameListItem>>().ConfigureAwait(false) ?? new List<ActiveGameListItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetActivePublicGamesAsync Error: {ex.Message}");
                return new List<ActiveGameListItem>();
            }
        }
        public async Task<List<ChatMessages>> SendChatMessageAsync(ChatMessages CM, string roomCode)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/gameplay/chat/send");
                request.Content = JsonContent.Create(new
                {
                    RoomCode = roomCode,
                    Message = CM
                });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<ChatMessages>();

                var messages = await response.Content.ReadFromJsonAsync<List<ChatMessages>>().ConfigureAwait(false) ?? new List<ChatMessages>();
                UpdateLastSeenChatIndexes(messages);
                ReceiveChatMessage?.Invoke(this, messages);
                return messages;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] SendChatMessageAsync Error: {ex.Message}");
                return new List<ChatMessages>();
            }
        }
        public async Task<PlayerInfo> UserConnectedSetID()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/auth/session");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<PlayerInfo>().ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }
        public async Task<PlayerInfo?> GoogleAuthentication(string idToken)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/auth/google");
                request.Content = JsonContent.Create(new
                {
                    IdToken = idToken
                });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"[ApiClient] GoogleAuthentication failed: {(int)response.StatusCode} {error}");
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<PlayerInfo>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GoogleAuthentication Error: {ex.Message}");
                return null;
            }
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
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, $"api/tournaments?type={Uri.EscapeDataString(type ?? "All")}");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<TournamentDTO>();

                return await response.Content.ReadFromJsonAsync<List<TournamentDTO>>().ConfigureAwait(false)
                    ?? new List<TournamentDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetAllTournaments Error: {ex.Message}");
                return new List<TournamentDTO>();
            }
        }
        internal async Task<TournamentDTO> JoinTournament(int TournamentID)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, $"api/tournaments/{TournamentID}/join");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<TournamentDTO>().ConfigureAwait(false);
                if (result?.StatusCode == "SUCCESS")
                    await RefreshPlayerInfoFromApi().ConfigureAwait(false);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] JoinTournament Error: {ex.Message}");
                return null;
            }
        }
        internal async Task<TournamentResultDTO> GetResultsTournament(int TournamentID)
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, $"api/tournaments/{TournamentID}/results");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<TournamentResultDTO>().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetResultsTournament Error: {ex.Message}");
                return null;
            }
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

        public async Task RefreshPlayerInfoFromApi()
        {
            var playerInfo = await GetProfile<PlayerInfo>().ConfigureAwait(false);
            ApplyPlayerInfoUpdate(playerInfo);
        }

        public async Task RefreshSessionFromApi()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/session/sync");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return;

                var syncInfo = await response.Content.ReadFromJsonAsync<SessionSyncInfo>().ConfigureAwait(false);
                ApplySessionSync(syncInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] RefreshSession Error: {ex.Message}");
            }
        }

        private void ApplyPlayerInfoUpdate(PlayerInfo? playerInfo)
        {
            if (playerInfo == null)
                return;

            var handler = PlayerInfoUpdate;
            if (handler != null)
                handler.Invoke(this, playerInfo);
            else
                UserInfo.Instance.player = playerInfo;
        }

        private void ApplyWalletBalance(decimal balance)
        {
            var wallet = UserInfo.Instance.player?.Wallet;
            if (wallet != null)
            {
                wallet.AvailableBalance = balance;
                return;
            }

            _ = RefreshPlayerInfoFromApi();
        }

        private void ApplySessionSync(SessionSyncInfo? syncInfo)
        {
            if (syncInfo == null)
                return;

            var player = UserInfo.Instance.player;
            if (player == null || player.PlayerId != syncInfo.PlayerId)
            {
                _ = RefreshPlayerInfoFromApi();
                return;
            }

            player.IsOnline = syncInfo.IsOnline;

            if (syncInfo.Wallet == null)
                return;

            player.Wallet ??= new PlayerWallet();
            player.Wallet.WalletId = syncInfo.Wallet.WalletId;
            player.Wallet.PlayerId = syncInfo.Wallet.PlayerId;
            player.Wallet.AddressType = syncInfo.Wallet.AddressType;
            player.Wallet.WalletAddress = syncInfo.Wallet.WalletAddress;
            player.Wallet.AvailableBalance = syncInfo.Wallet.AvailableBalance;
        }

        private void QueuePlayerInfoRefreshPolling(decimal? previousBalance)
        {
            _ = RefreshPlayerInfoUntilBalanceChanges(previousBalance);
        }

        private async Task RefreshPlayerInfoUntilBalanceChanges(decimal? previousBalance)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt == 0 ? 2 : 5)).ConfigureAwait(false);
                await RefreshSessionFromApi().ConfigureAwait(false);

                if (!previousBalance.HasValue)
                    return;

                var currentBalance = UserInfo.Instance.player?.Wallet?.AvailableBalance;
                if (currentBalance.HasValue && currentBalance.Value != previousBalance.Value)
                    return;
            }
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
            try
            {
                using var request = CreateApiRequest(HttpMethod.Post, "api/nfts/mint");
                request.Content = JsonContent.Create(new { Amount = amount });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return "Failed";

                var result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (amount > 0 && result.Contains("Success", StringComparison.OrdinalIgnoreCase))
                    await RefreshPlayerInfoFromApi().ConfigureAwait(false);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] MintNFT Error: {ex.Message}");
                return "Failed";
            }
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
                var previousBalance = UserInfo.Instance.player?.Wallet?.AvailableBalance;
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/transactions/broadcast");
                request.Content = JsonContent.Create(new { TxBase64 = txBase64 });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new BlockchainResult { Success = false, Error = "Request failed" };

                var result = await response.Content.ReadFromJsonAsync<BlockchainResult>().ConfigureAwait(false)
                    ?? new BlockchainResult { Success = false, Error = "Empty response" };
                if (result.Success)
                    QueuePlayerInfoRefreshPolling(previousBalance);

                return result;
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
                var previousBalance = UserInfo.Instance.player?.Wallet?.AvailableBalance;
                using var request = CreateApiRequest(HttpMethod.Post, "api/payments/swap/execute");
                request.Content = JsonContent.Create(new { RequestId = requestId, SignedTxBase64 = signedTxBase64 });
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new BlockchainResult { Success = false, Error = "Request failed" };

                var result = await response.Content.ReadFromJsonAsync<BlockchainResult>().ConfigureAwait(false)
                    ?? new BlockchainResult { Success = false, Error = "Empty response" };
                if (result.Success)
                    QueuePlayerInfoRefreshPolling(previousBalance);

                return result;
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

        public async Task<List<WalletBonusHistoryItem>> GetWalletBonusHistory()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/wallet-hub/bonuses");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<WalletBonusHistoryItem>();

                return await response.Content.ReadFromJsonAsync<List<WalletBonusHistoryItem>>().ConfigureAwait(false)
                    ?? new List<WalletBonusHistoryItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetWalletBonusHistory Error: {ex.Message}");
                return new List<WalletBonusHistoryItem>();
            }
        }

        public async Task<List<WalletDepositHistoryItem>> GetWalletDepositHistory()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/wallet-hub/deposits");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<WalletDepositHistoryItem>();

                return await response.Content.ReadFromJsonAsync<List<WalletDepositHistoryItem>>().ConfigureAwait(false)
                    ?? new List<WalletDepositHistoryItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetWalletDepositHistory Error: {ex.Message}");
                return new List<WalletDepositHistoryItem>();
            }
        }

        public async Task<List<WalletWithdrawalHistoryItem>> GetWalletWithdrawalHistory()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/wallet-hub/withdrawals");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<WalletWithdrawalHistoryItem>();

                return await response.Content.ReadFromJsonAsync<List<WalletWithdrawalHistoryItem>>().ConfigureAwait(false)
                    ?? new List<WalletWithdrawalHistoryItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetWalletWithdrawalHistory Error: {ex.Message}");
                return new List<WalletWithdrawalHistoryItem>();
            }
        }

        public async Task<List<WalletGameHistoryItem>> GetWalletGameHistory()
        {
            try
            {
                using var request = CreateApiRequest(HttpMethod.Get, "api/wallet-hub/games");
                using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    return new List<WalletGameHistoryItem>();

                return await response.Content.ReadFromJsonAsync<List<WalletGameHistoryItem>>().ConfigureAwait(false)
                    ?? new List<WalletGameHistoryItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiClient] GetWalletGameHistory Error: {ex.Message}");
                return new List<WalletGameHistoryItem>();
            }
        }

        private void StartChatPolling()
        {
            StopChatPolling();
            _chatPollingCts = new CancellationTokenSource();
            _chatPollingTask = Task.Run(() => ChatPollingLoopAsync(_chatPollingCts.Token));
        }

        private void StopChatPolling()
        {
            try
            {
                _chatPollingCts?.Cancel();
            }
            catch
            {
            }
            finally
            {
                _chatPollingCts?.Dispose();
                _chatPollingCts = null;
                _chatPollingTask = null;
            }
        }

        private async Task ChatPollingLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!Connected || string.IsNullOrWhiteSpace(getAuthToken()))
                    {
                        await Task.Delay(1500, token).ConfigureAwait(false);
                        continue;
                    }

                    var roomCode = GlobalConstants.RoomCode ?? string.Empty;
                    if (!string.Equals(_lastPolledRoomCode, roomCode, StringComparison.Ordinal))
                    {
                        _lastPolledRoomCode = roomCode;
                        _lastSeenRoomChatIndex = 0;
                    }

                    await PullChatUpdatesAsync(roomCode).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ApiClient] ChatPollingLoop Error: {ex.Message}");
                }

                await Task.Delay(1500, token).ConfigureAwait(false);
            }
        }

        private async Task PullChatUpdatesAsync(string roomCode)
        {
            var roomCodeValue = string.IsNullOrWhiteSpace(roomCode) ? string.Empty : Uri.EscapeDataString(roomCode);
            var lastSeenIndex = string.IsNullOrWhiteSpace(roomCode) ? _lastSeenPrivateChatIndex : _lastSeenRoomChatIndex;
            using var request = CreateApiRequest(HttpMethod.Get, $"api/gameplay/chat/pull?roomCode={roomCodeValue}&lastSeenIndex={lastSeenIndex}");
            using var response = await _apiClient.SendAsync(request).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(roomCode) && response.StatusCode == HttpStatusCode.Conflict)
            {
                // Room state is stale on client; recover to private polling mode.
                Console.WriteLine($"[ApiClient] ChatPolling recovered from stale room '{roomCode}'. Switching to private polling.");
              //  GlobalConstants.RoomCode = string.Empty;
                _lastPolledRoomCode = string.Empty;
                _lastSeenRoomChatIndex = 0;
                return;
            }
            if (!response.IsSuccessStatusCode)
                return;

            var updates = await response.Content.ReadFromJsonAsync<List<ChatMessages>>().ConfigureAwait(false) ?? new List<ChatMessages>();
            if (updates.Count == 0)
                return;

            UpdateLastSeenChatIndexes(updates);
            ReceiveChatMessage?.Invoke(this, updates);
        }

        private void UpdateLastSeenChatIndexes(List<ChatMessages> messages)
        {
            if (messages == null || messages.Count == 0)
                return;

            foreach (var message in messages)
            {
                if (message == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(message.RoomCode))
                {
                    if (message.Index > _lastSeenRoomChatIndex)
                        _lastSeenRoomChatIndex = message.Index;
                }
                else
                {
                    if (message.Index > _lastSeenPrivateChatIndex)
                        _lastSeenPrivateChatIndex = message.Index;
                }
            }
        }

        private sealed class GameplayJoinResponse
        {
            public string RoomCode { get; set; } = string.Empty;
            public string GameType { get; set; } = string.Empty;
            public decimal BetAmount { get; set; }
            public string State { get; set; } = string.Empty;
        }

        private sealed class GameplayReadyResponse
        {
            public string RoomCode { get; set; } = string.Empty;
            public string GameType { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public bool Started { get; set; }
            public string SeatsJson { get; set; } = string.Empty;
            public string RollsString { get; set; } = string.Empty;
            public List<GameplaySeatInfo> SeatAssignments { get; set; } = new();
        }

        private sealed class GameplaySeatInfo
        {
            public string PlayerType { get; set; } = string.Empty;
            public int PlayerId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string PictureUrl { get; set; } = string.Empty;
        }
    }
}
