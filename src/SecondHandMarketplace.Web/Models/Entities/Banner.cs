namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng Banner (B5) — banner trang chủ, Admin quản lý.</summary>
public class Banner
{
    public int MaBanner { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string DuongDanAnh { get; set; } = string.Empty;
    public string? LienKet { get; set; }
    public int ThuTuHienThi { get; set; } = 1;
    public bool DangHienThi { get; set; } = true;
    public DateTime? BatDauTu { get; set; }
    public DateTime? KetThucLuc { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
}
