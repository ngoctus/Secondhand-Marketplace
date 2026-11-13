using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Products;

/// <summary>Một dòng sản phẩm vừa truy vấn xong, chưa có điểm sao và khu vực.</summary>
public sealed record ProductCardSource(
    int MaSanPham,
    string Ten,
    decimal? Gia,
    string? MoTa,
    string TrangThai,
    int MaCuaHang,
    string TenShop,
    string? Logo,
    string? DuongDanAnh,
    string? ConditionName,
    string? CategoryName);

/// <summary>
/// Dựng thẻ sản phẩm từ dữ liệu thô. Điểm sao và khu vực lấy một lần cho cả trang
/// (không truy vấn theo từng thẻ), nên mọi trang dùng _ProductCard đều hiển thị đủ 6 thành phần.
/// </summary>
public static class ProductCardMapper
{
    public static async Task<List<ProductCardVm>> TaoDanhSachAsync(
        IReadOnlyList<ProductCardSource> sources, IShopDisplayReader hienThiShop, CancellationToken ct = default)
    {
        var hienThi = await hienThiShop.LayTheoCuaHangAsync(sources.Select(s => s.MaCuaHang), ct);

        return sources.Select(s =>
        {
            hienThi.TryGetValue(s.MaCuaHang, out var shop);
            return new ProductCardVm
            {
                Id = s.MaSanPham,
                Name = s.Ten,
                Price = s.Gia ?? 0m,
                ImageUrl = s.DuongDanAnh,
                ShopId = s.MaCuaHang,
                ShopName = s.TenShop,
                ShopLogoUrl = s.Logo,
                ConditionName = s.ConditionName,
                CategoryName = s.CategoryName,
                MoTaNgan = ProductCardVm.TaoMoTaNgan(s.MoTa),
                Status = s.TrangThai,
                DiemTrungBinh = shop?.DiemTrungBinh,
                SoDanhGia = shop?.SoDanhGia ?? 0,
                KhuVuc = shop?.KhuVuc
            };
        }).ToList();
    }
}
