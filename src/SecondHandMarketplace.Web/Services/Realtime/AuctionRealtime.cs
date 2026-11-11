using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Hubs;
using SecondHandMarketplace.Web.Models.ViewModels.Auctions;

namespace SecondHandMarketplace.Web.Services.Realtime;

public interface IAuctionRealtime
{
    /// <summary>Đẩy trạng thái trung lập của phiên tới mọi người đang xem.</summary>
    Task BroadcastStatusAsync(int auctionId, AuctionStatusDto status);

    /// <summary>Đẩy thông báo MỚI NHẤT chưa đọc (do stored procedure vừa tạo) tới một người dùng.</summary>
    Task PushLatestNotificationAsync(int userId, string? url = null);

    /// <summary>Đẩy thông báo mới nhất tới chủ shop và mọi người từng đặt giá của phiên.</summary>
    Task PushToParticipantsAsync(int auctionId);
}

public sealed class AuctionRealtime(
    AppDbContext db,
    IHubContext<AuctionHub> auctionHub,
    IHubContext<NotificationHub> notificationHub,
    ILogger<AuctionRealtime> log) : IAuctionRealtime
{
    // Giao dịch DB đã commit nên không dùng CancellationToken của request:
    // người dùng đóng tab không được làm mất thông báo của người khác.
    public async Task BroadcastStatusAsync(int auctionId, AuctionStatusDto status)
    {
        try
        {
            await auctionHub.Clients.Group($"auction-{auctionId}")
                .SendAsync("ReceiveBidUpdate", status, CancellationToken.None);
        }
        catch (Exception ex) { log.LogWarning(ex, "Không đẩy được trạng thái phiên {Id}.", auctionId); }
    }

    public async Task PushLatestNotificationAsync(int userId, string? url = null)
    {
        try
        {
            // Nội dung lấy từ bảng ThongBao nên luôn khớp với thứ SQL đã ghi; chỉ lấy tin mới trong 2 phút.
            var since = DateTime.UtcNow.AddMinutes(-2);
            var n = await db.ThongBaos.AsNoTracking()
                .Where(x => x.MaNguoiDung == userId && !x.DaDoc && x.NgayTao >= since)
                .OrderByDescending(x => x.MaThongBao)
                .FirstOrDefaultAsync();
            if (n is null) return;

            var unread = await db.ThongBaos.AsNoTracking()
                .CountAsync(x => x.MaNguoiDung == userId && !x.DaDoc);

            await notificationHub.Clients.Group($"user-{userId}").SendAsync("ReceiveNotification", new
            {
                id = n.MaThongBao,
                type = n.Loai,
                title = n.TieuDe,
                content = n.NoiDung,
                referenceId = n.MaThamChieu,
                createdAt = n.NgayTao,
                url,
                unreadCount = unread
            }, CancellationToken.None);
        }
        catch (Exception ex) { log.LogWarning(ex, "Không đẩy được thông báo cho user {UserId}.", userId); }
    }

    public async Task PushToParticipantsAsync(int auctionId)
    {
        try
        {
            var ids = await db.LuotDatGias.AsNoTracking()
                .Where(l => l.MaPhienDauGia == auctionId)
                .Select(l => l.MaNguoiDung).Distinct().ToListAsync();
            var owner = await db.PhienDauGias.AsNoTracking()
                .Where(p => p.MaPhienDauGia == auctionId)
                .Select(p => (int?)p.SanPham.CuaHang.MaNguoiDung).FirstOrDefaultAsync();
            if (owner.HasValue) ids.Add(owner.Value);

            foreach (var uid in ids.Distinct())
                await PushLatestNotificationAsync(uid, $"/dau-gia/{auctionId}");
        }
        catch (Exception ex) { log.LogWarning(ex, "Không đẩy thông báo cho người tham gia phiên {Id}.", auctionId); }
    }
}