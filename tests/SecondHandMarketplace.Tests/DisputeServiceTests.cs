using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Disputes;
using SecondHandMarketplace.Web.Services.Disputes;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class DisputeServiceTests
{
    private AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task MoTranhChapAsync_ChanKhiKhongPhaiNguoiMua()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChanNguoiKhac");
        var service = new DisputeService(db);

        db.DonHangs.Add(new DonHang
        {
            MaDonHang = 1,
            MaDon = "DH001",
            MaNguoiDung = 20, // Người mua thật
            MaCuaHang = 1,
            TenNguoiNhan = "A",
            SoDienThoaiNhan = "0901234567",
            DiaChiNhan = "HCM",
            TienHang = 100000,
            TongTien = 100000
        });
        await db.SaveChangesAsync();

        // User 99 (không phải người mua) cố mở tranh chấp
        var (thanhCong, thongBao) = await service.MoTranhChapAsync(99, new MoTranhChapViewModel
        {
            MaDonHang = 1,
            LyDo = "Hàng vỡ"
        });

        Assert.False(thanhCong);
        Assert.Contains("không có quyền", thongBao);
    }

    [Fact]
    public async Task NguoiBanPhanHoiAsync_CapNhatThanhCong()
    {
        using var db = GetInMemoryDbContext("DisputeTest_SellerReply");
        var service = new DisputeService(db);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);

        var don = new DonHang
        {
            MaDonHang = 1,
            MaDon = "DH001",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TenNguoiNhan = "A",
            SoDienThoaiNhan = "0901234567",
            DiaChiNhan = "HCM",
            TienHang = 100000,
            TongTien = 100000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 1,
            MaDonHang = 1,
            MaNguoiTao = 20,
            LyDo = "Hàng trầy xước",
            TrangThai = "OPEN",
            DonHang = don
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.NguoiBanPhanHoiAsync(30, 1, "Shop đã kiểm tra trước khi gửi");
        Assert.True(thanhCong);

        var tc = await db.TranhChaps.FindAsync(1);
        Assert.Equal("PROCESSING", tc!.TrangThai);
        Assert.Equal("Shop đã kiểm tra trước khi gửi", tc.PhanHoiNguoiBan);
    }
}
