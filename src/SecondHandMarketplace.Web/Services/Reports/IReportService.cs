using SecondHandMarketplace.Web.Models.ViewModels.Reports;

namespace SecondHandMarketplace.Web.Services.Reports;

/// <summary>Nghiệp vụ Báo cáo vi phạm và Xử lý báo cáo.</summary>
public interface IReportService
{
    Task<(bool ThanhCong, string? ThongBao)> TaoBaoCaoAsync(int maNguoiBaoCao, TaoBaoCaoViewModel vm);
    Task<List<BaoCaoItemViewModel>> LayLichSuBaoCaoCuaToiAsync(int maNguoiBaoCao);
    Task<AdminReportListViewModel> LayDanhSachBaoCaoAdminAsync(string? trangThai, string? loaiDoiTuong);
    Task<(bool ThanhCong, string? ThongBao)> XuLyBaoCaoAdminAsync(int maBaoCao, int adminId, string hanhDong, string? ghiChu);
}
