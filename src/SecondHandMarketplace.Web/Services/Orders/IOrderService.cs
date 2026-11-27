using SecondHandMarketplace.Web.Models.ViewModels.Orders;

namespace SecondHandMarketplace.Web.Services.Orders;

/// <summary>Nghiệp vụ Đặt hàng, Thanh toán và Đơn hàng của người mua và người bán.</summary>
public interface IOrderService
{
    Task<CheckoutViewModel> ChuanBiCheckoutTuGioAsync(int maNguoiDung);
    Task<CheckoutViewModel?> ChuanBiCheckoutMuaNgayAsync(int maNguoiDung, int maSanPham, int soLuong = 1);
    Task<DatHangResultViewModel> DatHangTuGioAsync(int maNguoiDung, CheckoutViewModel model);
    Task<DatHangResultViewModel> MuaNgayAsync(int maNguoiDung, int maSanPham, CheckoutViewModel model);
    Task<List<DonHangItemViewModel>> LayDanhSachDonMuaAsync(int maNguoiDung, string? trangThai);
    Task<DonHangChiTietViewModel?> LayChiTietDonMuaAsync(int maNguoiDung, int maDonHang);
    Task<(bool ThanhCong, string? ThongBao)> HuyDonMuaAsync(int maNguoiDung, int maDonHang, string lyDo);
    Task<(bool ThanhCong, string? ThongBao)> XacNhanDaNhanHangAsync(int maNguoiDung, int maDonHang);
    Task<List<DonHangItemViewModel>> LayDanhSachDonTheoMaAsync(int maNguoiDung, List<string> danhSachMaDon);
    Task<(bool ThanhCong, string? ThongBao)> XacNhanThanhToanChuyenKhoanAsync(int maNguoiDung, int maDonHang);
    Task<(bool ThanhCong, string? ThongBao)> XacNhanThanhToanTatCaChuyenKhoanAsync(int maNguoiDung, IEnumerable<int> maDonHangs);

    /// <summary>
    /// Người bán xác nhận đã nhận tiền thanh toán (thủ công qua VietQR hoặc tiền mặt).
    /// Tuân thủ quy định Task D5.
    /// </summary>
    Task<(bool ThanhCong, string? ThongBao)> XacNhanDaNhanTienShopAsync(int chuShopId, int maDonHang);

    /// <summary>
    /// Task E-01 (Demo): Mô phỏng thanh toán chuyển khoản VietQR thành công ngay trên màn hình người mua (chỉ dùng trong Development).
    /// Tự động cập nhật đơn hàng sang CONFIRMED và thanh toán sang PAID.
    /// </summary>
    Task<(bool ThanhCong, string? ThongBao)> MoPhongThanhToanDemoAsync(int maNguoiDung, int maDonHang);

    /// <summary>
    /// Task E-01 (Demo): Mô phỏng thanh toán tất cả các đơn hàng chuyển khoản VietQR thành công.
    /// </summary>
    Task<(bool ThanhCong, string? ThongBao)> MoPhongThanhToanTatCaDemoAsync(int maNguoiDung, IEnumerable<int> maDonHangs);
}
