using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Accounts;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.Notifications;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// Kiểm thử ModerationService bằng EF InMemory (giống các test khác của repo).
/// LƯU Ý: InMemory KHÔNG thực thi UNIQUE/CHECK/trigger/khóa dòng của SQL Server. Các bảo đảm ở tầng database
/// được kiểm bằng database/tests/c2_smoke_test.sql. Ở đây kiểm logic C# (idempotent, đúng nhánh HIGH/LOW, khóa, FAILED...).
/// </summary>
public class ModerationServiceTests
{
    private const int AdminId = 1, SellerId = 10, ShopId = 1;

    private sealed class FakeAudit : IAuditLogService
    {
        public List<string> Actions { get; } = new();
        public Task LogAsync(int? maNguoiDung, string hanhDong, string loaiDoiTuong, int? doiTuongId = null,
            string ketQua = "SUCCESS", string? lyDo = null, string? chiTiet = null)
        {
            Actions.Add(hanhDong);
            return Task.CompletedTask;
        }
    }

    private sealed class ScriptedScanner : IKeywordScanner
    {
        public List<KeywordMatch> Hits { get; set; } = new();
        public bool Throw { get; set; }
        public int Calls { get; private set; }

        public Task<IReadOnlyList<KeywordMatch>> ScanAsync(string? name, string? description, CancellationToken ct = default)
        {
            Calls++;
            if (Throw) throw new InvalidOperationException("Mất kết nối khi quét từ khóa");
            return Task.FromResult<IReadOnlyList<KeywordMatch>>(Hits.ToList());
        }
    }

    private sealed class Host
    {
        public required string Store { get; init; }
        public required AppDbContext Db { get; init; }
        public required ScriptedScanner Scanner { get; init; }
        public required FakeAudit Audit { get; init; }
        public required ModerationService Service { get; init; }
        public required UserAdminService UserAdmin { get; init; }
    }

    private static DbContextOptions<AppDbContext> Options(string store) =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(store)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

    private static Host NewHost(string? sharedStore = null, ScriptedScanner? scanner = null)
    {
        var store = sharedStore ?? Guid.NewGuid().ToString();
        var db = new AppDbContext(Options(store));
        var audit = new FakeAudit();
        scanner ??= new ScriptedScanner();
        var userAdmin = new UserAdminService(db, audit);
        var service = new ModerationService(db, scanner, userAdmin, new NotificationService(db), audit,
            NullLogger<ModerationService>.Instance);
        return new Host { Store = store, Db = db, Scanner = scanner, Audit = audit, Service = service, UserAdmin = userAdmin };
    }

    /// <summary>Người dùng, shop, và một sản phẩm PENDING_REVIEW giống kết quả của sp_GuiDuyetSanPham (lượt mở + PhanTichAI QUEUED).</summary>
    private static Host Seed(string sellerRole = "USER", bool queuedAiRow = true, string name = "Điện thoại cũ", string? desc = "Máy dùng ổn định, còn bảo hành")
    {
        var h = NewHost();
        h.Db.NguoiDungs.AddRange(
            new NguoiDung { MaNguoiDung = AdminId, HoTen = "Admin", Email = "admin@t.local", VaiTro = "ADMIN", TrangThai = "ACTIVE" },
            new NguoiDung { MaNguoiDung = SellerId, HoTen = "Seller", Email = "seller@t.local", VaiTro = sellerRole, TrangThai = "ACTIVE" });
        h.Db.CuaHangs.Add(new CuaHang { MaCuaHang = ShopId, MaNguoiDung = SellerId, TenShop = "Shop", TrangThai = "ACTIVE" });
        h.Db.SaveChanges();
        AddPending(h, 100, name, desc, queuedAiRow);
        return h;
    }

    private static int AddPending(Host h, int productId, string name, string? desc, bool queuedAiRow = true)
    {
        h.Db.SanPhams.Add(new SanPham { MaSanPham = productId, MaCuaHang = ShopId, Ten = name, MoTa = desc, TrangThai = "PENDING_REVIEW", Gia = 1000 });
        var turn = new LuotKiemDuyet { MaLuotKiemDuyet = productId * 10, MaSanPham = productId, ThoiGianGui = DateTime.UtcNow };
        h.Db.LuotKiemDuyets.Add(turn);
        if (queuedAiRow)
            h.Db.PhanTichAIs.Add(new PhanTichAI { MaPhanTichAI = productId * 10, MaLuotKiemDuyet = turn.MaLuotKiemDuyet, TrangThai = "QUEUED", NgayTao = DateTime.UtcNow });
        h.Db.SaveChanges();
        return turn.MaLuotKiemDuyet;
    }

