using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Ai;
using SecondHandMarketplace.Web.Services.Areas;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.Products;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>Kiểm thử C-03: đồng bộ ngôn ngữ VI|EN cho Chatbox AI, auto-failover FallbackApiKey và AI tham khảo giá.</summary>
public class AiChatServiceTests
{
    private sealed class QueueHttpHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responders) : HttpMessageHandler
    {
        private int _index;
        public List<(string? Auth, string Body)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var auth = request.Headers.TryGetValues("Authorization", out var vals) ? vals.FirstOrDefault() : null;
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((auth, body));

            var responder = responders[Math.Min(_index++, responders.Length - 1)];
            return responder(request);
        }
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static HttpResponseMessage OpenAiOk(string content)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content } } }
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage HttpError(HttpStatusCode status, string message = "error") =>
        new(status)
        {
            Content = new StringContent($"{{\"error\":{{\"status\":\"ERR\",\"message\":\"{message}\"}}}}", Encoding.UTF8, "application/json")
        };

    private static AiChatService CreateService(
        Dictionary<string, string?> configValues,
        QueueHttpHandler handler,
        StubProductService? products = null,
        StubCategoryService? categories = null,
        IAiSettingsService? settings = null,
        IKeywordScanner? keywordScanner = null)
    {
        return new AiChatService(
            products ?? new StubProductService(),
            categories ?? new StubCategoryService(),
            new StubKhuVucService(),
            BuildConfig(configValues),
            NullLogger<AiChatService>.Instance,
            new HttpClient(handler),
            settings,
            keywordScanner);
    }

    [Fact]
    public void AiOptions_GetChatApiKeys_ReturnsPrimaryThenUniqueFallback()
    {
        var cfg = BuildConfig(new()
        {
            ["Ai:ApiKey"] = "AIza-primary",
            ["Ai:FallbackApiKey"] = "AIza-backup"
        });
        Assert.Equal(new[] { "AIza-primary", "AIza-backup" }, AiOptions.FromConfiguration(cfg).GetChatApiKeys());

        var dupCfg = BuildConfig(new()
        {
            ["Ai:ApiKey"] = "AIza-same",
            ["Ai:FallbackApiKey"] = "AIza-same"
        });
        Assert.Equal(new[] { "AIza-same" }, AiOptions.FromConfiguration(dupCfg).GetChatApiKeys());
    }

    [Fact]
    public async Task AskAsync_EnglishLanguage_AppendsEnglishDirectiveAndLocalizesErrors()
    {
        var handler = new QueueHttpHandler(_ => OpenAiOk("Here are the matching items."));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler);

        var ok = await svc.AskAsync("10", AppRoles.User, "Find a laptop", "en", CancellationToken.None);
        Assert.True(ok.IsSuccess);
        Assert.Equal("Here are the matching items.", ok.Reply);
        Assert.Single(handler.Requests);
        using var reqDoc = JsonDocument.Parse(handler.Requests[0].Body);
        var systemContent = reqDoc.RootElement.GetProperty("messages")[0].GetProperty("content").GetString();
        Assert.Contains("The user's interface language is English", systemContent);

        var emptyErr = await svc.AskAsync("10", AppRoles.User, "   ", "en", CancellationToken.None);
        Assert.Equal(AiChatStatus.InvalidInput, emptyErr.Status);
        Assert.Contains("Please enter a message", emptyErr.Error);
    }

    [Fact]
    public async Task AskAsync_PrimaryKeyReturns429_FailoversToFallbackApiKey()
    {
        var handler = new QueueHttpHandler(
            _ => HttpError(HttpStatusCode.TooManyRequests, "quota exceeded"),
            _ => OpenAiOk("Xin chào từ khóa dự phòng!"));

        var svc = CreateService(new()
        {
            ["Ai:ApiKey"] = "AIza-primary",
            ["Ai:FallbackApiKey"] = "AIza-backup"
        }, handler);

        var res = await svc.AskAsync("10", AppRoles.User, "Tìm bàn học", "vi", CancellationToken.None);

        Assert.True(res.IsSuccess);
        Assert.Equal("Xin chào từ khóa dự phòng!", res.Reply);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("Bearer AIza-primary", handler.Requests[0].Auth);
        Assert.Equal("Bearer AIza-backup", handler.Requests[1].Auth);
    }

    [Fact]
    public async Task SuggestPriceAsync_WithKeywordMatchesAndAi_ReturnsGroundedStatsAndSuggestedPrice()
    {
        var products = new StubProductService(filter =>
        {
            if (!string.IsNullOrEmpty(filter.Keyword))
            {
                return new PagedResult<ProductCardVm>(
                    new[]
                    {
                        new ProductCardVm { Id = 1, Name = "iPhone 12 64GB", Price = 6_000_000m, ConditionName = "Tốt" },
                        new ProductCardVm { Id = 2, Name = "iPhone 12 128GB", Price = 6_500_000m, ConditionName = "Như mới" },
                        new ProductCardVm { Id = 3, Name = "iPhone 12 256GB", Price = 7_500_000m, ConditionName = "Như mới" }
                    },
                    totalItems: 3, page: 1, pageSize: 20);
            }
            return PagedResult<ProductCardVm>.Empty();
        });

        var llmJson = """
            {
              "recommended_price_vnd": 6800000,
              "explanation": "Based on 3 iPhone 12 listings (6M–7.5M VND), 6,800,000 VND fits Like New condition well."
            }
            """;
        var handler = new QueueHttpHandler(_ => OpenAiOk(llmJson));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler, products);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Cần bán iPhone 12 128GB màu xanh",
            CategoryId: 1,
            ConditionId: 1,
            Language: "en"));

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Data);
        Assert.Equal("KEYWORD", res.Data!.MatchScope);
        Assert.Equal(3, res.Data.SampleCount);
        Assert.Equal(6_000_000m, res.Data.MinPriceVnd);
        Assert.Equal(6_500_000m, res.Data.MedianPriceVnd);
        Assert.Equal(7_500_000m, res.Data.MaxPriceVnd);
        Assert.Equal(6_800_000m, res.Data.RecommendedPriceVnd);
        Assert.True(res.Data.UsedAi);
        Assert.Equal(3, res.Data.Samples.Count);
        Assert.All(res.Data.Samples, s => Assert.StartsWith("/Products/Details/", s.Url));
    }

    [Fact]
    public async Task AskAsync_ArrayError503OnPrimaryModel_FallsBackToStableModelWithoutCrashing()
    {
        var array503 = """[{"error":{"code":503,"message":"This model is currently experiencing high demand.","status":"UNAVAILABLE"}}]""";
        var handler = new QueueHttpHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(array503, Encoding.UTF8, "application/json")
            },
            _ => OpenAiOk("Hello from stable fallback model!"));

        var svc = CreateService(new()
        {
            ["Ai:ApiKey"] = "AIza-primary",
            ["Ai:DefaultModel"] = "gemini-3.1-flash-lite"
        }, handler);

        var res = await svc.AskAsync("10", AppRoles.User, "Find a used phone", "en", CancellationToken.None);

        Assert.True(res.IsSuccess);
        Assert.Equal("Hello from stable fallback model!", res.Reply);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("\"model\":\"gemini-3.1-flash-lite\"", handler.Requests[0].Body);
        Assert.Contains("\"model\":\"gemini-3.5-flash-lite\"", handler.Requests[1].Body);
    }

    [Fact]
    public async Task SuggestPriceAsync_LowSeverityHangCamKeyword_AlsoBlocksImmediately()
    {
        var handler = new QueueHttpHandler(_ => OpenAiOk("""{"recommended_price_vnd": 500000, "explanation": "Should not run"}"""));
        var scanner = new StubKeywordScanner(new KeywordMatch(1, "ma túy", "HANG_CAM", ModerationPolicy.KeywordLow, "Ten", 0));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler, keywordScanner: scanner);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Ma túy đá",
            CategoryId: 2,
            ConditionId: 1,
            Language: "vi"));

        Assert.False(res.IsSuccess);
        Assert.Equal(AiChatStatus.InvalidInput, res.Status);
        Assert.Contains("bị cấm", res.Error);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SuggestPriceAsync_MultiWordProduct_StopsAtSpecificPhraseAndDoesNotPullGenericSingleWord()
    {
        var seenKeywords = new List<string?>();
        var products = new StubProductService(filter =>
        {
            seenKeywords.Add(filter.Keyword);
            if (filter.Keyword is "noi chien khong dau" or "noi chien")
            {
                return new PagedResult<ProductCardVm>(
                    new[] { new ProductCardVm { Id = 31, Name = "Nồi chiên không dầu 5L", Price = 700_000m, ConditionName = "Tốt" } },
                    totalItems: 1, page: 1, pageSize: 20);
            }
            if (filter.Keyword == "noi")
            {
                return new PagedResult<ProductCardVm>(
                    new[] { new ProductCardVm { Id = 32, Name = "Bộ nồi inox 3 món", Price = 220_000m, ConditionName = "Tốt" } },
                    totalItems: 1, page: 1, pageSize: 20);
            }
            return PagedResult<ProductCardVm>.Empty();
        });

        var handler = new QueueHttpHandler(_ => OpenAiOk("""{"recommended_price_vnd": 750000, "explanation": "Khớp đúng nồi chiên."}"""));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler, products);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Nồi chiên không dầu Philips HD9252",
            CategoryId: 1,
            ConditionId: 1,
            Language: "vi"));

        Assert.True(res.IsSuccess);
        Assert.Equal(1, res.Data!.SampleCount);
        Assert.Single(res.Data.Samples);
        Assert.Equal("Nồi chiên không dầu 5L", res.Data.Samples[0].Name);
        Assert.DoesNotContain("noi", seenKeywords);
    }

    [Fact]
    public async Task SuggestPriceAsync_WrongCategorySelected_FindsExactKeywordMatchAcrossMarketplace()
    {
        var products = new StubProductService(filter =>
        {
            // Giả lập sản phẩm thật nằm ở danh mục khác (CategoryId == null khi nới toàn sàn mới thấy)
            if (filter.CategoryId is null && filter.Keyword == "noi chien khong dau")
            {
                return new PagedResult<ProductCardVm>(
                    new[] { new ProductCardVm { Id = 31, Name = "Nồi chiên không dầu 5L", Price = 700_000m, ConditionName = "Tốt" } },
                    totalItems: 1, page: 1, pageSize: 20);
            }
            return PagedResult<ProductCardVm>.Empty();
        });

        var handler = new QueueHttpHandler(_ => OpenAiOk("""{"recommended_price_vnd": 750000, "explanation": "Đã tìm thấy tin tương tự trên sàn."}"""));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler, products);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Nồi chiên không dầu Philips HD9252",
            CategoryId: 2, // Chọn sai danh mục
            ConditionId: 1,
            Language: "vi"));

        Assert.True(res.IsSuccess);
        Assert.Equal("KEYWORD", res.Data!.MatchScope);
        Assert.Single(res.Data.Samples);
        Assert.Equal("Nồi chiên không dầu 5L", res.Data.Samples[0].Name);
    }

    [Fact]
    public async Task SuggestPriceAsync_HighSeverityForbiddenKeyword_BlocksImmediatelyWithoutCallingLlm()
    {
        var handler = new QueueHttpHandler(_ => OpenAiOk("""{"recommended_price_vnd": 500000, "explanation": "Should not run"}"""));
        var scanner = new StubKeywordScanner(new KeywordMatch(1, "ma túy", "HANG_CAM", ModerationPolicy.KeywordHigh, "Ten", 0));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler, keywordScanner: scanner);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Ma túy đá",
            CategoryId: 2,
            ConditionId: 1,
            Language: "vi"));

        Assert.False(res.IsSuccess);
        Assert.Equal(AiChatStatus.InvalidInput, res.Status);
        Assert.Contains("bị cấm", res.Error);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task SuggestPriceAsync_NoKeywordMatch_DoesNotPullUnrelatedCategoryItems()
    {
        var products = new StubProductService(filter =>
        {
            if (string.IsNullOrEmpty(filter.Keyword))
            {
                return new PagedResult<ProductCardVm>(
                    new[]
                    {
                        new ProductCardVm { Id = 21, Name = "Bình gốm men rạn", Price = 650_000m, ConditionName = "Cũ" },
                        new ProductCardVm { Id = 22, Name = "Balo máy ảnh", Price = 400_000m, ConditionName = "Tốt" }
                    },
                    totalItems: 2, page: 1, pageSize: 20);
            }
            return PagedResult<ProductCardVm>.Empty();
        });

        var llmJson = """
            {
              "recommended_price_vnd": 1200000,
              "explanation": "Hiện sàn chưa có tin tương tự; theo giá đồ cũ thị trường ước lượng khoảng 1.200.000đ."
            }
            """;
        var handler = new QueueHttpHandler(_ => OpenAiOk(llmJson));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler, products);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Nồi chiên không dầu Philips HD9252",
            CategoryId: 2,
            ConditionId: 2,
            Language: "vi"));

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Data);
        Assert.Equal("NONE", res.Data!.MatchScope);
        Assert.Equal(0, res.Data.SampleCount);
        Assert.Empty(res.Data.Samples);
        Assert.Null(res.Data.MinPriceVnd);
        Assert.True(res.Data.UsedAi);
        Assert.Equal(1_200_000m, res.Data.RecommendedPriceVnd);
    }

    [Fact]
    public async Task SuggestPriceAsync_WithKeywordMatches_SurvivesLlmOutageWithMarketplaceStats()
    {
        var products = new StubProductService(filter =>
        {
            if (!string.IsNullOrEmpty(filter.Keyword))
            {
                return new PagedResult<ProductCardVm>(
                    new[]
                    {
                        new ProductCardVm { Id = 21, Name = "Bàn gỗ sồi", Price = 400_000m, ConditionName = "Tốt" },
                        new ProductCardVm { Id = 22, Name = "Bàn học gấp", Price = 600_000m, ConditionName = "Như mới" },
                        new ProductCardVm { Id = 23, Name = "Bàn làm việc", Price = 800_000m, ConditionName = "Tốt" }
                    },
                    totalItems: 3, page: 1, pageSize: 20);
            }
            return PagedResult<ProductCardVm>.Empty();
        });

        var handler = new QueueHttpHandler(_ => HttpError(HttpStatusCode.TooManyRequests, "rate limited"));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler, products);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Bàn gỗ học sinh cũ",
            CategoryId: 2,
            ConditionId: 2,
            Language: "vi"));

        Assert.True(res.IsSuccess);
        Assert.NotNull(res.Data);
        Assert.Equal("KEYWORD", res.Data!.MatchScope);
        Assert.False(res.Data.UsedAi);
        Assert.Equal(400_000m, res.Data.MinPriceVnd);
        Assert.Equal(600_000m, res.Data.MedianPriceVnd);
        Assert.Equal(800_000m, res.Data.MaxPriceVnd);
        Assert.Equal(600_000m, res.Data.RecommendedPriceVnd);
        Assert.Contains("tương tự đang bán", res.Data.Explanation);
    }

    [Fact]
    public async Task SuggestPriceAsync_LlmProposesAbsurdPrice_FallsBackToGroundedBaseline()
    {
        var products = new StubProductService(_ => new PagedResult<ProductCardVm>(
            new[]
            {
                new ProductCardVm { Id = 1, Name = "Tai nghe A", Price = 500_000m },
                new ProductCardVm { Id = 2, Name = "Tai nghe B", Price = 600_000m },
                new ProductCardVm { Id = 3, Name = "Tai nghe C", Price = 700_000m }
            },
            totalItems: 3, page: 1, pageSize: 20));

        var handler = new QueueHttpHandler(_ => OpenAiOk("""{"recommended_price_vnd": 99000000, "explanation": "Test"}"""));
        var svc = CreateService(new() { ["Ai:ApiKey"] = "AIza-primary" }, handler, products);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Tai nghe bluetooth",
            CategoryId: 1,
            ConditionId: 2,
            Language: "vi"));

        Assert.True(res.IsSuccess);
        Assert.Equal(600_000m, res.Data!.RecommendedPriceVnd);
    }

    [Fact]
    public async Task SuggestPriceAsync_NoListingsAndNoApiKey_ReturnsNoneScopeWithClearExplanation()
    {
        var handler = new QueueHttpHandler(_ => OpenAiOk("{}"));
        var svc = CreateService(new Dictionary<string, string?>(), handler);

        var res = await svc.SuggestPriceAsync("10", AppRoles.User, new AiPriceSuggestInput(
            Name: "Máy ảnh film hiếm",
            CategoryId: 1,
            ConditionId: 2,
            Language: "en"));

        Assert.True(res.IsSuccess);
        Assert.Equal("NONE", res.Data!.MatchScope);
        Assert.Null(res.Data.RecommendedPriceVnd);
        Assert.False(res.Data.UsedAi);
        Assert.Contains("no comparable listings", res.Data.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    // ------------------------------------------------------------------ Stubs

    private sealed class StubKeywordScanner(params KeywordMatch[] matches) : IKeywordScanner
    {
        public Task<IReadOnlyList<KeywordMatch>> ScanAsync(string? name, string? description, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<KeywordMatch>>(matches);
    }

    private sealed class StubProductService(Func<ProductSearchVm, PagedResult<ProductCardVm>>? search = null) : IProductService
    {
        public Task<PagedResult<ProductCardVm>> SearchAsync(ProductSearchVm filter) =>
            Task.FromResult(search is null ? PagedResult<ProductCardVm>.Empty() : search(filter));

        public Task<ProductDetailVm?> GetDetailAsync(int id) => Task.FromResult<ProductDetailVm?>(null);
        public Task<(decimal Min, decimal Max)> LayBienGiaAsync(CancellationToken cancellationToken = default) => Task.FromResult((0m, 0m));
        public Task<PagedResult<MyProductVm>> GetMyAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProductFormVm?> GetFormAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error, int Id)> CreateAsync(int maNguoiDung, ProductFormVm form, IReadOnlyList<ProductImageInput> anh, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> UpdateAsync(int maNguoiDung, int maSanPham, ProductFormVm form, IReadOnlyList<ProductImageInput> anhMoi, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> HideAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> GuiDuyetAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DeleteAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DatAnhChinhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error, int? MaAnhChinhMoi)> XoaAnhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubCategoryService : ICategoryService
    {
        public Task<IReadOnlyList<DanhMuc>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DanhMuc>>(new[]
            {
                new DanhMuc { MaDanhMuc = 1, Ten = "Đồ điện tử", DangHienThi = true },
                new DanhMuc { MaDanhMuc = 2, Ten = "Nội thất", DangHienThi = true }
            });

        public Task<IReadOnlyList<TinhTrangSanPham>> GetConditionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TinhTrangSanPham>>(new[]
            {
                new TinhTrangSanPham { MaTinhTrang = 1, Ma = "LIKE_NEW", Ten = "Như mới", DangSuDung = true },
                new TinhTrangSanPham { MaTinhTrang = 2, Ma = "GOOD", Ten = "Tốt", DangSuDung = true }
            });

        public Task<(bool Success, string? Error, int Id)> SaveCategoryAsync(int? id, string name, int order, bool visible, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error, int Id)> SaveConditionAsync(int? id, string code, string name, string? description, int order, bool active, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DeleteConditionAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubKhuVucService : IKhuVucService
    {
        public Task<IReadOnlyList<TinhThanhItem>> GetTinhThanhAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TinhThanhItem>>(Array.Empty<TinhThanhItem>());
        public Task<IReadOnlyList<PhuongXaItem>> GetPhuongXaTheoTinhAsync(int maTinh, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PhuongXaItem>>(Array.Empty<PhuongXaItem>());
        public Task<bool> PhuongXaThuocTinhAsync(int maTinh, int maXa, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(int? MaTinh, int? MaXa)> GetKhuVucDaChonAsync(HttpContext httpContext, CancellationToken cancellationToken = default) => Task.FromResult<(int?, int?)>((null, null));
        public Task<bool> LuuKhuVucDaChonAsync(HttpContext httpContext, int? maTinh, int? maXa, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
