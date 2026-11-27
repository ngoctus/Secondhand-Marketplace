using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Controllers;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Areas;
using SecondHandMarketplace.Web.Services.Categories;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Reviews;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// Test bộ lọc trang tìm kiếm sau kế hoạch B-04: khối kết quả phải là partial riêng để JS nạp lại,
/// thanh trượt giá lấy biên từ dữ liệu thật và giá nhập tay thắng phân khúc giá cũ.
/// </summary>
public class ProductsSearchControllerTests
{
    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Search_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static ProductsController NewController(AppDbContext db)
    {
        var cache = new TestDistributedCache();
        var controller = new ProductsController(
            new ProductService(db, new ReviewService(db)),
            new CategoryService(db, cache),
            new KhuVucService(db));

        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static async Task NapTinAsync(AppDbContext db, params (int Ma, decimal? Gia, string TrangThai)[] tins)
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
        db.DanhMucs.Add(new DanhMuc { MaDanhMuc = 1, Ten = "Laptop", ThuTuHienThi = 1 });

        foreach (var (ma, gia, trangThai) in tins)
            db.SanPhams.Add(new SanPham
            {
                MaSanPham = ma,
                MaCuaHang = 1,
                MaDanhMuc = 1,
                Ten = $"Tin {ma}",
                ChuoiTimKiem = $"tin {ma}",
                MoTa = new string('a', 60),
                Gia = gia,
                TrangThai = trangThai,
                NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(ma),
                NgayCapNhat = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(ma)
            });

        await db.SaveChangesAsync();
    }

    private static void GoiAjax(ProductsController controller) =>
        controller.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

    private static async Task<ProductSearchResultVm> TimKiemAsync(AppDbContext db, ProductSearchVm? filter = null)
    {
        var ketQua = await NewController(db).Index(filter ?? new ProductSearchVm(), default);
        return Assert.IsType<ProductSearchResultVm>(((ViewResult)ketQua).Model);
    }

    [Fact]
    public async Task Index_TuAjax_ChiTraVePartialKhoiKetQua()
    {
        var db = NewDb();
        await NapTinAsync(db, (1, 100_000m, "ACTIVE"), (2, 200_000m, "ACTIVE"));
        var controller = NewController(db);
        GoiAjax(controller);

        var ketQua = await controller.Index(new ProductSearchVm(), default);

        var partial = Assert.IsType<PartialViewResult>(ketQua);
        Assert.Equal("_SearchResults", partial.ViewName);
        var vm = Assert.IsType<ProductSearchResultVm>(partial.Model);
        Assert.Equal(2, vm.Result.TotalItems);
    }

    [Fact]
    public async Task Index_RequestThuong_VanTraVeDayDuTrang()
    {
        var db = NewDb();
        await NapTinAsync(db, (1, 100_000m, "ACTIVE"));

        var ketQua = await NewController(db).Index(new ProductSearchVm(), default);

        Assert.IsType<ViewResult>(ketQua);
    }

    [Fact]
    public async Task Index_BienGiaLayTuTinDangBanKhongTinhTinAnHoacGiaTrong()
    {
        var db = NewDb();
        await NapTinAsync(db, (1, 150_000m, "ACTIVE"), (2, 900_000m, "ACTIVE"),
                                (3, 50_000m, "HIDDEN"), (4, null, "ACTIVE"), (5, 5_000_000m, "SOLD"));

        var vm = await TimKiemAsync(db);

        Assert.Equal(150_000m, vm.BienGiaMin);
        Assert.Equal(900_000m, vm.BienGiaMax);
    }

    [Fact]
    public async Task Index_KhongCoTinDangBan_ThanhTruotKhongCoBien()
    {
        var db = NewDb();
        await NapTinAsync(db, (1, 150_000m, "HIDDEN"));

        var vm = await TimKiemAsync(db);

        Assert.Null(vm.BienGiaMin);
        Assert.Null(vm.BienGiaMax);
    }

    [Fact]
    public async Task Index_GiaKeoTuSlider_ThangPhanKhucGiaCo()
    {
        var db = NewDb();
        await NapTinAsync(db, (1, 80_000m, "ACTIVE"), (2, 200_000m, "ACTIVE"), (3, 300_000m, "ACTIVE"));

        // PriceRange=1 là "Dưới 100.000đ"; người mua đã kéo slider thì hai giá nhập tay phải thắng.
        var vm = await TimKiemAsync(db, new ProductSearchVm { PriceRange = 1, MinPrice = 150_000m, MaxPrice = 250_000m });

        Assert.Equal(1, vm.Result.TotalItems);
        Assert.Equal(200_000m, vm.Result.Items.Single().Price);
    }
}
