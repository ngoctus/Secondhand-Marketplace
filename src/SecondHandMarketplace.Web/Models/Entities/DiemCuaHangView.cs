namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>
/// Dòng đọc từ view dbo.vw_DiemCuaHang (database/11_danhgia.sql).
/// View là nguồn duy nhất cho điểm sao shop: chỉ tính đánh giá VISIBLE,
/// SoDonThanhCong chỉ tính đơn COMPLETED. Keyless vì view không có khóa.
/// </summary>
public sealed class DiemCuaHangView
{
    public int MaCuaHang { get; set; }
    public decimal DiemTrungBinh { get; set; }
    public int SoDanhGia { get; set; }
    public int SoDonThanhCong { get; set; }
}
