using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Cart;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Services.Carts;
using SecondHandMarketplace.Web.Services.Orders;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class FakeCartService : ICartService
{
    public CartIndexViewModel GioHangReturn { get; set; } = new();

    public Task<CartIndexViewModel> LayGioHangAsync(int maNguoiDung) => Task.FromResult(GioHangReturn);
    public Task<(bool ThanhCong, string? ThongBao)> ThemVaoGioAsync(int maNguoiDung, int maSanPham) => Task.FromResult((true, (string?)"OK"));
    public Task<bool> CapNhatTrangThaiChonAsync(int maNguoiDung, int maGioHang, bool daChon) => Task.FromResult(true);
    public Task<bool> CapNhatChonTatCaTheoShopAsync(int maNguoiDung, int maCuaHang, bool daChon) => Task.FromResult(true);
    public Task<bool> XoaKhoiGioAsync(int maNguoiDung, int maGioHang) => Task.FromResult(true);
    public Task<int> DemSoLuongMonAsync(int maNguoiDung) => Task.FromResult(0);
}

public class OrderServiceTests
{
    private AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ChuanBiCheckoutTuGioAsync_LayDungThongTinNguoiNhanVaTienHang()
    {
        using var db = GetInMemoryDbContext("OrderTest_CheckoutTuGio");
        var fakeCart = new FakeCartService();

        db.NguoiDungs.Add(new NguoiDung
        {
            MaNguoiDung = 1,
            HoTen = "Nguyen Van A",
            SoDienThoai = "0987654321",
            DiaChiMacDinh = "123 Le Loi, Q1"
        });
        await db.SaveChangesAsync();

        fakeCart.GioHangReturn = new CartIndexViewModel
        {
            DanhSachNhomShop = new List<CartShopGroupViewModel>
            {
                new()
                {
                    MaCuaHang = 10,
                    TenShop = "Shop A",
                    Items = new List<CartItemViewModel>
                    {
                        new() { MaSanPham = 100, TenSanPham = "Laptop Cu", Gia = 5000000, DaChon = true, CoTheMua = true }
                    }
                }
            }
        };

        var service = new OrderService(db, fakeCart);
        var result = await service.ChuanBiCheckoutTuGioAsync(1);

        Assert.Equal("Nguyen Van A", result.TenNguoiNhan);
        Assert.Equal("0987654321", result.SoDienThoaiNhan);
        Assert.Equal("123 Le Loi, Q1", result.DiaChiNhan);
        Assert.Equal(5000000, result.TienHang);
    }

    [Fact]
    public async Task ChuanBiCheckoutMuaNgayAsync_KhongChoPhepMuaSanPhamCuaChinhShopMinh()
    {
        using var db = GetInMemoryDbContext("OrderTest_MuaNgayCuaMinh");
        var fakeCart = new FakeCartService();

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 10, TenShop = "Shop Cua Toi", TrangThai = "ACTIVE" };
        var sp = new SanPham
        {
            MaSanPham = 200,
            MaCuaHang = 1,
            Ten = "May Doc Sach",
            Gia = 1500000,
            TrangThai = "ACTIVE",
            DaXoa = false,
            CuaHang = shop
        };
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var result = await service.ChuanBiCheckoutMuaNgayAsync(10, 200);

        Assert.Null(result); // Trả về null vì không được mua sản phẩm của shop mình
    }

    [Fact]
    public async Task HuyDonMuaAsync_LyDoRong_TraVeLoi()
    {
        using var db = GetInMemoryDbContext("OrderTest_HuyDonRong");
        var fakeCart = new FakeCartService();

        var service = new OrderService(db, fakeCart);
        var (thanhCong, thongBao) = await service.HuyDonMuaAsync(1, 10, "   ");

        Assert.False(thanhCong);
        Assert.Equal("Vui lòng nhập lý do hủy đơn.", thongBao);
    }

    [Fact]
    public async Task MuaNgayAsync_HaiBuyerDuaKhoCungMotSanPham_BuyerSauBiChan()
    {
        using var db = GetInMemoryDbContext("OrderTest_DuaKho");
        var fakeCart = new FakeCartService();

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 99, TenShop = "Tech Store", TrangThai = "ACTIVE" };
        var sp = new SanPham
        {
            MaSanPham = 500,
            MaCuaHang = 1,
            Ten = "iPhone 13 Pro Max",
            Gia = 15000000,
            TrangThai = "ACTIVE",
            DaXoa = false,
            CuaHang = shop
        };
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);

        var checkoutVm = new CheckoutViewModel
        {
            TenNguoiNhan = "Buyer Test",
            SoDienThoaiNhan = "0901234567",
            DiaChiNhan = "123 Nguyen Hue",
            PhuongThucThanhToan = "COD"
        };

        // Buyer 1 (id 1) đặt mua trước
        var res1 = await service.MuaNgayAsync(1, 500, checkoutVm);
        Assert.True(res1.ThanhCong);
        Assert.NotEmpty(res1.DanhSachMaDon);

        // Kiểm tra sản phẩm đã chuyển trạng thái sang SOLD
        var spSauKhiMua = await db.SanPhams.FindAsync(500);
        Assert.Equal("SOLD", spSauKhiMua!.TrangThai);

        // Buyer 2 (id 2) cũng cố mua ngay sản phẩm 500
        var res2 = await service.MuaNgayAsync(2, 500, checkoutVm);
        Assert.False(res2.ThanhCong);
        Assert.Empty(res2.DanhSachMaDon);
        Assert.Contains("không tồn tại hoặc đã được bán", res2.ThongBao);
    }

    [Fact]
    public async Task HuyDonMuaAsync_DonDaHoanTat_KhongChoHuy()
    {
        using var db = GetInMemoryDbContext("OrderTest_HuyDonSaiTrangThai");
        var fakeCart = new FakeCartService();

        var don = new DonHang
        {
            MaDonHang = 10,
            MaNguoiDung = 1,
            MaCuaHang = 2,
            TrangThai = "COMPLETED",
            TrangThaiThanhToan = "PAID",
            TongTien = 200000,
            NgayTao = DateTime.UtcNow
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var (ok, msg) = await service.HuyDonMuaAsync(1, 10, "Đổi ý muốn hủy");

        Assert.False(ok);
        Assert.Contains("Chỉ có thể hủy đơn hàng khi đơn ở trạng thái chờ duyệt hoặc đã xác nhận", msg);
    }

    [Fact]
    public async Task XacNhanDaNhanHangAsync_DonChuaGiao_KhongChoXacNhan()
    {
        using var db = GetInMemoryDbContext("OrderTest_NhanHangSaiTrangThai");
        var fakeCart = new FakeCartService();

        var don = new DonHang
        {
            MaDonHang = 20,
            MaNguoiDung = 1,
            MaCuaHang = 2,
            TrangThai = "SHIPPING", // Chưa DELIVERED
            TrangThaiThanhToan = "PAID",
            TongTien = 500000,
            NgayTao = DateTime.UtcNow
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var (ok, msg) = await service.XacNhanDaNhanHangAsync(1, 20);

        Assert.False(ok);
        Assert.Contains("Chỉ có thể xác nhận khi đơn hàng đã được giao đến bạn", msg);
    }
}