    private static KeywordMatch Low(int id, string phrase = "kw-low", string field = "Ten") => new(id, phrase, "NHOM", "LOW", field, 0);
    private static KeywordMatch High(int id, string phrase = "kw-high", string field = "Ten") => new(id, phrase, "NHOM", "HIGH", field, 0);

    private static void AddViolations(AppDbContext db, int sellerId, int count, int firstTurnId = 9000)
    {
        for (var i = 0; i < count; i++)
            db.LichSuViPhams.Add(new LichSuViPham
            {
                MaNguoiBan = sellerId, MaSanPham = 900 + i, MaLuotKiemDuyet = firstTurnId + i, MucDo = "HIGH",
                NguonGhiNhan = "AUTO_HIGH", ChiTietTuKhoa = "[]", ThoiGian = DateTime.UtcNow
            });
        db.SaveChanges();
    }

    // ---------------------------------------------------------------- không có / LOW / HIGH
    [Fact]
    public async Task NoKeywords_SavesDoneNoneAndLeavesProductForAdmin()
    {
        var h = Seed();

        await h.Service.PhanTichSanPhamAsync(100);

        var ai = await h.Db.PhanTichAIs.SingleAsync();
        Assert.Equal("DONE", ai.TrangThai);
        Assert.Equal("NONE", ai.MucDoTuKhoa);
        Assert.Equal("APPROVE", ai.DeXuat);
        Assert.NotNull(ai.ThoiGianHoanThanh);
        Assert.Equal("PENDING_REVIEW", (await h.Db.SanPhams.SingleAsync()).TrangThai);
        Assert.Null((await h.Db.LuotKiemDuyets.SingleAsync()).QuyetDinh);
        Assert.Empty(h.Db.LichSuViPhams);
        var review = await h.Service.GetReviewAsync(100);
        Assert.True(review!.CanApprove);
    }

    [Fact]
    public async Task OnlyLow_WaitsForAdmin_NoRejectNoViolationNoCounter()
    {
        var h = Seed();
        h.Scanner.Hits = new() { Low(1) };

        await h.Service.PhanTichSanPhamAsync(100);

        var ai = await h.Db.PhanTichAIs.SingleAsync();
        Assert.Equal("DONE", ai.TrangThai);
        Assert.Equal("LOW", ai.MucDoTuKhoa);
        Assert.Equal("NEEDS_REVIEW", ai.DeXuat);
        Assert.Equal("PENDING_REVIEW", (await h.Db.SanPhams.SingleAsync()).TrangThai);
        Assert.Null((await h.Db.LuotKiemDuyets.SingleAsync()).QuyetDinh);
        Assert.Empty(h.Db.LichSuViPhams);
        Assert.Equal(0, await h.Service.GetViolationCountAsync(SellerId));

        var review = await h.Service.GetReviewAsync(100);
        Assert.True(review!.CanApprove);           // LOW: Admin được duyệt (không vi phạm)
        Assert.True(review.RejectionRecordsViolation);   // nhưng nếu từ chối thì ghi 1 vi phạm
    }

    [Fact]
    public async Task High_AutoRejectsWithSystemDecisionAndRecordsOneViolation()
    {
        var h = Seed();
        h.Scanner.Hits = new() { High(1) };

        await h.Service.PhanTichSanPhamAsync(100);

        var product = await h.Db.SanPhams.SingleAsync();
        Assert.Equal("REJECTED", product.TrangThai);

        var turn = await h.Db.LuotKiemDuyets.SingleAsync();
        Assert.Equal("REJECTED", turn.QuyetDinh);
        Assert.True(turn.QuyetDinhTuDong);
        Assert.Null(turn.MaQuanTriVien);                       // không giả mạo Admin
        Assert.True(turn.LyDo!.Length >= 10);                  // thỏa CK_LuotKiemDuyet_LyDoTuChoi

        var history = await h.Db.LichSuTrangThaiSanPhams.SingleAsync();
        Assert.Equal(("PENDING_REVIEW", "REJECTED", "SYSTEM", null), (history.TrangThaiCu, history.TrangThaiMoi, history.LoaiNguoiThucHien, history.NguoiThucHienId));

        var violation = await h.Db.LichSuViPhams.SingleAsync();
        Assert.Equal((SellerId, "HIGH", "AUTO_HIGH", null), (violation.MaNguoiBan, violation.MucDo, violation.NguonGhiNhan, violation.MaQuanTriVien));

        var note = await h.Db.ThongBaos.SingleAsync();
        Assert.Equal((SellerId, "PRODUCT", 100), (note.MaNguoiDung, note.Loai, note.MaThamChieu));
        Assert.Contains("AUTO_REJECT_PRODUCT", h.Audit.Actions);
    }

