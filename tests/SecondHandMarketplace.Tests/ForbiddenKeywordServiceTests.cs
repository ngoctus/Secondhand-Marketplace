using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Moderation;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>AddAsync gọi sp_ThemTuKhoaCam (SQL Server) nên chỉ kiểm phần validate ở đây; phần procedure nằm trong SQL smoke test.</summary>
public class ForbiddenKeywordServiceTests
{
    private sealed class NoAudit : IAuditLogService
    {
        public Task LogAsync(int? maNguoiDung, string hanhDong, string loaiDoiTuong, int? doiTuongId = null,
            string ketQua = "SUCCESS", string? lyDo = null, string? chiTiet = null) => Task.CompletedTask;
    }

    private static (AppDbContext Db, ForbiddenKeywordService Service) New()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.TuKhoaCams.AddRange(
            new TuKhoaCam { MaTuKhoaCam = 1, CumTu = "hàng giả", CumTuChuanHoa = "hang gia", Nhom = "HANG_GIA", MucDo = "HIGH", DangApDung = true },
            new TuKhoaCam { MaTuKhoaCam = 2, CumTu = "đặt cọc trước", CumTuChuanHoa = "dat coc truoc", Nhom = "LUA_DAO", MucDo = "LOW", DangApDung = true },
            new TuKhoaCam { MaTuKhoaCam = 3, CumTu = "trúng thưởng", CumTuChuanHoa = "trung thuong", Nhom = "LUA_DAO", MucDo = "LOW", DangApDung = false });
        db.SaveChanges();
        return (db, new ForbiddenKeywordService(db, new NoAudit()));
    }

    [Fact]
    public async Task List_FiltersByLevelActiveAndSearch_HighFirst()
    {
        var (_, s) = New();
        var all = await s.ListAsync();
        Assert.Equal(3, all.Count);
        Assert.Equal("HIGH", all[0].Level);

        Assert.Single(await s.ListAsync(level: "high"));
        Assert.Equal(2, (await s.ListAsync(active: true)).Count);
        Assert.Equal(2, (await s.ListAsync(search: "LUA_DAO")).Count);
        Assert.Single(await s.ListAsync(search: "hàng"));
    }

    [Fact]
    public async Task SetActive_TogglesAndReportsMissing()
    {
        var (db, s) = New();
        Assert.True((await s.SetActiveAsync(1, false, 1)).Success);
        Assert.False((await db.TuKhoaCams.AsNoTracking().SingleAsync(k => k.MaTuKhoaCam == 1)).DangApDung);
        Assert.True((await s.SetActiveAsync(3, true, 1)).Success);
        Assert.True((await db.TuKhoaCams.AsNoTracking().SingleAsync(k => k.MaTuKhoaCam == 3)).DangApDung);
        Assert.False((await s.SetActiveAsync(99, true, 1)).Success);
    }

    [Fact]
    public async Task Update_ChangesGroupLevelNote_ButNotPhrase()
    {
        var (db, s) = New();
        var r = await s.UpdateAsync(2, " hang_cam ", "high", "  rất nghiêm trọng ", 1);
        Assert.True(r.Success);
        var k = await db.TuKhoaCams.AsNoTracking().SingleAsync(x => x.MaTuKhoaCam == 2);
        Assert.Equal(("HANG_CAM", "HIGH", "rất nghiêm trọng", "đặt cọc trước"), (k.Nhom, k.MucDo, k.GhiChu, k.CumTu));
    }

    [Theory]
    [InlineData("MEDIUM", null, null)]
    [InlineData("", null, null)]
    [InlineData("LOW", "nhóm có dấu", null)]
    [InlineData("LOW", "ABCDEFGHIJKLMNOPQRSTUVWXYZ_0123456789", null)]   // > 30 ký tự
    public async Task Update_InvalidInput_IsRejectedAndChangesNothing(string level, string? group, string? note)
    {
        var (db, s) = New();
        var r = await s.UpdateAsync(1, group, level, note, 1);
        Assert.False(r.Success);
        Assert.Equal("HIGH", (await db.TuKhoaCams.AsNoTracking().SingleAsync(x => x.MaTuKhoaCam == 1)).MucDo);
    }

    [Theory]
    [InlineData("", "LOW")]
    [InlineData("   ", "HIGH")]
    [InlineData("hợp lệ", "URGENT")]
    public async Task Add_InvalidInput_IsRejectedBeforeTouchingTheProcedure(string phrase, string level)
    {
        var (db, s) = New();
        var r = await s.AddAsync(phrase, null, level, null, 1);
        Assert.False(r.Success);
        Assert.Equal(3, await db.TuKhoaCams.CountAsync());
    }
}
