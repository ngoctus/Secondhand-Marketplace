using Microsoft.AspNetCore.SignalR;

namespace SecondHandMarketplace.Web.Hubs;

/// <summary>
/// SignalR Hub phục vụ cập nhật giá đấu, lượt đặt và trạng thái phiên đấu giá thời gian thực.
/// </summary>
public class AuctionHub : Hub
{
    /// <summary>
    /// Tham gia phòng theo dõi một phiên đấu giá cụ thể.
    /// </summary>
    public async Task JoinAuction(int auctionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
    }

    /// <summary>
    /// Rời phòng theo dõi phiên đấu giá.
    /// </summary>
    public async Task LeaveAuction(int auctionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction-{auctionId}");
    }
}
