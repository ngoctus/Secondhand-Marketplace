using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.ViewModels.Reviews;

// ============================================================
// ViewModels cho module Đánh giá (C2 – ReviewService)
// Dùng chung cho User (giao diện người mua) và Admin (rà soát).
// ============================================================

/// <summary>
/// Dữ liệu gửi lên khi người mua tạo đánh giá mới.
/// Validation khớp với ràng buộc của sp_TaoDanhGia và bảng DanhGia.
/// </summary>
public sealed class TaoDanhGiaVm
{
    /// <summary>Mã đơn hàng COMPLETED cần đánh giá.</summary>
    [Required]
    public int MaDonHang { get; set; }

    /// <summary>Số sao: 1 đến 5.</summary>
    [Required(ErrorMessage = "Vui lòng chọn số sao.")]
    [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5.")]
    public byte SoSao { get; set; }

    /// <summary>Nhận xét tùy chọn, tối đa 1 000 ký tự.</summary>
    [StringLength(1000, ErrorMessage = "Nội dung đánh giá không được vượt quá 1 000 ký tự.")]
    public string? NoiDung { get; set; }
}

/// <summary>
/// Một dòng đánh giá hiển thị ở trang shop hoặc danh sách đánh giá của người mua.
/// Không bao gồm Email, SoDienThoai, DiaChiMacDinh của người mua (bảo vệ quyền riêng tư).
/// </summary>
public sealed class DanhGiaItemVm
{
    public int MaDanhGia { get; init; }
    public int MaDonHang { get; init; }
    public int MaCuaHang { get; init; }
    public int MaNguoiMua { get; init; }
    /// <summary>Họ tên người mua (chỉ hiện tên, không lộ thông tin liên lạc).</summary>
    public string TenNguoiMua { get; init; } = string.Empty;
    /// <summary>Ảnh đại diện người mua (có thể null).</summary>
    public string? AnhDaiDienNguoiMua { get; init; }
    public byte SoSao { get; init; }
    public string? NoiDung { get; init; }
    public string TrangThai { get; init; } = "VISIBLE";
    public DateTime NgayTao { get; init; }
}

/// <summary>
/// Dòng đánh giá nổi bật cho khối "Người dùng nói gì" ở trang chủ (B).
/// Chỉ thông tin công khai: tên + ảnh đại diện người mua, tên sản phẩm đã mua, tên shop.
/// MaSanPham null khi sản phẩm đã gỡ bán, để trang chủ không đặt link vào trang 404.
/// </summary>
public sealed class DanhGiaNoiBatVm
{
    public int MaDanhGia { get; init; }
    public byte SoSao { get; init; }
    public string NoiDung { get; init; } = string.Empty;
    public string TenNguoiMua { get; init; } = string.Empty;
    public string? AnhDaiDienNguoiMua { get; init; }
    public string TenSanPham { get; init; } = string.Empty;
    public int? MaSanPham { get; init; }
    public string TenShop { get; init; } = string.Empty;
    public DateTime NgayTao { get; init; }
}

/// <summary>
/// Kết quả trả về khi admin lọc/xem đánh giá theo shop, kèm thống kê điểm.
/// </summary>
public sealed class DanhSachDanhGiaTheoShopVm
{
    public int MaCuaHang { get; init; }
    public string TenShop { get; init; } = string.Empty;
    /// <summary>Điểm trung bình tính từ vw_DiemCuaHang (chỉ VISIBLE).</summary>
    public decimal DiemTrungBinh { get; init; }
    /// <summary>Số đánh giá VISIBLE.</summary>
    public int SoDanhGia { get; init; }
    /// <summary>Danh sách đánh giá có phân trang.</summary>
    public IReadOnlyList<DanhGiaItemVm> Items { get; init; } = Array.Empty<DanhGiaItemVm>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize));
}

/// <summary>
/// Thống kê điểm shop đọc từ vw_DiemCuaHang.
/// Dùng để hiển thị trên thẻ sản phẩm, trang chi tiết, trang shop công khai (phối hợp với B, A).
/// </summary>
public sealed class DiemCuaHangVm
{
    public int MaCuaHang { get; init; }
    public decimal DiemTrungBinh { get; init; }
    public int SoDanhGia { get; init; }
    public int SoDonThanhCong { get; init; }
}

/// <summary>
/// Một dòng đánh giá trong danh sách đánh giá của tôi (/User/Reviews).
/// </summary>
public sealed class DanhGiaCuaToiItemVm
{
    public int MaDanhGia { get; init; }
    public int MaDonHang { get; init; }
    public string MaDon { get; init; } = string.Empty;
    public int MaCuaHang { get; init; }
    public string TenShop { get; init; } = string.Empty;
    public byte SoSao { get; init; }
    public string? NoiDung { get; init; }
    public string TrangThai { get; init; } = "VISIBLE";
    public DateTime NgayTao { get; init; }
    public List<string> TenSanPhams { get; init; } = new();
    public string? AnhSanPhamDaiDien { get; init; }
}

