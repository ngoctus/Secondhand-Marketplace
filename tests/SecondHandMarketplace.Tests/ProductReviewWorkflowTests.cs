using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Accounts;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.Notifications;
using SecondHandMarketplace.Web.Services.ProductReview;
using SecondHandMarketplace.Web.Services.Products;
using Xunit;
using ApiProductsController = SecondHandMarketplace.Web.Controllers.Api.v1.ProductsController;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// C5 – kiểm thử đầu-cuối luồng duyệt tin: người bán gửi duyệt → C2 quét từ khóa (HIGH/LOW/NONE/FAILED)
/// → Admin duyệt/từ chối/gỡ → ngưỡng 3 vi phạm tự khóa tài khoản.
///
/// LƯU Ý: InMemory KHÔNG chạy stored procedure/trigger/khóa dòng của SQL Server. Vì vậy hai phương thức
/// ExecuteDecisionAsync/ExecuteRemoveAsync (được khai báo protected virtual đúng cho mục đích này) được ghi đè
/// để mô phỏng chính xác: sp_DuyetSanPham/sp_TuChoiSanPham, sp_ChuyenTrangThaiSanPham và trigger ghi vi phạm LOW
/// khi Admin từ chối (database/17_...sql). Toàn bộ phần còn lại của ProductReviewService/ModerationService là mã thật.
/// </summary>
public class ProductReviewWorkflowTests
{
    private const int AdminId = 1, SellerId = 10, ShopId = 1;

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

    /// <summary>Ghi đè 2 lời gọi procedure để chạy được trên InMemory; mô phỏng đúng hiệu ứng DB thật.</summary>
    private sealed class InMemoryReviewService : ProductReviewService
    {
        private readonly AppDbContext _db;

        public InMemoryReviewService(
            AppDbContext db,
            IModerationService moderation,
            INotificationService notifications,
            IAuditLogService auditLog,
            Microsoft.Extensions.Logging.ILogger<ProductReviewService> logger)
            : base(db, moderation, notifications, auditLog, logger)
        {
            _db = db;
        }

        /// <summary>Mô phỏng SP ném lỗi SQL nghiệp vụ (50022/50210/50211) để kiểm tra chuyển tải thông báo.</summary>
        public int? SimulateSqlError { get; set; }

        protected override async Task ExecuteDecisionAsync(int maSanPham, int adminId, bool approve, string? reason, CancellationToken ct)
        {
            if (SimulateSqlError is int number)
            {
                throw new ReviewDatabaseException(number, number == 50022
                    ? "Sản phẩm không còn ở trạng thái chờ duyệt."
                    : "Sản phẩm chưa được phân tích từ khóa xong.");
            }

            var product = await _db.SanPhams.SingleAsync(p => p.MaSanPham == maSanPham, ct);
            var turn = await _db.LuotKiemDuyets.SingleAsync(l => l.MaSanPham == maSanPham && l.QuyetDinh == null, ct);
            var analysis = await _db.PhanTichAIs.AsNoTracking()
                .FirstOrDefaultAsync(a => a.MaLuotKiemDuyet == turn.MaLuotKiemDuyet, ct);
            var sellerId = await _db.CuaHangs.AsNoTracking()
                .Where(c => c.MaCuaHang == product.MaCuaHang).Select(c => c.MaNguoiDung).SingleAsync(ct);

            var now = DateTime.UtcNow;
            var oldStatus = product.TrangThai;

            turn.QuyetDinh = approve ? "APPROVED" : "REJECTED";
            turn.MaQuanTriVien = adminId;
            turn.ThoiGianQuyetDinh = now;
            turn.LyDo = approve ? null : reason;
            turn.QuyetDinhTuDong = false;

            product.TrangThai = approve ? "ACTIVE" : ModerationPolicy.ProductRejected;
            product.NgayCapNhat = now;
            _db.LichSuTrangThaiSanPhams.Add(new LichSuTrangThaiSanPham
            {
                MaSanPham = maSanPham,
                TrangThaiCu = oldStatus,
                TrangThaiMoi = product.TrangThai,
                LoaiNguoiThucHien = "ADMIN",
                NguoiThucHienId = adminId,
                LyDo = reason,
                ThoiGian = now
            });

            // Trigger của database/17: Admin từ chối lượt có kết quả LOW ⇒ ghi đúng 1 vi phạm trong cùng transaction.
            if (!approve && analysis is { TrangThai: ModerationPolicy.AnalysisDone, MucDoTuKhoa: ModerationPolicy.LevelLow }
                && !await _db.LichSuViPhams.AnyAsync(v => v.MaLuotKiemDuyet == turn.MaLuotKiemDuyet, ct))
            {
                _db.LichSuViPhams.Add(new LichSuViPham
                {
                    MaNguoiBan = sellerId,
                    MaSanPham = maSanPham,
                    MaLuotKiemDuyet = turn.MaLuotKiemDuyet,
                    MucDo = ModerationPolicy.KeywordLow,
                    NguonGhiNhan = ModerationPolicy.SourceAdminRejectLow,
                    ChiTietTuKhoa = analysis.DauHieu ?? "[]",
                    MaQuanTriVien = adminId,
                    ThoiGian = now
                });
            }

            await _db.SaveChangesAsync(ct);
        }

