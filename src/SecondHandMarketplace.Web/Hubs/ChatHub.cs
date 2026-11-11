using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SecondHandMarketplace.Web.Services.Chat;

namespace SecondHandMarketplace.Web.Hubs;

/// <summary>Chat 1-1. Mã người gửi luôn lấy từ phiên đăng nhập, không nhận từ client.</summary>
[Authorize(Roles = AppRoles.User)]
public class ChatHub(IChatService chat) : Hub
{
    private int UserId => Context.User?.LayNguoiDungId() ?? throw new HubException("Bạn chưa đăng nhập.");

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{UserId}");
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(int conversationId, string content)
    {
        var uid = UserId;
        var r = await chat.SendAsync(uid, conversationId, content);
        if (!r.Ok) throw new HubException(r.Error ?? "Không gửi được tin nhắn.");

        // Gửi cho cả người gửi (mọi tab đang mở) và người nhận.
        await Clients.Groups($"user-{uid}", $"user-{r.ReceiverId}").SendAsync("ReceiveMessage", r.Message);
    }

    public async Task MarkRead(int conversationId)
    {
        var uid = UserId;
        var senderId = await chat.MarkReadAsync(uid, conversationId);
        if (senderId is int s)
            await Clients.Group($"user-{s}").SendAsync("MessagesRead", new { conversationId, readerId = uid });
    }
}