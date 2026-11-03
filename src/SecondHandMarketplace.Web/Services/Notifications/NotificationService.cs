using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Notifications;

public sealed class NotificationService(AppDbContext db) : INotificationService
{
    private const int MaxTitleLength = 200;    // ThongBao.TieuDe nvarchar(200)
    private const int MaxContentLength = 1000; // ThongBao.NoiDung nvarchar(1000)

    public async Task<(bool Success, string? Error)> CreateAsync(int userId, string type, string title, string content,
        int? referenceId = null, CancellationToken cancellationToken = default)
    {
        var error = Validate(type, title, content);
        if (error is not null) return (false, error);
        EnsureNoPendingChanges();

        if (!await db.NguoiDungs.AnyAsync(x => x.MaNguoiDung == userId, cancellationToken))
            return (false, "Không tìm thấy người nhận thông báo.");

        var entity = NewEntity(userId, type, title, content, referenceId);
        db.ThongBaos.Add(entity);
        error = await TrySaveAsync(new[] { entity }, cancellationToken);
        return error is null ? (true, null) : (false, error);
    }

    public async Task<(bool Success, string? Error, int SentCount)> CreateForAllUsersAsync(string type, string title,
        string content, int? referenceId = null, CancellationToken cancellationToken = default)
    {
        var error = Validate(type, title, content);
        if (error is not null) return (false, error, 0);
        EnsureNoPendingChanges();

        var userIds = await db.NguoiDungs.AsNoTracking()
            .Where(x => x.TrangThai == "ACTIVE")
            .Select(x => x.MaNguoiDung)
            .ToListAsync(cancellationToken);
        if (userIds.Count == 0) return (false, "Không có người dùng nào để nhận thông báo.", 0);

        // Một lần SaveChanges = một transaction: gửi đủ tất cả hoặc không gửi ai.
        var entities = userIds.Select(id => NewEntity(id, type, title, content, referenceId)).ToList();
        db.ThongBaos.AddRange(entities);
        error = await TrySaveAsync(entities, cancellationToken);
        return error is null ? (true, null, entities.Count) : (false, error, 0);
    }

    public async Task<NotificationPage> GetForUserAsync(int userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.ThongBaos.AsNoTracking().Where(x => x.MaNguoiDung == userId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.NgayTao).ThenByDescending(x => x.MaThongBao)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        return new NotificationPage(items, total);
    }

    public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default) =>
        db.ThongBaos.AsNoTracking().CountAsync(x => x.MaNguoiDung == userId && !x.DaDoc, cancellationToken);

    public async Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
    {
        EnsureNoPendingChanges();

        // Lọc theo cả MaNguoiDung: thông báo của người khác không bao giờ được nạp, nên không thể bị sửa.
        var notification = await db.ThongBaos
            .SingleOrDefaultAsync(x => x.MaThongBao == notificationId && x.MaNguoiDung == userId, cancellationToken);
        if (notification is null) return false;

        if (!notification.DaDoc)
        {
            notification.DaDoc = true;
            await db.SaveChangesAsync(cancellationToken);
        }
        return true;
    }

    // SaveChanges lưu mọi thay đổi đang chờ trong AppDbContext dùng chung (scoped). Nếu nơi gọi còn thay đổi chưa lưu,
    // service sẽ vô tình lưu dở dang nghiệp vụ của họ nên báo lỗi ngay thay vì âm thầm lưu.
    private void EnsureNoPendingChanges()
    {
        if (db.ChangeTracker.HasChanges())
            throw new InvalidOperationException(
                "AppDbContext đang có thay đổi chưa lưu của nơi gọi. Hãy SaveChanges nghiệp vụ chính trước khi gọi INotificationService.");
    }

    // Không tự mở transaction: nếu nơi gọi đang có transaction trên cùng DbContext thì lệnh lưu nằm trong transaction đó.
    // Khi lưu lỗi phải gỡ các dòng vừa thêm khỏi ChangeTracker, nếu không lần SaveChanges tiếp theo của nơi gọi sẽ lưu lại và lỗi theo.
    private async Task<string?> TrySaveAsync(IReadOnlyCollection<ThongBao> entities, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException)
        {
            foreach (var entity in entities) db.Entry(entity).State = EntityState.Detached;
            return "Không lưu được thông báo.";
        }
    }

    private static string? Validate(string type, string title, string content)
    {
        if (!NotificationTypes.IsValid(type)) return "Loại thông báo không hợp lệ.";
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > MaxTitleLength)
            return $"Tiêu đề bắt buộc và tối đa {MaxTitleLength} ký tự.";
        if (string.IsNullOrWhiteSpace(content) || content.Trim().Length > MaxContentLength)
            return $"Nội dung bắt buộc và tối đa {MaxContentLength} ký tự.";
        return null;
    }

    // NgayTao phải đặt tay: entity không khai báo default SQL nên EF sẽ gửi 0001-01-01 nếu để trống.
    private static ThongBao NewEntity(int userId, string type, string title, string content, int? referenceId) => new()
    {
        MaNguoiDung = userId,
        Loai = type,
        TieuDe = title.Trim(),
        NoiDung = content.Trim(),
        MaThamChieu = referenceId,
        DaDoc = false,
        NgayTao = DateTime.UtcNow
    };
}
