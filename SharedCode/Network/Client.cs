using Microsoft.AspNetCore.SignalR.Client;
using SharedCode.Constants;

namespace SharedCode.Network
{
    public class Client
    {
        private HubConnection _hubConnection;
        private string _messages;

        // Event Definitions using standard .NET event patterns
        public event EventHandler<(string SeatName, int DiceValue)> ReceivedRequest;
        public event EventHandler<(string PlayerType, int PlayerId, string UserName, string PictureUrl)> PlayerSeated;
        public event EventHandler<(string GameType, string seatsData)> GameStarted;
        public event EventHandler<(string GameType, int GameCost, string RoomCode)> RoomJoined;

        public Client()
        {
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
            // Player seat event
            _hubConnection.On<string, int, string, string>("PlayerSeat", (playerType, playerId, userName, pictureUrl) =>
            {
                PlayerSeated?.Invoke(this, (playerType, playerId, userName, pictureUrl));
                _messages = $"{playerType}: {userName} has joined.";
                Console.WriteLine(_messages);
            });

            // Game start event
            _hubConnection.On<string, string>("GameStarted", (GameType, seatsData) =>
            {
                Console.WriteLine("Starting Game " + DateTime.Now, GameType, seatsData);
                //Game(GameType, playerCount, PlayerColor)
               GameStarted?.Invoke(this, (GameType, seatsData));
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
                Console.WriteLine("Connection lost. Reconnecting...");
                if (error != null)
                {
                    Console.WriteLine($"Reconnecting due to: {error.Message}");
                }
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                Console.WriteLine($"Reconnected. ConnectionId: {connectionId}");
                return Task.CompletedTask;
            };

            _hubConnection.Closed += async error =>
            {
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
                Console.WriteLine("Already connected.");
                return;
            }

            try
            {
                await _hubConnection.StartAsync().ConfigureAwait(false);
                Console.WriteLine("Connection started. Waiting for messages from the server...");
            }
            catch (Exception ex)
            {
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
        public async Task CreateJoinLobbyAsync(int playerId, string userName, string pictureUrl, string gameType, int gameCost, string roomName)
        {
            try
            {
                var roomCode = await _hubConnection.InvokeAsync<string>("CreateJoinLobby", playerId, userName, pictureUrl, gameType, gameCost, roomName).ConfigureAwait(false);
                Console.WriteLine($"Joined room: {roomCode}");
                RoomJoined?.Invoke(this, (gameType, gameCost, roomCode));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateJoinLobbyAsync: {ex.Message}");
            }
        }

        /// <summary>
        /// Send a message to the server.
        /// </summary>
        public async Task<string> SendMessageAsync(string line, string command)
        {
            try
            {
                string result = await _hubConnection.InvokeAsync<string>("Send", "client", line, command).ConfigureAwait(false);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
                return string.Empty;
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
            if (GlobalConstants.RoomCode!="")
            {
                _ = _hubConnection.InvokeAsync("LeaveCloseLobby", playerId, GlobalConstants.RoomCode).ConfigureAwait(false);
                GlobalConstants.RoomCode = "";
            }
        }
    }
}