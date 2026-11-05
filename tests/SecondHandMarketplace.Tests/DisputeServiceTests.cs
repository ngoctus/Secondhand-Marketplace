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
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Shop 1" };
        db.CuaHangs.Add(shop);

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
            TongTien = 100000,
            TrangThai = "DELIVERED",
            CuaHang = shop
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
    public async Task MoTranhChapAsync_ChanKhiTrangThaiDonKhongHopLe()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChanTrangThaiDon");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Shop 1" };
        db.CuaHangs.Add(shop);

        // Đơn hàng đang ở trạng thái PENDING (chưa giao, chưa nhận)
        db.DonHangs.Add(new DonHang
        {
            MaDonHang = 2,
            MaDon = "DH002",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TenNguoiNhan = "A",
            SoDienThoaiNhan = "0901234567",
            DiaChiNhan = "HCM",
            TienHang = 100000,
            TongTien = 100000,
            TrangThai = "PENDING",
            CuaHang = shop
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.MoTranhChapAsync(20, new MoTranhChapViewModel
        {
            MaDonHang = 2,
            LyDo = "Hàng chưa giao nhưng muốn khiếu nại"
        });

        Assert.False(thanhCong);
        Assert.Contains("Chỉ có thể mở khiếu nại", thongBao);
    }

    [Fact]
    public async Task MoTranhChapAsync_ChanKhiLyDoRong()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChanLyDoRong");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var (thanhCong, thongBao) = await service.MoTranhChapAsync(20, new MoTranhChapViewModel
        {
            MaDonHang = 1,
            LyDo = "   "
        });

        Assert.False(thanhCong);
        Assert.Contains("nhập lý do", thongBao);
    }

    [Fact]
    public async Task MoTranhChapAsync_ChongMoTrungTranhChap()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChongTrung");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Shop 1" };
        db.CuaHangs.Add(shop);

        var don = new DonHang
        {
            MaDonHang = 3,
            MaDon = "DH003",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TenNguoiNhan = "A",
            SoDienThoaiNhan = "0901234567",
            DiaChiNhan = "HCM",
            TienHang = 100000,
            TongTien = 100000,
            TrangThai = "DELIVERED",
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 10,
            MaDonHang = 3,
            MaNguoiTao = 20,
            LyDo = "Lần 1",
            TrangThai = "OPEN"
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.MoTranhChapAsync(20, new MoTranhChapViewModel
        {
            MaDonHang = 3,
            LyDo = "Lần 2 khi đang có OPEN"
        });

        Assert.False(thanhCong);
        Assert.Contains("đang chờ xử lý", thongBao);
    }

    [Fact]
    public async Task NguoiBanPhanHoiAsync_CapNhatThanhCongVaGuiThongBao()
    {
        using var db = GetInMemoryDbContext("DisputeTest_SellerReply");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

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

        // Kiểm tra đã gửi thông báo cho người mua
        Assert.Contains(fakeNotify.Notifications, n => n.UserId == 20);
    }

    [Fact]
    public async Task XuLyTranhChapAdminAsync_RefundBuyer_CapNhatTrangThaiDonVaHoanTien()
    {
        using var db = GetInMemoryDbContext("DisputeTest_RefundBuyer");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);

        var don = new DonHang
        {
            MaDonHang = 10,
            MaDon = "DH010",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TenNguoiNhan = "A",
            SoDienThoaiNhan = "0901234567",
            DiaChiNhan = "HCM",
            TienHang = 200000,
            TongTien = 200000,
            TrangThai = "DELIVERED",
            TrangThaiThanhToan = "UNPAID",
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 5,
            MaDonHang = 10,
            MaNguoiTao = 20,
            LyDo = "Hàng hỏng hoàn toàn",
            TrangThai = "PROCESSING",
            DonHang = don
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyTranhChapAdminAsync(5, 999, "REFUND_BUYER", "Chấp thuận hoàn tiền");
        Assert.True(thanhCong);

        var tc = await db.TranhChaps.FindAsync(5);
        Assert.Equal("RESOLVED", tc!.TrangThai);
        Assert.Equal("CANCELLED", don.TrangThai);
        Assert.Equal("REFUNDED", don.TrangThaiThanhToan);

        // Kiểm tra audit log và notification
        Assert.Contains(fakeAudit.Logs, l => l.Action == "RESOLVE_DISPUTE");
        Assert.Contains(fakeNotify.Notifications, n => n.UserId == 20);
        Assert.Contains(fakeNotify.Notifications, n => n.UserId == 30);
    }
}
