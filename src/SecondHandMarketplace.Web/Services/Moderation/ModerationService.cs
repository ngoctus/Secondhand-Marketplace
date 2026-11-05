using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Accounts;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Notifications;

namespace SecondHandMarketplace.Web.Services.Moderation;

public sealed class ModerationService(
    AppDbContext db,
    IKeywordScanner scanner,
    IUserAdminService userAdmin,
    INotificationService notifications,
    IAuditLogService auditLog,
    ILogger<ModerationService> logger) : IModerationService
{
    private sealed record OpenTurn(int LuotId, int ProductId, string Name, string? Description, int SellerId);

    /// <summary>Việc cần làm SAU khi transaction đã commit (khóa tài khoản, thông báo, nhật ký).</summary>
    private sealed class PostCommit
    {
        public int SellerId { get; init; }
        public int ProductId { get; init; }
        public string ProductName { get; init; } = "";
        public bool NewViolation { get; set; }
        public bool AutoRejected { get; set; }
        public string? Reason { get; set; }
        public string Terms { get; set; } = "";
    }

    // =====================================================================
    // PHÂN TÍCH
    // =====================================================================
    public async Task PhanTichSanPhamAsync(int maSanPham, CancellationToken cancellationToken = default)
    {
        var turn = await FindOpenTurnAsync(maSanPham, cancellationToken);
        if (turn is null)
        {
            logger.LogWarning("PhanTichSanPhamAsync: sản phẩm {ProductId} không có lượt kiểm duyệt đang chờ (không còn PENDING_REVIEW?), bỏ qua.", maSanPham);
            return;
        }

        var clock = Stopwatch.StartNew();
        IReadOnlyList<KeywordMatch> matches;
        try
        {
            matches = await scanner.ScanAsync(turn.Name, turn.Description, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Quét từ khóa lỗi cho sản phẩm {ProductId}, lượt {TurnId}.", maSanPham, turn.LuotId);
            await MarkFailedAsync(turn.LuotId, ex.Message, cancellationToken);
            return;
        }
        clock.Stop();

        var outcome = ModerationDecision.Evaluate(matches);
        var post = await ApplyAnalysisAsync(turn.LuotId, outcome, (int)clock.ElapsedMilliseconds, cancellationToken);
        if (post is not null) await RunPostCommitAsync(post, cancellationToken);
    }

    private async Task<PostCommit?> ApplyAnalysisAsync(int turnId, ModerationOutcome outcome, int elapsedMs, CancellationToken ct)
    {
        // Khóa dòng lượt đã tuần tự hóa các request; UNIQUE(LichSuViPham.MaLuotKiemDuyet) là chốt chặn cuối.
        // Nếu vẫn đụng UNIQUE (request khác ghi trước) thì thử lại một lần: lần sau sẽ thấy vi phạm đã có.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ApplyAnalysisOnceAsync(turnId, outcome, elapsedMs, ct);
            }
            catch (DbUpdateException ex) when (attempt < 2 && IsUniqueViolation(ex))
            {
                logger.LogWarning(ex, "Đụng UNIQUE khi ghi phân tích lượt {TurnId}, thử lại.", turnId);
                DiscardPendingChanges();
            }
        }
    }

    private async Task<PostCommit?> ApplyAnalysisOnceAsync(int turnId, ModerationOutcome outcome, int elapsedMs, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var turn = await LockTurnAsync(turnId, ct);
        if (turn is null || turn.QuyetDinh is not null)
        {
            await tx.CommitAsync(ct);   // đã có quyết định (Admin hoặc request khác): không làm gì
            return null;
        }

        var product = await LockProductAsync(turn.MaSanPham, ct);
        if (product is null || product.DaXoa || product.TrangThai != ModerationPolicy.ProductPending)
        {
            await tx.CommitAsync(ct);
            return null;
        }

        var sellerId = await db.CuaHangs.AsNoTracking()
            .Where(c => c.MaCuaHang == product.MaCuaHang)
            .Select(c => c.MaNguoiDung)
            .SingleAsync(ct);

        var now = DateTime.UtcNow;
        var ai = await GetOrCreateAnalysisAsync(turnId, now, ct);

        ai.TrangThai = ModerationPolicy.AnalysisDone;
        ai.SoLanThu += 1;
        ai.MucDoTuKhoa = outcome.Level;
        ai.MucRuiRo = outcome.MucRuiRo;
        ai.DeXuat = outcome.DeXuat;
        ai.DauHieu = outcome.DauHieuJson;
        ai.GiaiThich = outcome.GiaiThich;
        ai.MaBamDauVao = outcome.FindingsHash;
        ai.PhienBanMoHinh = ModerationPolicy.ModelVersion;
        ai.ThoiGianXuLyMs = Math.Max(0, elapsedMs);
        ai.LoiXuLy = null;
        ai.ThoiGianHoanThanh = now;

        var post = new PostCommit { SellerId = sellerId, ProductId = product.MaSanPham, ProductName = product.Ten };

        if (outcome.Level == ModerationPolicy.LevelHigh)
        {
            // Một lượt tối đa một vi phạm, dù nhiều từ khóa HIGH hay phân tích lại nhiều lần,
            // và không cộng thêm nếu lượt này đã được Admin xác nhận LOW trước đó.
            if (!await db.LichSuViPhams.AnyAsync(v => v.MaLuotKiemDuyet == turnId, ct))
            {
                db.LichSuViPhams.Add(new LichSuViPham
                {
                    MaNguoiBan = sellerId,
                    MaSanPham = product.MaSanPham,
                    MaLuotKiemDuyet = turnId,
                    MucDo = ModerationPolicy.KeywordHigh,
                    NguonGhiNhan = ModerationPolicy.SourceAutoHigh,
                    ChiTietTuKhoa = outcome.DauHieuJson,
                    MaQuanTriVien = null,
                    ThoiGian = now
                });
                post.NewViolation = true;
            }

            var terms = ModerationDecision.BuildTermsText(outcome.Matches, ModerationPolicy.KeywordHigh);
            var reason = Truncate($"Tự động từ chối: tên hoặc mô tả chứa từ khóa cấm mức HIGH ({terms}).", 500);

            // Đóng lượt bằng quyết định HỆ THỐNG (không giả mạo Admin) - cần database/15_...sql.
            turn.QuyetDinh = "REJECTED";
            turn.LyDo = reason;
            turn.MaQuanTriVien = null;
            turn.ThoiGianQuyetDinh = now;
            turn.QuyetDinhTuDong = true;

            // PENDING_REVIEW -> REJECTED là bước hợp lệ của fn_ChuyenTrangThaiSanPhamHopLe; ghi lịch sử với SYSTEM.
            product.TrangThai = ModerationPolicy.ProductRejected;
            product.NgayCapNhat = now;
            db.LichSuTrangThaiSanPhams.Add(new LichSuTrangThaiSanPham
            {
                MaSanPham = product.MaSanPham,
                TrangThaiCu = ModerationPolicy.ProductPending,
                TrangThaiMoi = ModerationPolicy.ProductRejected,
                LoaiNguoiThucHien = "SYSTEM",
                NguoiThucHienId = null,
                LyDo = reason,
                ThoiGian = now
            });

            post.AutoRejected = true;
            post.Reason = reason;
            post.Terms = terms;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return post;
    }

    private async Task MarkFailedAsync(int turnId, string message, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var turn = await LockTurnAsync(turnId, ct);
        if (turn is null || turn.QuyetDinh is not null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        var ai = await GetOrCreateAnalysisAsync(turnId, DateTime.UtcNow, ct);
        if (ai.TrangThai == ModerationPolicy.AnalysisDone)
        {
            // Giữ kết quả tốt gần nhất, không hạ DONE xuống FAILED.
            if (db.Entry(ai).State == EntityState.Added) db.Entry(ai).State = EntityState.Detached;
            await tx.CommitAsync(ct);
            return;
        }

        ai.TrangThai = ModerationPolicy.AnalysisFailed;
        ai.SoLanThu += 1;
        ai.LoiXuLy = Truncate(string.IsNullOrWhiteSpace(message) ? "Lỗi không xác định khi quét từ khóa." : message, 1000);
        ai.MucDoTuKhoa = null;
        ai.MucRuiRo = null;
        ai.DeXuat = null;
        ai.ThoiGianHoanThanh = null;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // =====================================================================
    // BỘ ĐẾM, KHÓA TÀI KHOẢN
    // =====================================================================
    public async Task<AdminDecisionFollowUp?> AfterAdminDecisionAsync(int maSanPham, CancellationToken cancellationToken = default)
    {
        // Lượt mới nhất của sản phẩm đã có quyết định (Admin duyệt/từ chối). Vi phạm LOW khi từ chối đã được trigger
        // (database/17_...sql) ghi trong cùng transaction với quyết định; ở đây chỉ đọc lại và áp dụng ngưỡng khóa.
        var turn = await db.LuotKiemDuyets.AsNoTracking()
            .Where(l => l.MaSanPham == maSanPham && l.QuyetDinh != null)
            .OrderByDescending(l => l.MaLuotKiemDuyet)
            .Select(l => new { l.MaLuotKiemDuyet, SellerId = l.SanPham.CuaHang.MaNguoiDung })
            .FirstOrDefaultAsync(cancellationToken);
        if (turn is null) return null;

        var recorded = await db.LichSuViPhams.AsNoTracking().AnyAsync(v => v.MaLuotKiemDuyet == turn.MaLuotKiemDuyet, cancellationToken);
        var lockedNow = await EnforceViolationThresholdAsync(turn.SellerId, cancellationToken);
        var count = await GetViolationCountAsync(turn.SellerId, cancellationToken);
        var status = await db.NguoiDungs.AsNoTracking().Where(u => u.MaNguoiDung == turn.SellerId)
            .Select(u => u.TrangThai).SingleOrDefaultAsync(cancellationToken);
        return new AdminDecisionFollowUp(recorded, count, lockedNow, status == "LOCKED");
    }

    public Task<int> GetViolationCountAsync(int sellerUserId, CancellationToken cancellationToken = default) =>
        db.LichSuViPhams.AsNoTracking().CountAsync(v => v.MaNguoiBan == sellerUserId, cancellationToken);

    public async Task<bool> EnforceViolationThresholdAsync(int sellerUserId, CancellationToken cancellationToken = default)
    {
        var count = await GetViolationCountAsync(sellerUserId, cancellationToken);
        if (count < ModerationPolicy.ViolationLockThreshold) return false;

        var user = await db.NguoiDungs.AsNoTracking()
            .Where(u => u.MaNguoiDung == sellerUserId)
            .Select(u => new { u.VaiTro, u.TrangThai })
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null || user.VaiTro == AppRoles.Admin || user.TrangThai != "ACTIVE") return false;

        var reason = $"Tự động khóa: đạt {count} vi phạm kiểm duyệt (ngưỡng {ModerationPolicy.ViolationLockThreshold}).";
        var (success, error) = await userAdmin.LockUserAsync(null, sellerUserId, reason, cancellationToken);
        if (!success)
        {
            logger.LogWarning("Không khóa được người bán {UserId} dù đạt {Count} vi phạm: {Error}", sellerUserId, count, error);
            return false;
        }

        await TryNotifyAsync(sellerUserId, NotificationTypes.System, "Tài khoản của bạn đã bị khóa",
            $"Tài khoản đã bị khóa tự động vì đạt {count} vi phạm kiểm duyệt sản phẩm. Vui lòng liên hệ quản trị viên để được xem xét.",
            null, cancellationToken);
        return true;
    }

    private async Task RunPostCommitAsync(PostCommit post, CancellationToken ct)
    {
        if (post.AutoRejected)
        {
            await TryAuditAsync(null, "AUTO_REJECT_PRODUCT", post.ProductId, post.Reason, ct);

            var count = await GetViolationCountAsync(post.SellerId, ct);
            await TryNotifyAsync(post.SellerId, NotificationTypes.Product, "Sản phẩm bị từ chối tự động",
                Truncate($"Sản phẩm \"{Truncate(post.ProductName, 120)}\" chứa từ khóa bị cấm ({post.Terms}) nên đã bị từ chối. " +
                         $"Bạn bị ghi nhận vi phạm ({count}/{ModerationPolicy.ViolationLockThreshold}). Hãy sửa nội dung rồi gửi duyệt lại.", 1000),
                post.ProductId, ct);
        }

        if (post.NewViolation) await EnforceViolationThresholdAsync(post.SellerId, ct);
    }

    // =====================================================================
    // ĐỌC CHO C1
    // =====================================================================
    public async Task<ModerationReview?> GetReviewAsync(int maSanPham, CancellationToken cancellationToken = default)
    {
        var turnId = await db.LuotKiemDuyets.AsNoTracking()
            .Where(l => l.MaSanPham == maSanPham && l.QuyetDinh == null)
            .Select(l => (int?)l.MaLuotKiemDuyet)
            .FirstOrDefaultAsync(cancellationToken);
        return turnId is null ? null : await GetReviewByTurnAsync(turnId.Value, cancellationToken);
    }

    public async Task<ModerationReview?> GetReviewByTurnAsync(int maLuotKiemDuyet, CancellationToken cancellationToken = default)
    {
        var turn = await db.LuotKiemDuyets.AsNoTracking()
            .Where(l => l.MaLuotKiemDuyet == maLuotKiemDuyet)
            .Select(l => new { l.MaLuotKiemDuyet, l.MaSanPham, l.QuyetDinh, SellerId = l.SanPham.CuaHang.MaNguoiDung })
            .SingleOrDefaultAsync(cancellationToken);
        if (turn is null) return null;

        var ai = await db.PhanTichAIs.AsNoTracking()
            .FirstOrDefaultAsync(a => a.MaLuotKiemDuyet == maLuotKiemDuyet, cancellationToken);
        var violation = await db.LichSuViPhams.AsNoTracking()
            .AnyAsync(v => v.MaLuotKiemDuyet == maLuotKiemDuyet, cancellationToken);
        var count = await GetViolationCountAsync(turn.SellerId, cancellationToken);

        var isOpen = turn.QuyetDinh is null;
        var status = ai?.TrangThai ?? ModerationPolicy.AnalysisMissing;
        var (canApprove, blocked) = ModerationDecision.CanApprove(isOpen, ai?.TrangThai, ai?.MucDoTuKhoa, violation);
        var rejectionCounts = isOpen && !violation && ai is { TrangThai: ModerationPolicy.AnalysisDone, MucDoTuKhoa: ModerationPolicy.LevelLow };

        return new ModerationReview(
            turn.MaLuotKiemDuyet, turn.MaSanPham, turn.SellerId, isOpen, status, ai?.MucDoTuKhoa,
            ModerationDecision.ParseEvidence(ai?.DauHieu), ai?.GiaiThich, ai?.LoiXuLy, ai?.SoLanThu ?? 0,
            violation, count, ModerationPolicy.ViolationLockThreshold,
            canApprove, blocked, rejectionCounts, CanReanalyze: isOpen);
    }

    // =====================================================================
    // TRỢ GIÚP
    // =====================================================================
    private Task<OpenTurn?> FindOpenTurnAsync(int maSanPham, CancellationToken ct) =>
        db.LuotKiemDuyets.AsNoTracking()
            .Where(l => l.MaSanPham == maSanPham && l.QuyetDinh == null
                        && l.SanPham.TrangThai == ModerationPolicy.ProductPending && !l.SanPham.DaXoa)
            .Select(l => new OpenTurn(l.MaLuotKiemDuyet, l.MaSanPham, l.SanPham.Ten, l.SanPham.MoTa, l.SanPham.CuaHang.MaNguoiDung))
            .FirstOrDefaultAsync(ct);

    /// <summary>Đọc lượt kèm khóa dòng (UPDLOCK) khi dùng SQL Server; luôn nạp lại để không dùng bản cũ trong context.</summary>
    private async Task<LuotKiemDuyet?> LockTurnAsync(int turnId, CancellationToken ct)
    {
        var turn = db.Database.IsRelational()
            ? await db.LuotKiemDuyets
                .FromSqlInterpolated($"SELECT * FROM dbo.LuotKiemDuyet WITH (UPDLOCK, ROWLOCK) WHERE MaLuotKiemDuyet = {turnId}")
                .SingleOrDefaultAsync(ct)
            : await db.LuotKiemDuyets.SingleOrDefaultAsync(l => l.MaLuotKiemDuyet == turnId, ct);
        if (turn is not null) await db.Entry(turn).ReloadAsync(ct);
        return turn;
    }

    private async Task<SanPham?> LockProductAsync(int productId, CancellationToken ct)
    {
        var product = db.Database.IsRelational()
            ? await db.SanPhams
                .FromSqlInterpolated($"SELECT * FROM dbo.SanPham WITH (UPDLOCK, ROWLOCK) WHERE MaSanPham = {productId}")
                .SingleOrDefaultAsync(ct)
            : await db.SanPhams.SingleOrDefaultAsync(p => p.MaSanPham == productId, ct);
        if (product is not null) await db.Entry(product).ReloadAsync(ct);
        return product;
    }

    /// <summary>Cập nhật dòng PhanTichAI có sẵn (sp_GuiDuyetSanPham đã tạo QUEUED); chỉ tạo mới khi chưa có.</summary>
    private async Task<PhanTichAI> GetOrCreateAnalysisAsync(int turnId, DateTime now, CancellationToken ct)
    {
        var ai = await db.PhanTichAIs.FirstOrDefaultAsync(a => a.MaLuotKiemDuyet == turnId, ct);
        if (ai is not null)
        {
            await db.Entry(ai).ReloadAsync(ct);
            return ai;
        }
        ai = new PhanTichAI { MaLuotKiemDuyet = turnId, TrangThai = ModerationPolicy.AnalysisQueued, NgayTao = now };
        db.PhanTichAIs.Add(ai);
        return ai;
    }

    private void DiscardPendingChanges()
    {
        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            if (entry.State == EntityState.Added) entry.State = EntityState.Detached;
            else if (entry.State is EntityState.Modified or EntityState.Deleted) entry.State = EntityState.Unchanged;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 2601 or 2627 };

    private async Task TryNotifyAsync(int userId, string type, string title, string content, int? referenceId, CancellationToken ct)
    {
        try
        {
            var (ok, error) = await notifications.CreateAsync(userId, type, title, content, referenceId, ct);
            if (!ok) logger.LogWarning("Không tạo được thông báo cho người dùng {UserId}: {Error}", userId, error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Lỗi khi tạo thông báo cho người dùng {UserId}.", userId);
        }
    }

    private async Task TryAuditAsync(int? actorId, string action, int productId, string? detail, CancellationToken ct)
    {
        try
        {
            await auditLog.LogAsync(actorId, action, "SanPham", productId, "SUCCESS", Truncate(detail, 500));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Không ghi được nhật ký {Action} cho sản phẩm {ProductId}.", action, productId);
        }
    }

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? "" : value.Length <= max ? value : value[..max];
}
