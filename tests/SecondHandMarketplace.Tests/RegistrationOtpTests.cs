using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Account;
using SecondHandMarketplace.Web.Services.Auth;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class FakeEmailSender : IEmailSender
{
    public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public class FakeAuthService : IAuthService
{
    public List<NguoiDung> Users { get; } = new();

    public Task<NguoiDung?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = Users.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant());
        return Task.FromResult(user);
    }

    public Task<AuthStatusResult> AuthenticateWithStatusAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = Users.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant());
        if (user is null) return Task.FromResult(AuthStatusResult.InvalidCredentials());
        if (string.Equals(user.TrangThai, "LOCKED", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthStatusResult.AccountLocked(user));
        return Task.FromResult(AuthStatusResult.Success(user));
    }

    public Task<(bool Success, string? Error, NguoiDung? User)> RegisterAsync(string fullName, string email, string phone, string password, CancellationToken cancellationToken = default)
    {
        var normEmail = email.Trim().ToLowerInvariant();
        if (Users.Any(u => u.Email == normEmail))
            return Task.FromResult<(bool, string?, NguoiDung?)>((false, "Email đã được đăng ký.", null));

        var user = new NguoiDung
        {
            MaNguoiDung = Users.Count + 1,
            HoTen = fullName.Trim(),
            Email = normEmail,
            SoDienThoai = phone.Trim(),
            VaiTro = "USER",
            TrangThai = "ACTIVE"
        };
        Users.Add(user);
        return Task.FromResult<(bool, string?, NguoiDung?)>((true, null, user));
    }

    public Task<NguoiDung?> GetUserAsync(int userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.MaNguoiDung == userId));

    public Task<(bool Success, string? Error)> UpdateProfileAsync(int userId, string fullName, string? phone, string? address, string? avatar = null, CancellationToken cancellationToken = default) =>
        Task.FromResult((true, (string?)null));

    public Task<(bool Success, string? Error)> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default) =>
        Task.FromResult((true, (string?)null));
}

