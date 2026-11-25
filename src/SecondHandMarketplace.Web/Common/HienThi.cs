using System.Globalization;

namespace SecondHandMarketplace.Web.Common;

/// <summary>Định dạng hiển thị: tiền, giờ Việt Nam (DB lưu UTC), nhãn trạng thái đa ngôn ngữ (VI | EN).</summary>
public static class HienThi
{
    private static readonly TimeSpan GioVn = TimeSpan.FromHours(7);

    public static bool LaTiengAnh =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase);

    public static string Chon(string vi, string en) => LaTiengAnh ? en : vi;

    public static DateTime SangGioVn(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(GioVn);
    public static DateTime TuGioVnSangUtc(DateTime vn) => DateTime.SpecifyKind(vn, DateTimeKind.Utc).Subtract(GioVn);

    public static string Tien(decimal v) =>
        v.ToString("#,##0", CultureInfo.InvariantCulture).Replace(',', '.') + "đ";

    public static string NgayGio(DateTime? utc) =>
        utc is null ? "—" : SangGioVn(utc.Value).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string NgayGioGiay(DateTime? utc) =>
        utc is null ? "—" : SangGioVn(utc.Value).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

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
        "PENDING" => Chon("Chờ xác nhận", "Pending Confirmation"),
        "CONFIRMED" => Chon("Đã xác nhận", "Confirmed"),
        "SHIPPING" => Chon("Đang giao", "Shipping"),
        "DELIVERED" => Chon("Đã giao", "Delivered"),
        "COMPLETED" => Chon("Hoàn tất", "Completed"),
        "CANCELLED" => Chon("Đã hủy", "Cancelled"),
        _ => s
    };

    /// <summary>
    /// Nhãn trạng thái hiển thị linh hoạt theo hình thức nhận hàng (Mục 4.3 cho Người D).
    /// Với GAP_TRUC_TIEP: SHIPPING hiện là "Đã hẹn gặp", DELIVERED hiện là "Đã nhận hàng", v.v.
    /// </summary>
    public static string TrangThaiDon(string s, string? hinhThucNhan)
    {
        if (string.Equals(hinhThucNhan, "GAP_TRUC_TIEP", StringComparison.OrdinalIgnoreCase))
        {
            return s switch
            {
                "PENDING" => Chon("Chờ xác nhận hẹn", "Pending Meetup"),
                "CONFIRMED" => Chon("Đã đồng ý hẹn", "Meetup Confirmed"),
                "SHIPPING" => Chon("Đã hẹn gặp", "Meetup Scheduled"),
                "DELIVERED" => Chon("Đã nhận hàng", "Handed Over"),
                "COMPLETED" => Chon("Hoàn tất", "Completed"),
                "CANCELLED" => Chon("Đã hủy hẹn", "Meetup Cancelled"),
                _ => s
            };
        }
        return TrangThaiDon(s);
    }

    public static string TenHinhThucNhan(string? htn) => htn switch
    {
        "GAP_TRUC_TIEP" => Chon("Gặp mặt trực tiếp", "In-Person Meetup"),
        "GIAO_HANG" => Chon("Giao hàng tận nơi", "Home Delivery"),
        _ => Chon("Giao hàng tận nơi", "Home Delivery")
    };

    public static string PhuongThucThanhToan(string s) => s switch
    {
        "COD" => Chon("Thanh toán khi nhận hàng", "Cash on Delivery (COD)"),
        "BANK_TRANSFER" => Chon("Chuyển khoản", "Bank Transfer"),
        "ONLINE" => Chon("Thanh toán online", "Online Payment"),
        _ => s
    };

    public static string TrangThaiThanhToan(string s) => s switch
    {
        "UNPAID" => Chon("Chưa thanh toán", "Unpaid"),
        "PENDING" => Chon("Đang xử lý", "Processing"),
        "PAID" => Chon("Đã thanh toán", "Paid"),
        "REFUNDED" => Chon("Đã hoàn tiền", "Refunded"),
        "FAILED" => Chon("Thất bại", "Failed"),
        "SUCCESS" => Chon("Thành công", "Success"),
        _ => s
    };

    public static string NguoiThucHien(string s) => s switch
    {
        "BUYER" => Chon("Người mua", "Buyer"),
        "SELLER" => Chon("Người bán", "Seller"),
        "ADMIN" => Chon("Quản trị viên", "Administrator"),
        "SYSTEM" => Chon("Hệ thống", "System"),
        _ => s
    };

    public static string TrangThaiTranhChap(string s) => s switch
    {
        "OPEN" => Chon("Chờ phản hồi", "Awaiting Response"),
        "PROCESSING" => Chon("Đang xử lý", "Under Review"),
        "RESOLVED" => Chon("Đã giải quyết", "Resolved"),
        "REJECTED" => Chon("Đã từ chối", "Rejected"),
        _ => s
    };

    public static string TrangThaiPhien(string s) => s switch
    {
        "SCHEDULED" => Chon("Sắp diễn ra", "Upcoming"),
        "RUNNING" => Chon("Đang diễn ra", "Live"),
        "ENDING" => Chon("Đang chốt kết quả", "Finalizing"),
        "ENDED" => Chon("Đã kết thúc", "Ended"),
        "CANCELLED" => Chon("Đã hủy", "Cancelled"),
        _ => s
    };

    /// <summary>Trạng thái sản phẩm dành cho Kênh Người Bán &amp; Admin.</summary>
    public static string TrangThaiSanPham(string? s) => s switch
    {
        "DRAFT" => Chon("Bản nháp", "Draft"),
        "PENDING_REVIEW" => Chon("Đang chờ duyệt", "Pending Review"),
        "ACTIVE" => Chon("Đang bán", "Active"),
        "REJECTED" => Chon("Bị từ chối", "Rejected"),
        "HIDDEN" => Chon("Đang ẩn", "Hidden"),
        "RESERVED" => Chon("Đã giữ chỗ", "Reserved"),
        "SOLD" => Chon("Đã bán", "Sold"),
        "REMOVED" => Chon("Đã gỡ", "Removed"),
        _ => s ?? ""
    };

    /// <summary>Trạng thái sản phẩm hiển thị công khai trên thẻ / chi tiết sản phẩm.</summary>
    public static string TrangThaiSanPhamCongKhai(string? s) => s switch
    {
        "RESERVED" => Chon("Đã giữ", "Reserved"),
        "SOLD" => Chon("Đã bán", "Sold"),
        _ => Chon("Còn hàng", "Available")
    };

    /// <summary>Trạng thái tài khoản người dùng / cửa hàng.</summary>
    public static string TrangThaiTaiKhoan(string? s) => s switch
    {
        "ACTIVE" => Chon("Hoạt động", "Active"),
        "LOCKED" => Chon("Đã khóa", "Locked"),
        "INACTIVE" => Chon("Ngừng hoạt động", "Inactive"),
        _ => s ?? ""
    };

    /// <summary>Ánh xạ tên Danh mục cơ bản từ DB sang ngôn ngữ hiện tại (VI | EN).</summary>
    public static string TenDanhMuc(string? ten) => ten?.Trim() switch
    {
        "Thời trang" => Chon("Thời trang", "Fashion"),
        "Đồ điện tử" => Chon("Đồ điện tử", "Electronics"),
        "Đồ gia dụng" => Chon("Đồ gia dụng", "Home Appliances"),
        "Sách & văn phòng phẩm" => Chon("Sách & văn phòng phẩm", "Books & Stationery"),
        "Xe cộ & phụ kiện" => Chon("Xe cộ & phụ kiện", "Vehicles & Accessories"),
        "Đồ chơi & mẹ bé" => Chon("Đồ chơi & mẹ bé", "Toys, Mom & Baby"),
        "Thể thao & dã ngoại" => Chon("Thể thao & dã ngoại", "Sports & Outdoors"),
        "Nội thất" => Chon("Nội thất", "Furniture"),
        "Khác" => Chon("Khác", "Others"),
        _ => ten ?? ""
    };

    /// <summary>Ánh xạ tên Tình trạng sản phẩm cơ bản (theo mã hoặc tên tiếng Việt trong DB) sang ngôn ngữ hiện tại.</summary>
    public static string TenTinhTrang(string? maHoacTen) => maHoacTen?.Trim() switch
    {
        "NEW" or "Mới 100%" => Chon("Mới 100%", "Brand New (100%)"),
        "LIKE_NEW" or "Như mới" => Chon("Như mới", "Like New"),
        "GOOD" or "Tốt" => Chon("Tốt", "Good"),
        "FAIR" or "Khá" => Chon("Khá", "Fair"),
        "WORN" or "Cũ" => Chon("Cũ", "Well Worn"),
        _ => maHoacTen ?? ""
    };

    /// <summary>Lớp CSS badge: "st st-pending", "st st-running"...</summary>
    public static string LopTrangThai(string s) => "st st-" + s.ToLowerInvariant();

    /// <summary>"Nguyễn Văn An" -> "N*** An". Che tên người đặt giá để bảo vệ riêng tư.</summary>
    public static string AnDanhTinh(string? hoTen)
    {
        if (string.IsNullOrWhiteSpace(hoTen)) return Chon("Ẩn danh", "Anonymous");
        var tu = hoTen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tu.Length == 1 ? tu[0][..1] + "***" : tu[0][..1] + "*** " + tu[^1];
    }
}
