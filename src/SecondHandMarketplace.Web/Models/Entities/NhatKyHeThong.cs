namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng NhatKyHeThong: ai đã làm gì, với đối tượng nào, lúc nào.</summary>
public class NhatKyHeThong
{
    public long MaNhatKyHeThong { get; set; }
    public int? MaNguoiDung { get; set; }
    public string HanhDong { get; set; } = string.Empty;      // VD: "KHOA_NGUOI_DUNG", "DUYET_SAN_PHAM"
    public string LoaiDoiTuong { get; set; } = string.Empty;  // VD: "NguoiDung", "SanPham"
    public int? DoiTuongId { get; set; }
    public string KetQua { get; set; } = "SUCCESS";           // SUCCESS | FAILED | DENIED
    public string? LyDo { get; set; }
    public string? ChiTiet { get; set; }
    public DateTime ThoiGian { get; set; }
}