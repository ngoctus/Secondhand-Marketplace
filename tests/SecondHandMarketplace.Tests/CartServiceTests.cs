using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Carts;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class CartServiceTests
{
    private AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task DemSoLuongMonAsync_TraVeDungSoLuong()
    {
        using var db = GetInMemoryDbContext("CartTest_DemSoLuong");
        var service = new CartService(db);

        db.GioHangs.Add(new GioHang { MaGioHang = 1, MaNguoiDung = 10, MaSanPham = 101, DaChon = true });
        db.GioHangs.Add(new GioHang { MaGioHang = 2, MaNguoiDung = 10, MaSanPham = 102, DaChon = true });
        db.GioHangs.Add(new GioHang { MaGioHang = 3, MaNguoiDung = 99, MaSanPham = 103, DaChon = true });
        await db.SaveChangesAsync();

        var count = await service.DemSoLuongMonAsync(10);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CapNhatTrangThaiChonAsync_ThayDoiDaChonThanhCong()
    {
        using var db = GetInMemoryDbContext("CartTest_CapNhatChon");
        var service = new CartService(db);

        var item = new GioHang { MaGioHang = 1, MaNguoiDung = 10, MaSanPham = 101, DaChon = true };
        db.GioHangs.Add(item);
        await db.SaveChangesAsync();

        var ok = await service.CapNhatTrangThaiChonAsync(10, 1, false);
        Assert.True(ok);

        var updated = await db.GioHangs.FindAsync(1);
        Assert.False(updated!.DaChon);
    }

    [Fact]
    public async Task XoaKhoiGioAsync_XoaDungMon()
    {
        using var db = GetInMemoryDbContext("CartTest_XoaMon");
        var service = new CartService(db);

        db.GioHangs.Add(new GioHang { MaGioHang = 1, MaNguoiDung = 10, MaSanPham = 101, DaChon = true });
        await db.SaveChangesAsync();

        var ok = await service.XoaKhoiGioAsync(10, 1);
        Assert.True(ok);

        var count = await db.GioHangs.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ThemVaoGioAsync_SanPhamCuaChinhShopMinh_TraVeLoi()
    {
        using var db = GetInMemoryDbContext("CartTest_OwnShop");
        var service = new CartService(db);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 10, TenShop = "My Shop", TrangThai = "ACTIVE" };
        db.CuaHangs.Add(shop);

        var sp = new SanPham
        {
            MaSanPham = 101,
            MaCuaHang = 1,
            Ten = "Áo khoác",
            Gia = 150000,
            TrangThai = "ACTIVE",
            DaXoa = false,
            CuaHang = shop
        };
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.ThemVaoGioAsync(10, 101);

        Assert.False(thanhCong);
        Assert.Contains("Không thể thêm sản phẩm của chính mình", thongBao);
    }

    [Fact]
    public async Task ThemVaoGioAsync_SanPhamDaCoTrongGio_BaoDaCoSan()
    {
        using var db = GetInMemoryDbContext("CartTest_DaCoTrongGio");
        var service = new CartService(db);

        db.GioHangs.Add(new GioHang { MaGioHang = 1, MaNguoiDung = 20, MaSanPham = 201, DaChon = true });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.ThemVaoGioAsync(20, 201);

        Assert.True(thanhCong);
        Assert.Contains("đã có sẵn trong giỏ hàng", thongBao);
    }

    [Fact]
    public async Task ThemVaoGioAsync_ThemMoiThanhCong()
    {
        using var db = GetInMemoryDbContext("CartTest_ThemMoiThanhCong");
        var service = new CartService(db);

        var shop = new CuaHang { MaCuaHang = 2, MaNguoiDung = 30, TenShop = "Other Shop", TrangThai = "ACTIVE" };
        db.CuaHangs.Add(shop);

        var sp = new SanPham
        {
            MaSanPham = 301,
            MaCuaHang = 2,
            Ten = "Tai nghe Sony",
            Gia = 500000,
            TrangThai = "ACTIVE",
            DaXoa = false,
            CuaHang = shop
        };
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.ThemVaoGioAsync(20, 301);

        Assert.True(thanhCong);
        Assert.Contains("thành công", thongBao);
        var item = await db.GioHangs.FirstOrDefaultAsync(g => g.MaNguoiDung == 20 && g.MaSanPham == 301);
        Assert.NotNull(item);
    }

}
