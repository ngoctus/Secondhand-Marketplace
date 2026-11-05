using SecondHandMarketplace.Web.Models.ViewModels.Disputes;

namespace SecondHandMarketplace.Web.Services.Disputes;

/// <summary>Nghiệp vụ Tranh chấp và Khiếu nại đơn hàng.</summary>
public interface IDisputeService
{
    Task<(bool ThanhCong, string? ThongBao)> MoTranhChapAsync(int maNguoiMua, MoTranhChapViewModel vm);
    Task<(bool ThanhCong, string? ThongBao)> NguoiBanPhanHoiAsync(int maChuShop, int maTranhChap, string phanHoi);
    Task<TranhChapDetailViewModel?> LayChiTietTranhChapAsync(int maTranhChap);
    Task<List<TranhChapDetailViewModel>> LayDanhSachTranhChapNguoiDungAsync(int maNguoiDung);
    Task<AdminDisputeListViewModel> LayDanhSachTranhChapAdminAsync(string? trangThai, int page = 1, int pageSize = 10);
    Task<(bool ThanhCong, string? ThongBao)> XuLyTranhChapAdminAsync(int maTranhChap, int adminId, string phanQuyet, string? ghiChu);
}
