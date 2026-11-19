using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Chat;

namespace SecondHandMarketplace.Web.Services.Chat;

public interface IChatService
{
    /// <summary>Người mua bấm "Chat" ở một sản phẩm: tìm hoặc tạo hội thoại với chủ shop.</summary>
    Task<(bool Ok, string? Error, ConversationVm? Conversation)> StartByProductAsync(int userId, int productId, CancellationToken ct = default);
    Task<IReadOnlyList<ConversationVm>> GetConversationsAsync(int userId, int skip = 0, int take = 50, CancellationToken ct = default);
    /// <summary>Trả null nếu userId không thuộc hội thoại. beforeId = phân trang tin cũ hơn.</summary>
    Task<IReadOnlyList<ChatMessageVm>?> GetMessagesAsync(int userId, int conversationId, long? beforeId, int take, CancellationToken ct = default);
    Task<(bool Ok, string? Error, ChatMessageVm? Message, int ReceiverId)> SendAsync(int userId, int conversationId, string? content);
    /// <summary>Đánh dấu đã đọc; trả mã người gửi nếu có tin được cập nhật (để báo "đã xem").</summary>
    Task<int?> MarkReadAsync(int userId, int conversationId);
    Task<int> CountUnreadAsync(int userId, CancellationToken ct = default);
}

public sealed class ChatService(AppDbContext db) : IChatService
{
    private const int MaxLength = 1000;
    private const int MaxPerMinute = 20;

    private static ChatMessageVm ToVm(TinNhan t, string? senderName = null) => new()
    {
        Id = t.MaTinNhan,
        ConversationId = t.MaHoiThoai,
        SenderId = t.MaNguoiGui,
        SenderName = senderName,
        Content = t.NoiDung,
        SentAtUtc = HienThi.Iso(t.NgayGui),
        Read = t.DaDoc
    };

    /// <summary>Mã người còn lại trong hội thoại; null nếu userId không thuộc hội thoại (hoặc không tồn tại).</summary>
    private Task<int?> OtherUserAsync(int userId, int conversationId, CancellationToken ct = default) =>
        db.HoiThoais.AsNoTracking()
            .Where(h => h.MaHoiThoai == conversationId && (h.MaNguoiDung1 == userId || h.MaNguoiDung2 == userId))
            .Select(h => (int?)(h.MaNguoiDung1 == userId ? h.MaNguoiDung2 : h.MaNguoiDung1))
            .FirstOrDefaultAsync(ct);

