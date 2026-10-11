using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Web2Hand.Data;
using Web2Hand.Hubs;
using Web2Hand.Models.Entities;
using Web2Hand.Models.Enums;
using Web2Hand.Services.Notifications;

namespace Web2Hand.Services.Auctions;

public class AuctionService : IAuctionService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IHubContext<AuctionHub> _hub;

    public AuctionService(AppDbContext db, INotificationService notifications, IHubContext<AuctionHub> hub)
    {
        _db = db;
        _notifications = notifications;
        _hub = hub;
    }

    public async Task<(bool Ok, string Message)> PlaceBidAsync(int auctionId, string bidderId, decimal amount)
    {
        var auction = await _db.Auctions.Include(a => a.Product).FirstOrDefaultAsync(a => a.Id == auctionId);
        if (auction == null)
            return (false, "Không tìm thấy phiên đấu giá.");

        var now = DateTime.UtcNow;
        if (auction.Status != AuctionStatus.Running || now < auction.StartTime || now >= auction.EndTime)
            return (false, "Phiên đấu giá không đang diễn ra.");

        if (auction.Product.SellerId == bidderId)
            return (false, "Bạn không thể đấu giá sản phẩm của chính mình.");

        // Người đang dẫn đầu (để báo "bị vượt giá" sau khi đặt thành công)
        var previousBidderId = await _db.Bids
            .Where(b => b.AuctionId == auctionId)
            .OrderByDescending(b => b.Amount)
            .Select(b => b.BidderId)
            .FirstOrDefaultAsync();

        // Bid đầu tiên chỉ cần >= giá khởi điểm, các bid sau phải >= giá hiện tại + bước giá
        var minBid = previousBidderId == null ? auction.StartPrice : auction.CurrentPrice + auction.BidStep;
        if (amount < minBid)
            return (false, $"Giá tối thiểu là {minBid:N0}đ.");

        auction.CurrentPrice = amount;
        _db.Bids.Add(new Bid { AuctionId = auctionId, BidderId = bidderId, Amount = amount });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Có người khác vừa đặt giá cùng lúc (RowVersion đã đổi)
            return (false, "Có người vừa đặt giá cao hơn, vui lòng thử lại.");
        }

        await _hub.Clients.Group(AuctionHub.GroupName(auctionId)).SendAsync("PriceUpdated", auctionId, amount);

        if (previousBidderId != null && previousBidderId != bidderId)
        {
            await _notifications.CreateAsync(previousBidderId, NotificationType.Outbid,
                $"Bạn đã bị vượt giá ở sản phẩm \"{auction.Product.Name}\".", $"/Products/Details/{auction.ProductId}");
        }

        return (true, "Đặt giá thành công.");
    }

    public async Task RefreshStatusesAsync()
    {
        var now = DateTime.UtcNow;

        // 1. Mở các phiên đã đến giờ
        var toStart = await _db.Auctions
            .Where(a => a.Status == AuctionStatus.Scheduled && a.StartTime <= now)
            .ToListAsync();
        foreach (var a in toStart)
            a.Status = AuctionStatus.Running;

        // 2. Đóng các phiên hết giờ, chọn người thắng
        var toEnd = await _db.Auctions
            .Include(a => a.Product)
            .Include(a => a.Bids)
            .Where(a => a.Status == AuctionStatus.Running && a.EndTime <= now)
            .ToListAsync();
        foreach (var a in toEnd)
        {
            a.Status = AuctionStatus.Ended;
            var top = a.Bids.OrderByDescending(b => b.Amount).FirstOrDefault();
            if (top != null)
            {
                a.WinnerId = top.BidderId;
                a.Product.Status = ProductStatus.Reserved;
                // TODO: tạo Order cho người thắng (gọi IOrderService)
            }
        }
        await _db.SaveChangesAsync();

        // 3. Gửi thông báo kết quả (sau khi đã lưu)
        foreach (var a in toEnd)
        {
            var link = $"/Products/Details/{a.ProductId}";
            if (a.WinnerId != null)
                await _notifications.CreateAsync(a.WinnerId, NotificationType.AuctionWon,
                    $"Chúc mừng! Bạn đã thắng đấu giá \"{a.Product.Name}\".", link);

            var losers = a.Bids.Select(b => b.BidderId).Distinct().Where(id => id != a.WinnerId);
            foreach (var id in losers)
                await _notifications.CreateAsync(id, NotificationType.AuctionLost,
                    $"Phiên đấu giá \"{a.Product.Name}\" đã kết thúc, bạn không thắng.", link);
        }
    }
}
