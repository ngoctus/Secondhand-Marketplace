namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng TuKhoaCam (CumTuChuanHoa là dạng đã chuẩn hóa dùng để so khớp).</summary>
public class TuKhoaCam
{
    public int MaTuKhoaCam { get; set; }
    public string CumTu { get; set; } = string.Empty;
    public string CumTuChuanHoa { get; set; } = string.Empty;
    public string? Nhom { get; set; }
    /// <summary>Mức độ: LOW (chờ Admin xem xét) hoặc HIGH (tự động từ chối). CHECK trong database/14_kiemduyet_tukhoa_c2.sql.</summary>
    public string MucDo { get; set; } = "LOW";
    public string? GhiChu { get; set; }
    public bool DangApDung { get; set; } = true;
    public DateTime NgayTao { get; set; }
}
