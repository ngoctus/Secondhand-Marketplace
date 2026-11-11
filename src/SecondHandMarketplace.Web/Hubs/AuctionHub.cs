using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace SecondHandMarketplace.Web.Hubs;

public class AuctionHub : Hub
{
    // auctionId -> các connectionId đang xem. Chỉ đúng khi chạy 1 server (nhiều server cần Redis backplane).
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<string, byte>> Viewers = new();
    private static string Group(int id) => $"auction-{id}";

    public async Task JoinAuction(int auctionId)
    {
        if (auctionId <= 0) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, Group(auctionId));
        var set = Viewers.GetOrAdd(auctionId, _ => new());
        set.TryAdd(Context.ConnectionId, 0);
        await Clients.Group(Group(auctionId)).SendAsync("ViewerCount", set.Count);
    }

    public async Task LeaveAuction(int auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(auctionId));
        if (Viewers.TryGetValue(auctionId, out var set) && set.TryRemove(Context.ConnectionId, out _))
            await Clients.Group(Group(auctionId)).SendAsync("ViewerCount", set.Count);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var (id, set) in Viewers)
        {
            if (!set.TryRemove(Context.ConnectionId, out _)) continue;
            if (set.IsEmpty) Viewers.TryRemove(id, out _);
            else await Clients.Group(Group(id)).SendAsync("ViewerCount", set.Count);
        }
        await base.OnDisconnectedAsync(exception);
    }
}