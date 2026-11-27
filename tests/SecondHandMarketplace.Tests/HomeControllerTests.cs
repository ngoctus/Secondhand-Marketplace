using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Controllers;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Auctions;
using SecondHandMarketplace.Web.Models.ViewModels.Home;
using SecondHandMarketplace.Web.Services.Auctions;
using SecondHandMarketplace.Web.Services.Banners;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Realtime;
using SecondHandMarketplace.Web.Services.Reviews;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// Test trang chủ sau khi chuyển truy vấn từ Razor view vào HomeController (kế hoạch B-01, B-02):
/// một lần nạp dùng cache, gom sản phẩm theo danh mục thành tab, và không cache khi DB trống.
/// </summary>
public class HomeControllerTests
{
    private sealed class NoopAuctionRealtime : IAuctionRealtime
    {
        public Task BroadcastStatusAsync(int auctionId, AuctionStatusDto status) => Task.CompletedTask;
        public Task PushLatestNotificationAsync(int userId, string? url = null) => Task.CompletedTask;
        public Task PushToParticipantsAsync(int auctionId) => Task.CompletedTask;
    }

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
            new AuctionService(db, new NoopAuctionRealtime()),
            cache,
            db);
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

    [Fact]
    public async Task SanPhamGoiY_HoTroCheDoMoiDangGiaTotGiaTietKiemVaLocDanhMuc()
    {
        var db = NewDb();
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
        db.DanhMucs.AddRange(
            new DanhMuc { MaDanhMuc = 1, Ten = "Đồ điện tử", ThuTuHienThi = 1 },
            new DanhMuc { MaDanhMuc = 2, Ten = "Thời trang", ThuTuHienThi = 2 });

        var goc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.SanPhams.AddRange(
            new SanPham { MaSanPham = 1, MaCuaHang = 1, MaDanhMuc = 1, Ten = "Laptop cũ", ChuoiTimKiem = "laptop cu", MoTa = "mota", Gia = 3_500_000, TrangThai = "ACTIVE", NgayTao = goc.AddMinutes(1), NgayCapNhat = goc.AddMinutes(1) },
            new SanPham { MaSanPham = 2, MaCuaHang = 1, MaDanhMuc = 1, Ten = "Cáp sạc nhanh", ChuoiTimKiem = "cap sac nhanh", MoTa = "mota", Gia = 60_000, TrangThai = "ACTIVE", NgayTao = goc.AddMinutes(2), NgayCapNhat = goc.AddMinutes(2) },
            new SanPham { MaSanPham = 3, MaCuaHang = 1, MaDanhMuc = 2, Ten = "Áo thun cotton", ChuoiTimKiem = "ao thun cotton", MoTa = "mota", Gia = 85_000, TrangThai = "ACTIVE", NgayTao = goc.AddMinutes(3), NgayCapNhat = goc.AddMinutes(3) },
            new SanPham { MaSanPham = 4, MaCuaHang = 1, MaDanhMuc = 2, Ten = "Áo khoác denim", ChuoiTimKiem = "ao khoac denim", MoTa = "mota", Gia = 250_000, TrangThai = "ACTIVE", NgayTao = goc.AddMinutes(4), NgayCapNhat = goc.AddMinutes(4) },
            new SanPham { MaSanPham = 5, MaCuaHang = 1, MaDanhMuc = 1, Ten = "Chuột không dây", ChuoiTimKiem = "chuot khong day", MoTa = "mota", Gia = 180_000, TrangThai = "ACTIVE", NgayTao = goc.AddMinutes(5), NgayCapNhat = goc.AddMinutes(5) });
        await db.SaveChangesAsync();

        var controller = NewController(db);

        // Mới đăng: trả đúng thứ tự mới nhất trước (MaSanPham = 5 đầu tiên)
        var newestResult = Assert.IsType<Microsoft.AspNetCore.Mvc.PartialViewResult>(await controller.SanPhamGoiY("newest", null));
        Assert.Equal("_HomeRecGrid", newestResult.ViewName);
        var newestItems = Assert.IsAssignableFrom<IReadOnlyList<SecondHandMarketplace.Web.Models.ViewModels.Products.ProductCardVm>>(newestResult.Model);
        Assert.Equal(4, newestItems.Count);
        Assert.Equal(5, newestItems[0].Id);

        // Giá tốt (PriceRange = 1 -> dưới 100.000đ): chỉ gồm sản phẩm 60k và 85k
        var bestPriceResult = Assert.IsType<Microsoft.AspNetCore.Mvc.PartialViewResult>(await controller.SanPhamGoiY("best_price", null));
        var bestPriceItems = Assert.IsAssignableFrom<IReadOnlyList<SecondHandMarketplace.Web.Models.ViewModels.Products.ProductCardVm>>(bestPriceResult.Model);
        Assert.Equal(2, bestPriceItems.Count);
        Assert.All(bestPriceItems, p => Assert.True(p.Price <= 100_000));

        // Giá tiết kiệm (price_asc): giá tăng dần, rẻ nhất 60k đứng đầu
        var bargainResult = Assert.IsType<Microsoft.AspNetCore.Mvc.PartialViewResult>(await controller.SanPhamGoiY("bargain", null));
        var bargainItems = Assert.IsAssignableFrom<IReadOnlyList<SecondHandMarketplace.Web.Models.ViewModels.Products.ProductCardVm>>(bargainResult.Model);
        Assert.Equal(4, bargainItems.Count);
        Assert.Equal(60_000, bargainItems[0].Price);
        Assert.Equal(85_000, bargainItems[1].Price);

        // Kết hợp chế độ Giá tốt + danh mục 1 (Đồ điện tử): chỉ còn cáp sạc 60k
        var filteredResult = Assert.IsType<Microsoft.AspNetCore.Mvc.PartialViewResult>(await controller.SanPhamGoiY("best_price", 1));
        var filteredItems = Assert.IsAssignableFrom<IReadOnlyList<SecondHandMarketplace.Web.Models.ViewModels.Products.ProductCardVm>>(filteredResult.Model);
        var only = Assert.Single(filteredItems);
        Assert.Equal(2, only.Id);
    }

    [Fact]
    public async Task Index_NapPhienDauGiaDangDienRaVaBuPhienSapMo()
    {
        var db = NewDb();
        await NapDuLieuAsync(db, (1, "Đồ điện tử", 4));
        var now = DateTime.UtcNow;

        db.PhienDauGias.AddRange(
            new PhienDauGia
            {
                MaPhienDauGia = 10,
                MaSanPham = 100,
                GiaKhoiDiem = 100_000,
                BuocGia = 10_000,
                GiaHienTai = 150_000,
                SoLuotDat = 3,
                ThoiGianBatDau = now.AddHours(-1),
                ThoiGianKetThuc = now.AddHours(2),
                TrangThai = "RUNNING"
            },
            new PhienDauGia
            {
                MaPhienDauGia = 11,
                MaSanPham = 101,
                GiaKhoiDiem = 200_000,
                BuocGia = 20_000,
                GiaHienTai = 200_000,
                SoLuotDat = 0,
                ThoiGianBatDau = now.AddHours(3),
                ThoiGianKetThuc = now.AddHours(8),
                TrangThai = "SCHEDULED"
            },
            new PhienDauGia
            {
                MaPhienDauGia = 12,
                MaSanPham = 102,
                GiaKhoiDiem = 50_000,
                BuocGia = 5_000,
                GiaHienTai = 90_000,
                SoLuotDat = 4,
                ThoiGianBatDau = now.AddHours(-5),
                ThoiGianKetThuc = now.AddHours(-1),
                TrangThai = "ENDED"
            });
        await db.SaveChangesAsync();

        var vm = await TrangChuAsync(NewController(db));

        Assert.Equal(2, vm.PhienDauGiaNoiBat.Count);
        Assert.Equal(10, vm.PhienDauGiaNoiBat[0].Id);
        Assert.Equal("RUNNING", vm.PhienDauGiaNoiBat[0].Status);
        Assert.Equal(11, vm.PhienDauGiaNoiBat[1].Id);
        Assert.Equal("SCHEDULED", vm.PhienDauGiaNoiBat[1].Status);
    }
}
