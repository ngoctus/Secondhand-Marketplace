namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Sổ địa chỉ (mặc định và địa chỉ dự phòng) của người dùng để chọn nhanh khi đặt hàng.</summary>
public class DiaChiNguoiDung
{
    public int MaDiaChi { get; set; }
    public int MaNguoiDung { get; set; }
    public string? NhanDiaChi { get; set; }
    public string TenNguoiNhan { get; set; } = string.Empty;
    public string SoDienThoaiNhan { get; set; } = string.Empty;
    public int? MaTinhThanh { get; set; }
    public int? MaPhuongXa { get; set; }
    public string? TenTinhThanh { get; set; }
    public string? TenPhuongXa { get; set; }
    public string DiaChiChiTiet { get; set; } = string.Empty;
    public bool LaMacDinh { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public NguoiDung NguoiDung { get; set; } = null!;

    public string DiaChiDayDu()
    {
        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(DiaChiChiTiet)) parts.Add(DiaChiChiTiet.Trim());
        if (!string.IsNullOrWhiteSpace(TenPhuongXa)) parts.Add(TenPhuongXa.Trim());
        if (!string.IsNullOrWhiteSpace(TenTinhThanh)) parts.Add(TenTinhThanh.Trim());
        return string.Join(", ", parts);
    }
}
