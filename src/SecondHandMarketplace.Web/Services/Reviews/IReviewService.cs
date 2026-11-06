namespace SecondHandMarketplace.Web.Services.Reviews;

using SecondHandMarketplace.Web.Models.ViewModels.Reviews;

/// <summary>Đánh giá người bán sau khi đơn hoàn tất</summary>
public interface IReviewService
{
    Task<UserReviewListViewModel> LayDanhSachChoDanhGiaAsync(int maNguoiMua);
    Task<(bool ThanhCong, string ThongBao)> TaoDanhGiaAsync(int maNguoiMua, TaoDanhGiaViewModel model);
    Task<AdminReviewListViewModel> LayDanhSachAdminAsync(int page);
    Task<(bool ThanhCong, string ThongBao)> DuyetAsync(int maDanhGia, int adminId, string trangThai, string? ghiChu);
}