public class RegistrationOtpTests
{
    private static AppDbContext GetDb(string name) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options);

    private static IDistributedCache CreateCache() => new TestDistributedCache();

    [Fact]
    public async Task StartRegistrationAsync_ValidModel_ReturnsTokenAndSendsEmail()
    {
        using var db = GetDb(nameof(StartRegistrationAsync_ValidModel_ReturnsTokenAndSendsEmail));
        var emailSender = new FakeEmailSender();
        var dataProtection = new EphemeralDataProtectionProvider();
        var authService = new FakeAuthService();
        var service = new RegistrationOtpService(db, emailSender, dataProtection, authService, CreateCache(), NullLogger<RegistrationOtpService>.Instance);

        var vm = new RegisterVm
        {
            FullName = "Nguyễn Văn A",
            Email = "nguyenvana@example.com",
            Phone = "0901234567",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        };

        var (success, error, token) = await service.StartRegistrationAsync(vm);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(token);
        Assert.Single(emailSender.SentEmails);
        Assert.Equal("nguyenvana@example.com", emailSender.SentEmails[0].To);
        Assert.Contains("Mã xác nhận", emailSender.SentEmails[0].Subject);
    }

    [Fact]
    public async Task StartRegistrationAsync_EmailAlreadyExists_ReturnsError()
    {
        using var db = GetDb(nameof(StartRegistrationAsync_EmailAlreadyExists_ReturnsError));
        db.NguoiDungs.Add(new NguoiDung
        {
            MaNguoiDung = 1,
            Email = "ton-tai@example.com",
            HoTen = "User Cũ",
            SoDienThoai = "0901111111",
            MatKhauBam = "hash",
            VaiTro = "USER",
            TrangThai = "ACTIVE"
        });
        await db.SaveChangesAsync();

        var emailSender = new FakeEmailSender();
        var dataProtection = new EphemeralDataProtectionProvider();
        var authService = new FakeAuthService();
        var service = new RegistrationOtpService(db, emailSender, dataProtection, authService, CreateCache(), NullLogger<RegistrationOtpService>.Instance);

        var vm = new RegisterVm
        {
            FullName = "User Mới",
            Email = "ton-tai@example.com",
            Phone = "0902222222",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        };

        var (success, error, token) = await service.StartRegistrationAsync(vm);

        Assert.False(success);
        Assert.Contains("Email này đã được sử dụng", error);
        Assert.Null(token);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task VerifyOtpAsync_CorrectCode_RegistersUserSuccessfully()
    {
        using var db = GetDb(nameof(VerifyOtpAsync_CorrectCode_RegistersUserSuccessfully));
        var emailSender = new FakeEmailSender();
        var dataProtection = new EphemeralDataProtectionProvider();
        var authService = new FakeAuthService();
        var service = new RegistrationOtpService(db, emailSender, dataProtection, authService, CreateCache(), NullLogger<RegistrationOtpService>.Instance);

        var vm = new RegisterVm
        {
            FullName = "Trần Thị B",
            Email = "tranthib@example.com",
            Phone = "0912345678",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        };

        var startResult = await service.StartRegistrationAsync(vm);
        Assert.True(startResult.Success);

        // Trích xuất mã OTP từ nội dung email gửi đi
        var body = emailSender.SentEmails[0].Body;
        var startIdx = body.IndexOf("dashed #68705A'>") + "dashed #68705A'>".Length;
        var code = body.Substring(startIdx, 6);

        var (success, error, user, newToken) = await service.VerifyOtpAsync(startResult.Token!, code);

        Assert.True(success);
        Assert.Null(error);
        Assert.NotNull(user);
        Assert.Equal("tranthib@example.com", user.Email);
        Assert.Null(newToken);
        Assert.Single(authService.Users);
    }

    [Fact]
    public async Task VerifyOtpAsync_WrongCode_ReturnsErrorAndDecrementsAttempts()
    {
        using var db = GetDb(nameof(VerifyOtpAsync_WrongCode_ReturnsErrorAndDecrementsAttempts));
        var emailSender = new FakeEmailSender();
        var dataProtection = new EphemeralDataProtectionProvider();
        var authService = new FakeAuthService();
        var service = new RegistrationOtpService(db, emailSender, dataProtection, authService, CreateCache(), NullLogger<RegistrationOtpService>.Instance);

        var vm = new RegisterVm
        {
            FullName = "Lê Văn C",
            Email = "levanc@example.com",
            Phone = "0934567890",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        };

        var startResult = await service.StartRegistrationAsync(vm);
        Assert.True(startResult.Success);

        var (success, error, user, newToken) = await service.VerifyOtpAsync(startResult.Token!, "000000");

        Assert.False(success);
        Assert.Contains("Mã OTP không chính xác", error);
        Assert.Contains("4 lần thử", error);
        Assert.Null(user);
        Assert.NotNull(newToken);
        Assert.Empty(authService.Users);
    }

    [Fact]
    public async Task StartRegistrationAsync_WhenOperationCanceled_ReturnsFriendlyErrorWithoutCrashing()
    {
        using var db = GetDb(nameof(StartRegistrationAsync_WhenOperationCanceled_ReturnsFriendlyErrorWithoutCrashing));
        var emailSender = new ThrowingEmailSender(new OperationCanceledException());
        var dataProtection = new EphemeralDataProtectionProvider();
        var authService = new FakeAuthService();
        var service = new RegistrationOtpService(db, emailSender, dataProtection, authService, CreateCache(), NullLogger<RegistrationOtpService>.Instance);

        var vm = new RegisterVm
        {
            FullName = "Nguyễn Văn D",
            Email = "nguyenvand@example.com",
            Phone = "0945678901",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        };

        var (success, error, token) = await service.StartRegistrationAsync(vm);

        Assert.False(success);
        Assert.NotNull(error);
        Assert.Contains("quá thời gian chờ", error);
        Assert.Null(token);
    }

    [Fact]
    public async Task StartRegistrationAsync_RapidSuccessiveCalls_ReusesTokenAndPreventsDuplicateEmail()
    {
        using var db = GetDb(nameof(StartRegistrationAsync_RapidSuccessiveCalls_ReusesTokenAndPreventsDuplicateEmail));
        var emailSender = new FakeEmailSender();
        var dataProtection = new EphemeralDataProtectionProvider();
        var authService = new FakeAuthService();
        var cache = CreateCache();
        var service = new RegistrationOtpService(db, emailSender, dataProtection, authService, cache, NullLogger<RegistrationOtpService>.Instance);

        var vm = new RegisterVm
        {
            FullName = "Nguyễn Văn Đúp",
            Email = "dup@example.com",
            Phone = "0987654321",
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        };

        var firstCall = await service.StartRegistrationAsync(vm);
        var secondCall = await service.StartRegistrationAsync(vm);

        Assert.True(firstCall.Success);
        Assert.True(secondCall.Success);
        Assert.Equal(firstCall.Token, secondCall.Token);
        Assert.Single(emailSender.SentEmails); // Chỉ gửi 1 email, không bị 2 mã OTP
    }
}

public class ThrowingEmailSender(Exception exceptionToThrow) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        throw exceptionToThrow;
    }
}
