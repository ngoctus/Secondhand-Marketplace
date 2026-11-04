using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Cart;
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
}
