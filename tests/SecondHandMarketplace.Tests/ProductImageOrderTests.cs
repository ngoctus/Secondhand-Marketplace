using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Reviews;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class ProductImageOrderTests
{
    private static AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedAsync(AppDbContext db)
    {
        db.NguoiDungs.Add(new NguoiDung { MaNguoiDung = 10, HoTen = "Seller", Email = "seller@test.local", TrangThai = "ACTIVE" });
        db.CuaHangs.Add(new CuaHang { MaCuaHang = 20, MaNguoiDung = 10, TenShop = "Shop Test", TrangThai = "ACTIVE" });
        db.DanhMucs.Add(new DanhMuc { MaDanhMuc = 1, Ten = "Laptop", DangHienThi = true, ThuTuHienThi = 1 });
        db.TinhTrangSanPhams.Add(new TinhTrangSanPham { MaTinhTrang = 1, Ten = "Đã dùng", DangSuDung = true });
        db.SanPhams.Add(new SanPham
        {
            MaSanPham = 100,
            MaCuaHang = 20,
            MaDanhMuc = 1,
            MaTinhTrang = 1,
            Ten = "Laptop cũ",
            Gia = 5_000_000,
            TrangThai = "HIDDEN",
            DaXoa = false
        });

        // Ảnh ThuTu = 1 đã bị xóa trước đó, còn lại 2,3,4 — đúng tình huống gây trùng unique key.
        db.AnhSanPhams.Add(new AnhSanPham { MaAnhSP = 1, MaSanPham = 100, DuongDan = "/uploads/products/a.jpg", ThuTu = 2, LaAnhChinh = true });
        db.AnhSanPhams.Add(new AnhSanPham { MaAnhSP = 2, MaSanPham = 100, DuongDan = "/uploads/products/b.jpg", ThuTu = 3, LaAnhChinh = false });
        db.AnhSanPhams.Add(new AnhSanPham { MaAnhSP = 3, MaSanPham = 100, DuongDan = "/uploads/products/c.jpg", ThuTu = 4, LaAnhChinh = false });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task UpdateAsync_ThemAnhMoi_NoiTiepThuTuLonNhat_KhongTrungKhoangTrong()
    {
        using var db = GetInMemoryDbContext("ImageOrder_GapAfterDelete");
        await SeedAsync(db);

        var service = new ProductService(db, new ReviewService(db));
        var form = new ProductFormVm
        {
            MaDanhMuc = 1,
            MaTinhTrang = 1,
            Ten = "Laptop cũ",
            Gia = 5_000_000
        };

        var (ok, error) = await service.UpdateAsync(10, 100, form,
            new[] { new ProductImageInput("/uploads/products/new.jpg", 1234) });

        Assert.True(ok, error);

        var anh = await db.AnhSanPhams.AsNoTracking()
            .Where(a => a.MaSanPham == 100)
            .OrderBy(a => a.ThuTu)
            .ToListAsync();

        Assert.Equal(4, anh.Count);
        var moi = Assert.Single(anh, a => a.DuongDan == "/uploads/products/new.jpg");
        Assert.Equal(5, moi.ThuTu);
        Assert.False(moi.LaAnhChinh);
        Assert.Single(anh, a => a.LaAnhChinh);
        Assert.Equal(2, anh[0].ThuTu);
    }

    [Fact]
    public async Task UpdateAsync_SanPhamChuaCoAnh_AnhDauTienThanhBia()
    {
        using var db = GetInMemoryDbContext("ImageOrder_NoImages");
        db.NguoiDungs.Add(new NguoiDung { MaNguoiDung = 10, HoTen = "Seller", Email = "seller2@test.local", TrangThai = "ACTIVE" });
        db.CuaHangs.Add(new CuaHang { MaCuaHang = 20, MaNguoiDung = 10, TenShop = "Shop Test", TrangThai = "ACTIVE" });
        db.DanhMucs.Add(new DanhMuc { MaDanhMuc = 1, Ten = "Laptop", DangHienThi = true, ThuTuHienThi = 1 });
        db.TinhTrangSanPhams.Add(new TinhTrangSanPham { MaTinhTrang = 1, Ten = "Đã dùng", DangSuDung = true });
        db.SanPhams.Add(new SanPham
        {
            MaSanPham = 101,
            MaCuaHang = 20,
            MaDanhMuc = 1,
            MaTinhTrang = 1,
            Ten = "Laptop chưa ảnh",
            Gia = 3_000_000,
            TrangThai = "HIDDEN",
            DaXoa = false
        });
        await db.SaveChangesAsync();

        var service = new ProductService(db, new ReviewService(db));
        var form = new ProductFormVm { MaDanhMuc = 1, MaTinhTrang = 1, Ten = "Laptop chưa ảnh", Gia = 3_000_000 };

        var (ok, error) = await service.UpdateAsync(10, 101, form,
            new[] { new ProductImageInput("/uploads/products/d.jpg", 1234) });

        Assert.True(ok, error);
        var moi = Assert.Single(await db.AnhSanPhams.AsNoTracking().Where(a => a.MaSanPham == 101).ToListAsync());
        Assert.Equal(1, moi.ThuTu);
        Assert.True(moi.LaAnhChinh);
    }
}
