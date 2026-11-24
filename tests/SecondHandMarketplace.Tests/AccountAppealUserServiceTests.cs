using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Accounts;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class AccountAppealUserServiceTests
{
    private static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetLockedAccountOverviewAsync_UserNotFound_ReturnsNull()
    {
        using var db = CreateInMemoryDb(nameof(GetLockedAccountOverviewAsync_UserNotFound_ReturnsNull));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var overview = await service.GetLockedAccountOverviewAsync(9999);

        Assert.Null(overview);
    }

    [Fact]
    public async Task GetLockedAccountOverviewAsync_UserLockedTier2_ReturnsOverviewWithVietQr()
    {
        using var db = CreateInMemoryDb(nameof(GetLockedAccountOverviewAsync_UserLockedTier2_ReturnsOverviewWithVietQr));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 20,
            HoTen = "Locked User",
            Email = "locked@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 2,
            NgayTao = DateTime.UtcNow.AddMonths(-1)
        };
        db.NguoiDungs.Add(user);
        db.CauHinhKhieuNaiTaiKhoans.Add(new CauHinhKhieuNaiTaiKhoan
        {
            Id = 1,
            SoLanGuiMoiChuKy = 2,
            CooldownPhut = 4320,
            PhiMoKhoaLan2 = 200_000,
            TenNganHang = "MB Bank",
            MaNganHang = "MB",
            SoTaiKhoan = "0987654321",
            ChuTaiKhoan = "CONG TY SECONDHAND"
        });
        await db.SaveChangesAsync();

        var overview = await service.GetLockedAccountOverviewAsync(20);

        Assert.NotNull(overview);
        Assert.True(overview.DangBiKhoa);
        Assert.Equal(2, overview.LanKhoaHienTai);
        Assert.True(overview.YeuCauPhiLan2);
        Assert.NotNull(overview.ThongTinPhiQr);
        Assert.Equal(200_000, overview.ThongTinPhiQr.SoTien);
        Assert.Contains("20", overview.ThongTinPhiQr.NoiDungChuyenKhoan);
        Assert.NotNull(overview.ThongTinPhiQr.QrDataUrl);
        Assert.StartsWith("data:image/png;base64,", overview.ThongTinPhiQr.QrDataUrl);
    }

    [Fact]
    public async Task SubmitAppealAsync_UserNotLocked_ReturnsError()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_UserNotLocked_ReturnsError));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 21,
            HoTen = "Active User",
            Email = "active@test.com",
            TrangThai = "ACTIVE"
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        var result = await service.SubmitAppealAsync(21, AccountAppealRules.TypeAppeal, "Tôi cam kết tài khoản của tôi không hề vi phạm chính sách", null);

        Assert.False(result.Success);
        Assert.Contains("không ở trạng thái bị khóa", result.Error);
    }

    [Fact]
    public async Task SubmitAppealAsync_Tier1Appeal_SuccessWithoutFee()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_Tier1Appeal_SuccessWithoutFee));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 22,
            HoTen = "Tier 1 User",
            Email = "tier1@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 1
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        var result = await service.SubmitAppealAsync(22, AccountAppealRules.TypeAppeal, "Tôi xin giải trình lý do bị khóa nhầm do nhầm lẫn hình ảnh sản phẩm", null);

        Assert.True(result.Success);
        Assert.True(result.MaKhieuNai.HasValue);

        var appealInDb = await db.KhieuNaiTaiKhoans.FindAsync(result.MaKhieuNai.Value);
        Assert.NotNull(appealInDb);
        Assert.Equal("PENDING", appealInDb.TrangThai);
        Assert.Equal(AccountAppealRules.TypeAppeal, appealInDb.LoaiYeuCau);
        Assert.Equal(0, appealInDb.SoTienPhi);
        Assert.Equal(1, appealInDb.LanKhoaThu);
    }

    [Fact]
    public async Task SubmitAppealAsync_Tier2UnlockRequest_WithoutProofImage_Fails()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_Tier2UnlockRequest_WithoutProofImage_Fails));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 23,
            HoTen = "Tier 2 User",
            Email = "tier2@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 2
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        var result = await service.SubmitAppealAsync(23, AccountAppealRules.TypeUnlockRequest, "Tôi xin cam kết không tái phạm các hành vi vi phạm này", null);

        Assert.False(result.Success);
        Assert.Contains("yêu cầu đính kèm ảnh biên lai", result.Error);
    }

    [Fact]
    public async Task SubmitAppealAsync_Tier2UnlockRequest_WithProofImage_SuccessWithFee()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_Tier2UnlockRequest_WithProofImage_SuccessWithFee));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 24,
            HoTen = "Tier 2 User Paid",
            Email = "tier2paid@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 2
        };
        db.NguoiDungs.Add(user);
        db.CauHinhKhieuNaiTaiKhoans.Add(new CauHinhKhieuNaiTaiKhoan
        {
            Id = 1,
            PhiMoKhoaLan2 = 200_000
        });
        await db.SaveChangesAsync();

        var result = await service.SubmitAppealAsync(24, AccountAppealRules.TypeUnlockRequest, "Tôi xin cam kết không tái phạm và đã hoàn thành chuyển khoản", "/uploads/appeals/receipt.jpg");

        Assert.True(result.Success);
        Assert.True(result.MaKhieuNai.HasValue);

        var appealInDb = await db.KhieuNaiTaiKhoans.FindAsync(result.MaKhieuNai.Value);
        Assert.NotNull(appealInDb);
        Assert.Equal("PENDING", appealInDb.TrangThai);
        Assert.Equal(200_000, appealInDb.SoTienPhi);
        Assert.Equal("/uploads/appeals/receipt.jpg", appealInDb.BangChungAnh);
    }

    [Fact]
    public async Task SubmitAppealAsync_Tier3UnlockRequest_FailsBlocked()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_Tier3UnlockRequest_FailsBlocked));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 25,
            HoTen = "Tier 3 User",
            Email = "tier3@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 3
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        var result = await service.SubmitAppealAsync(25, AccountAppealRules.TypeUnlockRequest, "Tôi xin gửi cam kết mở khóa tài khoản lần thứ 3 của tôi", "/uploads/appeals/receipt.jpg");

        Assert.False(result.Success);
        Assert.Contains("Tài khoản đã bị khóa từ 3 lần trở lên", result.Error);
    }

    [Fact]
    public async Task SubmitAppealAsync_DuplicatePendingAppeal_Fails()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_DuplicatePendingAppeal_Fails));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 26,
            HoTen = "User With Pending",
            Email = "pending@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 1
        };
        db.NguoiDungs.Add(user);
        db.KhieuNaiTaiKhoans.Add(new KhieuNaiTaiKhoan
        {
            MaKhieuNai = 101,
            MaNguoiDung = 26,
            LoaiYeuCau = AccountAppealRules.TypeAppeal,
            TrangThai = "PENDING",
            NoiDung = "Đơn đầu tiên đã gửi giải trình",
            LanKhoaThu = 1,
            NgayTao = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await service.SubmitAppealAsync(26, AccountAppealRules.TypeAppeal, "Đơn thứ 2 khi đơn 1 đang chờ xử lý từ admin", null);

        Assert.False(result.Success);
        Assert.Contains("đang có một yêu cầu đang chờ quản trị viên xử lý", result.Error);
    }

    [Fact]
    public async Task SubmitAppealAsync_CooldownActive_Fails()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_CooldownActive_Fails));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 27,
            HoTen = "User In Cooldown",
            Email = "cooldown@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 1
        };
        db.NguoiDungs.Add(user);
        db.CauHinhKhieuNaiTaiKhoans.Add(new CauHinhKhieuNaiTaiKhoan
        {
            Id = 1,
            SoLanGuiMoiChuKy = 2,
            CooldownPhut = 4320
        });

        // 2 đơn bị REJECTED gần đây (cách đây 1 giờ)
        db.KhieuNaiTaiKhoans.Add(new KhieuNaiTaiKhoan
        {
            MaKhieuNai = 102,
            MaNguoiDung = 27,
            LoaiYeuCau = AccountAppealRules.TypeAppeal,
            TrangThai = "REJECTED",
            NoiDung = "Đơn 1 bị từ chối giải trình vi phạm",
            LanKhoaThu = 1,
            NgayTao = DateTime.UtcNow.AddHours(-2),
            NgayXuLy = DateTime.UtcNow.AddHours(-1)
        });
        db.KhieuNaiTaiKhoans.Add(new KhieuNaiTaiKhoan
        {
            MaKhieuNai = 103,
            MaNguoiDung = 27,
            LoaiYeuCau = AccountAppealRules.TypeAppeal,
            TrangThai = "REJECTED",
            NoiDung = "Đơn 2 bị từ chối giải trình vi phạm",
            LanKhoaThu = 1,
            NgayTao = DateTime.UtcNow.AddMinutes(-50),
            NgayXuLy = DateTime.UtcNow.AddMinutes(-30)
        });
        await db.SaveChangesAsync();

        var result = await service.SubmitAppealAsync(27, AccountAppealRules.TypeAppeal, "Đơn 3 khi đang trong thời gian chờ cooldown", null);

        Assert.False(result.Success);
        Assert.Contains("Bạn đã gửi đủ số lượt trong chu kỳ hiện tại", result.Error);
    }

    [Fact]
    public async Task SubmitAppealAsync_Tier2Appeal_FreeAndNoProofRequired()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_Tier2Appeal_FreeAndNoProofRequired));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 28,
            HoTen = "Tier 2 Appeal Free",
            Email = "tier2appeal@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 2
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        // Gửi APPEAL ở lần khóa 2: Miễn phí, không cần ảnh
        var result = await service.SubmitAppealAsync(28, AccountAppealRules.TypeAppeal, "Tôi bị khóa nhầm sản phẩm lần 2, xin xem xét minh oan", null);

        Assert.True(result.Success);
        Assert.True(result.MaKhieuNai.HasValue);

        var appeal = await db.KhieuNaiTaiKhoans.FindAsync(result.MaKhieuNai.Value);
        Assert.NotNull(appeal);
        Assert.Equal(0, appeal.SoTienPhi);
        Assert.False(appeal.YeuCauPhi);
        Assert.Null(appeal.BangChungAnh);
        Assert.Equal("PENDING", appeal.TrangThai);
    }

    [Fact]
    public async Task SubmitAppealAsync_Tier3Appeal_Allowed()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_Tier3Appeal_Allowed));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 29,
            HoTen = "Tier 3 User Appeal",
            Email = "tier3appeal@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 3
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        // Gửi APPEAL ở lần khóa 3: Được phép (khác với UNLOCK_REQUEST bị chặn)
        var result = await service.SubmitAppealAsync(29, AccountAppealRules.TypeAppeal, "Tài khoản bị khóa lần 3 nhưng do quét nhầm bài đăng hợp lệ", null);

        Assert.True(result.Success);
        Assert.True(result.MaKhieuNai.HasValue);

        var appeal = await db.KhieuNaiTaiKhoans.FindAsync(result.MaKhieuNai.Value);
        Assert.NotNull(appeal);
        Assert.Equal(3, appeal.LanKhoaThu);
        Assert.Equal(AccountAppealRules.TypeAppeal, appeal.LoaiYeuCau);
        Assert.Equal("PENDING", appeal.TrangThai);
    }

    [Fact]
    public async Task SubmitAppealAsync_AfterCooldownExpired_AllowsSubmission()
    {
        using var db = CreateInMemoryDb(nameof(SubmitAppealAsync_AfterCooldownExpired_AllowsSubmission));
        var fakeAudit = new FakeAuditLogService();
        var service = new AccountAppealUserService(db, fakeAudit, NullLogger<AccountAppealUserService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 30,
            HoTen = "User Expired Cooldown",
            Email = "expired_cooldown@test.com",
            TrangThai = "LOCKED",
            SoLanBiKhoa = 1
        };
        db.NguoiDungs.Add(user);
        db.CauHinhKhieuNaiTaiKhoans.Add(new CauHinhKhieuNaiTaiKhoan
        {
            Id = 1,
            SoLanGuiMoiChuKy = 2,
            CooldownPhut = 4320 // 3 ngày
        });

        // 2 đơn bị REJECTED từ 4 ngày trước (Cooldown 3 ngày đã hết)
        db.KhieuNaiTaiKhoans.Add(new KhieuNaiTaiKhoan
        {
            MaKhieuNai = 201,
            MaNguoiDung = 30,
            LoaiYeuCau = AccountAppealRules.TypeAppeal,
            TrangThai = "REJECTED",
            NoiDung = "Đơn 1 cũ bị từ chối 5 ngày trước",
            LanKhoaThu = 1,
            NgayTao = DateTime.UtcNow.AddDays(-5),
            NgayXuLy = DateTime.UtcNow.AddDays(-5)
        });
        db.KhieuNaiTaiKhoans.Add(new KhieuNaiTaiKhoan
        {
            MaKhieuNai = 202,
            MaNguoiDung = 30,
            LoaiYeuCau = AccountAppealRules.TypeAppeal,
            TrangThai = "REJECTED",
            NoiDung = "Đơn 2 cũ bị từ chối 4 ngày trước",
            LanKhoaThu = 1,
            NgayTao = DateTime.UtcNow.AddDays(-4),
            NgayXuLy = DateTime.UtcNow.AddDays(-4)
        });
        await db.SaveChangesAsync();

        // Gửi đơn mới khi Cooldown đã hết -> Thành công, mở chu kỳ mới!
        var result = await service.SubmitAppealAsync(30, AccountAppealRules.TypeAppeal, "Đơn mới gửi sau khi hết thời gian chờ Cooldown", null);

        Assert.True(result.Success);
        Assert.True(result.MaKhieuNai.HasValue);

        var appeal = await db.KhieuNaiTaiKhoans.FindAsync(result.MaKhieuNai.Value);
        Assert.NotNull(appeal);
        Assert.Equal("PENDING", appeal.TrangThai);
    }
}
