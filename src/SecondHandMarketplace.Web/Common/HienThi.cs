using System.Globalization;

namespace SecondHandMarketplace.Web.Common;

/// <summary>Định dạng hiển thị: tiền, giờ Việt Nam (DB lưu UTC), nhãn trạng thái.</summary>
public static class HienThi
{
    private static readonly TimeSpan GioVn = TimeSpan.FromHours(7);

    public static DateTime SangGioVn(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(GioVn);
    public static DateTime TuGioVnSangUtc(DateTime vn) => DateTime.SpecifyKind(vn, DateTimeKind.Utc).Subtract(GioVn);

    public static string Tien(decimal v) =>
        v.ToString("#,##0", CultureInfo.InvariantCulture).Replace(',', '.') + "đ";

    public static string NgayGio(DateTime? utc) =>
        utc is null ? "—" : SangGioVn(utc.Value).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string Ngay(DateTime? utc) =>
        utc is null ? "—" : SangGioVn(utc.Value).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static string GioGiay(DateTime utc) =>
        SangGioVn(utc).ToString("HH:mm:ss dd/MM", CultureInfo.InvariantCulture);

    /// <summary>Chuỗi ISO UTC cho JavaScript (new Date(...)).</summary>
    public static string Iso(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    /// <summary>Giá trị cho thẻ input datetime-local (giờ Việt Nam).</summary>
    public static string ChoInputGio(DateTime? utc) =>
        utc is null ? "" : SangGioVn(utc.Value).ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);

    public static string TrangThaiDon(string s) => s switch
    {
        "PENDING" => "Chờ xác nhận",
        "CONFIRMED" => "Đã xác nhận",
        "SHIPPING" => "Đang giao",
        "DELIVERED" => "Đã giao",
        "COMPLETED" => "Hoàn tất",
        "CANCELLED" => "Đã hủy",
        _ => s
    };

    public static string PhuongThucThanhToan(string s) => s switch
    {
        "COD" => "Thanh toán khi nhận hàng",
        "BANK_TRANSFER" => "Chuyển khoản",
        "ONLINE" => "Thanh toán online",
        _ => s
    };

    public static string TrangThaiThanhToan(string s) => s switch
    {
        "UNPAID" => "Chưa thanh toán",
        "PENDING" => "Đang xử lý",
        "PAID" => "Đã thanh toán",
        "REFUNDED" => "Đã hoàn tiền",
        "FAILED" => "Thất bại",
        "SUCCESS" => "Thành công",
        _ => s
    };

    public static string NguoiThucHien(string s) => s switch
    {
        "BUYER" => "Người mua",
        "SELLER" => "Người bán",
        "ADMIN" => "Quản trị viên",
        "SYSTEM" => "Hệ thống",
        _ => s
    };

    public static string TrangThaiPhien(string s) => s switch
    {
        "SCHEDULED" => "Sắp diễn ra",
        "RUNNING" => "Đang diễn ra",
        "ENDING" => "Đang chốt kết quả",
        "ENDED" => "Đã kết thúc",
        "CANCELLED" => "Đã hủy",
        _ => s
    };

    /// <summary>Lớp CSS badge: "st st-pending", "st st-running"...</summary>
    public static string LopTrangThai(string s) => "st st-" + s.ToLowerInvariant();

    /// <summary>"Nguyễn Văn An" -> "N*** An". Che tên người đặt giá để bảo vệ riêng tư.</summary>
    public static string AnDanhTinh(string? hoTen)
    {
        if (string.IsNullOrWhiteSpace(hoTen)) return "Ẩn danh";
        var tu = hoTen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tu.Length == 1 ? tu[0][..1] + "***" : tu[0][..1] + "*** " + tu[^1];
    }
}