    [Fact]
    public async Task HighAndLow_IsHighAndStillOneViolation()
    {
        var h = Seed();
        h.Scanner.Hits = new() { Low(1), High(2) };

        await h.Service.PhanTichSanPhamAsync(100);

        Assert.Equal("HIGH", (await h.Db.PhanTichAIs.SingleAsync()).MucDoTuKhoa);
        Assert.Equal("REJECTED", (await h.Db.SanPhams.SingleAsync()).TrangThai);
        Assert.Single(h.Db.LichSuViPhams);
    }

    [Fact]
    public async Task ManyHighKeywords_InNameAndDescription_CountOnce_AndEvidenceIsValidJson()
    {
        var h = Seed();
        h.Scanner.Hits = new() { High(1, "hàng giả", "Ten"), High(2, "vũ khí", "MoTa"), High(3, "ma túy", "MoTa"), Low(4, "replica", "Ten") };

        await h.Service.PhanTichSanPhamAsync(100);

        Assert.Equal(1, await h.Db.LichSuViPhams.CountAsync());
        var ai = await h.Db.PhanTichAIs.SingleAsync();
        using var doc = JsonDocument.Parse(ai.DauHieu!);
        Assert.Equal(4, doc.RootElement.GetArrayLength());
        var review = await h.Service.GetReviewAsync(100);   // lượt đã đóng nên null, đọc theo lượt
        Assert.Null(review);
        var byTurn = await h.Service.GetReviewByTurnAsync(1000);
        Assert.Equal(new[] { "MoTa", "MoTa", "Ten", "Ten" }, byTurn!.Matches.Select(m => m.Field).OrderBy(x => x).ToArray());
        Assert.Contains(byTurn.Matches, m => m.Phrase == "hàng giả" && m.Level == "HIGH" && m.Field == "Ten");
        Assert.Contains(byTurn.Matches, m => m.Phrase == "vũ khí" && m.Field == "MoTa");
    }

    // ---------------------------------------------------------------- Admin LOW
    // ---------------------------------------------------------------- phân tích lại
    [Fact]
    public async Task Reanalysis_UpdatesSameRow_NeverCreatesSecondRow_OrSecondViolation()
    {
        var h = Seed();
        h.Scanner.Hits = new() { Low(1) };
        await h.Service.PhanTichSanPhamAsync(100);
        await h.Service.PhanTichSanPhamAsync(100);
        await h.Service.PhanTichSanPhamAsync(100);

        var ai = await h.Db.PhanTichAIs.SingleAsync();           // đúng một dòng, là dòng QUEUED có sẵn
        Assert.Equal(1000, ai.MaPhanTichAI);
        Assert.Equal(3, ai.SoLanThu);
        Assert.Empty(h.Db.LichSuViPhams);
    }

    [Fact]
    public async Task Analysis_CreatesRowWhenSubmitProcedureDidNotCreateOne()
    {
        var h = Seed(queuedAiRow: false);
        await h.Service.PhanTichSanPhamAsync(100);
        await h.Service.PhanTichSanPhamAsync(100);
        Assert.Equal(1, await h.Db.PhanTichAIs.CountAsync());
    }

    [Fact]
    public async Task LowThenHigh_NotYetRecorded_RecordsOneViolationAndRejects()
    {
        var h = Seed();
        h.Scanner.Hits = new() { Low(1) };
        await h.Service.PhanTichSanPhamAsync(100);
        Assert.Empty(h.Db.LichSuViPhams);

        h.Scanner.Hits = new() { Low(1), High(2) };              // Admin vừa đặt thêm từ khóa HIGH rồi phân tích lại
        await h.Service.PhanTichSanPhamAsync(100);

        Assert.Equal(1, await h.Db.LichSuViPhams.CountAsync());
        Assert.Equal("REJECTED", (await h.Db.SanPhams.SingleAsync()).TrangThai);
        Assert.Equal("HIGH", (await h.Db.PhanTichAIs.SingleAsync()).MucDoTuKhoa);
    }