    private async Task<List<ConversationVm>> LoadAsync(int userId, int? onlyId, CancellationToken ct, int skip = 0, int take = 50)
    {
        var q = db.HoiThoais.AsNoTracking().Where(h => h.MaNguoiDung1 == userId || h.MaNguoiDung2 == userId);
        if (onlyId is int one) q = q.Where(h => h.MaHoiThoai == one);

        var rows = await q
            .OrderByDescending(h => h.NgayTinCuoi ?? h.NgayTao)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 100))
            .Select(h => new
            {
                h.MaHoiThoai,
                h.NoiDungCuoi,
                h.NgayTinCuoi,
                h.NgayTao,
                Is1 = h.MaNguoiDung1 == userId,
                Name1 = h.NguoiDung1.HoTen,
                Name2 = h.NguoiDung2.HoTen,
                Shop1 = db.CuaHangs.Where(c => c.MaNguoiDung == h.MaNguoiDung1).Select(c => c.TenShop).FirstOrDefault(),
                Shop2 = db.CuaHangs.Where(c => c.MaNguoiDung == h.MaNguoiDung2).Select(c => c.TenShop).FirstOrDefault(),
                Unread = h.TinNhans.Count(t => t.MaNguoiGui != userId && !t.DaDoc)
            })
            .ToListAsync(ct);

        return rows.Select(r => new ConversationVm
        {
            Id = r.MaHoiThoai,
            Title = r.Is1 ? r.Name2 : r.Name1,
            Shop = r.Is1 ? r.Shop2 : r.Shop1,
            LastMessage = r.NoiDungCuoi,
            LastAtUtc = HienThi.Iso(r.NgayTinCuoi ?? r.NgayTao),
            Unread = r.Unread
        }).ToList();
    }

    public async Task<(bool Ok, string? Error, ConversationVm? Conversation)> StartByProductAsync(int userId, int productId, CancellationToken ct = default)
    {
        var me = await db.NguoiDungs.AsNoTracking().Where(x => x.MaNguoiDung == userId)
            .Select(x => new { x.VaiTro, x.TrangThai }).FirstOrDefaultAsync(ct);
        if (me is null || me.VaiTro != "USER" || me.TrangThai != "ACTIVE")
            return (false, "Chỉ tài khoản người dùng đang hoạt động mới nhắn tin được.", null);

        // Người nhận luôn lấy từ sản phẩm trên server, không tin dữ liệu client gửi lên.
        var shop = await db.SanPhams.AsNoTracking()
            .Where(p => p.MaSanPham == productId && !p.DaXoa
                        && p.CuaHang.TrangThai == "ACTIVE" && p.CuaHang.NguoiDung.TrangThai == "ACTIVE")
            .Select(p => new { OwnerId = p.CuaHang.MaNguoiDung })
            .FirstOrDefaultAsync(ct);
        if (shop is null) return (false, "Không tìm thấy người bán của sản phẩm này.", null);
        if (shop.OwnerId == userId) return (false, "Đây là sản phẩm của bạn.", null);

        var a = Math.Min(userId, shop.OwnerId);
        var b = Math.Max(userId, shop.OwnerId);

        var id = await db.HoiThoais.Where(h => h.MaNguoiDung1 == a && h.MaNguoiDung2 == b)
            .Select(h => (int?)h.MaHoiThoai).FirstOrDefaultAsync(ct);
        if (id is null)
        {
            var h = new HoiThoai { MaNguoiDung1 = a, MaNguoiDung2 = b, NgayTao = DateTime.UtcNow };
            db.HoiThoais.Add(h);
            try
            {
                await db.SaveChangesAsync(ct);
                id = h.MaHoiThoai;
            }
            catch (DbUpdateException)
            {
                // Hai request cùng tạo một lúc: lấy bản đã được tạo trước.
                db.Entry(h).State = EntityState.Detached;
                id = await db.HoiThoais.AsNoTracking().Where(x => x.MaNguoiDung1 == a && x.MaNguoiDung2 == b)
                    .Select(x => (int?)x.MaHoiThoai).FirstOrDefaultAsync(ct);
                if (id is null) return (false, "Không tạo được cuộc trò chuyện.", null);
            }
        }

        var conv = (await LoadAsync(userId, id, ct)).FirstOrDefault();
        return conv is null ? (false, "Không tải được cuộc trò chuyện.", null) : (true, null, conv);
    }

    public async Task<IReadOnlyList<ConversationVm>> GetConversationsAsync(int userId, int skip = 0, int take = 50, CancellationToken ct = default) =>
        await LoadAsync(userId, null, ct, skip, take);

    public async Task<IReadOnlyList<ChatMessageVm>?> GetMessagesAsync(int userId, int conversationId, long? beforeId, int take, CancellationToken ct = default)
    {
        if (await OtherUserAsync(userId, conversationId, ct) is null) return null;

        take = Math.Clamp(take, 1, 50);
        var q = db.TinNhans.AsNoTracking().Where(t => t.MaHoiThoai == conversationId);
        if (beforeId is long before) q = q.Where(t => t.MaTinNhan < before);

        var rows = await q.OrderByDescending(t => t.MaTinNhan).Take(take).ToListAsync(ct);
        rows.Reverse();   // trả theo thứ tự cũ -> mới
        return rows.Select(t => ToVm(t)).ToList();
    }

    public async Task<(bool Ok, string? Error, ChatMessageVm? Message, int ReceiverId)> SendAsync(int userId, int conversationId, string? content)
    {
        content = (content ?? "").Trim();
        if (content.Length == 0) return (false, "Tin nhắn không được để trống.", null, 0);
        if (content.Length > MaxLength) return (false, $"Tin nhắn tối đa {MaxLength} ký tự.", null, 0);

        var other = await OtherUserAsync(userId, conversationId);
        if (other is null) return (false, "Không tìm thấy cuộc trò chuyện.", null, 0);
        var otherId = other.Value;

        var users = await db.NguoiDungs.AsNoTracking()
            .Where(x => x.MaNguoiDung == userId || x.MaNguoiDung == otherId)
            .Select(x => new { x.MaNguoiDung, x.HoTen, x.VaiTro, x.TrangThai })
            .ToListAsync();
        if (users.Count != 2 || users.Any(u => u.VaiTro != "USER" || u.TrangThai != "ACTIVE"))
            return (false, "Không thể nhắn tin với tài khoản này.", null, 0);

        var now = DateTime.UtcNow;
        var since = now.AddMinutes(-1);
        var recent = await db.TinNhans.CountAsync(t => t.MaNguoiGui == userId && t.NgayGui > since);
        if (recent >= MaxPerMinute) return (false, "Bạn gửi quá nhanh, vui lòng chờ một chút.", null, 0);

        var conv = await db.HoiThoais.FirstOrDefaultAsync(h => h.MaHoiThoai == conversationId);
        if (conv is null) return (false, "Không tìm thấy cuộc trò chuyện.", null, 0);

        var msg = new TinNhan
        {
            MaHoiThoai = conversationId,
            MaNguoiGui = userId,
            NoiDung = content,
            DaDoc = false,
            NgayGui = now      // đặt tay: entity không khai báo default SQL
        };
        db.TinNhans.Add(msg);
        conv.NoiDungCuoi = content.Length > 200 ? content[..197] + "..." : content;
        conv.NgayTinCuoi = now;
        await db.SaveChangesAsync();

        var sender = users.FirstOrDefault(u => u.MaNguoiDung == userId);
        if (sender is null) return (false, "Không tìm thấy tài khoản người gửi.", null, 0);
        return (true, null, ToVm(msg, sender.HoTen), otherId);
    }

    public async Task<int?> MarkReadAsync(int userId, int conversationId)
    {
        var other = await OtherUserAsync(userId, conversationId);
        if (other is null) return null;

        var n = await db.TinNhans
            .Where(t => t.MaHoiThoai == conversationId && t.MaNguoiGui != userId && !t.DaDoc)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.DaDoc, true));
        return n > 0 ? other : null;
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken ct = default) =>
        db.TinNhans.AsNoTracking().CountAsync(t => t.MaNguoiGui != userId && !t.DaDoc
            && (t.HoiThoai.MaNguoiDung1 == userId || t.HoiThoai.MaNguoiDung2 == userId), ct);
}