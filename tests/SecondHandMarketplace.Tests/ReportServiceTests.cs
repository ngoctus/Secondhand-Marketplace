using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Reports;
using SecondHandMarketplace.Web.Services.Reports;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class ReportServiceTests
{
    private AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task TaoBaoCaoAsync_ChongGuiTrungKhiDangPending()
    {
        using var db = GetInMemoryDbContext("ReportTest_ChongTrung");
        var service = new ReportService(db, new FakeUserAdminService(), new FakeShopService(), new FakeAuditLogService(), new FakeNotificationService());

        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 1,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Lừa đảo",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.TaoBaoCaoAsync(5, new TaoBaoCaoViewModel
        {
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Lừa đảo lần 2"
        });

        Assert.False(thanhCong);
        Assert.Contains("đang chờ", thongBao);
    }

    [Fact]
    public async Task TaoBaoCaoAsync_LoaiDoiTuongChuThuong_VanChongTrung()
    {
        using var db = GetInMemoryDbContext("ReportTest_ChongTrungChuThuong");
        var service = new ReportService(db, new FakeUserAdminService(), new FakeShopService(), new FakeAuditLogService(), new FakeNotificationService());

        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 1,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Hàng giả",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        // Gửi loại đối tượng bằng chữ thường "product"
        var (thanhCong, thongBao) = await service.TaoBaoCaoAsync(5, new TaoBaoCaoViewModel
        {
            LoaiDoiTuong = "product",
            MaDoiTuong = 100,
            LyDo = "Hàng giả lần nữa"
        });

        Assert.False(thanhCong);
        Assert.Contains("đang chờ", thongBao);
    }

    [Fact]
    public async Task TaoBaoCaoAsync_LyDoRong_TraVeLoi()
    {
        using var db = GetInMemoryDbContext("ReportTest_LyDoRong");
        var service = new ReportService(db, new FakeUserAdminService(), new FakeShopService(), new FakeAuditLogService(), new FakeNotificationService());

        var (thanhCong, thongBao) = await service.TaoBaoCaoAsync(5, new TaoBaoCaoViewModel
        {
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "   "
        });

        Assert.False(thanhCong);
        Assert.Equal("Vui lòng nhập lý do báo cáo.", thongBao);
    }

    [Fact]
    public async Task TaoBaoCaoAsync_LyDoQuaDai_TraVeLoi()
    {
        using var db = GetInMemoryDbContext("ReportTest_LyDoQuaDai");
        var service = new ReportService(db, new FakeUserAdminService(), new FakeShopService(), new FakeAuditLogService(), new FakeNotificationService());

        var (thanhCong, thongBao) = await service.TaoBaoCaoAsync(5, new TaoBaoCaoViewModel
        {
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = new string('A', 256)
        });

        Assert.False(thanhCong);
        Assert.Equal("Lý do báo cáo không được vượt quá 255 ký tự.", thongBao);
    }

    [Fact]
    public async Task TaoBaoCaoAsync_MoTaQuaDai_TraVeLoi()
    {
        using var db = GetInMemoryDbContext("ReportTest_MoTaQuaDai");
        var service = new ReportService(db, new FakeUserAdminService(), new FakeShopService(), new FakeAuditLogService(), new FakeNotificationService());

        var (thanhCong, thongBao) = await service.TaoBaoCaoAsync(5, new TaoBaoCaoViewModel
        {
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Hàng giả",
            MoTa = new string('B', 1001)
        });

        Assert.False(thanhCong);
        Assert.Equal("Mô tả chi tiết không được vượt quá 1000 ký tự.", thongBao);
    }

    [Fact]
    public async Task XuLyBaoCaoAdminAsync_DismissThanhCong()
    {
        using var db = GetInMemoryDbContext("ReportTest_Dismiss");
        var fakeAudit = new FakeAuditLogService();
        var fakeNotify = new FakeNotificationService();
        var service = new ReportService(db, new FakeUserAdminService(), new FakeShopService(), fakeAudit, fakeNotify);

        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 1,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Spam",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyBaoCaoAdminAsync(1, 999, "DISMISS", "Không vi phạm");
        Assert.True(thanhCong);

        var bc = await db.BaoCaos.FindAsync(1);
        Assert.Equal("DISMISSED", bc!.TrangThai);
        Assert.Equal("Không vi phạm", bc.GhiChuXuLy);
        Assert.Equal(999, bc.NguoiXuLyId);

        // Audit log and notification to reporter
        Assert.Contains(fakeAudit.Logs, l => l.Action == "DISMISS_REPORT" && l.TargetId == 1 && l.Result == "SUCCESS");
        Assert.Contains(fakeNotify.Notifications, n => n.UserId == 5 && n.Title.Contains("xem xét báo cáo"));
    }

    [Fact]
    public async Task XuLyBaoCaoAdminAsync_ResolveUser_GoiUserAdminServiceVaGuiThongBao()
    {
        using var db = GetInMemoryDbContext("ReportTest_ResolveUser");
        var fakeUserAdmin = new FakeUserAdminService();
        var fakeAudit = new FakeAuditLogService();
        var fakeNotify = new FakeNotificationService();
        var service = new ReportService(db, fakeUserAdmin, new FakeShopService(), fakeAudit, fakeNotify);

        db.NguoiDungs.Add(new NguoiDung
        {
            MaNguoiDung = 20,
            HoTen = "User Vi Pham",
            Email = "vipham@example.com",
            TrangThai = "ACTIVE"
        });
        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 2,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "USER",
            MaDoiTuong = 20,
            LyDo = "Lừa đảo chiếm đoạt",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyBaoCaoAdminAsync(2, 999, "RESOLVE", "Khóa tài khoản do vi phạm nghiêm trọng");
        Assert.True(thanhCong);

        // UserAdminService was called to lock user
        Assert.Single(fakeUserAdmin.LockedUsers);
        Assert.Equal(20, fakeUserAdmin.LockedUsers[0].UserId);
        Assert.Equal(999, fakeUserAdmin.LockedUsers[0].ActorId);

        // Audit log & notifications
        Assert.Contains(fakeAudit.Logs, l => l.Action == "RESOLVE_REPORT" && l.TargetId == 2 && l.Result == "SUCCESS");
        Assert.Contains(fakeNotify.Notifications, n => n.UserId == 5); // Reporter
        Assert.Contains(fakeNotify.Notifications, n => n.UserId == 20); // Violated User
    }

    [Fact]
    public async Task XuLyBaoCaoAdminAsync_LockUserThatBai_TraVeLoiVaAuditFailed()
    {
        using var db = GetInMemoryDbContext("ReportTest_LockUserFail");
        var fakeUserAdmin = new FakeUserAdminService { LockResult = (false, "Không thể khóa tài khoản ADMIN.") };
        var fakeAudit = new FakeAuditLogService();
        var fakeNotify = new FakeNotificationService();
        var service = new ReportService(db, fakeUserAdmin, new FakeShopService(), fakeAudit, fakeNotify);

        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 10,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "USER",
            MaDoiTuong = 1,
            LyDo = "Tố cáo Admin",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.XuLyBaoCaoAdminAsync(10, 999, "RESOLVE", "Yêu cầu khóa");
        Assert.False(thanhCong);
        Assert.Equal("Không thể khóa tài khoản ADMIN.", thongBao);

        // Báo cáo không được đánh dấu RESOLVED
        var bc = await db.BaoCaos.FindAsync(10);
        Assert.Equal("PENDING", bc!.TrangThai);

        // Ghi audit log FAILED
        Assert.Contains(fakeAudit.Logs, l => l.Action == "RESOLVE_REPORT" && l.TargetId == 10 && l.Result == "FAILED");
    }

    [Fact]
    public async Task XuLyBaoCaoAdminAsync_ResolveShop_GoiShopServiceVaGuiThongBao()
    {
        using var db = GetInMemoryDbContext("ReportTest_ResolveShop");
        var fakeShop = new FakeShopService();
        var fakeAudit = new FakeAuditLogService();
        var fakeNotify = new FakeNotificationService();
        var service = new ReportService(db, new FakeUserAdminService(), fakeShop, fakeAudit, fakeNotify);

        db.CuaHangs.Add(new CuaHang
        {
            MaCuaHang = 30,
            MaNguoiDung = 15,
            TenShop = "Shop Lua Dao",
            TrangThai = "ACTIVE"
        });
        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 3,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "SHOP",
            MaDoiTuong = 30,
            LyDo = "Bán hàng cấm",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyBaoCaoAdminAsync(3, 999, "RESOLVE", "Khóa shop vi phạm chính sách");
        Assert.True(thanhCong);

        // ShopService was called to lock shop
        Assert.Single(fakeShop.LockedShops);
        Assert.Equal(30, fakeShop.LockedShops[0].ShopId);
        Assert.Equal(999, fakeShop.LockedShops[0].ActorId);

        // Audit log & notifications
        Assert.Contains(fakeAudit.Logs, l => l.Action == "RESOLVE_REPORT" && l.TargetId == 3 && l.Result == "SUCCESS");
        Assert.Contains(fakeNotify.Notifications, n => n.UserId == 5); // Reporter
        Assert.Contains(fakeNotify.Notifications, n => n.UserId == 15); // Shop owner
    }

    [Fact]
    public async Task XuLyBaoCaoAdminAsync_LockShopThatBai_TraVeLoiVaAuditFailed()
    {
        using var db = GetInMemoryDbContext("ReportTest_LockShopFail");
        var fakeShop = new FakeShopService { LockResult = (false, "Không tìm thấy cửa hàng.") };
        var fakeAudit = new FakeAuditLogService();
        var fakeNotify = new FakeNotificationService();
        var service = new ReportService(db, new FakeUserAdminService(), fakeShop, fakeAudit, fakeNotify);

        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 11,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "SHOP",
            MaDoiTuong = 9999,
            LyDo = "Shop không tồn tại",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.XuLyBaoCaoAdminAsync(11, 999, "RESOLVE", "Khóa shop");
        Assert.False(thanhCong);
        Assert.Equal("Không tìm thấy cửa hàng.", thongBao);

        // Báo cáo không được đánh dấu RESOLVED
        var bc = await db.BaoCaos.FindAsync(11);
        Assert.Equal("PENDING", bc!.TrangThai);

        // Ghi audit log FAILED
        Assert.Contains(fakeAudit.Logs, l => l.Action == "RESOLVE_REPORT" && l.TargetId == 11 && l.Result == "FAILED");
    }

    [Fact]
    public async Task XuLyBaoCaoAdminAsync_Dismiss_GhiChuQuaDai_TraVeLoi()
    {
        using var db = GetInMemoryDbContext("ReportTest_DismissGhiChuQuaDai");
        var service = new ReportService(db, new FakeUserAdminService(), new FakeShopService(), new FakeAuditLogService(), new FakeNotificationService());

        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 15,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Vi phạm bản quyền",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        string ghiChuQuaDai = new string('x', 501);
        var (thanhCong, thongBao) = await service.XuLyBaoCaoAdminAsync(15, 999, "DISMISS", ghiChuQuaDai);

        Assert.False(thanhCong);
        Assert.Contains("không được vượt quá 500 ký tự", thongBao);

        var bc = await db.BaoCaos.FindAsync(15);
        Assert.Equal("PENDING", bc!.TrangThai);
    }
}
