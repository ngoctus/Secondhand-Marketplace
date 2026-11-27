using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Services.Banners;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// B-01/B-02 (vòng 2): ảnh banner upload phải qua kiểm tra Magic Bytes rồi mới ghi xuống
/// wwwroot/uploads/banners, và mọi lần lưu/xóa banner phải xóa cache "home:catalog" để slider
/// trang chủ đổi ngay khi Admin demo.
/// </summary>
public class BannerUploadAndCacheTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase($"Banner_{Guid.NewGuid()}").Options);

    private static byte[] AnhJpgThat() => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x01, 0x01];

    private static byte[] FileGiaMao() => "GET / HTTP/1.1\r\nHost: evil\r\n".Select(c => (byte)c).ToArray();

    private sealed class FakeFile : IFormFile
    {
        private readonly byte[] _noiDung;

        public FakeFile(byte[] noiDung, string tenFile)
        {
            _noiDung = noiDung;
            FileName = tenFile;
        }

        public string Name => "anhTaiLen";
        public string FileName { get; }
        public string ContentType => "application/octet-stream";
        public string ContentDisposition => $"form-data; name=\"{Name}\"; filename=\"{FileName}\"";
        public long Length => _noiDung.LongLength;
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public Stream OpenReadStream() => new MemoryStream(_noiDung);
        public void CopyTo(Stream target) => OpenReadStream().CopyTo(target);
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) =>
            OpenReadStream().CopyToAsync(target, cancellationToken);
    }

    private static async Task BanDoCache(IDistributedCache cache) =>
        await cache.SetStringAsync(HomeCacheHelper.CatalogCacheKey, "cached", new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });

    [Fact]
    public async Task LuuBanner_XoaCacheTrangChuDeSliderCapNhatNgay()
    {
        using var db = NewDb();
        var cache = new TestDistributedCache();
        await BanDoCache(cache);

        var (success, _, _) = await new BannerService(db, cache).SaveAsync(
            null, "Tuần lễ đồ cũ", "Giảm giá", "/uploads/banners/new.jpg", null, 1, true, null, null);

        Assert.True(success);
        Assert.Null(await cache.GetStringAsync(HomeCacheHelper.CatalogCacheKey));
    }

    [Fact]
    public async Task XoaBanner_CungXoaCacheTrangChu()
    {
        using var db = NewDb();
        var cache = new TestDistributedCache();
        var banners = new BannerService(db, cache);
        var (_, _, maBanner) = await banners.SaveAsync(
            null, "Banner demo", null, "/uploads/banners/old.jpg", null, 1, true, null, null);
        await BanDoCache(cache);

        var (success, _) = await banners.DeleteAsync(maBanner);

        Assert.True(success);
        Assert.Null(await cache.GetStringAsync(HomeCacheHelper.CatalogCacheKey));
    }

    [Fact]
    public async Task BannerBiChuoiValidate_GiuNguyenCache()
    {
        using var db = NewDb();
        var cache = new TestDistributedCache();
        await BanDoCache(cache);

        var (success, _, _) = await new BannerService(db, cache).SaveAsync(
            null, "A", null, "/uploads/banners/x.jpg", null, 1, true, null, null);

        Assert.False(success);
        Assert.Equal("cached", await cache.GetStringAsync(HomeCacheHelper.CatalogCacheKey));
    }

    [Fact]
    public async Task FileKhongPhaiAnh_BiTuChoiTheoMagicBytes()
    {
        var (hopLe, loi, _) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(new FakeFile(FileGiaMao(), "banner.jpg"));

        Assert.False(hopLe);
        Assert.Contains("không phải", loi);
    }

    [Fact]
    public async Task AnhBannerThat_LuuVaoThuMucUploadsBanners()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), $"rm_banner_{Guid.NewGuid():N}");
        try
        {
            var file = new FakeFile(AnhJpgThat(), "banner.jpg");
            var (hopLe, _, ext) = await UploadAnhAnToan.KiemTraAnhHopLeAsync(file);
            Assert.True(hopLe);

            var environment = new TestWebHostEnvironment(webRoot);
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:RootPath"] = webRoot,
                ["Storage:PrivateRootPath"] = Path.Combine(webRoot, "private")
            }).Build();
            var storage = new SecondHandMarketplace.Web.Services.Storage.UploadStorage(
                environment, configuration, NullLogger<SecondHandMarketplace.Web.Services.Storage.UploadStorage>.Instance);
            var relativePath = $"/uploads/banners/{Guid.NewGuid():N}{ext}";
            await using var input = file.OpenReadStream();
            var duongDan = await storage.SaveAsync(input, relativePath, false, file.ContentType);
            var fullPath = UploadStoragePaths.ResolveLocalUpload(duongDan, webRoot, "banners")!;

            Assert.StartsWith("/uploads/banners/", duongDan);
            Assert.EndsWith(".jpg", duongDan);
            Assert.Equal(AnhJpgThat().LongLength, new FileInfo(fullPath).Length);
        }
        finally
        {
            if (Directory.Exists(webRoot)) Directory.Delete(webRoot, recursive: true);
        }
    }
}
