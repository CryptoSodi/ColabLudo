using System;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading;
using LudoClient.CoreEngine;
using LudoClient;
namespace CommonCode
{
    public class Program
    {
        public static String GameID = "12";
        static void Main(string[] args)
        {
            string url = "http://localhost:8084/";
            using (WebApp.Start(url))
            {
                Console.WriteLine("Server running on {0}", url);

                // Example of sending a message from the server to all clients
                var context = GlobalHost.ConnectionManager.GetHubContext<MyHub>();
                Thread.Sleep(1000);
                context.Clients.All.ReceiveMessage("Server", "Hello from the server!");

                string line;
                while ((line = Console.ReadLine()) != null)
                {
                    // Send a message to the server
                    context.Clients.All.ReceiveMessage("Server", line);
                    // Optionally, send a request to the server to get a server-generated message
                }
            }
            // Keep the server running until someone hits 'x'
            while (true)
            {
                ConsoleKeyInfo ki = Console.ReadKey(true);
                if (ki.Key == ConsoleKey.X)
                {
                    break;
                }
            }
        }
        public static Engine eng = new Engine(new Gui(new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat()));
        [HubName("LudoHub")]
        public class MyHub : Hub
        {
         
            // This method is called by clients to send a message to all clients
            public string Send(string name, string message,string commandtype)
            {
                Console.WriteLine($"{name}: {message}:{commandtype}");
                Clients.All.addMessage(name, GameID);
                if(commandtype == "MovePiece")
                { 
                    //eng.MovePiece();
                }
                else
                {
                  return  eng.SeatTurn(message);
                }
                return "0";
            }

            // Example method to send a message from the server to a specific client
            public void SendServerMessage(string message)
            {
                Clients.Caller.ReceiveMessage("Server", message);
            }
        }
    }
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCors(CorsOptions.AllowAll);
            app.MapSignalR();
        }
    }
}
