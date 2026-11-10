namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng DanhGia (mỗi đơn hàng COMPLETED có tối đa một đánh giá của người mua dành cho shop).
/// TrangThai: VISIBLE, HIDDEN. SoSao: 1 đến 5.
/// Điểm trung bình của shop đọc từ vw_DiemCuaHang, không tự tính lại.
/// </summary>
public class DanhGia
{
    public int MaDanhGia { get; set; }
    public int MaDonHang { get; set; }
    public int MaNguoiMua { get; set; }
    public int MaCuaHang { get; set; }
    public byte SoSao { get; set; }
    public string? NoiDung { get; set; }
    public string TrangThai { get; set; } = "VISIBLE";
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public DonHang DonHang { get; set; } = null!;
    /// <summary>Người mua đã viết đánh giá.</summary>
    public NguoiDung NguoiMua { get; set; } = null!;
    public CuaHang CuaHang { get; set; } = null!;
}
