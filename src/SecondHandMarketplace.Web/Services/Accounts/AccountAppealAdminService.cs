using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.AuditLogs;
using SecondHandMarketplace.Web.Services.Auth;
using SecondHandMarketplace.Web.Services.Notifications;

namespace SecondHandMarketplace.Web.Services.Accounts;

/// <summary>
/// Cài đặt nghiệp vụ Khiếu nại & Mở khóa tài khoản phía Admin (Task KN-03):
/// quản lý hàng chờ, xem chi tiết, duyệt mở khóa (transaction), từ chối và cấu hình Cooldown / VietQR.
/// </summary>
public sealed class AccountAppealAdminService(
    AppDbContext db,
    IUserAdminService userAdmin,
    INotificationService notifications,
    IAuditLogService auditLog,
    ILogger<AccountAppealAdminService> logger,
    IEmailSender? emailSender = null) : IAccountAppealAdminService
{
    private const int MaxAttemptsPerCycleLimit = 20;
    private const int MaxCooldownMinutesLimit = 365 * 24 * 60;
    private const decimal MaxUnlockFeeLimit = 100_000_000m;

    public async Task<AdminAccountAppealPageDto> GetAdminListAsync(
        AdminAccountAppealFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Clamp(filter.Page, 1, 10000);
        var normalizedStatus = NormalizeStatusFilter(filter.TrangThai);
        var normalizedType = NormalizeTypeFilter(filter.LoaiYeuCau);
        var keyword = filter.TuKhoa?.Trim();

        var pendingCount = await CountPendingAsync(cancellationToken);

        var query = db.KhieuNaiTaiKhoans.AsNoTracking().AsQueryable();

        if (normalizedStatus is not null)
            query = query.Where(k => k.TrangThai == normalizedStatus);

        if (normalizedType is not null)
            query = query.Where(k => k.LoaiYeuCau == normalizedType);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var idText = keyword.StartsWith('#') ? keyword[1..] : keyword;
            var hasId = int.TryParse(idText, out var parsedId) && parsedId > 0;
            query = query.Where(k =>
                (hasId && k.MaKhieuNai == parsedId)
                || k.NguoiDung.HoTen.Contains(keyword)
                || k.NguoiDung.Email.Contains(keyword)
                || (k.MaGiaoDichThamChieu != null && k.MaGiaoDichThamChieu.Contains(keyword)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)AccountAppealRules.PageSize);

        var ordered = normalizedStatus == AccountAppealRules.StatusPending
            ? query.OrderBy(k => k.NgayTao).ThenBy(k => k.MaKhieuNai)
            : query.OrderByDescending(k => k.TrangThai == AccountAppealRules.StatusPending)
                .ThenByDescending(k => k.NgayTao)
                .ThenByDescending(k => k.MaKhieuNai);

        var items = await ordered
            .Skip((page - 1) * AccountAppealRules.PageSize)
            .Take(AccountAppealRules.PageSize)
            .Select(k => new AdminAccountAppealRowDto(
                k.MaKhieuNai,
                k.MaNguoiDung,
                k.NguoiDung.HoTen,
                k.NguoiDung.Email,
                k.NguoiDung.TrangThai,
                k.NguoiDung.SoLanBiKhoa,
                k.LoaiYeuCau,
                k.LanKhoaThu,
                k.YeuCauPhi,
                k.SoTienPhi,
                k.BangChungAnh != null && k.BangChungAnh != "",
                k.TrangThai,
                k.NgayTao,
                k.NgayXuLy,
                k.NguoiXuLy != null ? k.NguoiXuLy.HoTen : null))
            .ToListAsync(cancellationToken);

        return new AdminAccountAppealPageDto(
            items,
            normalizedStatus,
            normalizedType,
            keyword,
            page,
            totalItems,
            totalPages,
            pendingCount);
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default) =>
        db.KhieuNaiTaiKhoans.AsNoTracking()
            .CountAsync(k => k.TrangThai == AccountAppealRules.StatusPending, cancellationToken);

    public async Task<AdminAccountAppealDetailDto?> GetAdminDetailAsync(
        int maKhieuNai,
        CancellationToken cancellationToken = default)
    {
        var appeal = await db.KhieuNaiTaiKhoans.AsNoTracking()
            .Include(k => k.NguoiDung)
            .Include(k => k.NguoiXuLy)
            .SingleOrDefaultAsync(k => k.MaKhieuNai == maKhieuNai, cancellationToken);

        if (appeal is null) return null;

        var user = appeal.NguoiDung;
        var userId = user.MaNguoiDung;
        var mocTinh = user.MocTinhViPham;

        var shop = await db.CuaHangs.AsNoTracking()
            .Where(c => c.MaNguoiDung == userId)
            .Select(c => new { c.TenShop, c.TrangThai })
            .FirstOrDefaultAsync(cancellationToken);

        // Thống kê vi phạm hiệu lực vs tổng lịch sử (đồng bộ 2 nguồn: Kiểm duyệt + Báo cáo RESOLVED)
        var totalModViolations = await db.LichSuViPhams.AsNoTracking()
            .CountAsync(v => v.MaNguoiBan == userId, cancellationToken);
        var effectiveModViolations = mocTinh is null
            ? totalModViolations
            : await db.LichSuViPhams.AsNoTracking()
                .CountAsync(v => v.MaNguoiBan == userId && v.ThoiGian > mocTinh.Value, cancellationToken);

        var totalReportProducts = await db.BaoCaos.AsNoTracking()
            .Where(b => b.TrangThai == "RESOLVED"
                        && b.LoaiDoiTuong == "PRODUCT"
                        && db.SanPhams.Any(p => p.MaSanPham == b.MaDoiTuong && p.CuaHang.MaNguoiDung == userId))
            .Select(b => b.MaDoiTuong)
            .Distinct()
            .CountAsync(cancellationToken);

        var effectiveReportProducts = mocTinh is null
            ? totalReportProducts
            : await db.BaoCaos.AsNoTracking()
                .Where(b => b.TrangThai == "RESOLVED"
                            && b.LoaiDoiTuong == "PRODUCT"
                            && b.NgayXuLy != null && b.NgayXuLy > mocTinh.Value
                            && db.SanPhams.Any(p => p.MaSanPham == b.MaDoiTuong && p.CuaHang.MaNguoiDung == userId))
                .Select(b => b.MaDoiTuong)
                .Distinct()
                .CountAsync(cancellationToken);

        var totalReportUserShop = await db.BaoCaos.AsNoTracking()
            .Where(b => b.TrangThai == "RESOLVED"
                        && ((b.LoaiDoiTuong == "USER" && b.MaDoiTuong == userId)
                            || (b.LoaiDoiTuong == "SHOP" && db.CuaHangs.Any(c => c.MaCuaHang == b.MaDoiTuong && c.MaNguoiDung == userId))))
            .CountAsync(cancellationToken);

        var effectiveReportUserShop = mocTinh is null
            ? totalReportUserShop
            : await db.BaoCaos.AsNoTracking()
                .Where(b => b.TrangThai == "RESOLVED"
                            && b.NgayXuLy != null && b.NgayXuLy > mocTinh.Value
                            && ((b.LoaiDoiTuong == "USER" && b.MaDoiTuong == userId)
                                || (b.LoaiDoiTuong == "SHOP" && db.CuaHangs.Any(c => c.MaCuaHang == b.MaDoiTuong && c.MaNguoiDung == userId))))
                .CountAsync(cancellationToken);

        var soViPhamHieuLuc = effectiveModViolations + effectiveReportProducts + effectiveReportUserShop;
        var tongViPhamLichSu = totalModViolations + totalReportProducts + totalReportUserShop;

        // Danh sách vi phạm gần đây để Admin đối chiếu
        var recentViolations = new List<LockedViolationItemDto>();

        var recentModRows = await db.LichSuViPhams.AsNoTracking()
            .Where(v => v.MaNguoiBan == userId)
            .OrderByDescending(v => v.ThoiGian)
            .Take(10)
            .Select(v => new { v.MucDo, v.NguonGhiNhan, v.MaSanPham, v.ThoiGian })
            .ToListAsync(cancellationToken);

        foreach (var v in recentModRows)
        {
            var statusTag = mocTinh is null || v.ThoiGian > mocTinh.Value ? "Hiệu lực" : "Trước mở khóa";
            var sourceText = v.NguonGhiNhan == "AUTO_HIGH" ? "Hệ thống tự chặn HIGH" : $"Admin từ chối mức {v.MucDo}";
            recentViolations.Add(new LockedViolationItemDto(
                $"Kiểm duyệt ({statusTag})",
                $"{sourceText} — Sản phẩm #{v.MaSanPham}",
                v.ThoiGian));
        }

        var recentReportRows = await db.BaoCaos.AsNoTracking()
            .Where(b => b.TrangThai == "RESOLVED"
                        && ((b.LoaiDoiTuong == "USER" && b.MaDoiTuong == userId)
                            || (b.LoaiDoiTuong == "SHOP" && db.CuaHangs.Any(c => c.MaCuaHang == b.MaDoiTuong && c.MaNguoiDung == userId))
                            || (b.LoaiDoiTuong == "PRODUCT" && db.SanPhams.Any(p => p.MaSanPham == b.MaDoiTuong && p.CuaHang.MaNguoiDung == userId))))
            .OrderByDescending(b => b.NgayXuLy ?? b.NgayTao)
            .Take(10)
            .Select(b => new { b.LoaiDoiTuong, b.MaDoiTuong, b.LyDo, Time = b.NgayXuLy ?? b.NgayTao })
            .ToListAsync(cancellationToken);

        foreach (var b in recentReportRows)
        {
            var statusTag = mocTinh is null || b.Time > mocTinh.Value ? "Hiệu lực" : "Trước mở khóa";
            recentViolations.Add(new LockedViolationItemDto(
                $"Báo cáo ({statusTag})",
                $"Báo cáo {b.LoaiDoiTuong} #{b.MaDoiTuong}: {b.LyDo}",
                b.Time));
        }

        recentViolations = recentViolations
            .OrderByDescending(x => x.ThoiGian)
            .Take(15)
            .ToList();

        var otherAppeals = await db.KhieuNaiTaiKhoans.AsNoTracking()
            .Where(k => k.MaNguoiDung == userId && k.MaKhieuNai != maKhieuNai)
            .OrderByDescending(k => k.NgayTao)
            .Take(20)
            .Select(k => new AccountAppealSummaryDto(
                k.MaKhieuNai,
                k.LoaiYeuCau,
                k.LanKhoaThu,
                k.NoiDung,
                k.BangChungAnh,
                k.YeuCauPhi,
                k.SoTienPhi,
                k.MaGiaoDichThamChieu,
                k.TrangThai,
                k.GhiChuAdmin,
                k.NgayTao,
                k.NgayXuLy))
            .ToListAsync(cancellationToken);

        return new AdminAccountAppealDetailDto(
            appeal.MaKhieuNai,
            user.MaNguoiDung,
            user.HoTen,
            user.Email,
            user.SoDienThoai,
            user.TrangThai,
            user.SoLanBiKhoa,
            user.LyDoKhoa,
            user.ThoiGianKhoa,
            user.MocTinhViPham,
            shop?.TenShop,
            shop?.TrangThai,
            appeal.LoaiYeuCau,
            appeal.LanKhoaThu,
            appeal.LyDoKhoaLucGui,
            appeal.NoiDung,
            appeal.BangChungAnh,
            appeal.YeuCauPhi,
            appeal.SoTienPhi,
            appeal.MaGiaoDichThamChieu,
            appeal.TrangThai,
            appeal.GhiChuAdmin,
            appeal.NguoiXuLyId,
            appeal.NguoiXuLy?.HoTen,
            appeal.NgayTao,
            appeal.NgayXuLy,
            soViPhamHieuLuc,
            tongViPhamLichSu,
            recentViolations,
            otherAppeals);
    }

    public async Task<AdminAccountAppealActionResult> ApproveAsync(
        int adminId,
        int maKhieuNai,
        string? ghiChuAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!await IsActiveAdminAsync(adminId, cancellationToken))
            return AdminAccountAppealActionResult.Fail("Chỉ quản trị viên đang hoạt động mới được xử lý đơn khiếu nại.");

        var cleanNote = ghiChuAdmin?.Trim();
        if (!string.IsNullOrEmpty(cleanNote) && cleanNote.Length > AccountAppealRules.MaxAdminNoteLength)
            return AdminAccountAppealActionResult.Fail($"Ghi chú quản trị viên tối đa {AccountAppealRules.MaxAdminNoteLength} ký tự.");

        IDbContextTransaction? tx = null;
        if (db.Database.IsRelational())
            tx = await db.Database.BeginTransactionAsync(cancellationToken);

        KhieuNaiTaiKhoan? appeal;
        bool giamSoLanBiKhoa;
        string userEmail;
        string userName;
        int targetUserId;
        string loaiYeuCau;
        int lanKhoaThu;

        try
        {
            appeal = await db.KhieuNaiTaiKhoans
                .Include(k => k.NguoiDung)
                .SingleOrDefaultAsync(k => k.MaKhieuNai == maKhieuNai, cancellationToken);

            if (appeal is null)
            {
                if (tx is not null) await tx.RollbackAsync(cancellationToken);
                return AdminAccountAppealActionResult.Fail("Không tìm thấy đơn khiếu nại.");
            }

            if (appeal.TrangThai != AccountAppealRules.StatusPending)
            {
                if (tx is not null) await tx.RollbackAsync(cancellationToken);
                return AdminAccountAppealActionResult.Fail("Đơn khiếu nại này đã được xử lý trước đó.");
            }

            giamSoLanBiKhoa = string.Equals(appeal.LoaiYeuCau, AccountAppealRules.TypeAppeal, StringComparison.OrdinalIgnoreCase);
            userEmail = appeal.NguoiDung.Email;
            userName = appeal.NguoiDung.HoTen;
            targetUserId = appeal.MaNguoiDung;
            loaiYeuCau = appeal.LoaiYeuCau;
            lanKhoaThu = appeal.LanKhoaThu;

            appeal.TrangThai = AccountAppealRules.StatusApproved;
            appeal.GhiChuAdmin = string.IsNullOrEmpty(cleanNote) ? null : cleanNote;
            appeal.NguoiXuLyId = adminId;
            appeal.NgayXuLy = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellationToken);

            var (unlocked, unlockError) = await userAdmin.UnlockUserAsync(
                adminId,
                targetUserId,
                giamSoLanBiKhoa,
                cancellationToken);

            if (!unlocked)
            {
                if (tx is not null) await tx.RollbackAsync(cancellationToken);
                return AdminAccountAppealActionResult.Fail(unlockError ?? "Không thể mở khóa tài khoản người dùng.");
            }

            if (tx is not null)
                await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }

        // Các tác vụ hậu xử lý best-effort (không đảo ngược giao dịch đã commit)
        var typeLabel = AccountAppealRules.RequestTypeLabel(loaiYeuCau, lanKhoaThu);
        var auditReason = string.IsNullOrEmpty(cleanNote)
            ? $"Chấp thuận đơn #{maKhieuNai} ({typeLabel}); giamSoLanBiKhoa={giamSoLanBiKhoa}."
            : $"Chấp thuận đơn #{maKhieuNai} ({typeLabel}); giamSoLanBiKhoa={giamSoLanBiKhoa}. Ghi chú: {cleanNote}";

        await TryAuditAsync(adminId, "APPROVE_ACCOUNT_APPEAL", "KhieuNaiTaiKhoan", maKhieuNai, auditReason);

        var notifyContent = giamSoLanBiKhoa
            ? $"Đơn {typeLabel} (#{maKhieuNai}) của bạn đã được chấp thuận. Tài khoản và cửa hàng đã mở khóa, bộ đếm vi phạm được đặt lại về 0/3 và số lần bị khóa đã được giảm 1."
            : $"Đơn {typeLabel} (#{maKhieuNai}) của bạn đã được chấp thuận. Tài khoản và cửa hàng đã mở khóa, bộ đếm vi phạm được đặt lại về 0/3.";
        if (!string.IsNullOrEmpty(cleanNote))
            notifyContent += $" Ghi chú từ quản trị viên: {cleanNote}";

        await TryNotifyAsync(
            targetUserId,
            "Tài khoản của bạn đã được mở khóa",
            Truncate(notifyContent, 1000),
            cancellationToken);

        await TrySendApprovalEmailAsync(userEmail, userName, maKhieuNai, typeLabel, giamSoLanBiKhoa, cleanNote, cancellationToken);

        var resultMessage = giamSoLanBiKhoa
            ? "Đã chấp thuận đơn khiếu nại khóa nhầm, mở khóa tài khoản, reset mốc vi phạm và giảm số lần bị khóa."
            : "Đã chấp thuận cam kết mở khóa, mở khóa tài khoản và reset mốc vi phạm về 0/3.";

        return AdminAccountAppealActionResult.Ok(resultMessage);
    }

    public async Task<AdminAccountAppealActionResult> RejectAsync(
        int adminId,
        int maKhieuNai,
        string? ghiChuAdmin,
        CancellationToken cancellationToken = default)
    {
        if (!await IsActiveAdminAsync(adminId, cancellationToken))
            return AdminAccountAppealActionResult.Fail("Chỉ quản trị viên đang hoạt động mới được xử lý đơn khiếu nại.");

        var cleanNote = ghiChuAdmin?.Trim() ?? string.Empty;
        if (cleanNote.Length < AccountAppealRules.MinAdminRejectNoteLength)
            return AdminAccountAppealActionResult.Fail($"Lý do từ chối bắt buộc tối thiểu {AccountAppealRules.MinAdminRejectNoteLength} ký tự.");
        if (cleanNote.Length > AccountAppealRules.MaxAdminNoteLength)
            return AdminAccountAppealActionResult.Fail($"Lý do từ chối tối đa {AccountAppealRules.MaxAdminNoteLength} ký tự.");

        var appeal = await db.KhieuNaiTaiKhoans
            .Include(k => k.NguoiDung)
            .SingleOrDefaultAsync(k => k.MaKhieuNai == maKhieuNai, cancellationToken);

        if (appeal is null)
            return AdminAccountAppealActionResult.Fail("Không tìm thấy đơn khiếu nại.");

        if (appeal.TrangThai != AccountAppealRules.StatusPending)
            return AdminAccountAppealActionResult.Fail("Đơn khiếu nại này đã được xử lý trước đó.");

        appeal.TrangThai = AccountAppealRules.StatusRejected;
        appeal.GhiChuAdmin = cleanNote;
        appeal.NguoiXuLyId = adminId;
        appeal.NgayXuLy = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var typeLabel = AccountAppealRules.RequestTypeLabel(appeal.LoaiYeuCau, appeal.LanKhoaThu);
        await TryAuditAsync(
            adminId,
            "REJECT_ACCOUNT_APPEAL",
            "KhieuNaiTaiKhoan",
            maKhieuNai,
            $"Từ chối đơn #{maKhieuNai} ({typeLabel}): {cleanNote}");

        await TrySendRejectionEmailAsync(
            appeal.NguoiDung.Email,
            appeal.NguoiDung.HoTen,
            maKhieuNai,
            typeLabel,
            cleanNote,
            cancellationToken);

        return AdminAccountAppealActionResult.Ok("Đã từ chối đơn khiếu nại. Tài khoản vẫn giữ trạng thái khóa.");
    }

    public async Task<AccountAppealSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var row = await db.CauHinhKhieuNaiTaiKhoans.AsNoTracking()
            .Include(c => c.NguoiCapNhat)
            .FirstOrDefaultAsync(c => c.Id == AccountAppealRules.ConfigRowId, cancellationToken);

        if (row is null)
        {
            var (d, h, m) = AccountAppealRules.SplitCooldownMinutes(AccountAppealRules.DefaultCooldownMinutes);
            return new AccountAppealSettingsDto(
                AccountAppealRules.DefaultMaxAttemptsPerCycle,
                AccountAppealRules.DefaultCooldownMinutes,
                d, h, m,
                AccountAppealRules.DefaultSecondLockFee,
                AccountAppealRules.DefaultBankName,
                AccountAppealRules.DefaultBankCode,
                AccountAppealRules.DefaultBankAccount,
                AccountAppealRules.DefaultBankOwner,
                DateTime.UtcNow,
                null);
        }

        var (days, hours, minutes) = AccountAppealRules.SplitCooldownMinutes(row.CooldownPhut);
        return new AccountAppealSettingsDto(
            row.SoLanGuiMoiChuKy,
            row.CooldownPhut,
            days,
            hours,
            minutes,
            row.PhiMoKhoaLan2,
            row.TenNganHang,
            row.MaNganHang,
            row.SoTaiKhoan,
            row.ChuTaiKhoan,
            row.NgayCapNhat,
            row.NguoiCapNhat?.HoTen);
    }

    public async Task<AdminAccountAppealActionResult> UpdateSettingsAsync(
        int adminId,
        UpdateAccountAppealSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await IsActiveAdminAsync(adminId, cancellationToken))
            return AdminAccountAppealActionResult.Fail("Chỉ quản trị viên đang hoạt động mới được cập nhật cấu hình.");

        if (request is null)
            return AdminAccountAppealActionResult.Fail("Dữ liệu cấu hình không hợp lệ.");

        if (request.SoLanGuiMoiChuKy < 1 || request.SoLanGuiMoiChuKy > MaxAttemptsPerCycleLimit)
            return AdminAccountAppealActionResult.Fail($"Số lần gửi mỗi chu kỳ phải từ 1 đến {MaxAttemptsPerCycleLimit}.");

        if (request.CooldownNgay < 0 || request.CooldownGio < 0 || request.CooldownPhutLe < 0)
            return AdminAccountAppealActionResult.Fail("Thời gian Cooldown (Ngày / Giờ / Phút) không được là số âm.");

        var totalCooldownMinutes = AccountAppealRules.CombineCooldownMinutes(
            request.CooldownNgay,
            request.CooldownGio,
            request.CooldownPhutLe);

        if (totalCooldownMinutes < 1 || totalCooldownMinutes > MaxCooldownMinutesLimit)
            return AdminAccountAppealActionResult.Fail("Tổng thời gian Cooldown phải từ 1 phút đến tối đa 365 ngày.");

        if (request.PhiMoKhoaLan2 < 0 || request.PhiMoKhoaLan2 > MaxUnlockFeeLimit)
            return AdminAccountAppealActionResult.Fail($"Phí mở khóa lần 2 phải từ 0 đến {MaxUnlockFeeLimit:N0}đ.");

        var bankName = request.TenNganHang?.Trim() ?? string.Empty;
        var bankCode = request.MaNganHang?.Trim().ToUpperInvariant() ?? string.Empty;
        var bankAccount = request.SoTaiKhoan?.Trim() ?? string.Empty;
        var bankOwner = request.ChuTaiKhoan?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(bankName) || bankName.Length > 100)
            return AdminAccountAppealActionResult.Fail("Tên ngân hàng bắt buộc và tối đa 100 ký tự.");
        if (string.IsNullOrEmpty(bankCode) || bankCode.Length > 20)
            return AdminAccountAppealActionResult.Fail("Mã ngân hàng bắt buộc và tối đa 20 ký tự (VD: MB, VCB, TCB).");
        if (string.IsNullOrEmpty(bankAccount) || bankAccount.Length > 30)
            return AdminAccountAppealActionResult.Fail("Số tài khoản nhận phí bắt buộc và tối đa 30 ký tự.");
        if (string.IsNullOrEmpty(bankOwner) || bankOwner.Length > 100)
            return AdminAccountAppealActionResult.Fail("Tên chủ tài khoản bắt buộc và tối đa 100 ký tự.");

        var now = DateTime.UtcNow;
        var entity = await db.CauHinhKhieuNaiTaiKhoans
            .FirstOrDefaultAsync(c => c.Id == AccountAppealRules.ConfigRowId, cancellationToken);

        if (entity is null)
        {
            entity = new CauHinhKhieuNaiTaiKhoan { Id = AccountAppealRules.ConfigRowId };
            db.CauHinhKhieuNaiTaiKhoans.Add(entity);
        }

        entity.SoLanGuiMoiChuKy = request.SoLanGuiMoiChuKy;
        entity.CooldownPhut = totalCooldownMinutes;
        entity.PhiMoKhoaLan2 = decimal.Round(request.PhiMoKhoaLan2, 2);
        entity.TenNganHang = bankName;
        entity.MaNganHang = bankCode;
        entity.SoTaiKhoan = bankAccount;
        entity.ChuTaiKhoan = bankOwner;
        entity.NgayCapNhat = now;
        entity.NguoiCapNhatId = adminId;

        await db.SaveChangesAsync(cancellationToken);

        await TryAuditAsync(
            adminId,
            "UPDATE_ACCOUNT_APPEAL_SETTINGS",
            "CauHinhKhieuNaiTaiKhoan",
            AccountAppealRules.ConfigRowId,
            $"ChuKy={entity.SoLanGuiMoiChuKy}, Cooldown={entity.CooldownPhut} phút, Phi={entity.PhiMoKhoaLan2:N0}đ, Bank={entity.MaNganHang}/{entity.SoTaiKhoan}");

        return AdminAccountAppealActionResult.Ok("Đã lưu cấu hình khiếu nại & mở khóa tài khoản. Cấu hình mới có hiệu lực ngay.");
    }

    // =====================================================================
    // TRỢ GIÚP NỘI BỘ
    // =====================================================================
    private Task<bool> IsActiveAdminAsync(int adminId, CancellationToken ct) =>
        db.NguoiDungs.AsNoTracking()
            .AnyAsync(u => u.MaNguoiDung == adminId && u.VaiTro == AppRoles.Admin && u.TrangThai == "ACTIVE", ct);

    private static string? NormalizeStatusFilter(string? raw)
    {
        var s = raw?.Trim().ToUpperInvariant();
        return s is AccountAppealRules.StatusPending or AccountAppealRules.StatusApproved or AccountAppealRules.StatusRejected
            ? s
            : null;
    }

    private static string? NormalizeTypeFilter(string? raw)
    {
        var t = raw?.Trim().ToUpperInvariant();
        return t is AccountAppealRules.TypeAppeal or AccountAppealRules.TypeUnlockRequest
            ? t
            : null;
    }

    private async Task TryAuditAsync(int adminId, string action, string targetType, int targetId, string reason)
    {
        try
        {
            await auditLog.LogAsync(adminId, action, targetType, targetId, "SUCCESS", Truncate(reason, 500));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Không ghi được AuditLog {Action} cho {TargetType} #{TargetId}.", action, targetType, targetId);
        }
    }

    private async Task TryNotifyAsync(int userId, string title, string content, CancellationToken ct)
    {
        try
        {
            var (ok, err) = await notifications.CreateAsync(userId, NotificationTypes.System, title, content, null, ct);
            if (!ok) logger.LogWarning("Không tạo được thông báo mở khóa cho user {UserId}: {Error}", userId, err);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Lỗi khi tạo thông báo mở khóa cho user {UserId}.", userId);
        }
    }

    private async Task TrySendApprovalEmailAsync(
        string toEmail,
        string userName,
        int maKhieuNai,
        string typeLabel,
        bool giamSoLanBiKhoa,
        string? adminNote,
        CancellationToken ct)
    {
        if (emailSender is null || string.IsNullOrWhiteSpace(toEmail)) return;
        try
        {
            var safeName = WebUtility.HtmlEncode(userName);
            var safeType = WebUtility.HtmlEncode(typeLabel);
            var safeNote = string.IsNullOrWhiteSpace(adminNote)
                ? string.Empty
                : $"<p><strong>Ghi chú từ quản trị viên:</strong> {WebUtility.HtmlEncode(adminNote)}</p>";
            var lockResetText = giamSoLanBiKhoa
                ? "Bộ đếm vi phạm đã được đặt lại về 0/3 và số lần bị khóa đã được khôi phục giảm 1."
                : "Bộ đếm vi phạm đã được đặt lại về 0/3. Vui lòng tuân thủ quy định sàn để tránh tái phạm.";

            var html = $"""
                <p>Xin chào <strong>{safeName}</strong>,</p>
                <p>Yêu cầu <strong>{safeType}</strong> (mã đơn <strong>#{maKhieuNai}</strong>) của bạn đã được Ban quản trị <strong>chấp thuận</strong>.</p>
                <p>Tài khoản Re:Market của bạn hiện đã được mở khóa và có thể đăng nhập lại bình thường. {lockResetText}</p>
                {safeNote}
                <p>Trân trọng,<br/>Ban quản trị Re:Market</p>
                """;

            await emailSender.SendAsync(toEmail, $"[Re:Market] Tài khoản đã được mở khóa (Đơn #{maKhieuNai})", html, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Gửi email thông báo chấp thuận đơn #{MaKhieuNai} tới {Email} thất bại.", maKhieuNai, toEmail);
        }
    }

    private async Task TrySendRejectionEmailAsync(
        string toEmail,
        string userName,
        int maKhieuNai,
        string typeLabel,
        string rejectReason,
        CancellationToken ct)
    {
        if (emailSender is null || string.IsNullOrWhiteSpace(toEmail)) return;
        try
        {
            var safeName = WebUtility.HtmlEncode(userName);
            var safeType = WebUtility.HtmlEncode(typeLabel);
            var safeReason = WebUtility.HtmlEncode(rejectReason);

            var html = $"""
                <p>Xin chào <strong>{safeName}</strong>,</p>
                <p>Yêu cầu <strong>{safeType}</strong> (mã đơn <strong>#{maKhieuNai}</strong>) của bạn chưa được chấp thuận.</p>
                <p><strong>Lý do từ chối:</strong> {safeReason}</p>
                <p>Tài khoản của bạn vẫn đang ở trạng thái tạm khóa. Bạn có thể đăng nhập để kiểm tra lượt gửi còn lại hoặc thời gian chờ tại trang mở khóa tài khoản.</p>
                <p>Trân trọng,<br/>Ban quản trị Re:Market</p>
                """;

            await emailSender.SendAsync(toEmail, $"[Re:Market] Kết quả xử lý đơn khiếu nại #{maKhieuNai}", html, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Gửi email thông báo từ chối đơn #{MaKhieuNai} tới {Email} thất bại.", maKhieuNai, toEmail);
        }
    }

    private static string Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? string.Empty : value.Length <= max ? value : value[..max];
}
