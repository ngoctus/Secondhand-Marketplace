using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Web2Hand.Data;
using Web2Hand.Models.Entities;

namespace Web2Hand.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db) => _db = db;

    // Client gọi: connection.invoke("SendMessage", receiverId, "nội dung")
    public async Task SendMessage(string receiverId, string content)
    {
        var senderId = Context.UserIdentifier!;

        _db.ChatMessages.Add(new ChatMessage { SenderId = senderId, ReceiverId = receiverId, Content = content });
        await _db.SaveChangesAsync();

        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, content);
    }
}
