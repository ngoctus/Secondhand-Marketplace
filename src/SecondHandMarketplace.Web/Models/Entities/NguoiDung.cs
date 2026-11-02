namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng NguoiDung.</summary>
public class NguoiDung
{
    public int MaNguoiDung { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string MatKhauBam { get; set; } = string.Empty;
    public string VaiTro { get; set; } = "USER";
    public string TrangThai { get; set; } = "ACTIVE";
    public string? LyDoKhoa { get; set; }
    public DateTime? ThoiGianKhoa { get; set; }
    public string? AnhDaiDien { get; set; }
    public string? DiaChiMacDinh { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
}