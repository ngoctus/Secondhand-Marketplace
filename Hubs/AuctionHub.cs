using Microsoft.AspNetCore.SignalR;

namespace Web2Hand.Hubs;

/// <summary>Realtime giá đấu giá. Ai cũng được xem; đặt giá thì đi qua Buyer/BidsController (cần đăng nhập).</summary>
public class AuctionHub : Hub
{
    public static string GroupName(int auctionId) => $"auction-{auctionId}";

    // Client gọi: connection.invoke("JoinAuction", auctionId)
    public Task JoinAuction(int auctionId)
        => Groups.AddToGroupAsync(Context.ConnectionId, GroupName(auctionId));
}
