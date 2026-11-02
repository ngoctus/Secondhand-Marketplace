namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Bảng PhanTichAI (mỗi lượt kiểm duyệt có tối đa một phân tích).
/// TrangThai: QUEUED, PROCESSING, DONE, FAILED. MucRuiRo: LOW, MEDIUM, HIGH. DeXuat: APPROVE, REJECT, NEEDS_REVIEW.
/// </summary>
public class PhanTichAI
{
    public int MaPhanTichAI { get; set; }
    public int MaLuotKiemDuyet { get; set; }
    public string TrangThai { get; set; } = "QUEUED";
    public int SoLanThu { get; set; }
    public string? MucRuiRo { get; set; }
    public string? DauHieu { get; set; }         // JSON
    public string? DeXuat { get; set; }
    public string? GiaiThich { get; set; }
    public string? MaBamDauVao { get; set; }
    public string? PhienBanMoHinh { get; set; }
    public int? ThoiGianXuLyMs { get; set; }
    public string? LoiXuLy { get; set; }
    public bool? QuanTriVienLamTheo { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime? ThoiGianHoanThanh { get; set; }

    public LuotKiemDuyet LuotKiemDuyet { get; set; } = null!;
}
