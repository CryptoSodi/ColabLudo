using Microsoft.AspNetCore.SignalR.Client;
using SharedCode.Constants;
using System.ComponentModel;
using System.Diagnostics;

namespace SharedCode.Network
{
    public class Client
    {
        public int playerID;
        private bool _connected;
        public HubConnection _hubConnection;
        private string _messages;

        // Event Definitions using standard .NET event patterns

        public event EventHandler<(string GameType, string seatsData, string rollsString)> GameStarted;
        public event EventHandler<(string GameType, double GameCost, string RoomCode)> RoomJoined;
        public event EventHandler<(string seats, string GameType, string GameCost)> ShowResults;
        public event EventHandler<(string PlayerType, int PlayerId, string UserName, string PictureUrl)> PlayerSeated;
        public event EventHandler<List<ChatMessages>> ReceiveChatMessage;
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
        public Client(int playerID)
        {
            this.playerID = playerID;
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
            // Player seat event
            _hubConnection.On<string, int, string, string>("PlayerSeat", (playerType, playerId, userName, pictureUrl) =>
            {
                PlayerSeated?.Invoke(this, (playerType, playerId, userName, pictureUrl));
                _messages = $"{playerType}: {userName} has joined.";
                Console.WriteLine(_messages);
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
                _messages = $"{user} says {message}";
                Console.WriteLine(_messages);
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
                UserConnectedSetID();
                Console.WriteLine("Already connected.");
                return;
            }
            try
            {
                await _hubConnection.StartAsync().ConfigureAwait(false);
                Connected = true;
                UserConnectedSetID();
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
        /// <summary>
        /// Creates or joins a lobby on the server, then triggers the RoomJoined event.
        /// </summary>
        public async Task CreateJoinLobbyAsync(PlayerDto player, GameDto gameDto)//string gameType, double gameCost, string roomCode
        {
            try
            {
                String roomCode = await _hubConnection.InvokeAsync<string>("CreateJoinLobby", player, gameDto).ConfigureAwait(false);
                Console.WriteLine($"Joined room: {roomCode}");
                RoomJoined?.Invoke(this, (gameDto.GameType, (double)gameDto.BetAmount, roomCode));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateJoinLobbyAsync: {ex.Message}");
            }
        }
        /// <summary>
        /// Send a message to the server.
        /// </summary>
        public async Task<GameCommand> SendMessageAsync(GameCommand line, string command)
        {
            try
            {
                GameCommand result = await _hubConnection.InvokeAsync<GameCommand>("Send", "client", line, command, GlobalConstants.RoomCode).ConfigureAwait(false);
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
        public void LeaveCloseLobby(int playerId)
        {
            if (GlobalConstants.RoomCode != "")
            {
                _ = _hubConnection.InvokeAsync("LeaveCloseLobby", playerId, GlobalConstants.RoomCode).ConfigureAwait(false);
                GlobalConstants.RoomCode = "";
            }
        }
        public async Task<List<GameCommand>> PullCommands(int lastSeenIndex, string RoomCode)
        {
            return await _hubConnection.InvokeAsync<List<GameCommand>>("PullCommands", lastSeenIndex, RoomCode);
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
        public async Task<DepositInfo> UserConnectedSetID()
        {
            DepositInfo info = new DepositInfo();
            try
            {
                info = await _hubConnection.InvokeAsync<DepositInfo>("UserConnectedSetID", playerID).ConfigureAwait(false);
            }
            catch (Exception wx)
            {
                Console.WriteLine(wx.Message);
            }
            return info;
        }

        /// <summary>
        /// Converts SOL to lamports and calls your lamport‐based SendSolAsync.
        /// </summary>
        public async Task<string> SendSolAsync(string destination, double solAmount)
        {
            // Use the generic InvokeAsync<DepositInfo>
            String info = await _hubConnection.InvokeAsync<String>("SendSol", playerID, destination, solAmount).ConfigureAwait(false);
            return info;
        }
    }
}