using Microsoft.AspNetCore.SignalR.Client;
using SharedCode.Constants;
using System.ComponentModel;

namespace SharedCode.Network
{
    public class Client
    {
        private bool _connected;
        public HubConnection _hubConnection;

        // Event Definitions using standard .NET event patterns

        public event EventHandler<(string GameType, string seatsData, string rollsString)> GameStarted;
        public event EventHandler<(string GameType, double GameCost, string RoomCode)> RoomJoined;
        public event EventHandler<(string seats, string GameType, string GameCost)> ShowResults;
        public event EventHandler<(string PlayerType, int PlayerId, string UserName, string PictureUrl)> PlayerSeated;
        public event EventHandler<List<ChatMessages>> ReceiveChatMessage;
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
            Connected = false;
            // Build connection with automatic reconnect
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(GlobalConstants.HubUrl + "LudoHub")
                .WithAutomaticReconnect()
                .Build();
            
            _ = ConnectAsync();
            RegisterHubEvents();
        }
        private void RegisterHubEvents()
        {
            // Player ReceiveMessage event
            _hubConnection.On<ChatMessages>("ReceiveChatHistory", msg =>
            {
                List<ChatMessages> lcm = new List<ChatMessages>();
                lcm.Add(msg);
                // This lambda runs on a non-UI thread:
                ReceiveChatMessage?.Invoke(this, (lcm));
            });
            _hubConnection.On<PlayerInfo>("PlayerInfoUpdate", playerInfo =>
            {
                // This lambda runs on a non-UI thread:
                PlayerInfoUpdate?.Invoke(this, (playerInfo));
            });
            // Player seat event
            _hubConnection.On<string, int, string, string>("PlayerSeat", (playerType, playerId, userName, pictureUrl) =>
            {
                PlayerSeated?.Invoke(this, (playerType, playerId, userName, pictureUrl));                
                Console.WriteLine($"{playerType}: {userName} has joined.");
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
            // Connection lifecycle events
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
            _hubConnection.Reconnected += connectionId =>
            {
                Connected = true;
                UserConnectedSetID();
                Console.WriteLine($"Reconnected. ConnectionId: {connectionId}");
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
                // Optionally, we can try to reconnect manually if automatic reconnect is not desired.
                // await ConnectAsync();
            };
        }
        /// <summary>
        /// Establish the connection to the server asynchronously.
        /// </summary>
        public async Task ConnectAsync()
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                Connected = true;
                await UserConnectedSetID();
                Console.WriteLine("Already connected.");
                return;
            }
            try
            {
                await _hubConnection.StartAsync().ConfigureAwait(false);
                Connected = true;
                await UserConnectedSetID();
                Console.WriteLine("Connection started. Waiting for messages from the server...");
            }
            catch (Exception ex)
            {
                Connected = false;
                Console.WriteLine($"Failed to start the connection: {ex.Message}");
                // Consider retry logic here if desired
            }
        }
        /// <summary>
        /// Disconnect from the server.
        /// </summary>
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
        /// Creates or joins a lobby on the server, then triggers the RoomJoined event.
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
        /// Send a message to the server.        
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
        /// <summary>
        /// Sends a Ready state for the given room code.
        /// </summary>
        public async Task ReadyAsync()
        {
            try
            {
                string result = await _hubConnection.InvokeAsync<string>("Ready", GlobalConstants.RoomCode).ConfigureAwait(false);
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
                _ = _hubConnection.InvokeAsync("LeaveCloseLobby", GlobalConstants.RoomCode).ConfigureAwait(false);
                GlobalConstants.RoomCode = "";
            }
        }
        public async Task<List<GameCommand>> PullCommands(int lastSeenIndex, string RoomCode)
        {
            return await _hubConnection.InvokeAsync<List<GameCommand>>("PullCommands", lastSeenIndex, RoomCode).ConfigureAwait(false);
        }
        /// <summary>
        /// Send a message to the server.
        /// </summary>
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

        /// <summary>
        /// Converts SOL to lamports and calls your lamport‐based SendSolAsync.
        /// </summary>
        public async Task<string> SendSolAsync(string destination, decimal solAmount)
        {
            // Use the generic InvokeAsync<DepositInfo>
            String info = await _hubConnection.InvokeAsync<String>("SendSol", destination, solAmount).ConfigureAwait(false);
            return info;
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
    }
}