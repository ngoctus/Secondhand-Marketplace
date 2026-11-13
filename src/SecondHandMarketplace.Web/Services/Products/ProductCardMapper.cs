using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Reviews;

namespace SecondHandMarketplace.Web.Services.Products;

/// <summary>Một dòng sản phẩm vừa truy vấn xong, chưa có điểm sao.</summary>
public sealed record ProductCardSource(
    int MaSanPham,
    string Ten,
    decimal? Gia,
    string? MoTa,
    string TrangThai,
    int MaCuaHang,
    string TenShop,
    string? Logo,
    string? KhuVuc,
    string? DuongDanAnh,
    string? ConditionName,
    string? CategoryName);

/// <summary>
/// Dựng thẻ sản phẩm từ dữ liệu thô. Điểm sao của mọi shop trong trang lấy một lần qua
/// IReviewService (nguồn duy nhất là view vw_DiemCuaHang của C), không truy vấn theo từng thẻ.
/// </summary>
public static class ProductCardMapper
{
    public static async Task<List<ProductCardVm>> TaoDanhSachAsync(
        IReadOnlyList<ProductCardSource> sources, IReviewService danhGia, CancellationToken ct = default)
    {
        var diem = await danhGia.LayDiemNhieuShopAsync(sources.Select(s => s.MaCuaHang).Distinct(), ct);

        return sources.Select(s =>
        {
            diem.TryGetValue(s.MaCuaHang, out var shop);
            // View trả DiemTrungBinh = 0 khi shop chưa có đánh giá VISIBLE nào: chỉ hiện sao khi thực sự có đánh giá.
            var coDanhGia = shop is not null && shop.SoDanhGia > 0;
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
                DiemTrungBinh = coDanhGia ? shop!.DiemTrungBinh : null,
                SoDanhGia = shop?.SoDanhGia ?? 0,
                KhuVuc = s.KhuVuc
            };
        }).ToList();
    }
}
