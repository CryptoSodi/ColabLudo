using System.Collections.Concurrent;
using LudoClient.CoreEngine;
using LudoClient;
using Microsoft.AspNetCore.SignalR;

public record User(string Name, string Room);
public record Message(string User, string Text);
public class AdvancedChatHub : Hub
{
    public static Engine eng = new Engine(new Gui(new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new Token(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat(), new PlayerSeat()));

    private static ConcurrentDictionary<string, User> _users = new();
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_users.TryGetValue(Context.ConnectionId, out var user))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, user.Room);
            await Clients.Group(user.Room).SendAsync("UserLeft", user.Name);
        }
    }
    public async Task JoinRoom(string userName, string roomName)
    {
        _users.TryAdd(Context.ConnectionId, new User(userName, roomName));
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        await Clients.Group(roomName).SendAsync("UserJoined", userName);
    }
    public async Task SendMessageToRoom(string roomName, string content)
    {
        var message = new Message(_users[Context.ConnectionId].Name, content);
        await Clients.Group(roomName).SendAsync("ReceiveMessage", message);
    }
    public string Send(string name, string message, string commandtype)
    {
        Console.WriteLine($"{name}: {message}:{commandtype}");
        //Clients.All.addMessage(name, GameID);
        if (commandtype == "MovePiece")
        {
            //eng.MovePiece();
        }
        else
        {
            return eng.SeatTurn(message);
        }
        return "0";
    }
}