        protected override async Task ExecuteRemoveAsync(int maSanPham, int adminId, string reason, CancellationToken ct)
        {
            var product = await _db.SanPhams.SingleAsync(p => p.MaSanPham == maSanPham, ct);
            var now = DateTime.UtcNow;
            var oldStatus = product.TrangThai;

            product.TrangThai = "REMOVED";
            product.LyDoGo = reason;
            product.NgayCapNhat = now;
            _db.LichSuTrangThaiSanPhams.Add(new LichSuTrangThaiSanPham
            {
                MaSanPham = maSanPham,
                TrangThaiCu = oldStatus,
                TrangThaiMoi = "REMOVED",
                LoaiNguoiThucHien = "ADMIN",
                NguoiThucHienId = adminId,
                LyDo = reason,
                ThoiGian = now
            });

            await _db.SaveChangesAsync(ct);
        }
    }

    private sealed class Host
    {
        public required AppDbContext Db { get; init; }
        public required ScriptedScanner Scanner { get; init; }
        public required FakeAuditLogService Audit { get; init; }
        public required ModerationService Moderation { get; init; }
        public required InMemoryReviewService Reviews { get; init; }
    }

    private static Host NewHost()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);
        var scanner = new ScriptedScanner();
        var audit = new FakeAuditLogService();
        var notifications = new NotificationService(db);
        var userAdmin = new UserAdminService(db, audit);
        var moderation = new ModerationService(db, scanner, userAdmin, notifications, audit,
            NullLogger<ModerationService>.Instance);
        var reviews = new InMemoryReviewService(db, moderation, notifications, audit,
            NullLogger<ProductReviewService>.Instance);

        db.NguoiDungs.AddRange(
            new NguoiDung { MaNguoiDung = AdminId, HoTen = "Admin", Email = "admin@t.local", VaiTro = "ADMIN", TrangThai = "ACTIVE" },
            new NguoiDung { MaNguoiDung = SellerId, HoTen = "Người bán", Email = "seller@t.local", VaiTro = "USER", TrangThai = "ACTIVE" });
        db.CuaHangs.Add(new CuaHang
        {
            MaCuaHang = ShopId, MaNguoiDung = SellerId, TenShop = "Shop Người Bán", TrangThai = "ACTIVE",
            SoDienThoaiLienHe = "0900000000"
        });
        db.SaveChanges();

        return new Host { Db = db, Scanner = scanner, Audit = audit, Moderation = moderation, Reviews = reviews };
    }

    /// <summary>
    /// Mô phỏng đúng kết quả sp_GuiDuyetSanPham: sản phẩm PENDING_REVIEW + một lượt mở + dòng PhanTichAI QUEUED.
    /// Đây là trạng thái mà cả API/MVC chuyển cho C2 ngay sau khi gửi duyệt.
    /// </summary>
    private static int GuiDuyet(Host h, int productId, string name = "Điện thoại cũ", string? desc = "Máy dùng ổn định, còn bảo hành")
    {
        h.Db.SanPhams.Add(new SanPham
        {
            MaSanPham = productId, MaCuaHang = ShopId, Ten = name, MoTa = desc, Gia = 1000m,
            TrangThai = ModerationPolicy.ProductPending, NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
        });
        var turnId = productId * 10;
        h.Db.LuotKiemDuyets.Add(new LuotKiemDuyet
        {
            MaLuotKiemDuyet = turnId, MaSanPham = productId, ThoiGianGui = DateTime.UtcNow
        });
        h.Db.PhanTichAIs.Add(new PhanTichAI
        {
            MaPhanTichAI = turnId, MaLuotKiemDuyet = turnId, TrangThai = ModerationPolicy.AnalysisQueued, NgayTao = DateTime.UtcNow
        });
        h.Db.SaveChanges();
        return turnId;
    }

    private static void AddActiveProduct(Host h, int productId, string name = "Sản phẩm đang bán")
    {
        h.Db.SanPhams.Add(new SanPham
        {
            MaSanPham = productId, MaCuaHang = ShopId, Ten = name, MoTa = "Đang bán bình thường", Gia = 2000m,
            TrangThai = "ACTIVE", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
        });
        h.Db.SaveChanges();
    }

    private static void AddViolations(AppDbContext db, int sellerId, int count, int firstTurnId = 9000)
    {
        for (var i = 0; i < count; i++)
        {
            db.LichSuViPhams.Add(new LichSuViPham
            {
                MaNguoiBan = sellerId, MaSanPham = 900 + i, MaLuotKiemDuyet = firstTurnId + i,
                MucDo = ModerationPolicy.KeywordHigh, NguonGhiNhan = ModerationPolicy.SourceAutoHigh,
                ChiTietTuKhoa = "[]", ThoiGian = DateTime.UtcNow
            });
        }
        db.SaveChanges();
    }

    private static KeywordMatch Low(int id, string phrase = "replica") => new(id, phrase, "HANG_GIA", ModerationPolicy.KeywordLow, "Ten", 0);
    private static KeywordMatch High(int id, string phrase = "hàng giả") => new(id, phrase, "HANG_GIA", ModerationPolicy.KeywordHigh, "Ten", 0);

    private static Task<SanPham> ProductAsync(Host h, int productId) =>
        h.Db.SanPhams.AsNoTracking().SingleAsync(p => p.MaSanPham == productId);

    private static Task<PhanTichAI> AnalysisAsync(Host h, int turnId) =>
        h.Db.PhanTichAIs.AsNoTracking().SingleAsync(a => a.MaLuotKiemDuyet == turnId);

    private static Task<NguoiDung> SellerAsync(Host h) =>
        h.Db.NguoiDungs.AsNoTracking().SingleAsync(u => u.MaNguoiDung == SellerId);

    // =====================================================================
    // 1. Tin sạch: NONE → Admin duyệt → ACTIVE, không vi phạm
    // =====================================================================
    [Fact]
    public async Task CleanProduct_AnalysisNone_AdminApproves_ProductActive_NoViolation()
    {
        var h = NewHost();
        var turnId = GuiDuyet(h, 100);

        await h.Moderation.PhanTichSanPhamAsync(100);

        var ai = await AnalysisAsync(h, turnId);
        Assert.Equal(ModerationPolicy.AnalysisDone, ai.TrangThai);
        Assert.Equal(ModerationPolicy.LevelNone, ai.MucDoTuKhoa);
        Assert.Equal(ModerationPolicy.ProductPending, (await ProductAsync(h, 100)).TrangThai);
        Assert.True((await h.Moderation.GetReviewAsync(100))!.CanApprove);

        var result = await h.Reviews.ApproveAsync(AdminId, 100);

        Assert.True(result.Success);
        Assert.Equal("ACTIVE", result.NewStatus);
        Assert.Equal("ACTIVE", (await ProductAsync(h, 100)).TrangThai);

        var turn = await h.Db.LuotKiemDuyets.AsNoTracking().SingleAsync(l => l.MaLuotKiemDuyet == turnId);
        Assert.Equal(("APPROVED", AdminId), (turn.QuyetDinh, turn.MaQuanTriVien));
        Assert.Empty(h.Db.LichSuViPhams);
        Assert.Equal(0, result.FollowUp!.ViolationCount);
        Assert.False(result.FollowUp.SellerLockedNow);

        var note = Assert.Single(await h.Db.ThongBaos.AsNoTracking().ToListAsync());
        Assert.Equal((SellerId, NotificationTypes.Product, 100), (note.MaNguoiDung, note.Loai, note.MaThamChieu));
        Assert.Contains("đã được duyệt", note.TieuDe);
    }

    // =====================================================================
    // 2. Từ khóa HIGH: hệ thống tự từ chối, 1 vi phạm AUTO_HIGH, thông báo
    // =====================================================================
    [Fact]
    public async Task HighKeyword_AutoRejects_RecordsOneAutoHighViolation_NotifiesSeller()
    {
        var h = NewHost();
        var turnId = GuiDuyet(h, 101, "Hàng giả siêu cấp", "Hàng giả replica cao cấp");
        h.Scanner.Hits = new() { High(1), Low(2) };

        await h.Moderation.PhanTichSanPhamAsync(101);

        var ai = await AnalysisAsync(h, turnId);
        Assert.Equal(ModerationPolicy.AnalysisDone, ai.TrangThai);
        Assert.Equal(ModerationPolicy.LevelHigh, ai.MucDoTuKhoa);
        Assert.Equal(ModerationPolicy.ProductRejected, (await ProductAsync(h, 101)).TrangThai);

        var turn = await h.Db.LuotKiemDuyets.AsNoTracking().SingleAsync(l => l.MaLuotKiemDuyet == turnId);
        Assert.Equal("REJECTED", turn.QuyetDinh);
        Assert.True(turn.QuyetDinhTuDong);
        Assert.Null(turn.MaQuanTriVien);                 // quyết định của hệ thống, không giả Admin

        var violation = Assert.Single(await h.Db.LichSuViPhams.AsNoTracking().ToListAsync());
        Assert.Equal((SellerId, ModerationPolicy.KeywordHigh, ModerationPolicy.SourceAutoHigh),
            (violation.MaNguoiBan, violation.MucDo, violation.NguonGhiNhan));
        Assert.Equal(turnId, violation.MaLuotKiemDuyet);
        Assert.Null(violation.MaQuanTriVien);

        var note = Assert.Single(await h.Db.ThongBaos.AsNoTracking().ToListAsync());
        Assert.Equal((SellerId, NotificationTypes.Product, 101), (note.MaNguoiDung, note.Loai, note.MaThamChieu));
        Assert.Contains("từ chối tự động", note.TieuDe);
        Assert.Contains("1/3", note.NoiDung);
    }

    // =====================================================================
    // 3. Từ khóa LOW: chờ Admin; Admin duyệt ⇒ ACTIVE, không vi phạm
    // =====================================================================
    [Fact]
    public async Task LowKeyword_AdminApproves_ProductActive_NoViolation()
    {
        var h = NewHost();
        var turnId = GuiDuyet(h, 102);
        h.Scanner.Hits = new() { Low(1) };

        await h.Moderation.PhanTichSanPhamAsync(102);

        var review = await h.Moderation.GetReviewAsync(102);
        Assert.Equal(ModerationPolicy.LevelLow, review!.Level);
        Assert.True(review.CanApprove);                  // LOW vẫn được duyệt
        Assert.True(review.RejectionRecordsViolation);   // ... nhưng từ chối thì ghi 1 vi phạm
        Assert.Empty(h.Db.LichSuViPhams);

        var result = await h.Reviews.ApproveAsync(AdminId, 102);

        Assert.True(result.Success);
        Assert.Equal("ACTIVE", (await ProductAsync(h, 102)).TrangThai);
        Assert.Empty(h.Db.LichSuViPhams);
        Assert.Equal(0, result.FollowUp!.ViolationCount);
        Assert.False(result.FollowUp.ViolationRecorded);
        Assert.Equal(ModerationPolicy.LevelLow, (await AnalysisAsync(h, turnId)).MucDoTuKhoa);
    }

    // =====================================================================
    // 4. Từ khóa LOW: Admin từ chối ⇒ REJECTED + 1 vi phạm ADMIN_TU_CHOI_LOW
    // =====================================================================
    [Fact]
    public async Task LowKeyword_AdminRejects_ProductRejected_OneAdminRejectLowViolation()
    {
        var h = NewHost();
        var turnId = GuiDuyet(h, 103);
        h.Scanner.Hits = new() { Low(1) };
        await h.Moderation.PhanTichSanPhamAsync(103);

        var reason = "Sản phẩm có dấu hiệu vi phạm quy định đăng bán.";
        var result = await h.Reviews.RejectAsync(AdminId, 103, reason);

        Assert.True(result.Success);
        Assert.Equal(ModerationPolicy.ProductRejected, result.NewStatus);
        Assert.Equal(ModerationPolicy.ProductRejected, (await ProductAsync(h, 103)).TrangThai);

        var turn = await h.Db.LuotKiemDuyets.AsNoTracking().SingleAsync(l => l.MaLuotKiemDuyet == turnId);
        Assert.Equal(("REJECTED", AdminId, reason), (turn.QuyetDinh, turn.MaQuanTriVien, turn.LyDo));
        Assert.False(turn.QuyetDinhTuDong);

        var violation = Assert.Single(await h.Db.LichSuViPhams.AsNoTracking().ToListAsync());
        Assert.Equal((SellerId, ModerationPolicy.KeywordLow, ModerationPolicy.SourceAdminRejectLow),
            (violation.MaNguoiBan, violation.MucDo, violation.NguonGhiNhan));
        Assert.Equal(AdminId, violation.MaQuanTriVien);

        Assert.True(result.FollowUp!.ViolationRecorded);
        Assert.Equal(1, result.FollowUp.ViolationCount);
        Assert.False(result.FollowUp.SellerLockedNow);

        var note = Assert.Single(await h.Db.ThongBaos.AsNoTracking().ToListAsync());
        Assert.Contains("bị từ chối", note.TieuDe);
        Assert.Contains(reason, note.NoiDung);
    }

    // =====================================================================
    // 5. Vi phạm thứ 3 (HIGH tự động hoặc LOW bị từ chối) ⇒ tự khóa tài khoản
    // =====================================================================
    [Fact]
    public async Task ThirdAccumulatedViolation_FromRejectedLow_AutoLocksSeller()
    {
        var h = NewHost();
        AddViolations(h.Db, SellerId, 2);
        GuiDuyet(h, 104);
        h.Scanner.Hits = new() { Low(1) };
        await h.Moderation.PhanTichSanPhamAsync(104);

        var result = await h.Reviews.RejectAsync(AdminId, 104, "Nội dung spam, từ chối theo quy định.");

        Assert.True(result.Success);
        Assert.Equal(3, result.FollowUp!.ViolationCount);
        Assert.True(result.FollowUp.SellerLockedNow);
        Assert.True(result.FollowUp.SellerLocked);

        var seller = await SellerAsync(h);
        Assert.Equal("LOCKED", seller.TrangThai);
        Assert.False(string.IsNullOrWhiteSpace(seller.LyDoKhoa));
        Assert.Equal("LOCKED", (await h.Db.CuaHangs.AsNoTracking().SingleAsync(c => c.MaCuaHang == ShopId)).TrangThai);
        Assert.Contains(h.Audit.Logs, l => l.Action == "LOCK_USER" && l.TargetId == SellerId);

        var systemNote = Assert.Single(await h.Db.ThongBaos.AsNoTracking()
            .Where(n => n.MaNguoiDung == SellerId && n.Loai == NotificationTypes.System).ToListAsync());
        Assert.Contains("đã bị khóa", systemNote.TieuDe);
    }

    [Fact]
    public async Task ThirdAccumulatedViolation_FromAutoHigh_AutoLocksSeller()
    {
        var h = NewHost();
        AddViolations(h.Db, SellerId, 2);
        GuiDuyet(h, 105);
        h.Scanner.Hits = new() { High(1) };

        await h.Moderation.PhanTichSanPhamAsync(105);    // vi phạm thứ 3 do hệ thống tự ghi

        Assert.Equal(3, await h.Moderation.GetViolationCountAsync(SellerId));
        Assert.Equal("LOCKED", (await SellerAsync(h)).TrangThai);
        Assert.Equal(ModerationPolicy.ProductRejected, (await ProductAsync(h, 105)).TrangThai);
    }

    // =====================================================================
    // 6. Chưa quét (QUEUED) / quét lỗi (FAILED): không được duyệt
    // =====================================================================
    [Fact]
    public async Task QueuedOrFailedAnalysis_AdminCannotApprove_ButCanRejectOrReanalyze()
    {
        var h = NewHost();

        // --- QUEUED: vừa gửi duyệt, chưa quét ---
        GuiDuyet(h, 106);
        var queued = await h.Moderation.GetReviewAsync(106);
        Assert.Equal(ModerationPolicy.AnalysisQueued, queued!.AnalysisStatus);
        Assert.False(queued.CanApprove);

        var queuedResult = await h.Reviews.ApproveAsync(AdminId, 106);
        Assert.False(queuedResult.Success);
        Assert.Contains("chưa được phân tích", queuedResult.Error!);
        Assert.Equal(ModerationPolicy.ProductPending, (await ProductAsync(h, 106)).TrangThai);
        Assert.Empty(h.Db.LichSuViPhams);

        // --- FAILED: quét lỗi ---
        GuiDuyet(h, 107);
        h.Scanner.Throw = true;
        await h.Moderation.PhanTichSanPhamAsync(107);

        var failed = await h.Moderation.GetReviewAsync(107);
        Assert.Equal(ModerationPolicy.AnalysisFailed, failed!.AnalysisStatus);
        Assert.False(failed.CanApprove);
        Assert.True(failed.CanReanalyze);

        var failedResult = await h.Reviews.ApproveAsync(AdminId, 107);
        Assert.False(failedResult.Success);
        Assert.Contains("chưa được phân tích", failedResult.Error!);
        Assert.Equal(ModerationPolicy.ProductPending, (await ProductAsync(h, 107)).TrangThai);

        // Từ chối vẫn được khi FAILED (không có kết quả LOW nên không ghi vi phạm).
        var reject = await h.Reviews.RejectAsync(AdminId, 107, "Không thể xác minh nội dung sản phẩm.");
        Assert.True(reject.Success);
        Assert.Empty(h.Db.LichSuViPhams);

        // Quét lại thành công trên cùng dòng phân tích rồi duyệt được.
        h.Scanner.Throw = false;
        GuiDuyet(h, 108);
        h.Scanner.Throw = true;
        await h.Moderation.PhanTichSanPhamAsync(108);
        h.Scanner.Throw = false;
        h.Scanner.Hits = new();
        await h.Moderation.PhanTichSanPhamAsync(108);

        Assert.Equal(ModerationPolicy.AnalysisDone, (await h.Moderation.GetReviewAsync(108))!.AnalysisStatus);
        Assert.True((await h.Reviews.ApproveAsync(AdminId, 108)).Success);
        Assert.Equal("ACTIVE", (await ProductAsync(h, 108)).TrangThai);
    }

    [Fact]
    public async Task Approve_WhenDatabaseReportsAlreadyDecided_ReturnsFriendlyMessage()
    {
        var h = NewHost();
        GuiDuyet(h, 109);
        await h.Moderation.PhanTichSanPhamAsync(109);

        h.Reviews.SimulateSqlError = 50022;
        var result = await h.Reviews.ApproveAsync(AdminId, 109);

        Assert.False(result.Success);
        Assert.True(result.AlreadyDecided);
        Assert.Contains("đã có người xử lý trước", result.Error!);
        Assert.Equal(ModerationPolicy.ProductPending, (await ProductAsync(h, 109)).TrangThai);
    }

    [Fact]
    public async Task Approve_IsIdempotent_SecondDecisionIsRejectedWithoutSideEffects()
    {
        var h = NewHost();
        GuiDuyet(h, 116);
        await h.Moderation.PhanTichSanPhamAsync(116);

        var first = await h.Reviews.ApproveAsync(AdminId, 116);
        var second = await h.Reviews.ApproveAsync(AdminId, 116);
        var rejectAgain = await h.Reviews.RejectAsync(AdminId, 116, "Thử từ chối lại sản phẩm đã duyệt.");

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.True(second.AlreadyDecided);
        Assert.False(rejectAgain.Success);
        Assert.True(rejectAgain.AlreadyDecided);

        Assert.Equal("ACTIVE", (await ProductAsync(h, 116)).TrangThai);
        Assert.Single(await h.Db.ThongBaos.AsNoTracking().ToListAsync());   // chỉ thông báo 1 lần
        Assert.Single(await h.Db.LichSuTrangThaiSanPhams.AsNoTracking()
            .Where(x => x.MaSanPham == 116).ToListAsync());                 // chỉ 1 lần chuyển trạng thái
        Assert.Empty(h.Db.LichSuViPhams);
    }

    [Fact]
    public async Task Approve_WhenTriggerBlocks_ReturnsDatabaseMessage()
    {
        var h = NewHost();
        GuiDuyet(h, 115);
        await h.Moderation.PhanTichSanPhamAsync(115);

        h.Reviews.SimulateSqlError = 50210;               // trigger C2 (DUYET bị chặn)
        var result = await h.Reviews.ApproveAsync(AdminId, 115);

        Assert.False(result.Success);
        Assert.False(result.AlreadyDecided);
        Assert.Contains("chưa được phân tích", result.Error!);
        Assert.Equal(ModerationPolicy.ProductPending, (await ProductAsync(h, 115)).TrangThai);
    }

    // =====================================================================
    // 7. Sản phẩm ACTIVE bị Admin gỡ ⇒ REMOVED, lưu lý do, thông báo
    // =====================================================================
    [Fact]
    public async Task ActiveProduct_AdminRemoves_StatusRemoved_ReasonAndNotificationRecorded()
    {
        var h = NewHost();
        AddActiveProduct(h, 110, "Sản phẩm vi phạm đang bán");
        var reason = "Sản phẩm vi phạm chính sách kinh doanh của sàn.";

        var result = await h.Reviews.RemoveProductAsync(AdminId, 110, reason);

        Assert.True(result.Success);
        Assert.Equal("REMOVED", result.NewStatus);

        var product = await ProductAsync(h, 110);
        Assert.Equal("REMOVED", product.TrangThai);
        Assert.Equal(reason, product.LyDoGo);

        var history = Assert.Single(await h.Db.LichSuTrangThaiSanPhams.AsNoTracking()
            .Where(x => x.MaSanPham == 110).ToListAsync());
        Assert.Equal(("ACTIVE", "REMOVED", "ADMIN", AdminId), (history.TrangThaiCu, history.TrangThaiMoi, history.LoaiNguoiThucHien, history.NguoiThucHienId));
        Assert.Equal(reason, history.LyDo);

        var note = Assert.Single(await h.Db.ThongBaos.AsNoTracking().ToListAsync());
        Assert.Equal((SellerId, NotificationTypes.Product, 110), (note.MaNguoiDung, note.Loai, note.MaThamChieu));
        Assert.Contains("bị gỡ", note.TieuDe);
        Assert.Contains(reason, note.NoiDung);

        Assert.Contains(h.Audit.Logs, l => l.Action == "REMOVE_PRODUCT" && l.TargetId == 110 && l.Reason == reason);
        Assert.Empty(h.Db.LichSuViPhams);
    }

    [Fact]
    public async Task Remove_OnlyAllowedForActiveProducts_AndRequiresValidReason()
    {
        var h = NewHost();
        GuiDuyet(h, 111);                                 // PENDING_REVIEW

        var pending = await h.Reviews.RemoveProductAsync(AdminId, 111, "Lý do gỡ sản phẩm hợp lệ.");
        Assert.False(pending.Success);
        Assert.Contains("ACTIVE", pending.Error!);

        AddActiveProduct(h, 112);
        var shortReason = await h.Reviews.RemoveProductAsync(AdminId, 112, "quá ngắn");
        Assert.False(shortReason.Success);
        Assert.Equal("ACTIVE", (await ProductAsync(h, 112)).TrangThai);
    }

    // =====================================================================
    // Quyền Admin: người bán thường không được quyết định
    // =====================================================================
    [Fact]
    public async Task NonAdmin_CannotApproveRejectOrRemove()
    {
        var h = NewHost();
        GuiDuyet(h, 113);
        await h.Moderation.PhanTichSanPhamAsync(113);
        AddActiveProduct(h, 114);

        var approve = await h.Reviews.ApproveAsync(SellerId, 113);
        var reject = await h.Reviews.RejectAsync(SellerId, 113, "Người bán tự từ chối sản phẩm của mình.");
        var remove = await h.Reviews.RemoveProductAsync(SellerId, 114, "Người bán tự gỡ sản phẩm của mình.");

        foreach (var result in new[] { approve, reject, remove })
        {
            Assert.False(result.Success);
            Assert.Contains("quản trị viên", result.Error!);
        }

        Assert.Equal(ModerationPolicy.ProductPending, (await ProductAsync(h, 113)).TrangThai);
        Assert.Equal("ACTIVE", (await ProductAsync(h, 114)).TrangThai);
        Assert.Empty(h.Db.ThongBaos);
    }

    // =====================================================================
    // Nối mắt xích API: SubmitReview phải kích hoạt PhanTichSanPhamAsync
    // =====================================================================
    [Fact]
    public async Task ApiSubmitReview_AfterSuccessfulSubmit_TriggersModerationAnalysis()
    {
        var products = new StubProductService { SubmitSucceeds = true };
        var moderation = new RecordingModeration();
        var controller = NewApiController(products, moderation);

        var result = await controller.SubmitReview(200, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ServiceResult>(ok.Value);
        Assert.True(body.Success);
        Assert.Equal((SellerId, 200), (products.SubmittedBy, products.SubmittedProductId));
        Assert.Equal(new[] { 200 }, moderation.AnalyzedProductIds);
    }

    [Fact]
    public async Task ApiSubmitReview_WhenSubmitFails_DoesNotRunAnalysis()
    {
        var products = new StubProductService { SubmitSucceeds = false, SubmitError = "Sản phẩm chưa đủ điều kiện gửi duyệt." };
        var moderation = new RecordingModeration();
        var controller = NewApiController(products, moderation);

        var result = await controller.SubmitReview(201, CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ServiceResult>(bad.Value);
        Assert.False(body.Success);
        Assert.Empty(moderation.AnalyzedProductIds);
    }

    [Fact]
    public async Task ApiSubmitReview_AnalysisInfrastructureError_DoesNotBreakSubmitResponse()
    {
        var products = new StubProductService { SubmitSucceeds = true };
        var moderation = new RecordingModeration { ThrowOnAnalyze = true };
        var controller = NewApiController(products, moderation);

        var result = await controller.SubmitReview(202, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(Assert.IsType<ServiceResult>(ok.Value).Success);
        Assert.Equal(new[] { 202 }, moderation.AnalyzedProductIds);   // đã gọi, lỗi hạ tầng bị nuốt an toàn
    }

    // =====================================================================

    private static ApiProductsController NewApiController(IProductService products, IModerationService moderation)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, SellerId.ToString()) }, "Test");
        return new ApiProductsController(products, moderation, NullLogger<ApiProductsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    /// <summary>Chỉ cần GuiDuyetAsync cho test nối mắt xích API; các hàm còn lại không được gọi.</summary>
    private sealed class StubProductService : IProductService
    {
        public bool SubmitSucceeds { get; init; }
        public string? SubmitError { get; init; }
        public int SubmittedBy { get; private set; }
        public int SubmittedProductId { get; private set; }

        public Task<(bool Success, string? Error)> GuiDuyetAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
        {
            SubmittedBy = maNguoiDung;
            SubmittedProductId = maSanPham;
            return Task.FromResult((SubmitSucceeds, SubmitSucceeds ? null : SubmitError));
        }

        public Task<PagedResult<ProductCardVm>> SearchAsync(ProductSearchVm filter) => throw new NotSupportedException();
        public Task<ProductDetailVm?> GetDetailAsync(int id) => throw new NotSupportedException();
        public Task<PagedResult<MyProductVm>> GetMyAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProductFormVm?> GetFormAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error, int Id)> CreateAsync(int maNguoiDung, ProductFormVm form, IReadOnlyList<ProductImageInput> anh, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> UpdateAsync(int maNguoiDung, int maSanPham, ProductFormVm form, IReadOnlyList<ProductImageInput> anhMoi, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> HideAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DeleteAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> DatAnhChinhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(bool Success, string? Error)> XoaAnhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingModeration : IModerationService
    {
        public List<int> AnalyzedProductIds { get; } = new();
        public bool ThrowOnAnalyze { get; init; }

        public Task PhanTichSanPhamAsync(int maSanPham, CancellationToken cancellationToken = default)
        {
            AnalyzedProductIds.Add(maSanPham);
            if (ThrowOnAnalyze) throw new InvalidOperationException("Lỗi hạ tầng giả lập.");
            return Task.CompletedTask;
        }

        public Task<ModerationReview?> GetReviewAsync(int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ModerationReview?> GetReviewByTurnAsync(int maLuotKiemDuyet, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AdminDecisionFollowUp?> AfterAdminDecisionAsync(int maSanPham, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> GetViolationCountAsync(int sellerUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> EnforceViolationThresholdAsync(int sellerUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
