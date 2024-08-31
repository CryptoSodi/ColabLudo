using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchMakerServer
{
    internal class Program
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
     
        [HubName("MatchMaker")]
        public class MyHub : Hub
        {
            // This method is called by clients to send a message to all clients
            public string Send(string name, string message, string commandtype)
            {
                Console.WriteLine($"{name}: {message}:{commandtype}");
                Clients.All.addMessage(name, GameID);
                if (commandtype == "MovePiece")
                {
                    //eng.MovePiece();
                }
                else
                {
                    
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