    [Fact]
    public async Task Analysis_NoOpWhenProductNotPendingOrTurnClosed()
    {
        var h = Seed();
        h.Scanner.Hits = new() { High(1) };
        await h.Service.PhanTichSanPhamAsync(100);               // tự từ chối, lượt đóng
        var calls = h.Scanner.Calls;

        await h.Service.PhanTichSanPhamAsync(100);               // gọi lại: không quét, không cộng
        await h.Service.PhanTichSanPhamAsync(12345);             // sản phẩm không tồn tại

        Assert.Equal(calls, h.Scanner.Calls);
        Assert.Equal(1, await h.Db.LichSuViPhams.CountAsync());
    }

    // ---------------------------------------------------------------- FAILED
    [Fact]
    public async Task Failed_NoViolation_NotApprovable_ThenReanalysisSucceedsOnSameRow()
    {
        var h = Seed();
        h.Scanner.Throw = true;

        await h.Service.PhanTichSanPhamAsync(100);               // không ném exception ra cho B

        var ai = await h.Db.PhanTichAIs.SingleAsync();
        Assert.Equal("FAILED", ai.TrangThai);
        Assert.False(string.IsNullOrWhiteSpace(ai.LoiXuLy));
        Assert.Equal(1, ai.SoLanThu);
        Assert.Empty(h.Db.LichSuViPhams);
        Assert.Equal("PENDING_REVIEW", (await h.Db.SanPhams.SingleAsync()).TrangThai);
        var review = await h.Service.GetReviewAsync(100);
        Assert.Equal("FAILED", review!.AnalysisStatus);
        Assert.False(review.CanApprove);
        Assert.True(review.CanReanalyze);

        h.Scanner.Throw = false;
        h.Scanner.Hits = new() { Low(1) };
        await h.Service.PhanTichSanPhamAsync(100);               // phân tích lại FAILED

        ai = await h.Db.PhanTichAIs.SingleAsync();
        Assert.Equal(1, await h.Db.PhanTichAIs.CountAsync());
        Assert.Equal("DONE", ai.TrangThai);
        Assert.Null(ai.LoiXuLy);
        Assert.Equal(2, ai.SoLanThu);
    }

    [Fact]
    public async Task Queued_IsNotApprovable()
    {
        var h = Seed();
        var review = await h.Service.GetReviewAsync(100);
        Assert.Equal("QUEUED", review!.AnalysisStatus);
        Assert.False(review.CanApprove);
    }

    [Fact]
    public async Task FailedReanalysis_DoesNotDowngradeExistingDoneResult()
    {
        var h = Seed();
        h.Scanner.Hits = new() { Low(1) };
        await h.Service.PhanTichSanPhamAsync(100);
        h.Scanner.Throw = true;
        await h.Service.PhanTichSanPhamAsync(100);
        Assert.Equal("DONE", (await h.Db.PhanTichAIs.SingleAsync()).TrangThai);
    }

