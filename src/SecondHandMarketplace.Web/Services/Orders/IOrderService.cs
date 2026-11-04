using SecondHandMarketplace.Web.Models.ViewModels.Orders;

namespace SecondHandMarketplace.Web.Services.Orders;

/// <summary>Nghiệp vụ Đặt hàng, Thanh toán và Đơn hàng của người mua.</summary>
public interface IOrderService
{
    Task<CheckoutViewModel> ChuanBiCheckoutTuGioAsync(int maNguoiDung);
    Task<CheckoutViewModel?> ChuanBiCheckoutMuaNgayAsync(int maNguoiDung, int maSanPham);
    Task<DatHangResultViewModel> DatHangTuGioAsync(int maNguoiDung, CheckoutViewModel model);
    Task<DatHangResultViewModel> MuaNgayAsync(int maNguoiDung, int maSanPham, CheckoutViewModel model);
    Task<List<DonHangItemViewModel>> LayDanhSachDonMuaAsync(int maNguoiDung, string? trangThai);
    Task<DonHangChiTietViewModel?> LayChiTietDonMuaAsync(int maNguoiDung, int maDonHang);
    Task<(bool ThanhCong, string? ThongBao)> HuyDonMuaAsync(int maNguoiDung, int maDonHang, string lyDo);
    Task<(bool ThanhCong, string? ThongBao)> XacNhanDaNhanHangAsync(int maNguoiDung, int maDonHang);
}
