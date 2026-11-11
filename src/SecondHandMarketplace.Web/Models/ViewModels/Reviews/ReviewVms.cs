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
