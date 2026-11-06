using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Products;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class ProductDeletionTests
{
    private AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task DeleteAsync_Success_SoftDeletesProductAndRemovesCartItems()
    {
        using var db = GetInMemoryDbContext("DeleteTest_Success");
        const int userId = 10;
        const int shopId = 20;
        const int productId = 100;

        db.NguoiDungs.Add(new NguoiDung { MaNguoiDung = userId, HoTen = "Seller", Email = "seller@test.local", TrangThai = "ACTIVE" });
        db.CuaHangs.Add(new CuaHang { MaCuaHang = shopId, MaNguoiDung = userId, TenShop = "Shop Test", TrangThai = "ACTIVE" });
        db.SanPhams.Add(new SanPham
        {
            MaSanPham = productId,
            MaCuaHang = shopId,
            Ten = "Áo khoác cũ",
            Gia = 150000,
            TrangThai = "ACTIVE",
            DaXoa = false
        });
        db.GioHangs.Add(new GioHang { MaGioHang = 1, MaNguoiDung = 99, MaSanPham = productId });
        await db.SaveChangesAsync();

        var service = new ProductService(db);
        var (success, error) = await service.DeleteAsync(userId, productId);

        Assert.True(success);
        Assert.Null(error);

        var sp = await db.SanPhams.FindAsync(productId);
        Assert.NotNull(sp);
        Assert.True(sp.DaXoa);
        Assert.NotNull(sp.ThoiGianXoa);

        // Giỏ hàng chứa sản phẩm đã bị dọn sạch
        Assert.Empty(db.GioHangs.Where(g => g.MaSanPham == productId));
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenProductIsSoldOrReserved()
    {
        using var db = GetInMemoryDbContext("DeleteTest_SoldOrReserved");
        const int userId = 10;
        const int shopId = 20;

        db.NguoiDungs.Add(new NguoiDung { MaNguoiDung = userId, HoTen = "Seller", Email = "seller2@test.local", TrangThai = "ACTIVE" });
        db.CuaHangs.Add(new CuaHang { MaCuaHang = shopId, MaNguoiDung = userId, TenShop = "Shop Test", TrangThai = "ACTIVE" });
        db.SanPhams.Add(new SanPham { MaSanPham = 201, MaCuaHang = shopId, Ten = "Đã bán", TrangThai = "SOLD", DaXoa = false });
        db.SanPhams.Add(new SanPham { MaSanPham = 202, MaCuaHang = shopId, Ten = "Đã cọc", TrangThai = "RESERVED", DaXoa = false });
        await db.SaveChangesAsync();

        var service = new ProductService(db);

        var (soldOk, soldErr) = await service.DeleteAsync(userId, 201);
        Assert.False(soldOk);
        Assert.Contains("không thể xóa", soldErr);

        var (resOk, resErr) = await service.DeleteAsync(userId, 202);
        Assert.False(resOk);
        Assert.Contains("không thể xóa", resErr);
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenNotBelongsToSeller()
    {
        using var db = GetInMemoryDbContext("DeleteTest_NotOwner");
        db.NguoiDungs.Add(new NguoiDung { MaNguoiDung = 1, HoTen = "User 1", Email = "u1@test.local", TrangThai = "ACTIVE" });
        db.NguoiDungs.Add(new NguoiDung { MaNguoiDung = 2, HoTen = "User 2", Email = "u2@test.local", TrangThai = "ACTIVE" });
        db.CuaHangs.Add(new CuaHang { MaCuaHang = 1, MaNguoiDung = 1, TenShop = "Shop 1", TrangThai = "ACTIVE" });
        db.CuaHangs.Add(new CuaHang { MaCuaHang = 2, MaNguoiDung = 2, TenShop = "Shop 2", TrangThai = "ACTIVE" });
        db.SanPhams.Add(new SanPham { MaSanPham = 50, MaCuaHang = 1, Ten = "SP của User 1", TrangThai = "ACTIVE", DaXoa = false });
        await db.SaveChangesAsync();

        var service = new ProductService(db);
        var (ok, err) = await service.DeleteAsync(2, 50); // User 2 tries to delete User 1's product

        Assert.False(ok);
        Assert.Contains("không thuộc cửa hàng", err);
    }
}
