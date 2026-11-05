using SecondHandMarketplace.Web.Models.ViewModels.Cart;

namespace SecondHandMarketplace.Web.Services.Carts;

/// <summary>Giao diện nghiệp vụ Giỏ hàng của người mua.</summary>
public interface ICartService
{
    Task<CartIndexViewModel> LayGioHangAsync(int maNguoiDung);
    Task<(bool ThanhCong, string? ThongBao)> ThemVaoGioAsync(int maNguoiDung, int maSanPham);
    Task<bool> CapNhatTrangThaiChonAsync(int maNguoiDung, int maGioHang, bool daChon);
    Task<bool> CapNhatChonTatCaTheoShopAsync(int maNguoiDung, int maCuaHang, bool daChon);
    Task<bool> XoaKhoiGioAsync(int maNguoiDung, int maGioHang);
    Task<int> DemSoLuongMonAsync(int maNguoiDung);
}
