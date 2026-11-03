using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Notifications;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class NotificationServiceTests
{
    private const int Alice = 1, Bob = 2, LockedUser = 3;

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        db.NguoiDungs.AddRange(
            new NguoiDung { MaNguoiDung = Alice, HoTen = "Alice", Email = "alice@t.local", TrangThai = "ACTIVE" },
            new NguoiDung { MaNguoiDung = Bob, HoTen = "Bob", Email = "bob@t.local", TrangThai = "ACTIVE" },
            new NguoiDung { MaNguoiDung = LockedUser, HoTen = "Locked", Email = "locked@t.local", TrangThai = "LOCKED" });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Create_ValidInput_StoresUnreadNotificationWithUtcDate()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        var before = DateTime.UtcNow.AddMinutes(-1);

        var (success, error) = await service.CreateAsync(Alice, NotificationTypes.Order, "  Đơn mới  ", "Bạn có đơn hàng mới", 42);

        Assert.True(success);
        Assert.Null(error);
        var row = await db.ThongBaos.SingleAsync();
        Assert.Equal(Alice, row.MaNguoiDung);
        Assert.Equal("ORDER", row.Loai);
        Assert.Equal("Đơn mới", row.TieuDe);
        Assert.Equal(42, row.MaThamChieu);
        Assert.False(row.DaDoc);
        Assert.True(row.NgayTao > before);
    }

    [Theory]
    [InlineData("INVALID", "Tiêu đề", "Nội dung")]
    [InlineData("SYSTEM", "   ", "Nội dung")]
    [InlineData("SYSTEM", "Tiêu đề", "")]
    public async Task Create_InvalidInput_ReturnsErrorAndStoresNothing(string type, string title, string content)
    {
        using var db = NewDb();
        var service = new NotificationService(db);

        var (success, error) = await service.CreateAsync(Alice, type, title, content);

        Assert.False(success);
        Assert.False(string.IsNullOrEmpty(error));
        Assert.Empty(db.ThongBaos);
    }

    [Fact]
    public async Task Create_TitleOrContentTooLong_IsRejected()
    {
        using var db = NewDb();
        var service = new NotificationService(db);

        var longTitle = await service.CreateAsync(Alice, NotificationTypes.System, new string('a', 201), "ok");
        var longContent = await service.CreateAsync(Alice, NotificationTypes.System, "ok", new string('a', 1001));

        Assert.False(longTitle.Success);
        Assert.False(longContent.Success);
        Assert.Empty(db.ThongBaos);
    }

    [Fact]
    public async Task Create_UnknownRecipient_ReturnsError()
    {
        using var db = NewDb();
        var service = new NotificationService(db);

        var (success, _) = await service.CreateAsync(999, NotificationTypes.System, "Tiêu đề", "Nội dung");

        Assert.False(success);
        Assert.Empty(db.ThongBaos);
    }

    [Fact]
    public async Task GetForUser_ReturnsOnlyOwnNotificationsNewestFirst()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        db.ThongBaos.AddRange(
            new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "cũ", NoiDung = "x", NgayTao = DateTime.UtcNow.AddHours(-2) },
            new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "mới", NoiDung = "x", NgayTao = DateTime.UtcNow },
            new ThongBao { MaNguoiDung = Bob, Loai = "SYSTEM", TieuDe = "của Bob", NoiDung = "x", NgayTao = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var page = await service.GetForUserAsync(Alice);

        Assert.Equal(2, page.TotalItems);
        Assert.Equal(new[] { "mới", "cũ" }, page.Items.Select(x => x.TieuDe).ToArray());
        Assert.All(page.Items, x => Assert.Equal(Alice, x.MaNguoiDung));
    }

    [Fact]
    public async Task GetForUser_Paginates()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        for (var i = 0; i < 5; i++)
            db.ThongBaos.Add(new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = $"t{i}", NoiDung = "x", NgayTao = DateTime.UtcNow.AddMinutes(i) });
        await db.SaveChangesAsync();

        var page2 = await service.GetForUserAsync(Alice, page: 2, pageSize: 2);

        Assert.Equal(5, page2.TotalItems);
        Assert.Equal(2, page2.Items.Count);
    }

    [Fact]
    public async Task CountUnread_CountsOnlyOwnUnread()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        db.ThongBaos.AddRange(
            new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "a", NoiDung = "x", DaDoc = false },
            new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "b", NoiDung = "x", DaDoc = false },
            new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "c", NoiDung = "x", DaDoc = true },
            new ThongBao { MaNguoiDung = Bob, Loai = "SYSTEM", TieuDe = "d", NoiDung = "x", DaDoc = false });
        await db.SaveChangesAsync();

        Assert.Equal(2, await service.CountUnreadAsync(Alice));
        Assert.Equal(1, await service.CountUnreadAsync(Bob));
        Assert.Equal(0, await service.CountUnreadAsync(LockedUser));
    }

    [Fact]
    public async Task MarkAsRead_OwnNotification_SetsDaDocAndLowersUnreadCount()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        var n = new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "a", NoiDung = "x" };
        db.ThongBaos.Add(n);
        await db.SaveChangesAsync();

        var ok = await service.MarkAsReadAsync(Alice, n.MaThongBao);

        Assert.True(ok);
        Assert.True((await db.ThongBaos.AsNoTracking().SingleAsync()).DaDoc);
        Assert.Equal(0, await service.CountUnreadAsync(Alice));
    }

    [Fact]
    public async Task MarkAsRead_OtherUsersNotification_ReturnsFalseAndLeavesItUnread()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        var n = new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "a", NoiDung = "x" };
        db.ThongBaos.Add(n);
        await db.SaveChangesAsync();

        var ok = await service.MarkAsReadAsync(Bob, n.MaThongBao);

        Assert.False(ok);
        Assert.False((await db.ThongBaos.AsNoTracking().SingleAsync()).DaDoc);
    }

    [Fact]
    public async Task MarkAsRead_UnknownId_ReturnsFalse()
    {
        using var db = NewDb();
        var service = new NotificationService(db);

        Assert.False(await service.MarkAsReadAsync(Alice, 12345));
    }

    [Fact]
    public async Task CreateForAllUsers_SendsToActiveUsersOnly()
    {
        using var db = NewDb();
        var service = new NotificationService(db);

        var (success, error, sent) = await service.CreateForAllUsersAsync(NotificationTypes.System, "Bảo trì", "Hệ thống bảo trì 22h");

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(2, sent);
        var recipients = await db.ThongBaos.Select(x => x.MaNguoiDung).OrderBy(x => x).ToListAsync();
        Assert.Equal(new[] { Alice, Bob }, recipients.ToArray());
    }

    [Fact]
    public async Task CreateForAllUsers_InvalidInput_SendsNothing()
    {
        using var db = NewDb();
        var service = new NotificationService(db);

        var (success, _, sent) = await service.CreateForAllUsersAsync("BAD", "Tiêu đề", "Nội dung");

        Assert.False(success);
        Assert.Equal(0, sent);
        Assert.Empty(db.ThongBaos);
    }

    [Fact]
    public async Task Create_BoundaryLengths_AreAccepted()
    {
        using var db = NewDb();
        var service = new NotificationService(db);

        var (success, _) = await service.CreateAsync(Alice, NotificationTypes.System, new string('a', 200), new string('b', 1000));

        Assert.True(success);
        Assert.Single(db.ThongBaos);
    }

    [Fact]
    public async Task Create_DoesNotChangeOtherUsersUnreadCount()
    {
        using var db = NewDb();
        var service = new NotificationService(db);

        await service.CreateAsync(Alice, NotificationTypes.System, "Tiêu đề", "Nội dung");

        Assert.Equal(1, await service.CountUnreadAsync(Alice));
        Assert.Equal(0, await service.CountUnreadAsync(Bob));
    }

    [Fact]
    public async Task Create_WithPendingChangesInContext_ThrowsAndDoesNotSaveThem()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        var pending = new NguoiDung { MaNguoiDung = 10, HoTen = "Pending", Email = "p@t.local", TrangThai = "ACTIVE" };
        db.NguoiDungs.Add(pending);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(Alice, NotificationTypes.System, "Tiêu đề", "Nội dung"));

        Assert.Equal(EntityState.Added, db.Entry(pending).State);
        Assert.False(await db.NguoiDungs.AsNoTracking().AnyAsync(x => x.MaNguoiDung == 10));
        Assert.Empty(db.ThongBaos);
    }

    [Fact]
    public async Task MarkAsRead_WithPendingChangesInContext_ThrowsAndLeavesNotificationUnread()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        var n = new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "a", NoiDung = "x" };
        db.ThongBaos.Add(n);
        await db.SaveChangesAsync();
        db.NguoiDungs.Add(new NguoiDung { MaNguoiDung = 10, HoTen = "Pending", Email = "p@t.local", TrangThai = "ACTIVE" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.MarkAsReadAsync(Alice, n.MaThongBao));

        Assert.False((await db.ThongBaos.AsNoTracking().SingleAsync()).DaDoc);
    }

    [Fact]
    public async Task MarkAsRead_AlreadyRead_StillReturnsTrue()
    {
        using var db = NewDb();
        var service = new NotificationService(db);
        var n = new ThongBao { MaNguoiDung = Alice, Loai = "SYSTEM", TieuDe = "a", NoiDung = "x", DaDoc = true };
        db.ThongBaos.Add(n);
        await db.SaveChangesAsync();

        Assert.True(await service.MarkAsReadAsync(Alice, n.MaThongBao));
    }

    [Fact]
    public async Task CreateForAllUsers_WhenNoActiveUser_ReturnsErrorAndSendsNothing()
    {
        using var db = NewDb();
        foreach (var u in await db.NguoiDungs.ToListAsync()) u.TrangThai = "LOCKED";
        await db.SaveChangesAsync();
        var service = new NotificationService(db);

        var (success, _, sent) = await service.CreateForAllUsersAsync(NotificationTypes.System, "Tiêu đề", "Nội dung");

        Assert.False(success);
        Assert.Equal(0, sent);
        Assert.Empty(db.ThongBaos);
    }
}
