using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Ai;
using SecondHandMarketplace.Web.Services.Areas;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Products;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// Kiểm thử 3 công cụ AI (search_products, recommend_products, draft_product_description)
/// bằng fake cho IProductService / ICategoryService / IKhuVucService, không cần database.
/// </summary>
public class AiToolExecutorTests
{
    private static AiToolExecutor NewExecutor(
        FakeAiProductService products,
        FakeAiCategoryService? categories = null,
        FakeAiKhuVucService? khuVuc = null) =>
        new(products, categories ?? new FakeAiCategoryService(), khuVuc ?? new FakeAiKhuVucService());

    [Fact]
    public async Task SearchProducts_WithLocation_AppliesProvinceAndWardToFilter()
    {
        var products = new FakeAiProductService();
        var executor = NewExecutor(products);

        using var args = JsonDocument.Parse("""
            {
              "keyword": "điện thoại",
              "province": "TP.HCM",
              "ward": "Tân Định"
            }
            """);

        var result = await executor.ExecuteAsync(
            AppRoles.User, AiToolExecutor.SearchProducts, args.RootElement, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.NotNull(products.LastFilter);
        Assert.Equal("điện thoại", products.LastFilter!.Keyword);
        Assert.Equal(79, products.LastFilter.MaTinhThanh);      // TP.HCM viết tắt -> Thành phố Hồ Chí Minh
        Assert.Equal(26737, products.LastFilter.MaPhuongXa);    // "Tân Định" khớp "Phường Tân Định"
    }

    [Fact]
    public async Task RecommendProducts_ValidArgs_ReturnsCandidatesWithDescription()
    {
        var products = new FakeAiProductService
        {
            SearchResult = new PagedResult<ProductCardVm>(
                new[]
                {
                    new ProductCardVm
                    {
                        Id = 12,
                        Name = "iPhone 12 128GB",
                        Price = 6_500_000m,
                        CategoryName = "Điện thoại",
                        ConditionName = "Như mới",
                        ShopName = "Shop Công Nghệ"
                    }
                },
                totalItems: 1,
                page: 1,
                pageSize: 12)
        };
        products.Details[12] = new ProductDetailVm
        {
            Id = 12,
            Name = "iPhone 12 128GB",
            Price = 6_500_000m,
            Description = "Máy còn bảo hành 3 tháng, kèm ốp lưng và củ sạc."
        };

        var executor = NewExecutor(products);

        using var args = JsonDocument.Parse("""
            { "keyword": "iphone", "max_price": 7000000, "province": "Hà Nội" }
            """);

        var result = await executor.ExecuteAsync(
            AppRoles.User, AiToolExecutor.RecommendProducts, args.RootElement, CancellationToken.None);

        Assert.False(result.IsError);
        Assert.Contains(12, products.DetailCalls);

        using var json = JsonDocument.Parse(result.Json);
        var candidates = json.RootElement.GetProperty("candidates");
        Assert.Equal(1, candidates.GetArrayLength());

        var candidate = candidates[0];
        Assert.Equal("iPhone 12 128GB", candidate.GetProperty("name").GetString());
        Assert.Contains("bảo hành", candidate.GetProperty("description").GetString());
        Assert.Equal("/Products/Details/12", candidate.GetProperty("url").GetString());
    }

    [Fact]
    public async Task DraftDescription_MissingName_ReturnsError()
    {
        var executor = NewExecutor(new FakeAiProductService());

        using var args = JsonDocument.Parse("""
            { "category": "Điện thoại", "condition": "Tốt" }
            """);

        var result = await executor.ExecuteAsync(
            AppRoles.User, AiToolExecutor.DraftProductDescription, args.RootElement, CancellationToken.None);

        Assert.True(result.IsError);
        using var json = JsonDocument.Parse(result.Json);
        Assert.Contains("Thiếu tên sản phẩm", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task DraftDescription_ValidInputs_ReturnsProvidedAndInstructions()
    {
        var executor = NewExecutor(new FakeAiProductService());

        using var args = JsonDocument.Parse("""
            {
              "name": "Bàn làm việc gỗ",
              "category": "Nội thất",
              "condition": "Tốt",
              "price": 850000,
              "details": "Kích thước 120x60cm, có 2 ngăn kéo."
            }
            """);

        var result = await executor.ExecuteAsync(
            AppRoles.User, AiToolExecutor.DraftProductDescription, args.RootElement, CancellationToken.None);

        Assert.False(result.IsError);

        using var json = JsonDocument.Parse(result.Json);
        var provided = json.RootElement.GetProperty("provided");

        Assert.Equal("Bàn làm việc gỗ", provided.GetProperty("name").GetString());
        Assert.Equal("Nội thất", provided.GetProperty("category").GetString());
        Assert.Equal("Tốt", provided.GetProperty("condition").GetString());
        Assert.Equal(850_000m, provided.GetProperty("priceVnd").GetDecimal());
        Assert.Equal("Kích thước 120x60cm, có 2 ngăn kéo.", provided.GetProperty("details").GetString());

        // Không bịa thêm dữ kiện: chỉ đúng 5 khóa người dùng đã cung cấp, không tự thêm mô tả hiện có.
        Assert.Equal(5, provided.EnumerateObject().Count());
        Assert.False(provided.TryGetProperty("currentDescription", out _));

        var instructions = json.RootElement.GetProperty("instructions");
        Assert.True(instructions.GetArrayLength() > 0);
        Assert.Contains(instructions.EnumerateArray(), i => i.GetString()!.Contains("Không thêm"));
    }

    // ------------------------------------------------------------------ Fake services

    private sealed class FakeAiProductService : IProductService
    {
        public ProductSearchVm? LastFilter { get; private set; }
        public List<int> DetailCalls { get; } = new();
        public Dictionary<int, ProductDetailVm> Details { get; } = new();
        public PagedResult<ProductCardVm> SearchResult { get; set; } = PagedResult<ProductCardVm>.Empty();

        public Task<PagedResult<ProductCardVm>> SearchAsync(ProductSearchVm filter)
        {
            LastFilter = filter;
            return Task.FromResult(SearchResult);
        }

        public Task<ProductDetailVm?> GetDetailAsync(int id)
        {
            DetailCalls.Add(id);
            return Task.FromResult(Details.TryGetValue(id, out var detail) ? detail : null);
        }

        public Task<PagedResult<MyProductVm>> GetMyAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ProductFormVm?> GetFormAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Success, string? Error, int Id)> CreateAsync(
            int maNguoiDung, ProductFormVm form, IReadOnlyList<ProductImageInput> anh,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(bool Success, string? Error)> UpdateAsync(
            int maNguoiDung, int maSanPham, ProductFormVm form, IReadOnlyList<ProductImageInput> anhMoi,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(bool Success, string? Error)> HideAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Success, string? Error)> GuiDuyetAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Success, string? Error)> DeleteAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Success, string? Error)> DatAnhChinhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Success, string? Error)> XoaAnhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAiCategoryService : ICategoryService
    {
        public Task<IReadOnlyList<DanhMuc>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DanhMuc>>(new[]
            {
                new DanhMuc { MaDanhMuc = 1, Ten = "Điện thoại", DangHienThi = true },
                new DanhMuc { MaDanhMuc = 2, Ten = "Nội thất", DangHienThi = true }
            });

        public Task<IReadOnlyList<TinhTrangSanPham>> GetConditionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TinhTrangSanPham>>(new[]
            {
                new TinhTrangSanPham { MaTinhTrang = 1, Ma = "LIKE_NEW", Ten = "Như mới", DangSuDung = true },
                new TinhTrangSanPham { MaTinhTrang = 2, Ma = "GOOD", Ten = "Tốt", DangSuDung = true }
            });

        public Task<(bool Success, string? Error, int Id)> SaveCategoryAsync(int? id, string name, int order, bool visible, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Success, string? Error)> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Success, string? Error, int Id)> SaveConditionAsync(int? id, string code, string name, string? description, int order, bool active, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Success, string? Error)> DeleteConditionAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeAiKhuVucService : IKhuVucService
    {
        private readonly List<TinhThanhItem> _provinces = new()
        {
            new TinhThanhItem(79, "Thành phố Hồ Chí Minh"),
            new TinhThanhItem(1, "Thành phố Hà Nội")
        };

        private readonly Dictionary<int, List<PhuongXaItem>> _wards = new()
        {
            [79] = new List<PhuongXaItem>
            {
                new PhuongXaItem(26734, "Phường Sài Gòn"),
                new PhuongXaItem(26737, "Phường Tân Định")
            },
            [1] = new List<PhuongXaItem>
            {
                new PhuongXaItem(4, "Phường Ba Đình")
            }
        };

        public Task<IReadOnlyList<TinhThanhItem>> GetTinhThanhAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TinhThanhItem>>(_provinces);

        public Task<IReadOnlyList<PhuongXaItem>> GetPhuongXaTheoTinhAsync(int maTinh, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PhuongXaItem>>(
                _wards.TryGetValue(maTinh, out var wards) ? wards : Array.Empty<PhuongXaItem>());

        public Task<bool> PhuongXaThuocTinhAsync(int maTinh, int maXa, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(int? MaTinh, int? MaXa)> GetKhuVucDaChonAsync(HttpContext httpContext, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> LuuKhuVucDaChonAsync(HttpContext httpContext, int? maTinh, int? maXa, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
