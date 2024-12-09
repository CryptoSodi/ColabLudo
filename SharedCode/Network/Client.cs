using SharedCode.Constants;
using SharedCode.CoreEngine;
using Microsoft.AspNetCore.SignalR.Client;

namespace SharedCode.Network
{
    public class Client
    {
        public static HubConnection _hubConnection;
        string Name;
        string Messages;

        public delegate void CallbackRecievedRequest(string SeatName, int diceValue);
        public event CallbackRecievedRequest RecievedRequest;

        public delegate void PlayerSeatRecieved(string playerType, int playerId, string userName, string pictureUrl);
        public event PlayerSeatRecieved PlayerSeat;

        public delegate void GameStart();
        public event GameStart gameStart;
        public Client()
        {
            _hubConnection = new HubConnectionBuilder().WithUrl(GlobalConstants.HubUrl+ "LudoHub").Build();
            _hubConnection.StartAsync();
            Console.WriteLine("Connection started. Waiting for messages from the server...");
            // Listen for messages from the server
            _hubConnection.On<string, int, string, string>("PlayerSeat", (playerType, playerId, userName, pictureUrl) =>
            {
                if(PlayerSeat!=null)
                PlayerSeat(playerType, playerId, userName, pictureUrl);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages = ($"{playerType}: {userName} has joined");
                });
            });
            _hubConnection.On("GameStart", () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Console.WriteLine("Starting Game " + DateTime.Now);
                    //Application.Current.MainPage = new Game("","","");
                });
            });
            _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages = ($"{user} says {message}");
                });
            });
        }
        public void CreateJoinLobby(int playerId, string userName, string pictureUrl, string gameType, int gameCost, string roomName)
        {
            _hubConnection.InvokeAsync<string>("CreateJoinLobby", playerId, userName, pictureUrl, gameType, gameCost, roomName).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    string roomCode = task.Result;
                    // Handle the result here
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        //Application.Current.MainPage = new GameRoom(gameType, gameCost, roomCode);
                        //Navigation.PushAsync(new GameRoom(gameType, gameCost, code));
                    });
                }
                else
                {
                    // Handle errors
                    Console.WriteLine(task.Exception?.Message);
                }
            });
        }
        public async Task<string> SendMessageAsync(string line, string commmand)
        {
            // await _hubConnection.InvokeAsync("JoinRoom", Name, commmand);
            // await _hubConnection.InvokeAsync("SendMessageToRoom", Name, commmand);
            string result = await _hubConnection.InvokeAsync<string>("Send", "client", line, commmand);
            // _hub.Invoke("Send", "client", line, commmand);
            return result;
        }
        public async Task Disconnect()
        {
            if (_hubConnection.State == HubConnectionState.Disconnected) return;
            await _hubConnection.StopAsync();
        }
        internal async void Ready(string roomCode)
        {
            await _hubConnection.InvokeAsync<string>("Ready", roomCode).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    string roomCode = task.Result;

                    Console.WriteLine(task.Result);
                }
                else
                {
                    // Handle errors
                    Console.WriteLine(task.Exception?.Message);
                }
            });
        }
    }
}