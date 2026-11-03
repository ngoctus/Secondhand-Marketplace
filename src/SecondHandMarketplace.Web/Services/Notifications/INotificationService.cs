using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Notifications;

/// <summary>Giá trị hợp lệ của cột ThongBao.Loai (khớp CHECK CK_ThongBao_Loai trong database/03_constraints.sql).</summary>
public static class NotificationTypes
{
    public const string Order = "ORDER";
    public const string Product = "PRODUCT";
    public const string System = "SYSTEM";

    public static bool IsValid(string? type) => type is Order or Product or System;
}

/// <summary>Một trang thông báo của một người dùng.</summary>
public sealed record NotificationPage(IReadOnlyList<ThongBao> Items, int TotalItems);

/// <summary>
/// Dịch vụ thông báo dùng chung. Các mảng khác (A, B, D, E) chỉ cần inject và gọi <see cref="CreateAsync"/>.
/// Mã người nhận luôn do nơi gọi truyền vào; service không tự lấy người đăng nhập.
/// Hợp đồng với nơi gọi:
/// - Service dùng chung AppDbContext (scoped). Nếu context còn thay đổi chưa lưu, các hàm ghi sẽ ném InvalidOperationException
///   (thay vì âm thầm lưu dở dang nghiệp vụ của nơi gọi): hãy SaveChanges nghiệp vụ chính xong rồi mới gọi.
/// - Service không tự mở/commit transaction. Nếu nơi gọi đang có transaction trên cùng DbContext, thông báo nằm trong transaction đó.
/// - Procedure đã TỰ tạo thông báo (Loai = ORDER), KHÔNG gọi thêm ở service kẻo trùng:
///   sp_TaoDonHangTheoShop (qua sp_TaoDonHang / sp_DatHangTuGio) báo chủ shop có đơn mới;
///   sp_CapNhatTrangThaiDon báo người mua và chủ shop về đổi trạng thái (trừ người thực hiện).
///   Các trường hợp còn lại (duyệt/từ chối sản phẩm, tranh chấp, báo cáo, đấu giá...) phải gọi CreateAsync.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Tạo một thông báo cho một người dùng.
    /// type: NotificationTypes.Order | Product | System. referenceId: mã đơn hàng/sản phẩm liên quan (nếu có).
    /// Trả (false, lỗi) khi dữ liệu không hợp lệ hoặc không có người nhận; không ném exception vì lỗi nghiệp vụ.
    /// </summary>
    Task<(bool Success, string? Error)> CreateAsync(int userId, string type, string title, string content,
        int? referenceId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin gửi toàn hệ thống. GIẢ ĐỊNH (tài liệu phân công chỉ ghi "toàn hệ thống", không định nghĩa đối tượng nhận):
    /// gửi cho mọi tài khoản đang ACTIVE, gồm cả ADMIN; tài khoản LOCKED không nhận. Đổi đối tượng chỉ cần sửa bộ lọc trong hàm này.
    /// Trả về số người nhận.
    /// </summary>
    Task<(bool Success, string? Error, int SentCount)> CreateForAllUsersAsync(string type, string title, string content,
        int? referenceId = null, CancellationToken cancellationToken = default);

    /// <summary>Danh sách thông báo của chính userId, mới nhất trước, có phân trang.</summary>
    Task<NotificationPage> GetForUserAsync(int userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>Số thông báo chưa đọc của userId (hiển thị trên chuông).</summary>
    Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu đã đọc. Chỉ thành công khi thông báo thuộc về userId; thông báo của người khác hoặc không tồn tại đều trả false.
    /// </summary>
    Task<bool> MarkAsReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default);
}
