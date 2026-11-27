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

    [Fact]
    public async Task DatAnhChinhVaXoaAnh_SanPhamDangBan_ChoDoiBiaVaGiuToiThieuMotAnh()
    {
        using var db = GetInMemoryDbContext("ImageOrder_ActiveCoverAndDelete");
        await SeedAsync(db);
        var sp = await db.SanPhams.FirstAsync(p => p.MaSanPham == 100);
        sp.TrangThai = "ACTIVE";
        await db.SaveChangesAsync();

        var service = new ProductService(db, new ReviewService(db));

        // Đổi ảnh bìa trực tiếp khi sản phẩm đang ACTIVE -> thành công không cần ẩn
        var (okCover, errCover) = await service.DatAnhChinhAsync(10, 100, 2);
        Assert.True(okCover, errCover);
        var anhSauDoi = await db.AnhSanPhams.AsNoTracking().Where(a => a.MaSanPham == 100).ToListAsync();
        Assert.True(anhSauDoi.Single(a => a.MaAnhSP == 2).LaAnhChinh);
        Assert.False(anhSauDoi.Single(a => a.MaAnhSP == 1).LaAnhChinh);

        // Xóa 2 ảnh thừa (còn 1 ảnh) -> cho phép
        var (okDel1, errDel1, _) = await service.XoaAnhAsync(10, 100, 1);
        Assert.True(okDel1, errDel1);
        var (okDel2, errDel2, _) = await service.XoaAnhAsync(10, 100, 3);
        Assert.True(okDel2, errDel2);

        // Cố xóa ảnh cuối cùng trên sản phẩm đang ACTIVE -> bị chặn với thông báo rõ ràng
        var (okDelLast, errDelLast, _) = await service.XoaAnhAsync(10, 100, 2);
        Assert.False(okDelLast);
        Assert.NotNull(errDelLast);
        Assert.Contains("ít nhất 1 ảnh", errDelLast);
    }

    [Fact]
    public async Task UnhideAsync_ChiAnKhongSua_MoLaiNgay_NeuDaSuaThiBatGuiDuyet()
    {
        using var db = GetInMemoryDbContext("Product_HideAndUnhide");
        await SeedAsync(db);
        var sp = await db.SanPhams.FirstAsync(p => p.MaSanPham == 100);
        sp.TrangThai = "ACTIVE";
        await db.SaveChangesAsync();

        var service = new ProductService(db, new ReviewService(db));

        // 1. Ẩn sản phẩm đang ACTIVE (không chỉnh sửa gì)
        var (okHide, errHide) = await service.HideAsync(10, 100);
        Assert.True(okHide, errHide);

        var (listSauAn, _) = await service.GetMyFilteredAsync(10, new SellerProductFilterVm());
        Assert.True(Assert.Single(listSauAn.Items).CoTheMoAnNgay);

        // 2. Mở ẩn trực tiếp -> quay về ACTIVE không cần gửi duyệt
        var (okUnhide, errUnhide) = await service.UnhideAsync(10, 100);
        Assert.True(okUnhide, errUnhide);
        Assert.Equal("ACTIVE", (await db.SanPhams.AsNoTracking().FirstAsync(p => p.MaSanPham == 100)).TrangThai);

        // 3. Ẩn lại rồi chỉnh sửa nội dung -> KHÔNG cho mở ẩn trực tiếp nữa
        Assert.True((await service.HideAsync(10, 100)).Success);
        var (okEdit, errEdit) = await service.UpdateAsync(10, 100, new ProductFormVm
        {
            MaDanhMuc = 1,
            MaTinhTrang = 1,
            Ten = "Laptop cũ đã chỉnh sửa giá",
            MoTa = "Mô tả đã được chỉnh sửa dài hơn hai mươi ký tự.",
            Gia = 4_800_000
        }, []);
        Assert.True(okEdit, errEdit);

        var (listSauSua, _) = await service.GetMyFilteredAsync(10, new SellerProductFilterVm());
        Assert.False(Assert.Single(listSauSua.Items).CoTheMoAnNgay);

        var (okUnhideAfterEdit, errAfterEdit) = await service.UnhideAsync(10, 100);
        Assert.False(okUnhideAfterEdit);
        Assert.NotNull(errAfterEdit);
    }
}
