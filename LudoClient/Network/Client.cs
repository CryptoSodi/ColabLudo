using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Maui.Controls.Shapes;

namespace LudoClient.Network
{
    internal class Client
    {
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
                RecievedRequest("",2);
            });
        }
        
        public delegate void CallbackRecievedRequest(string SeatName, int diceValue);
        public event CallbackRecievedRequest RecievedRequest;

        public void SendMessage(string line)
        {
            _hub.Invoke("Send", "Client", line);
        }
    }
}