    // ---------------------------------------------------------------- LOW: Admin duyệt / từ chối
    /// <summary>Giả lập đúng thứ tự của DB khi Admin quyết định: UPDATE lượt (+ trigger file 17 ghi vi phạm LOW khi từ chối).</summary>
    private static async Task AdminDecidesAsync(Host h, int productId, bool approve, bool simulateLowTrigger = true)
    {
        var turn = await h.Db.LuotKiemDuyets.SingleAsync(l => l.MaSanPham == productId && l.QuyetDinh == null);
        var product = await h.Db.SanPhams.SingleAsync(p => p.MaSanPham == productId);
        turn.QuyetDinh = approve ? "APPROVED" : "REJECTED";
        turn.MaQuanTriVien = AdminId;
        turn.ThoiGianQuyetDinh = DateTime.UtcNow;
        turn.LyDo = approve ? null : "Admin từ chối sản phẩm này";
        product.TrangThai = approve ? "ACTIVE" : "REJECTED";
        var ai = await h.Db.PhanTichAIs.SingleAsync(a => a.MaLuotKiemDuyet == turn.MaLuotKiemDuyet);
        if (!approve && simulateLowTrigger && ai.TrangThai == "DONE" && ai.MucDoTuKhoa == "LOW"
            && !await h.Db.LichSuViPhams.AnyAsync(v => v.MaLuotKiemDuyet == turn.MaLuotKiemDuyet))
        {
            h.Db.LichSuViPhams.Add(new LichSuViPham
            {
                MaNguoiBan = SellerId, MaSanPham = productId, MaLuotKiemDuyet = turn.MaLuotKiemDuyet, MucDo = "LOW",
                NguonGhiNhan = "ADMIN_TU_CHOI_LOW", ChiTietTuKhoa = ai.DauHieu ?? "[]", MaQuanTriVien = AdminId, ThoiGian = DateTime.UtcNow
            });
        }
        await h.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task Low_AdminApproves_NoViolation_NoLock()
    {
        var h = Seed();
        AddViolations(h.Db, SellerId, 2);
        h.Scanner.Hits = new() { Low(1) };
        await h.Service.PhanTichSanPhamAsync(100);
        Assert.True((await h.Service.GetReviewAsync(100))!.CanApprove);

        await AdminDecidesAsync(h, 100, approve: true);
        var follow = await h.Service.AfterAdminDecisionAsync(100);

        Assert.False(follow!.ViolationRecorded);
        Assert.Equal(2, follow.ViolationCount);                // không tăng
        Assert.False(follow.SellerLockedNow);
        Assert.Equal("ACTIVE", (await h.Db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == SellerId)).TrangThai);
    }

    [Fact]
    public async Task Low_AdminRejects_OneViolation_RepeatedFollowUpsDoNotAddMore()
    {
        var h = Seed();
        h.Scanner.Hits = new() { Low(1), Low(2, "kw2", "MoTa") };
        await h.Service.PhanTichSanPhamAsync(100);
        Assert.Empty(h.Db.LichSuViPhams);                      // chưa quyết định thì chưa đếm

        await AdminDecidesAsync(h, 100, approve: false);       // DB (trigger) ghi vi phạm cùng quyết định
        var first = await h.Service.AfterAdminDecisionAsync(100);
        var again = await h.Service.AfterAdminDecisionAsync(100);
        var otherContext = new ModerationService(new AppDbContext(Options(h.Store)), h.Scanner, h.UserAdmin,
            new NotificationService(h.Db), h.Audit, NullLogger<ModerationService>.Instance);
        var third = await otherContext.AfterAdminDecisionAsync(100);

        Assert.True(first!.ViolationRecorded); Assert.Equal(1, first.ViolationCount);
        Assert.True(again!.ViolationRecorded); Assert.Equal(1, again.ViolationCount);
        Assert.Equal(1, third!.ViolationCount);
        var v = await h.Db.LichSuViPhams.SingleAsync();
        Assert.Equal(("LOW", "ADMIN_TU_CHOI_LOW", AdminId), (v.MucDo, v.NguonGhiNhan, v.MaQuanTriVien));
        Assert.Equal(1, await h.Service.GetViolationCountAsync(SellerId));
    }

    [Fact]
    public async Task Low_AdminRejects_WithoutDbTrigger_FollowUpReportsNoViolation()
    {
        // Nhắc: nếu chưa chạy file 17 thì KHÔNG có vi phạm nào được ghi; service không tự ghi bù (tránh trùng với trigger).
        var h = Seed();
        h.Scanner.Hits = new() { Low(1) };
        await h.Service.PhanTichSanPhamAsync(100);
        await AdminDecidesAsync(h, 100, approve: false, simulateLowTrigger: false);

        var follow = await h.Service.AfterAdminDecisionAsync(100);

        Assert.False(follow!.ViolationRecorded);
        Assert.Equal(0, follow.ViolationCount);
    }

    [Fact]
    public async Task Reject_WhenQueuedOrFailedOrNoKeyword_IsNotAViolation()
    {
        var h = Seed();                                         // QUEUED
        await AdminDecidesAsync(h, 100, approve: false);
        Assert.Empty(h.Db.LichSuViPhams);

        var h2 = Seed();
        h2.Scanner.Throw = true;                                // FAILED
        await h2.Service.PhanTichSanPhamAsync(100);
        await AdminDecidesAsync(h2, 100, approve: false);
        Assert.Empty(h2.Db.LichSuViPhams);

        var h3 = Seed();                                        // DONE nhưng không từ khóa
        await h3.Service.PhanTichSanPhamAsync(100);
        await AdminDecidesAsync(h3, 100, approve: false);
        Assert.Empty(h3.Db.LichSuViPhams);
    }

