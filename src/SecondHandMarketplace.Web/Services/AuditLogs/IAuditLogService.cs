namespace SecondHandMarketplace.Web.Services.AuditLogs;

public interface IAuditLogService
{
    /// <summary>
    /// Ghi nhật ký vào bảng NhatKyHeThong. Gọi trong mọi hành động quan trọng
    /// (khóa user, duyệt/ẩn sản phẩm, xử lý đơn...).
    /// ketQua: SUCCESS | FAILED | DENIED.
    /// </summary>
    Task LogAsync(int? maNguoiDung, string hanhDong, string loaiDoiTuong, int? doiTuongId = null,
        string ketQua = "SUCCESS", string? lyDo = null, string? chiTiet = null);
}