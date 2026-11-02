namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng CuaHang (mỗi người dùng tối đa 1 cửa hàng).</summary>
public class CuaHang
{
    public int MaCuaHang { get; set; }
    public int MaNguoiDung { get; set; }
    public string TenShop { get; set; } = string.Empty;
    public string SoDienThoaiLienHe { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? Logo { get; set; }
    public string TrangThai { get; set; } = "ACTIVE";
    public string? LyDoKhoa { get; set; }
    public DateTime? ThoiGianKhoa { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }

    public NguoiDung NguoiDung { get; set; } = null!;
}