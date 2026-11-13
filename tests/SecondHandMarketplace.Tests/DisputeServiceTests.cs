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
    public async Task MoTranhChapAsync_BangChungAnhQuaDai_BaoLoi()
    {
        using var db = GetInMemoryDbContext("DisputeTest_AnhQuaDai");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Shop 1" };
        db.CuaHangs.Add(shop);

        db.DonHangs.Add(new DonHang
        {
            MaDonHang = 15,
            MaDon = "DH015",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "DELIVERED",
            CuaHang = shop
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.MoTranhChapAsync(20, new MoTranhChapViewModel
        {
            MaDonHang = 15,
            LyDo = "Hàng trầy xước",
            BangChungAnh = new string('x', 501)
        });

        Assert.False(thanhCong);
        Assert.Contains("không được vượt quá 500 ký tự", thongBao);
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

    [Fact]
    public async Task XuLyTranhChapAdminAsync_RefundBuyer_DonCompleted_SanPhamSoldChuyenLaiActive()
    {
        using var db = GetInMemoryDbContext("DisputeTest_RefundCompletedOrder");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        var sp = new SanPham
        {
            MaSanPham = 50,
            MaCuaHang = 1,
            Ten = "Tai nghe Bluetooth",
            Gia = 300000,
            TrangThai = "SOLD", // Đã bán vì đơn COMPLETED
            DaXoa = false
        };
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);

        var don = new DonHang
        {
            MaDonHang = 20,
            MaDon = "DH020",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "COMPLETED",
            TrangThaiThanhToan = "PAID",
            TongTien = 300000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.ChiTietDonHangs.Add(new ChiTietDonHang
        {
            MaChiTietDonHang = 1,
            MaDonHang = 20,
            MaSanPham = 50,
            TenSanPham = "Tai nghe Bluetooth",
            Gia = 300000,
            DangGiuCho = false,
            SanPham = sp
        });

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 6,
            MaDonHang = 20,
            MaNguoiTao = 20,
            LyDo = "Hàng bị lỗi sau 1 ngày sử dụng",
            TrangThai = "PROCESSING",
            DonHang = don
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyTranhChapAdminAsync(6, 999, "REFUND_BUYER", "Hoàn tiền cho khách");
        Assert.True(thanhCong);

        // Đơn bị hủy, tiền được hoàn
        Assert.Equal("CANCELLED", don.TrangThai);
        Assert.Equal("REFUNDED", don.TrangThaiThanhToan);

        // Sản phẩm SOLD được khôi phục về ACTIVE
        var spSauXuLy = await db.SanPhams.FindAsync(50);
        Assert.Equal("ACTIVE", spSauXuLy!.TrangThai);

        // Lịch sử trạng thái sản phẩm được ghi nhận
        var lichSuSp = await db.LichSuTrangThaiSanPhams.FirstOrDefaultAsync(l => l.MaSanPham == 50);
        Assert.NotNull(lichSuSp);
        Assert.Equal("SOLD", lichSuSp.TrangThaiCu);
        Assert.Equal("ACTIVE", lichSuSp.TrangThaiMoi);
    }

    [Fact]
    public async Task XuLyTranhChapAdminAsync_PaySeller_CapNhatTrangThaiCompletedVaSold()
    {
        using var db = GetInMemoryDbContext("DisputeTest_PaySeller");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        var sp = new SanPham
        {
            MaSanPham = 55,
            MaCuaHang = 1,
            Ten = "Đồng hồ thông minh",
            Gia = 500000,
            TrangThai = "RESERVED",
            DaXoa = false
        };
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);

        var don = new DonHang
        {
            MaDonHang = 30,
            MaDon = "DH030",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "SHIPPING",
            TrangThaiThanhToan = "UNPAID",
            TongTien = 500000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.ChiTietDonHangs.Add(new ChiTietDonHang
        {
            MaChiTietDonHang = 2,
            MaDonHang = 30,
            MaSanPham = 55,
            TenSanPham = "Đồng hồ thông minh",
            Gia = 500000,
            DangGiuCho = true,
            SanPham = sp
        });

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 7,
            MaDonHang = 30,
            MaNguoiTao = 20,
            LyDo = "Buyer khiếu nại vô căn cứ",
            TrangThai = "PROCESSING",
            DonHang = don
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyTranhChapAdminAsync(7, 999, "RELEASE_SELLER", "Người bán đúng, chuyển tiền");
        Assert.True(thanhCong);

        // Đơn chuyển sang COMPLETED, tiền PAID
        Assert.Equal("COMPLETED", don.TrangThai);
        Assert.Equal("PAID", don.TrangThaiThanhToan);

        // Sản phẩm chuyển sang SOLD
        var spSauXuLy = await db.SanPhams.FindAsync(55);
        Assert.Equal("SOLD", spSauXuLy!.TrangThai);
    }

    [Fact]
    public async Task XuLyTranhChapAdminAsync_Reject_GiuNguyenTrangThaiDonHang()
    {
        using var db = GetInMemoryDbContext("DisputeTest_Reject");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);

        var don = new DonHang
        {
            MaDonHang = 40,
            MaDon = "DH040",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "DELIVERED",
            TrangThaiThanhToan = "UNPAID",
            TongTien = 150000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 8,
            MaDonHang = 40,
            MaNguoiTao = 20,
            LyDo = "Khiếu nại không hợp lý",
            TrangThai = "PROCESSING",
            DonHang = don
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyTranhChapAdminAsync(8, 999, "REJECT", "Bác bỏ");
        Assert.True(thanhCong);

        var tc = await db.TranhChaps.FindAsync(8);
        Assert.Equal("REJECTED", tc!.TrangThai);

        // Trạng thái đơn giữ nguyên DELIVERED
        Assert.Equal("DELIVERED", don.TrangThai);
    }

    [Fact]
    public async Task MoTranhChapAsync_DonDaCoTranhChapResolved_KhongChoMoLai()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChongMoSauResolved");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);

        var don = new DonHang
        {
            MaDonHang = 50,
            MaDon = "DH050",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "DELIVERED",
            TrangThaiThanhToan = "PAID",
            TongTien = 100000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 20,
            MaDonHang = 50,
            MaNguoiTao = 20,
            LyDo = "Đã giải quyết xong trước đó",
            TrangThai = "RESOLVED"
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.MoTranhChapAsync(20, new MoTranhChapViewModel
        {
            MaDonHang = 50,
            LyDo = "Muốn mở khiếu nại lần nữa"
        });

        Assert.False(thanhCong);
        Assert.Contains("không thể mở lại", thongBao);
    }

    [Fact]
    public async Task XuLyTranhChapAdminAsync_Refund_ResetDangGiuChoVaPhucHoiSanPham()
    {
        using var db = GetInMemoryDbContext("DisputeTest_RefundResetDangGiuCho");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);

        var sp = new SanPham
        {
            MaSanPham = 60,
            MaCuaHang = 1,
            Ten = "Áo khoác",
            Gia = 200000,
            TrangThai = "SOLD"
        };
        db.SanPhams.Add(sp);

        var don = new DonHang
        {
            MaDonHang = 60,
            MaDon = "DH060",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "DELIVERED",
            TrangThaiThanhToan = "PAID",
            TongTien = 200000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.ChiTietDonHangs.Add(new ChiTietDonHang
        {
            MaDonHang = 60,
            MaSanPham = 60,
            Gia = 200000,
                        DangGiuCho = true
        });

        db.ThanhToans.Add(new ThanhToan
        {
            MaDonHang = 60,
            SoTien = 200000,
            PhuongThucThanhToan = "ONLINE",
            TrangThaiThanhToan = "SUCCESS"
        });

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 25,
            MaDonHang = 60,
            MaNguoiTao = 20,
            LyDo = "Hàng bị rách",
            TrangThai = "PROCESSING",
            DonHang = don
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyTranhChapAdminAsync(25, 999, "REFUND_BUYER", "Hoàn tiền cho khách");
        Assert.True(thanhCong);

        var donDb = await db.DonHangs.FindAsync(60);
        Assert.Equal("CANCELLED", donDb!.TrangThai);
        Assert.Equal("REFUNDED", donDb.TrangThaiThanhToan);

        var ctDb = await db.ChiTietDonHangs.FirstAsync(c => c.MaDonHang == 60);
        Assert.False(ctDb.DangGiuCho);

        var spDb = await db.SanPhams.FindAsync(60);
        Assert.Equal("ACTIVE", spDb!.TrangThai);

        var ttDb = await db.ThanhToans.FirstAsync(t => t.MaDonHang == 60);
        Assert.Equal("REFUNDED", ttDb.TrangThaiThanhToan);
    }

    [Fact]
    public async Task XuLyTranhChapAdminAsync_PaySeller_DonDaCompleted_KhongLoi()
    {
        using var db = GetInMemoryDbContext("DisputeTest_PaySellerCompleted");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);

        var don = new DonHang
        {
            MaDonHang = 70,
            MaDon = "DH070",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "COMPLETED",
            TrangThaiThanhToan = "PAID",
            TongTien = 120000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 26,
            MaDonHang = 70,
            MaNguoiTao = 20,
            LyDo = "Khiếu nại sau khi hoàn tất",
            TrangThai = "PROCESSING",
            DonHang = don
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyTranhChapAdminAsync(26, 999, "PAY_SELLER", "Duyệt trả tiền người bán");
        Assert.True(thanhCong);

        var tcDb = await db.TranhChaps.FindAsync(26);
        Assert.Equal("RESOLVED", tcDb!.TrangThai);

        var donDb = await db.DonHangs.FindAsync(70);
        Assert.Equal("COMPLETED", donDb!.TrangThai);
    }

    [Fact]
    public async Task MoTranhChapAsync_DonDaCoTranhChapRejected_KhongChoMoLai()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChongMoSauRejected");
        var fakeNotify = new FakeNotificationService();
        var fakeAudit = new FakeAuditLogService();
        var service = new DisputeService(db, fakeNotify, fakeAudit);

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);

        var don = new DonHang
        {
            MaDonHang = 51,
            MaDon = "DH051",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "DELIVERED",
            TrangThaiThanhToan = "PAID",
            TongTien = 100000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);

        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 21,
            MaDonHang = 51,
            MaNguoiTao = 20,
            LyDo = "Đã bị admin bác bỏ trước đó",
            TrangThai = "REJECTED"
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.MoTranhChapAsync(20, new MoTranhChapViewModel
        {
            MaDonHang = 51,
            LyDo = "Cố mở khiếu nại lần 2 sau khi bị REJECT"
        });

        Assert.False(thanhCong);
        Assert.Contains("không thể mở lại", thongBao);
    }

    [Fact]
    public async Task ChuanBiMoTranhChapAsync_DonHangKhongTonTai_TraVeFalse()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChuanBi_DonNotFound");
        var service = new DisputeService(db, new FakeNotificationService(), new FakeAuditLogService());

        var (hopLe, thongBao, vm, maTc) = await service.ChuanBiMoTranhChapAsync(20, 9999);
        Assert.False(hopLe);
        Assert.Contains("Không tìm thấy đơn hàng", thongBao);
        Assert.Null(vm);
    }

    [Fact]
    public async Task ChuanBiMoTranhChapAsync_DonHangKhongDungTrangThai_TraVeFalse()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChuanBi_TrangThaiSai");
        var service = new DisputeService(db, new FakeNotificationService(), new FakeAuditLogService());

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);
        var don = new DonHang
        {
            MaDonHang = 80,
            MaDon = "DH080",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "PENDING",
            TongTien = 100000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var (hopLe, thongBao, vm, maTc) = await service.ChuanBiMoTranhChapAsync(20, 80);
        Assert.False(hopLe);
        Assert.Contains("Chỉ có thể mở khiếu nại", thongBao);
        Assert.Null(vm);
    }

    [Fact]
    public async Task ChuanBiMoTranhChapAsync_DonHangDaCoTranhChap_TraVeFalseVaMaTranhChap()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChuanBi_DaCoTranhChap");
        var service = new DisputeService(db, new FakeNotificationService(), new FakeAuditLogService());

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop" };
        db.CuaHangs.Add(shop);
        var don = new DonHang
        {
            MaDonHang = 81,
            MaDon = "DH081",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "DELIVERED",
            TongTien = 100000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);
        db.TranhChaps.Add(new TranhChap
        {
            MaTranhChap = 88,
            MaDonHang = 81,
            MaNguoiTao = 20,
            LyDo = "Lý do cũ",
            TrangThai = "OPEN"
        });
        await db.SaveChangesAsync();

        var (hopLe, thongBao, vm, maTc) = await service.ChuanBiMoTranhChapAsync(20, 81);
        Assert.False(hopLe);
        Assert.Contains("Đơn hàng này đã có khiếu nại", thongBao);
        Assert.Equal(88, maTc);
        Assert.Null(vm);
    }

    [Fact]
    public async Task ChuanBiMoTranhChapAsync_HopLe_TraVeTrueVaViewModel()
    {
        using var db = GetInMemoryDbContext("DisputeTest_ChuanBi_HopLe");
        var service = new DisputeService(db, new FakeNotificationService(), new FakeAuditLogService());

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 30, TenShop = "Test Shop 1" };
        db.CuaHangs.Add(shop);
        var don = new DonHang
        {
            MaDonHang = 82,
            MaDon = "DH082",
            MaNguoiDung = 20,
            MaCuaHang = 1,
            TrangThai = "DELIVERED",
            TongTien = 250000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var (hopLe, thongBao, vm, maTc) = await service.ChuanBiMoTranhChapAsync(20, 82);
        Assert.True(hopLe);
        Assert.Null(thongBao);
        Assert.NotNull(vm);
        Assert.Equal(82, vm!.MaDonHang);
        Assert.Equal("DH082", vm.MaDon);
        Assert.Equal("Test Shop 1", vm.TenShop);
        Assert.Equal(250000, vm.TongTien);
        Assert.Null(maTc);
    }
}
