namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng DanhMuc.</summary>
public class DanhMuc
{
    public int MaDanhMuc { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int ThuTuHienThi { get; set; }
    public bool DangHienThi { get; set; } = true;
}