using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace LudoClient.Network
{
    internal class Client
    {
        public delegate void CallbackRecievedRequest(string SeatName, int diceValue);
        public event CallbackRecievedRequest RecievedRequest;
        IHubProxy _hub;
        string url = @"http://localhost:8084";
        public Client()
        {
            var connection = new HubConnection(url);
            _hub = connection.CreateHubProxy("LudoHub");
            // Start the connection
            connection.Start();
            Console.WriteLine("Connection started. Waiting for messages from the server...");
            // Listen for messages from the server
            _hub.On<string, string>("ReceiveMessage", (sender, message) =>
            {
                Console.WriteLine($"{sender}: {message}");
                RecievedRequest(sender, 2);
            });
        }
        public async Task<string> SendMessageAsync(string line, string commmand)
        {
            string result = await _hub.Invoke<string>("Send", "client", line, commmand);
            // _hub.Invoke("Send", "client", line, commmand);
            return result;
        }
    }
}
