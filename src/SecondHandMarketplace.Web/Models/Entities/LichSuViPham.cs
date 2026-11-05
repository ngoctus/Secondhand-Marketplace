namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng LichSuViPham (C2): mỗi dòng là một vi phạm kiểm duyệt được tính vào bộ đếm của người bán.
/// Bộ đếm = số dòng theo MaNguoiBan. UNIQUE theo MaLuotKiemDuyet nên một lượt tối đa một vi phạm.
/// NguonGhiNhan: AUTO_HIGH (hệ thống, MaQuanTriVien null) | ADMIN_XAC_NHAN_LOW (Admin xác nhận LOW).
/// </summary>
public class LichSuViPham
{
    public int MaViPham { get; set; }
    public int MaNguoiBan { get; set; }
    public int MaSanPham { get; set; }
    public int MaLuotKiemDuyet { get; set; }
    public string MucDo { get; set; } = string.Empty;
    public string NguonGhiNhan { get; set; } = string.Empty;
    public string ChiTietTuKhoa { get; set; } = "[]";   // JSON
    public int? MaQuanTriVien { get; set; }
    public DateTime ThoiGian { get; set; }
}
