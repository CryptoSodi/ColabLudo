using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace LudoClient.Network
{
    public class Client
    {
        private readonly HubConnection _hubConnection;
        string Name;
        string Messages;
        bool IsConnected;
        public delegate void CallbackRecievedRequest(string SeatName, int diceValue);
        public event CallbackRecievedRequest RecievedRequest;
        string url = @"http://localhost:8084";
        public Client()
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:8084/LudoHub")
                .Build();
             _hubConnection.StartAsync();
            IsConnected = true;
            Console.WriteLine("Connection started. Waiting for messages from the server...");
            
            // Listen for messages from the server
            _hubConnection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Messages = ($"{user} says {message}");
                });
            });
        }
        public void CreateJoinRoom(string userName, int gameType, int gameCost, string roomName, ControlView.ShareBox shareBox)
        {
            _hubConnection.InvokeAsync<string>("CreateJoinRoom", userName, gameCost, gameType, roomName).ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    string code = task.Result;
                    // Handle the result here
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        shareBox.SetShareCode(code);
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

            IsConnected = false;
        }
    }
}
