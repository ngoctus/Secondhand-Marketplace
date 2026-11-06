using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Moderation;
using SecondHandMarketplace.Web.Services.Notifications;

namespace SecondHandMarketplace.Web.Services.ProductReview;

/// <summary>Lỗi do stored procedure/trigger ném ra (số lỗi SQL + thông báo tiếng Việt do chính SQL cung cấp).</summary>
public sealed class ReviewDatabaseException(int number, string message) : Exception(message)
{
    public int Number { get; } = number;
}

public class ProductReviewService(
    AppDbContext db,
    IModerationService moderation,
    INotificationService notifications,
    IAuditLogService auditLog,
    ILogger<ProductReviewService> logger) : IProductReviewService
{
    // Số lỗi SQL đã biết (06_procedures.sql, 15_kiemduyet_tukhoa.sql)
    private const int ErrNotPending = 50022;
    private static readonly HashSet<int> KnownSqlErrors = new()
    {
        50010, 50011, 50012, 50013,                 // sp_ChuyenTrangThaiSanPham
        50020, 50021, 50022, 50023,                 // sp_QuyetDinhKiemDuyet
        50210, 50211                                // trigger chặn duyệt của C2
    };

    // =====================================================================
    // DANH SÁCH
    // =====================================================================
    public async Task<ProductListPage> GetListAsync(string? tab, int page, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        return tab == ProductReviewTabs.Active
            ? await GetActiveAsync(page, cancellationToken)
            : await GetPendingAsync(page, cancellationToken);
    }

    private async Task<ProductListPage> GetPendingAsync(int page, CancellationToken ct)
    {
        // Chỉ lượt đang chờ Admin: lượt mở + sản phẩm PENDING_REVIEW. Sản phẩm bị C2 tự từ chối (HIGH) đã đóng lượt nên không có ở đây.
        var query = db.LuotKiemDuyets.AsNoTracking()
            .Where(l => l.QuyetDinh == null && l.SanPham.TrangThai == "PENDING_REVIEW" && !l.SanPham.DaXoa);
        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(l => l.ThoiGianGui).ThenBy(l => l.MaLuotKiemDuyet)
            .Skip((page - 1) * ProductReviewRules.PageSize).Take(ProductReviewRules.PageSize)
            .Select(l => new
            {
                l.MaLuotKiemDuyet, l.ThoiGianGui, l.MaSanPham,
                l.SanPham.Ten, l.SanPham.Gia, l.SanPham.TrangThai,
                TenDanhMuc = l.SanPham.DanhMuc != null ? l.SanPham.DanhMuc.Ten : null,
                Anh = l.SanPham.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault(),
                l.SanPham.MaCuaHang, l.SanPham.CuaHang.TenShop,
                MaNguoiBan = l.SanPham.CuaHang.MaNguoiDung,
                TenNguoiBan = l.SanPham.CuaHang.NguoiDung.HoTen,
                EmailNguoiBan = l.SanPham.CuaHang.NguoiDung.Email
            })
            .ToListAsync(ct);

        var turnIds = rows.Select(r => r.MaLuotKiemDuyet).ToList();
        var analyses = turnIds.Count == 0
            ? new Dictionary<int, (string Status, string? Level)>()
            : (await db.PhanTichAIs.AsNoTracking()
                .Where(a => turnIds.Contains(a.MaLuotKiemDuyet))
                .Select(a => new { a.MaLuotKiemDuyet, a.TrangThai, a.MucDoTuKhoa })
                .ToListAsync(ct)).ToDictionary(a => a.MaLuotKiemDuyet, a => (Status: a.TrangThai, Level: a.MucDoTuKhoa));

        var items = rows.Select(r =>
        {
            var has = analyses.TryGetValue(r.MaLuotKiemDuyet, out var a);
            return new ProductListItem(r.MaSanPham, r.MaLuotKiemDuyet, r.Ten, r.Gia, r.TenDanhMuc, r.Anh, r.TrangThai,
                r.MaCuaHang, r.TenShop, r.MaNguoiBan, r.TenNguoiBan, r.EmailNguoiBan, r.ThoiGianGui,
                has ? a.Status : ModerationPolicy.AnalysisMissing, has ? a.Level : null);
        }).ToList();

        return new ProductListPage(ProductReviewTabs.Pending, items, page, total, TotalPages(total));
    }

    private async Task<ProductListPage> GetActiveAsync(int page, CancellationToken ct)
    {
        var query = db.SanPhams.AsNoTracking().Where(p => p.TrangThai == "ACTIVE" && !p.DaXoa);
        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(p => p.NgayCapNhat).ThenByDescending(p => p.MaSanPham)
            .Skip((page - 1) * ProductReviewRules.PageSize).Take(ProductReviewRules.PageSize)
            .Select(p => new ProductListItem(
                p.MaSanPham, null, p.Ten, p.Gia, p.DanhMuc != null ? p.DanhMuc.Ten : null,
                p.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault(),
                p.TrangThai, p.MaCuaHang, p.CuaHang.TenShop, p.CuaHang.MaNguoiDung,
                p.CuaHang.NguoiDung.HoTen, p.CuaHang.NguoiDung.Email, p.NgayCapNhat, null, null))
            .ToListAsync(ct);
        return new ProductListPage(ProductReviewTabs.Active, rows, page, total, TotalPages(total));
    }

    // =====================================================================
    // CHI TIẾT
    // =====================================================================
    public async Task<ProductReviewDetail?> GetDetailAsync(int maSanPham, CancellationToken cancellationToken = default)
    {
        var p = await db.SanPhams.AsNoTracking()
            .Where(x => x.MaSanPham == maSanPham && !x.DaXoa)
            .Select(x => new
            {
                x.MaSanPham, x.Ten, x.MoTa, x.Gia, x.TrangThai, x.LyDoGo, x.NgayTao, x.NgayCapNhat,
                TenDanhMuc = x.DanhMuc != null ? x.DanhMuc.Ten : null,
                TenTinhTrang = x.TinhTrang != null ? x.TinhTrang.Ten : null,
                x.MaCuaHang, x.CuaHang.TenShop, TrangThaiShop = x.CuaHang.TrangThai,
                MaNguoiBan = x.CuaHang.MaNguoiDung,
                TenNguoiBan = x.CuaHang.NguoiDung.HoTen,
                EmailNguoiBan = x.CuaHang.NguoiDung.Email,
                TrangThaiNguoiBan = x.CuaHang.NguoiDung.TrangThai
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (p is null) return null;

        var images = await db.AnhSanPhams.AsNoTracking()
            .Where(a => a.MaSanPham == maSanPham)
            .OrderBy(a => a.ThuTu).Select(a => a.DuongDan)
            .ToListAsync(cancellationToken);

        var turns = await db.LuotKiemDuyets.AsNoTracking()
            .Where(l => l.MaSanPham == maSanPham)
            .OrderByDescending(l => l.MaLuotKiemDuyet)
            .ToListAsync(cancellationToken);
        var turnIds = turns.Select(t => t.MaLuotKiemDuyet).ToList();
        var analyses = (await db.PhanTichAIs.AsNoTracking()
            .Where(a => turnIds.Contains(a.MaLuotKiemDuyet)).ToListAsync(cancellationToken))
            .ToDictionary(a => a.MaLuotKiemDuyet);
        var violationTurns = (await db.LichSuViPhams.AsNoTracking()
            .Where(v => v.MaSanPham == maSanPham).Select(v => v.MaLuotKiemDuyet).ToListAsync(cancellationToken)).ToHashSet();
        var history = await db.LichSuTrangThaiSanPhams.AsNoTracking()
            .Where(h => h.MaSanPham == maSanPham)
            .OrderByDescending(h => h.ThoiGian).ThenByDescending(h => h.MaLichSuTrangThaiSanPham)
            .ToListAsync(cancellationToken);

        var userIds = turns.Where(t => t.MaQuanTriVien != null).Select(t => t.MaQuanTriVien!.Value)
            .Concat(history.Where(h => h.NguoiThucHienId != null).Select(h => h.NguoiThucHienId!.Value))
            .Distinct().ToList();
        var names = userIds.Count == 0
            ? new Dictionary<int, string>()
            : await db.NguoiDungs.AsNoTracking().Where(u => userIds.Contains(u.MaNguoiDung))
                .ToDictionaryAsync(u => u.MaNguoiDung, u => u.HoTen, cancellationToken);
        string? NameOf(int? id) => id is not null && names.TryGetValue(id.Value, out var n) ? n : null;

        var turnRows = turns.Select(t =>
        {
            analyses.TryGetValue(t.MaLuotKiemDuyet, out var a);
            return new ReviewTurnRow(t.MaLuotKiemDuyet, t.ThoiGianGui, t.QuyetDinh, t.LyDo, t.MaQuanTriVien, NameOf(t.MaQuanTriVien),
                t.ThoiGianQuyetDinh, t.QuyetDinhTuDong, a?.TrangThai ?? ModerationPolicy.AnalysisMissing, a?.MucDoTuKhoa,
                a?.GiaiThich, a?.LoiXuLy, violationTurns.Contains(t.MaLuotKiemDuyet),
                ModerationDecision.ParseEvidence(a?.DauHieu));
        }).ToList();

        var historyRows = history.Select(h => new StatusHistoryRow(h.TrangThaiCu, h.TrangThaiMoi, h.LoaiNguoiThucHien,
            h.LoaiNguoiThucHien == "SYSTEM" ? null : NameOf(h.NguoiThucHienId), h.LyDo, h.ThoiGian)).ToList();

        return new ProductReviewDetail
        {
            MaSanPham = p.MaSanPham, Ten = p.Ten, MoTa = p.MoTa, Gia = p.Gia, TrangThai = p.TrangThai, LyDoGo = p.LyDoGo,
            TenDanhMuc = p.TenDanhMuc, TenTinhTrang = p.TenTinhTrang, NgayTao = p.NgayTao, NgayCapNhat = p.NgayCapNhat,
            MaCuaHang = p.MaCuaHang, TenShop = p.TenShop, TrangThaiShop = p.TrangThaiShop,
            MaNguoiBan = p.MaNguoiBan, TenNguoiBan = p.TenNguoiBan, EmailNguoiBan = p.EmailNguoiBan, TrangThaiNguoiBan = p.TrangThaiNguoiBan,
            SoViPhamNguoiBan = await moderation.GetViolationCountAsync(p.MaNguoiBan, cancellationToken),
            NguongKhoa = ModerationPolicy.ViolationLockThreshold,
            Anh = images,
            Review = await moderation.GetReviewAsync(maSanPham, cancellationToken),
            Turns = turnRows,
            StatusHistory = historyRows
        };
    }

    // =====================================================================
    // QUYẾT ĐỊNH
    // =====================================================================
    public async Task<ProductDecisionResult> ApproveAsync(int adminId, int maSanPham, CancellationToken cancellationToken = default)
    {
        var denied = await RequireAdminAsync(adminId, cancellationToken);
        if (denied is not null) return denied;

        // Kiểm tra lại trạng thái hiện tại (không tin dữ liệu form). DB vẫn là chốt chặn cuối: sp_QuyetDinhKiemDuyet khóa lượt
        // (UPDLOCK) và trigger của C2 chặn duyệt QUEUED/FAILED/chưa quét/HIGH, nên hai request đồng thời không thể cùng thành công.
        var review = await moderation.GetReviewAsync(maSanPham, cancellationToken);
        if (review is null)
            return ProductDecisionResult.Fail("Sản phẩm không còn ở trạng thái chờ duyệt (có thể đã được xử lý).", alreadyDecided: true);
        if (!review.CanApprove)
            return ProductDecisionResult.Fail(review.ApproveBlockedReason ?? "Sản phẩm chưa thể duyệt.");

        try
        {
            await ExecuteDecisionAsync(maSanPham, adminId, approve: true, reason: null, cancellationToken);
        }
        catch (ReviewDatabaseException ex)
        {
            return DecisionFailure(ex);
        }
        return await AfterDecisionAsync(adminId, maSanPham, review.MaNguoiBan, "ACTIVE", approve: true, reason: null, cancellationToken);
    }

    public async Task<ProductDecisionResult> RejectAsync(int adminId, int maSanPham, string? reason, CancellationToken cancellationToken = default)
    {
        var denied = await RequireAdminAsync(adminId, cancellationToken);
        if (denied is not null) return denied;
        var reasonError = ValidateReason(reason, out var cleanReason);
        if (reasonError is not null) return ProductDecisionResult.Fail(reasonError);

        var review = await moderation.GetReviewAsync(maSanPham, cancellationToken);
        if (review is null)
            return ProductDecisionResult.Fail("Sản phẩm không còn ở trạng thái chờ duyệt (có thể đã được xử lý).", alreadyDecided: true);
        // Từ chối thường vẫn được khi QUEUED/FAILED/chưa quét (procedure cho phép, không ghi vi phạm vì chưa có kết quả LOW hoàn chỉnh).
        // HIGH không do Admin xử lý: C2 đã tự từ chối và ghi vi phạm; C1 không can thiệp.
        if (review.Level == ModerationPolicy.LevelHigh)
            return ProductDecisionResult.Fail("Sản phẩm có từ khóa cấm mức HIGH do hệ thống xử lý, Admin không quyết định thủ công.");

        try
        {
            await ExecuteDecisionAsync(maSanPham, adminId, approve: false, cleanReason, cancellationToken);
        }
        catch (ReviewDatabaseException ex)
        {
            return DecisionFailure(ex);
        }
        return await AfterDecisionAsync(adminId, maSanPham, review.MaNguoiBan, "REJECTED", approve: false, cleanReason, cancellationToken);
    }

    public async Task<ProductDecisionResult> RemoveProductAsync(int adminId, int maSanPham, string? reason, CancellationToken cancellationToken = default)
    {
        var denied = await RequireAdminAsync(adminId, cancellationToken);
        if (denied is not null) return denied;
        var reasonError = ValidateReason(reason, out var cleanReason);
        if (reasonError is not null) return ProductDecisionResult.Fail(reasonError);

        var product = await db.SanPhams.AsNoTracking()
            .Where(p => p.MaSanPham == maSanPham && !p.DaXoa)
            .Select(p => new { p.Ten, p.TrangThai, SellerId = p.CuaHang.MaNguoiDung })
            .SingleOrDefaultAsync(cancellationToken);
        if (product is null) return ProductDecisionResult.Fail("Không tìm thấy sản phẩm.");
        if (product.TrangThai != "ACTIVE")
            return ProductDecisionResult.Fail("Chỉ gỡ được sản phẩm đang bán (ACTIVE).");

        try
        {
            await ExecuteRemoveAsync(maSanPham, adminId, cleanReason!, cancellationToken);
        }
        catch (ReviewDatabaseException ex)
        {
            return DecisionFailure(ex);
        }

        await TryAuditAsync(adminId, "REMOVE_PRODUCT", maSanPham, cleanReason, cancellationToken);
        await TryNotifyAsync(product.SellerId, "Sản phẩm của bạn đã bị gỡ",
            Truncate($"Sản phẩm \"{Truncate(product.Ten, 120)}\" đã bị quản trị viên gỡ khỏi sàn. Lý do: {cleanReason}", 1000), maSanPham, cancellationToken);
        AdminDecisionFollowUp? followUp = null;
        try
        {
            var lockedNow = await moderation.EnforceViolationThresholdAsync(product.SellerId, cancellationToken);
            var count = await moderation.GetViolationCountAsync(product.SellerId, cancellationToken);
            var isLocked = await db.NguoiDungs.AsNoTracking().Where(u => u.MaNguoiDung == product.SellerId).Select(u => u.TrangThai == "LOCKED").SingleOrDefaultAsync(cancellationToken);
            followUp = new AdminDecisionFollowUp(false, count, lockedNow, isLocked);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EnforceViolationThresholdAsync lỗi sau khi gỡ sản phẩm {ProductId}.", maSanPham);
        }

        return new ProductDecisionResult(true, null, false, "REMOVED", followUp, null);
    }

    /// <summary>Gọi sp_DuyetSanPham / sp_TuChoiSanPham (transaction + UPDLOCK nằm trong procedure). Tách ra để kiểm thử.</summary>
    protected virtual async Task ExecuteDecisionAsync(int maSanPham, int adminId, bool approve, string? reason, CancellationToken ct)
    {
        try
        {
            if (approve)
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC dbo.sp_DuyetSanPham @MaSanPham = {maSanPham}, @QuanTriVienId = {adminId}", ct);
            else
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC dbo.sp_TuChoiSanPham @MaSanPham = {maSanPham}, @QuanTriVienId = {adminId}, @LyDo = {reason}", ct);
        }
        catch (SqlException ex) when (KnownSqlErrors.Contains(ex.Number))
        {
            throw new ReviewDatabaseException(ex.Number, ex.Message);
        }
    }

    protected virtual async Task ExecuteRemoveAsync(int maSanPham, int adminId, string reason, CancellationToken ct)
    {
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC dbo.sp_ChuyenTrangThaiSanPham @MaSanPham = {maSanPham}, @TrangThaiMoi = 'REMOVED', @LoaiNguoiThucHien = 'ADMIN', @NguoiThucHienId = {adminId}, @LyDo = {reason}", ct);
        }
        catch (SqlException ex) when (KnownSqlErrors.Contains(ex.Number))
        {
            throw new ReviewDatabaseException(ex.Number, ex.Message);
        }
    }

    /// <summary>Chỉ chạy SAU KHI quyết định đã lưu thành công: ngưỡng khóa (C2) rồi thông báo người bán.</summary>
    private async Task<ProductDecisionResult> AfterDecisionAsync(
        int adminId, int maSanPham, int sellerId, string newStatus, bool approve, string? reason, CancellationToken ct)
    {
        AdminDecisionFollowUp? followUp = null;
        string? warning = null;
        try
        {
            followUp = await moderation.AfterAdminDecisionAsync(maSanPham, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Quyết định đã commit; chỉ phần kiểm tra khóa tài khoản lỗi. C2 có thể chạy lại bằng EnforceViolationThresholdAsync.
            logger.LogError(ex, "AfterAdminDecisionAsync lỗi sau khi quyết định sản phẩm {ProductId}.", maSanPham);
            warning = "Quyết định đã lưu nhưng chưa kiểm tra được ngưỡng khóa tài khoản người bán; cần chạy lại kiểm tra.";
        }

        var title = approve ? "Sản phẩm đã được duyệt" : "Sản phẩm bị từ chối";
        var name = await db.SanPhams.AsNoTracking().Where(p => p.MaSanPham == maSanPham).Select(p => p.Ten).SingleOrDefaultAsync(ct) ?? "";
        var content = approve
            ? $"Sản phẩm \"{Truncate(name, 120)}\" đã được duyệt và đang hiển thị trên sàn."
            : $"Sản phẩm \"{Truncate(name, 120)}\" bị từ chối. Lý do: {reason}";
        await TryNotifyAsync(sellerId, title, Truncate(content, 1000), maSanPham, ct);

        return new ProductDecisionResult(true, null, false, newStatus, followUp, warning);
    }

    // =====================================================================
    // LỊCH SỬ VI PHẠM
    // =====================================================================
    public async Task<ViolationPage> GetViolationsAsync(int? sellerId, int page, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        var query = db.LichSuViPhams.AsNoTracking().AsQueryable();
        string? sellerName = null;
        if (sellerId is not null)
        {
            query = query.Where(v => v.MaNguoiBan == sellerId);
            sellerName = await db.NguoiDungs.AsNoTracking().Where(u => u.MaNguoiDung == sellerId)
                .Select(u => u.HoTen).SingleOrDefaultAsync(cancellationToken);
        }
        var total = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderByDescending(v => v.ThoiGian).ThenByDescending(v => v.MaViPham)
            .Skip((page - 1) * ProductReviewRules.PageSize).Take(ProductReviewRules.PageSize)
            .ToListAsync(cancellationToken);

        var sellerIds = rows.Select(r => r.MaNguoiBan).Distinct().ToList();
        var productIds = rows.Select(r => r.MaSanPham).Distinct().ToList();
        var userIds = sellerIds.Concat(rows.Where(r => r.MaQuanTriVien != null).Select(r => r.MaQuanTriVien!.Value)).Distinct().ToList();

        var users = userIds.Count == 0 ? new Dictionary<int, NguoiDung>()
            : await db.NguoiDungs.AsNoTracking().Where(u => userIds.Contains(u.MaNguoiDung))
                .ToDictionaryAsync(u => u.MaNguoiDung, cancellationToken);
        var products = productIds.Count == 0 ? new Dictionary<int, string>()
            : await db.SanPhams.AsNoTracking().Where(p => productIds.Contains(p.MaSanPham))
                .ToDictionaryAsync(p => p.MaSanPham, p => p.Ten, cancellationToken);
        // Bộ đếm chung của C2: số dòng LichSuViPham theo người bán (không tạo bộ đếm riêng).
        var counts = sellerIds.Count == 0 ? new Dictionary<int, int>()
            : (await db.LichSuViPhams.AsNoTracking().Where(v => sellerIds.Contains(v.MaNguoiBan))
                .GroupBy(v => v.MaNguoiBan).Select(g => new { Id = g.Key, N = g.Count() })
                .ToListAsync(cancellationToken)).ToDictionary(x => x.Id, x => x.N);

        var items = rows.Select(v =>
        {
            users.TryGetValue(v.MaNguoiBan, out var seller);
            var admin = v.MaQuanTriVien is { } aid && users.TryGetValue(aid, out var a) ? a.HoTen : null;
            return new ViolationRow(v.MaViPham, v.MaNguoiBan, seller?.HoTen ?? "", seller?.Email ?? "", seller?.TrangThai ?? "",
                seller?.VaiTro ?? "", counts.GetValueOrDefault(v.MaNguoiBan), v.MaSanPham,
                products.GetValueOrDefault(v.MaSanPham) ?? "", v.MaLuotKiemDuyet, v.MucDo, v.NguonGhiNhan, v.ThoiGian,
                v.MaQuanTriVien, admin, ModerationDecision.ParseEvidence(v.ChiTietTuKhoa));
        }).ToList();

        return new ViolationPage(sellerId, sellerName, items, page, total, TotalPages(total), ModerationPolicy.ViolationLockThreshold);
    }

    // =====================================================================
    // TRỢ GIÚP
    // =====================================================================
    private async Task<ProductDecisionResult?> RequireAdminAsync(int adminId, CancellationToken ct)
    {
        var ok = await db.NguoiDungs.AsNoTracking()
            .AnyAsync(u => u.MaNguoiDung == adminId && u.VaiTro == AppRoles.Admin && u.TrangThai == "ACTIVE", ct);
        return ok ? null : ProductDecisionResult.Fail("Chỉ quản trị viên đang hoạt động mới được thực hiện thao tác này.");
    }

    private static string? ValidateReason(string? reason, out string? clean)
    {
        clean = reason?.Trim();
        if (string.IsNullOrEmpty(clean) || clean.Length < ProductReviewRules.MinReasonLength)
            return $"Lý do bắt buộc và phải có ít nhất {ProductReviewRules.MinReasonLength} ký tự.";
        if (clean.Length > ProductReviewRules.MaxReasonLength)
            return $"Lý do tối đa {ProductReviewRules.MaxReasonLength} ký tự.";
        return null;
    }

    private static ProductDecisionResult DecisionFailure(ReviewDatabaseException ex) =>
        ex.Number == ErrNotPending
            ? ProductDecisionResult.Fail("Sản phẩm không còn ở trạng thái chờ duyệt (đã có người xử lý trước).", alreadyDecided: true)
            : ProductDecisionResult.Fail(ex.Message);

    private async Task TryNotifyAsync(int userId, string title, string content, int productId, CancellationToken ct)
    {
        try
        {
            var (ok, error) = await notifications.CreateAsync(userId, NotificationTypes.Product, title, content, productId, ct);
            if (!ok) logger.LogWarning("Không tạo được thông báo cho người dùng {UserId}: {Error}", userId, error);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Lỗi khi tạo thông báo cho người dùng {UserId}.", userId);
        }
    }

    private async Task TryAuditAsync(int adminId, string action, int productId, string? reason, CancellationToken ct)
    {
        try
        {
            await auditLog.LogAsync(adminId, action, "SanPham", productId, "SUCCESS", Truncate(reason, 500));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Không ghi được nhật ký {Action} cho sản phẩm {ProductId}.", action, productId);
        }
    }

    private static int TotalPages(int total) => (int)Math.Ceiling(total / (double)ProductReviewRules.PageSize);

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? "" : value.Length <= max ? value : value[..max];
}