    [Fact]
    public async Task AfterAdminDecision_ReturnsNullWhenNoDecidedTurn()
    {
        var h = Seed();
        Assert.Null(await h.Service.AfterAdminDecisionAsync(100));     // lượt còn mở
        Assert.Null(await h.Service.AfterAdminDecisionAsync(54321));
    }

    [Fact]
    public async Task ThirdViolation_ByAdminRejectingLow_LocksOnce_AndNotifiesOnce()
    {
        var h = Seed();
        AddViolations(h.Db, SellerId, 2);
        h.Scanner.Hits = new() { Low(1) };
        await h.Service.PhanTichSanPhamAsync(100);
        Assert.Equal("ACTIVE", (await h.Db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == SellerId)).TrangThai); // LOW chưa quyết định: chưa đếm

        await AdminDecidesAsync(h, 100, approve: false);
        var first = await h.Service.AfterAdminDecisionAsync(100);
        var second = await h.Service.AfterAdminDecisionAsync(100);

        Assert.True(first!.SellerLockedNow); Assert.True(first.SellerLocked); Assert.Equal(3, first.ViolationCount);
        Assert.False(second!.SellerLockedNow); Assert.True(second.SellerLocked);
        var seller = await h.Db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == SellerId);
        Assert.Equal("LOCKED", seller.TrangThai);
        Assert.False(string.IsNullOrWhiteSpace(seller.LyDoKhoa));
        Assert.Contains("LOCK_USER", h.Audit.Actions);
        Assert.Equal(1, h.Db.ThongBaos.Count(n => n.MaNguoiDung == SellerId && n.Loai == "SYSTEM"));
    }

    // ---------------------------------------------------------------- bộ đếm + khóa
    [Fact]
    public async Task ThirdHighViolation_AutoLocks_ButTwoViolationsDoNot()
    {
        var h = Seed();
        AddViolations(h.Db, SellerId, 1);
        h.Scanner.Hits = new() { High(1) };
        await h.Service.PhanTichSanPhamAsync(100);               // vi phạm thứ 2
        Assert.Equal("ACTIVE", (await h.Db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == SellerId)).TrangThai);

        AddPending(h, 101, "Sản phẩm khác", "Mô tả khác khá dài");
        await h.Service.PhanTichSanPhamAsync(101);               // vi phạm thứ 3
        Assert.Equal(3, await h.Service.GetViolationCountAsync(SellerId));
        Assert.Equal("LOCKED", (await h.Db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == SellerId)).TrangThai);
    }

    [Fact]
    public async Task AdminAccount_IsNeverLocked()
    {
        var h = Seed(sellerRole: "ADMIN");
        AddViolations(h.Db, SellerId, 3);

        var locked = await h.Service.EnforceViolationThresholdAsync(SellerId);

        Assert.False(locked);
        Assert.Equal("ACTIVE", (await h.Db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == SellerId)).TrangThai);
    }

    [Fact]
    public async Task EnforceThreshold_IsIdempotent_AndUnlockKeepsHistory()
    {
        var h = Seed();
        AddViolations(h.Db, SellerId, 3);

        Assert.True(await h.Service.EnforceViolationThresholdAsync(SellerId));
        Assert.False(await h.Service.EnforceViolationThresholdAsync(SellerId));   // đã khóa: không khóa/thông báo lại
        Assert.Equal(1, h.Db.ThongBaos.Count(n => n.Loai == "SYSTEM"));

        var (ok, _) = await h.UserAdmin.UnlockUserAsync(AdminId, SellerId);
        Assert.True(ok);
        Assert.Equal("ACTIVE", (await h.Db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == SellerId)).TrangThai);
        Assert.Equal(3, await h.Db.LichSuViPhams.CountAsync());                   // mở khóa không xóa lịch sử
        Assert.Equal(3, await h.Service.GetViolationCountAsync(SellerId));
    }

    [Fact]
    public async Task Violations_OfOtherSellers_AreNotCounted()
    {
        var h = Seed();
        AddViolations(h.Db, 999, 5);
        Assert.Equal(0, await h.Service.GetViolationCountAsync(SellerId));
        Assert.False(await h.Service.EnforceViolationThresholdAsync(SellerId));
    }
}