/// <summary>
/// Danh sách đánh giá do chính người mua hiện tại đã viết (có phân trang).
/// </summary>
public sealed class DanhSachDanhGiaCuaToiVm
{
    public IReadOnlyList<DanhGiaCuaToiItemVm> Items { get; init; } = Array.Empty<DanhGiaCuaToiItemVm>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize));
}

// ============================================================
// C4 – Admin rà soát shop bị đánh giá thấp
// ============================================================

/// <summary>
/// Một shop trong danh sách rà soát của Admin.
/// Điểm và số đơn lấy từ vw_DiemCuaHang (chỉ tính đánh giá VISIBLE), nên ẩn một đánh giá
/// sẽ làm thay đổi trực tiếp các chỉ số này ở lần tải kế tiếp.
/// </summary>
public sealed class ShopCanRaSoatItemVm
{
    public int MaCuaHang { get; init; }
    public string TenShop { get; init; } = string.Empty;
    /// <summary>ACTIVE hoặc LOCKED.</summary>
    public string TrangThaiShop { get; init; } = "ACTIVE";
    /// <summary>Lý do khóa gần nhất (chỉ có khi TrangThaiShop = LOCKED).</summary>
    public string? LyDoKhoa { get; init; }

    public int ChuShopId { get; init; }
    public string TenChuShop { get; init; } = string.Empty;
    public string EmailChuShop { get; init; } = string.Empty;
    public string? SoDienThoai { get; init; }
    /// <summary>Phường/Xã, Tỉnh/Thành; "Chưa cập nhật" nếu shop chưa khai địa chỉ.</summary>
    public string KhuVuc { get; init; } = string.Empty;

    public decimal DiemTrungBinh { get; init; }
    public int SoDanhGia { get; init; }
    public int SoDonThanhCong { get; init; }
    /// <summary>Số báo cáo vi phạm vào shop và các sản phẩm của shop.</summary>
    public int SoBaoCao { get; init; }
    /// <summary>Tiêu chí rà soát: điểm dưới 3.0 và có ít nhất 5 đánh giá VISIBLE.</summary>
    public bool LaShopCanRaSoat { get; init; }
}

/// <summary>Bộ lọc trạng thái/tìm kiếm cho trang rà soát shop.</summary>
public sealed class ShopCanRaSoatFilterVm
{
    /// <summary>Từ khóa tìm theo tên shop, tên chủ shop hoặc email.</summary>
    public string? Search { get; set; }

    /// <summary>"can_ra_soat" (mặc định, điểm &lt; 3.0 và &gt;= 5 đánh giá) hoặc "all".</summary>
    public string? Tab { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>Chuẩn hóa tab về đúng hai giá trị hợp lệ.</summary>
    public string TabChuanHoa => string.Equals(Tab, "all", StringComparison.OrdinalIgnoreCase)
        ? "all"
        : "can_ra_soat";
}

/// <summary>Kết quả trang danh sách shop cần rà soát (có phân trang và bộ lọc).</summary>
public sealed class DanhSachShopCanRaSoatVm
{
    public IReadOnlyList<ShopCanRaSoatItemVm> Items { get; init; } = Array.Empty<ShopCanRaSoatItemVm>();
    public int CurrentPage { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalItems / (double)Math.Max(1, PageSize));

    /// <summary>Tổng số shop thỏa tiêu chí điểm thấp trên toàn hệ thống (độc lập với tab/tìm kiếm).</summary>
    public int TongShopCanRaSoat { get; init; }

    /// <summary>Giữ lại bộ lọc để view dựng lại tab, ô tìm kiếm và liên kết phân trang.</summary>
    public ShopCanRaSoatFilterVm Filter { get; init; } = new();
}

/// <summary>Một đơn gần đây của shop, hiển thị kèm trang chi tiết rà soát.</summary>
public sealed class DonHangGanDayVm
{
    public int MaDonHang { get; init; }
    public string MaDon { get; init; } = string.Empty;
    public string TenNguoiNhan { get; init; } = string.Empty;
    public decimal TongTien { get; init; }
    public string TrangThai { get; init; } = string.Empty;
    public DateTime NgayTao { get; init; }
}

/// <summary>Dữ liệu trang chi tiết rà soát: hồ sơ shop + đánh giá (kể cả HIDDEN) + đơn gần đây.</summary>
public sealed class ChiTietShopRaSoatVm
{
    public ShopCanRaSoatItemVm Shop { get; init; } = new();
    public DanhSachDanhGiaTheoShopVm DanhGias { get; init; } = new();
    public IReadOnlyList<DonHangGanDayVm> DonHangsGanDay { get; init; } = Array.Empty<DonHangGanDayVm>();
}
