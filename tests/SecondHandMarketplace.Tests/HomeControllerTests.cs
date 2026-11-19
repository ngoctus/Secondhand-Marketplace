using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Controllers;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Home;
using SecondHandMarketplace.Web.Services.Banners;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Reviews;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// Test trang chủ sau khi chuyển truy vấn từ Razor view vào HomeController (kế hoạch B-01, B-02):
/// một lần nạp dùng cache, gom sản phẩm theo danh mục thành tab, và không cache khi DB trống.
/// </summary>
public class HomeControllerTests
{
    private static AppDbContext NewDb([System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Home_{Guid.NewGuid()}_{name}")
            .Options;
        return new AppDbContext(options);
    }

    private static HomeController NewController(AppDbContext db)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var reviews = new ReviewService(db);
        return new HomeController(
            new ProductService(db, reviews),
            new CategoryService(db, cache),
            new BannerService(db),
            reviews,
            cache);
    }

    private static async Task<HomeIndexVm> TrangChuAsync(HomeController controller) =>
        (HomeIndexVm)(((Microsoft.AspNetCore.Mvc.ViewResult)await controller.Index()).Model)!;

    private static async Task NapDuLieuAsync(AppDbContext db, params (int Ma, string Ten, int SoTin)[] danhMucs)
    {
        db.NguoiDungs.Add(new NguoiDung { MaNguoiDung = 1, HoTen = "Chủ shop", Email = "shop@test.local", TrangThai = "ACTIVE" });
        db.CuaHangs.Add(new CuaHang
        {
            MaCuaHang = 1,
            MaNguoiDung = 1,
            TenShop = "Shop 1",
            SoDienThoaiLienHe = "0100000000",
            TrangThai = "ACTIVE",
            NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        var maTin = 100;
        var thuTu = 0;
        foreach (var (ma, ten, soTin) in danhMucs)
        {
            db.DanhMucs.Add(new DanhMuc { MaDanhMuc = ma, Ten = ten, ThuTuHienThi = ma });
            for (var i = 0; i < soTin; i++)
            {
                var ngay = thuTu + i;
                db.SanPhams.Add(new SanPham
                {
                    MaSanPham = maTin++,
                    MaCuaHang = 1,
                    MaDanhMuc = ma,
                    Ten = $"{ten} {i + 1}",
                    ChuoiTimKiem = TextHelper.ChuanHoa($"{ten} {i + 1}"),
                    MoTa = new string('a', 140),
                    Gia = 100_000 + ngay,
                    TrangThai = "ACTIVE",
                    NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(ngay),
                    NgayCapNhat = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(ngay)
                });
            }
            // Lệch 10 phút giữa các danh mục để danh mục nạp sau luôn có tin mới nhất.
            thuTu += 10;
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Index_GomTinTheoDanhMucVaBoDanhMucDuBonTin()
    {
        var db = NewDb();
        // Danh mục 1 có 5 tin, danh mục 2 chỉ 3 tin -> chỉ danh mục 1 lên tab, mỗi tab tối đa 4 tin.
        await NapDuLieuAsync(db, (1, "Laptop", 5), (2, "Ghế", 3));

        var vm = await TrangChuAsync(NewController(db));

        var tab = Assert.Single(vm.TabsDanhMuc);
        Assert.Equal(1, tab.MaDanhMuc);
        Assert.Equal("Laptop", tab.TenDanhMuc);
        Assert.Equal(4, tab.SanPhams.Count);
        Assert.All(tab.SanPhams, p => Assert.Equal(1, p.CategoryId));
    }

    [Fact]
    public async Task Index_CatSanPhamNoiBatBonTinDungDanhMucMoiNhat()
    {
        var db = NewDb();
        await NapDuLieuAsync(db, (1, "Laptop", 4), (2, "Ghế", 4));

        var vm = await TrangChuAsync(NewController(db));

        Assert.Equal(4, vm.SanPhamNoiBat.Count);
        // Danh mục 2 có tin mới nhất (thứ tự nạp sau) nên phải là tab đầu tiên.
        Assert.Equal(2, vm.TabsDanhMuc.First().MaDanhMuc);
    }

    [Fact]
    public async Task Index_LanThuHaiDocTuCacheKhongCanDuLieuTrongDatabase()
    {
        var db = NewDb();
        await NapDuLieuAsync(db, (1, "Laptop", 5));
        var controller = NewController(db);

        var lanDau = await TrangChuAsync(controller);

        db.SanPhams.RemoveRange(db.SanPhams);
        await db.SaveChangesAsync();

        var lanHai = await TrangChuAsync(controller);

        Assert.NotEmpty(lanDau.SanPhamNoiBat);
        Assert.Equal(lanDau.SanPhamNoiBat.Count, lanHai.SanPhamNoiBat.Count);
    }

    [Fact]
    public async Task Index_KhiDatabaseTrongThiKhongCacheDeNapLaiVanCoTin()
    {
        var db = NewDb();
        var controller = NewController(db);

        var lucDau = await TrangChuAsync(controller);
        Assert.Empty(lucDau.SanPhamNoiBat);

        await NapDuLieuAsync(db, (1, "Laptop", 4));

        var sauKhiCoDuLieu = await TrangChuAsync(controller);
        Assert.NotEmpty(sauKhiCoDuLieu.SanPhamNoiBat);
    }
}
