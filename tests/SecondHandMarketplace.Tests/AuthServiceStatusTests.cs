using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Security;
using SecondHandMarketplace.Web.Services.Auth;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class AuthServiceStatusTests
{
    private AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AuthenticateWithStatusAsync_EmailKhongTonTai_TraVeInvalidCredentials()
    {
        AuthService.ResetLockouts();
        using var db = CreateDbContext("AuthStatus_EmailNotFound");
        var passwordService = new MatKhauService();
        var fakeAudit = new FakeAuditLogService();
        var authService = new AuthService(db, passwordService, fakeAudit, NullLogger<AuthService>.Instance);

        var result = await authService.AuthenticateWithStatusAsync("notfound@example.com", "Password123!");

        Assert.Equal(AuthResultStatus.InvalidCredentials, result.Status);
        Assert.Null(result.User);
        Assert.Contains("Email hoặc mật khẩu không chính xác", result.ErrorMessage);
    }

    [Fact]
    public async Task AuthenticateWithStatusAsync_DungEmail_SaiMatKhau_TaiKhoanActive_TraVeInvalidCredentials()
    {
        AuthService.ResetLockouts();
        using var db = CreateDbContext("AuthStatus_WrongPassword_Active");
        var passwordService = new MatKhauService();
        var fakeAudit = new FakeAuditLogService();
        var authService = new AuthService(db, passwordService, fakeAudit, NullLogger<AuthService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 1,
            Email = "active_user@example.com",
            HoTen = "Active User",
            MatKhauBam = passwordService.Bam("CorrectPassword123!"),
            TrangThai = "ACTIVE"
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        var result = await authService.AuthenticateWithStatusAsync("active_user@example.com", "WrongPassword!");

        Assert.Equal(AuthResultStatus.InvalidCredentials, result.Status);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task AuthenticateWithStatusAsync_DungEmail_SaiMatKhau_TaiKhoanLocked_VanTraVeInvalidCredentials_ChongLoEmail()
    {
        AuthService.ResetLockouts();
        using var db = CreateDbContext("AuthStatus_WrongPassword_Locked");
        var passwordService = new MatKhauService();
        var fakeAudit = new FakeAuditLogService();
        var authService = new AuthService(db, passwordService, fakeAudit, NullLogger<AuthService>.Instance);

        var lockedUser = new NguoiDung
        {
            MaNguoiDung = 2,
            Email = "locked_user@example.com",
            HoTen = "Locked User",
            MatKhauBam = passwordService.Bam("CorrectPassword123!"),
            TrangThai = "LOCKED",
            LyDoKhoa = "Vi phạm nhiều lần"
        };
        db.NguoiDungs.Add(lockedUser);
        await db.SaveChangesAsync();

        // Gõ SAI mật khẩu của tài khoản bị khóa -> Bắt buộc phải trả InvalidCredentials chứ KHÔNG được trả AccountLocked
        var result = await authService.AuthenticateWithStatusAsync("locked_user@example.com", "RandomGuess!");

        Assert.Equal(AuthResultStatus.InvalidCredentials, result.Status);
        Assert.Null(result.User);
    }

    [Fact]
    public async Task AuthenticateWithStatusAsync_DungMatKhau_TaiKhoanLocked_TraVeAccountLocked()
    {
        AuthService.ResetLockouts();
        using var db = CreateDbContext("AuthStatus_CorrectPassword_Locked");
        var passwordService = new MatKhauService();
        var fakeAudit = new FakeAuditLogService();
        var authService = new AuthService(db, passwordService, fakeAudit, NullLogger<AuthService>.Instance);

        var lockedUser = new NguoiDung
        {
            MaNguoiDung = 3,
            Email = "locked_correct@example.com",
            HoTen = "Locked User 3",
            MatKhauBam = passwordService.Bam("ValidPassword123!"),
            TrangThai = "LOCKED",
            LyDoKhoa = "Đã bị khóa do nhận 3 gậy kiểm duyệt",
            SoLanBiKhoa = 1
        };
        db.NguoiDungs.Add(lockedUser);
        await db.SaveChangesAsync();

        // Gõ ĐÚNG mật khẩu của tài khoản bị khóa -> Trả AccountLocked kèm thông tin User
        var result = await authService.AuthenticateWithStatusAsync("locked_correct@example.com", "ValidPassword123!");

        Assert.Equal(AuthResultStatus.AccountLocked, result.Status);
        Assert.NotNull(result.User);
        Assert.Equal(3, result.User.MaNguoiDung);
        Assert.Equal("LOCKED", result.User.TrangThai);
    }

    [Fact]
    public async Task AuthenticateWithStatusAsync_DungMatKhau_TaiKhoanActive_TraVeSuccess()
    {
        AuthService.ResetLockouts();
        using var db = CreateDbContext("AuthStatus_CorrectPassword_Active");
        var passwordService = new MatKhauService();
        var fakeAudit = new FakeAuditLogService();
        var authService = new AuthService(db, passwordService, fakeAudit, NullLogger<AuthService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 4,
            Email = "normal_active@example.com",
            HoTen = "Normal Active User",
            MatKhauBam = passwordService.Bam("MySecurePass123!"),
            TrangThai = "ACTIVE"
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        var result = await authService.AuthenticateWithStatusAsync("normal_active@example.com", "MySecurePass123!");

        Assert.Equal(AuthResultStatus.Success, result.Status);
        Assert.NotNull(result.User);
        Assert.Equal("normal_active@example.com", result.User.Email);
    }

    [Fact]
    public async Task AuthenticateWithStatusAsync_SaiMatKhau5Lan_TraVeTempLockout()
    {
        AuthService.ResetLockouts();
        using var db = CreateDbContext("AuthStatus_LockoutTest");
        var passwordService = new MatKhauService();
        var fakeAudit = new FakeAuditLogService();
        var authService = new AuthService(db, passwordService, fakeAudit, NullLogger<AuthService>.Instance);

        var user = new NguoiDung
        {
            MaNguoiDung = 5,
            Email = "lockout_target@example.com",
            HoTen = "Lockout Target",
            MatKhauBam = passwordService.Bam("ActualPassword123!"),
            TrangThai = "ACTIVE"
        };
        db.NguoiDungs.Add(user);
        await db.SaveChangesAsync();

        // 4 lần đầu: InvalidCredentials
        for (int i = 0; i < 4; i++)
        {
            var res = await authService.AuthenticateWithStatusAsync("lockout_target@example.com", "WrongPass");
            Assert.Equal(AuthResultStatus.InvalidCredentials, res.Status);
        }

        // Lần thứ 5: kích hoạt TempLockout
        var fifthResult = await authService.AuthenticateWithStatusAsync("lockout_target@example.com", "WrongPass");
        Assert.Equal(AuthResultStatus.TempLockout, fifthResult.Status);
        Assert.NotNull(fifthResult.LockoutEnd);
        Assert.True(fifthResult.LockoutEnd > DateTime.UtcNow);

        // Lần thứ 6 (kể cả gõ đúng pass trong lúc đang bị khóa tạm): Vẫn trả TempLockout
        var sixthResult = await authService.AuthenticateWithStatusAsync("lockout_target@example.com", "ActualPassword123!");
        Assert.Equal(AuthResultStatus.TempLockout, sixthResult.Status);
    }
}
