using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Services.Accounts;

/// <summary>
/// Cài đặt nghiệp vụ Khiếu nại & Mở khóa tài khoản phía người dùng (Task KN-06).
/// </summary>
public sealed class AccountAppealUserService : IAccountAppealUserService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly ILogger<AccountAppealUserService> _logger;

    public AccountAppealUserService(
        AppDbContext db,
        IAuditLogService audit,
        ILogger<AccountAppealUserService> logger)
    {
        _db = db;
        _audit = audit;
        _logger = logger;
    }

    public async Task<LockedAccountOverviewDto?> GetLockedAccountOverviewAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.NguoiDungs.AsNoTracking()
            .FirstOrDefaultAsync(u => u.MaNguoiDung == userId, cancellationToken);

        if (user is null) return null;

        var isLocked = string.Equals(user.TrangThai, "LOCKED", StringComparison.OrdinalIgnoreCase);
        var effectiveLockCount = AccountAppealRules.NormalizeLockCount(user.SoLanBiKhoa, isLocked);

        // Đọc cấu hình khiếu nại (1 dòng)
        var config = await _db.CauHinhKhieuNaiTaiKhoans.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == AccountAppealRules.ConfigRowId, cancellationToken);

        var maxAttempts = config?.SoLanGuiMoiChuKy ?? AccountAppealRules.DefaultMaxAttemptsPerCycle;
        var cooldownMinutes = config?.CooldownPhut ?? AccountAppealRules.DefaultCooldownMinutes;
        var fee = config?.PhiMoKhoaLan2 ?? AccountAppealRules.DefaultSecondLockFee;
        var bankName = config?.TenNganHang ?? AccountAppealRules.DefaultBankName;
        var bankCode = config?.MaNganHang ?? AccountAppealRules.DefaultBankCode;
        var bankAccount = config?.SoTaiKhoan ?? AccountAppealRules.DefaultBankAccount;
        var bankOwner = config?.ChuTaiKhoan ?? AccountAppealRules.DefaultBankOwner;

        // Thu thập các gậy vi phạm hiệu lực (phát sinh sau MocTinhViPham)
        var mocTinh = user.MocTinhViPham;
        var violationItems = new List<LockedViolationItemDto>();

        var countViPhamKiemDuyet = await _db.LichSuViPhams
            .CountAsync(v => v.MaNguoiBan == userId && (mocTinh == null || v.ThoiGian > mocTinh.Value), cancellationToken);

        var viPhamKiemDuyet = await _db.LichSuViPhams.AsNoTracking()
            .Where(v => v.MaNguoiBan == userId && (mocTinh == null || v.ThoiGian > mocTinh.Value))
            .OrderByDescending(v => v.ThoiGian)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var v in viPhamKiemDuyet)
        {
            violationItems.Add(new LockedViolationItemDto(
                "Kiểm duyệt sản phẩm",
                $"Vi phạm kiểm duyệt mức {v.MucDo} (Sản phẩm #{v.MaSanPham})",
                v.ThoiGian));
        }

        var countSpBiBaoCao = await _db.BaoCaos
            .Where(b => b.TrangThai == "RESOLVED" && b.LoaiDoiTuong == "PRODUCT" &&
                        (mocTinh == null || (b.NgayXuLy != null && b.NgayXuLy > mocTinh.Value)) &&
                        _db.SanPhams.Any(p => p.MaSanPham == b.MaDoiTuong && p.CuaHang.MaNguoiDung == userId))
            .Select(b => b.MaDoiTuong)
            .Distinct()
            .CountAsync(cancellationToken);

        var countBaoCaoNguoiDungShop = await _db.BaoCaos
            .Where(b => b.TrangThai == "RESOLVED" &&
                        (mocTinh == null || (b.NgayXuLy != null && b.NgayXuLy > mocTinh.Value)) &&
                        ((b.LoaiDoiTuong == "USER" && b.MaDoiTuong == userId) ||
                         (b.LoaiDoiTuong == "SHOP" && _db.CuaHangs.Any(c => c.MaCuaHang == b.MaDoiTuong && c.MaNguoiDung == userId))))
            .CountAsync(cancellationToken);

        var tongViPham = countViPhamKiemDuyet + countSpBiBaoCao + countBaoCaoNguoiDungShop;

        var baoCaoViPham = await _db.BaoCaos.AsNoTracking()
            .Where(b => b.TrangThai == "RESOLVED"
                        && (mocTinh == null || (b.NgayXuLy != null && b.NgayXuLy > mocTinh.Value))
                        && ((b.LoaiDoiTuong == "USER" && b.MaDoiTuong == userId)
                            || (b.LoaiDoiTuong == "SHOP" && _db.CuaHangs.Any(c => c.MaCuaHang == b.MaDoiTuong && c.MaNguoiDung == userId))
                            || (b.LoaiDoiTuong == "PRODUCT" && _db.SanPhams.Any(p => p.MaSanPham == b.MaDoiTuong && p.CuaHang.MaNguoiDung == userId))))
            .OrderByDescending(b => b.NgayXuLy ?? b.NgayTao)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var b in baoCaoViPham)
        {
            violationItems.Add(new LockedViolationItemDto(
                "Báo cáo vi phạm",
                $"Báo cáo {b.LoaiDoiTuong} #{b.MaDoiTuong}: {b.LyDo}",
                b.NgayXuLy ?? b.NgayTao));
        }

        // Sắp xếp vi phạm mới nhất lên đầu
        violationItems = violationItems.OrderByDescending(x => x.ThoiGian).ToList();

        // Lấy danh sách đơn khiếu nại trong đợt khóa hiện tại
        var lockStart = user.ThoiGianKhoa ?? DateTime.MinValue;
        var appealsThisLock = await _db.KhieuNaiTaiKhoans.AsNoTracking()
            .Where(k => k.MaNguoiDung == userId && k.NgayTao >= lockStart)
            .OrderByDescending(k => k.NgayTao)
            .ToListAsync(cancellationToken);

        var appealSummaries = appealsThisLock.Select(a => new AccountAppealSummaryDto(
            a.MaKhieuNai,
            a.LoaiYeuCau,
            a.LanKhoaThu,
            a.NoiDung,
            a.BangChungAnh,
            a.YeuCauPhi,
            a.SoTienPhi,
            a.MaGiaoDichThamChieu,
            a.TrangThai,
            a.GhiChuAdmin,
            a.NgayTao,
            a.NgayXuLy
        )).ToList();

        var donDangCho = appealsThisLock.Any(a => a.TrangThai == AccountAppealRules.StatusPending);
        var donGanNhat = appealSummaries.FirstOrDefault();

        // Đánh giá chu kỳ & Cooldown
        var rejectedTimesAsc = appealsThisLock
            .Where(a => a.TrangThai == AccountAppealRules.StatusRejected && a.NgayXuLy.HasValue)
            .OrderBy(a => a.NgayXuLy!.Value)
            .Select(a => a.NgayXuLy!.Value)
            .ToList();

        var cycleEval = AccountAppealRules.EvaluateCycle(rejectedTimesAsc, maxAttempts, cooldownMinutes, DateTime.UtcNow);

        // Quyền gửi và phí VietQR
        var choPhepKhieuNaiKhoaNham = isLocked;
        var choPhepCamKetMoKhoa = isLocked && AccountAppealRules.IsUnlockRequestAllowed(effectiveLockCount);
        var yeuCauPhiLan2 = isLocked && effectiveLockCount == AccountAppealRules.FeeLockTier;

        UnlockFeeQrDto? qrDto = null;
        if (yeuCauPhiLan2)
        {
            var transferContent = AccountAppealRules.BuildTransferContent(userId, effectiveLockCount);
            var qrDataUrl = VietQrGenerator.GenerateMockQrDataUrl(bankCode, bankAccount, fee, transferContent);
            qrDto = new UnlockFeeQrDto(bankName, bankCode, bankAccount, bankOwner, fee, transferContent, qrDataUrl);
        }

        return new LockedAccountOverviewDto(
            MaNguoiDung: user.MaNguoiDung,
            HoTen: user.HoTen,
            Email: user.Email,
            DangBiKhoa: isLocked,
            LanKhoaHienTai: effectiveLockCount,
            LyDoKhoa: user.LyDoKhoa,
            ThoiGianKhoa: user.ThoiGianKhoa,
            SoViPhamHieuLuc: tongViPham,
            NguongKhoa: AccountAppealRules.PermanentLockThreshold,
            ViPhamHieuLuc: violationItems,
            ChoPhepKhieuNaiKhoaNham: choPhepKhieuNaiKhoaNham,
            ChoPhepCamKetMoKhoa: choPhepCamKetMoKhoa,
            YeuCauPhiLan2: yeuCauPhiLan2,
            ThongTinPhiQr: qrDto,
            CoDonDangCho: donDangCho,
            SoLuotDaGuiChuKy: cycleEval.AttemptsUsedInCurrentCycle,
            SoLuotToiDaChuKy: maxAttempts,
            SoLuotConLaiChuKy: cycleEval.AttemptsRemainingInCurrentCycle,
            DangTrongCooldown: cycleEval.IsInCooldown,
            CooldownKetThucUtc: cycleEval.CooldownEndsAtUtc,
            CooldownPhutCauHinh: cooldownMinutes,
            DonGanNhat: donGanNhat,
            LichSuDonDotNay: appealSummaries
        );
    }

    public async Task<SubmitAccountAppealResult> SubmitAppealAsync(
        int userId,
        string loaiYeuCau,
        string noiDung,
        string? bangChungAnh,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra loại yêu cầu hợp lệ
        if (!AccountAppealRules.IsValidRequestType(loaiYeuCau))
        {
            return SubmitAccountAppealResult.Fail("Loại yêu cầu không hợp lệ.");
        }
        var normalizedType = AccountAppealRules.NormalizeRequestType(loaiYeuCau);

        // 2. Kiểm tra độ dài nội dung giải trình / cam kết
        var trimmedContent = (noiDung ?? string.Empty).Trim();
        if (trimmedContent.Length < AccountAppealRules.MinContentLength)
        {
            return SubmitAccountAppealResult.Fail($"Nội dung giải trình / cam kết phải có ít nhất {AccountAppealRules.MinContentLength} ký tự.");
        }
        if (trimmedContent.Length > AccountAppealRules.MaxContentLength)
        {
            return SubmitAccountAppealResult.Fail($"Nội dung không được vượt quá {AccountAppealRules.MaxContentLength} ký tự.");
        }

        // 3. Kiểm tra ảnh bằng chứng (nếu có)
        var trimmedImage = bangChungAnh?.Trim();
        if (!string.IsNullOrEmpty(trimmedImage))
        {
            if (trimmedImage.Length > AccountAppealRules.MaxEvidencePathLength)
            {
                return SubmitAccountAppealResult.Fail("Đường dẫn ảnh bằng chứng không được vượt quá 500 ký tự.");
            }
            if (!UrlAnToan.HopLe(trimmedImage))
            {
                return SubmitAccountAppealResult.Fail("Đường dẫn ảnh bằng chứng không hợp lệ.");
            }
        }

        // 4. Kiểm tra tài khoản tồn tại và đang LOCKED
        var user = await _db.NguoiDungs.FirstOrDefaultAsync(u => u.MaNguoiDung == userId, cancellationToken);
        if (user is null)
        {
            return SubmitAccountAppealResult.Fail("Không tìm thấy thông tin tài khoản.");
        }

        if (!string.Equals(user.TrangThai, "LOCKED", StringComparison.OrdinalIgnoreCase))
        {
            return SubmitAccountAppealResult.Fail("Tài khoản của bạn hiện không ở trạng thái bị khóa.");
        }

        var lockCount = AccountAppealRules.NormalizeLockCount(user.SoLanBiKhoa, true);

        // 5. Kiểm tra quyền theo lần khóa
        if (normalizedType == AccountAppealRules.TypeUnlockRequest)
        {
            if (!AccountAppealRules.IsUnlockRequestAllowed(lockCount))
            {
                return SubmitAccountAppealResult.Fail("Tài khoản đã bị khóa từ 3 lần trở lên, không thể gửi cam kết mở khóa. Vui lòng chọn 'Khiếu nại khóa nhầm / Xem xét đặc biệt'.");
            }

            if (AccountAppealRules.RequiresUnlockFee(normalizedType, lockCount) && string.IsNullOrEmpty(trimmedImage))
            {
                return SubmitAccountAppealResult.Fail("Cam kết mở khóa lần 2 yêu cầu đính kèm ảnh biên lai chuyển khoản phí mở khóa 200.000đ.");
            }
        }

        // 6. Chống gửi trùng đơn PENDING trong đợt khóa hiện tại
        var lockStart = user.ThoiGianKhoa ?? DateTime.MinValue;
        var coDonPending = await _db.KhieuNaiTaiKhoans.AnyAsync(
            k => k.MaNguoiDung == userId && k.NgayTao >= lockStart && k.TrangThai == AccountAppealRules.StatusPending,
            cancellationToken);

        if (coDonPending)
        {
            return SubmitAccountAppealResult.Fail("Bạn đang có một yêu cầu đang chờ quản trị viên xử lý. Vui lòng đợi kết quả trước khi gửi đơn mới.");
        }

        // 7. Đọc cấu hình Cooldown & kiểm tra chu kỳ
        var config = await _db.CauHinhKhieuNaiTaiKhoans.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == AccountAppealRules.ConfigRowId, cancellationToken);

        var maxAttempts = config?.SoLanGuiMoiChuKy ?? AccountAppealRules.DefaultMaxAttemptsPerCycle;
        var cooldownMinutes = config?.CooldownPhut ?? AccountAppealRules.DefaultCooldownMinutes;
        var fee = config?.PhiMoKhoaLan2 ?? AccountAppealRules.DefaultSecondLockFee;

        var rejectedTimesAsc = await _db.KhieuNaiTaiKhoans.AsNoTracking()
            .Where(k => k.MaNguoiDung == userId
                        && k.TrangThai == AccountAppealRules.StatusRejected
                        && k.NgayXuLy != null
                        && k.NgayTao >= lockStart)
            .OrderBy(k => k.NgayXuLy!.Value)
            .Select(k => k.NgayXuLy!.Value)
            .ToListAsync(cancellationToken);

        var cycleEval = AccountAppealRules.EvaluateCycle(rejectedTimesAsc, maxAttempts, cooldownMinutes, DateTime.UtcNow);
        if (cycleEval.IsInCooldown && cycleEval.CooldownEndsAtUtc.HasValue)
        {
            var remaining = cycleEval.CooldownEndsAtUtc.Value - DateTime.UtcNow;
            var formatted = AccountAppealRules.FormatRemainingTime(remaining);
            return SubmitAccountAppealResult.Fail($"Bạn đã gửi đủ số lượt trong chu kỳ hiện tại. Vui lòng đợi {formatted} để gửi lại.");
        }

        // 8. Tạo và lưu bản ghi đơn khiếu nại
        var yeuCauPhi = AccountAppealRules.RequiresUnlockFee(normalizedType, lockCount);
        var soTienPhi = yeuCauPhi ? fee : 0m;
        var maGiaoDich = yeuCauPhi ? AccountAppealRules.BuildTransferContent(userId, lockCount) : null;

        var appeal = new KhieuNaiTaiKhoan
        {
            MaNguoiDung = userId,
            LoaiYeuCau = normalizedType,
            LanKhoaThu = lockCount,
            LyDoKhoaLucGui = string.IsNullOrWhiteSpace(user.LyDoKhoa) ? "Tài khoản bị khóa do vi phạm quy định sàn" : user.LyDoKhoa.Trim(),
            NoiDung = trimmedContent,
            BangChungAnh = string.IsNullOrEmpty(trimmedImage) ? null : trimmedImage,
            YeuCauPhi = yeuCauPhi,
            SoTienPhi = soTienPhi,
            MaGiaoDichThamChieu = maGiaoDich,
            TrangThai = AccountAppealRules.StatusPending,
            NgayTao = DateTime.UtcNow
        };

        try
        {
            _db.KhieuNaiTaiKhoans.Add(appeal);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu đơn khiếu nại tài khoản của User {UserId}", userId);
            return SubmitAccountAppealResult.Fail("Không thể gửi đơn do xung đột dữ liệu hoặc đang có đơn chờ duyệt.");
        }

        // 9. Ghi Audit Log thành công (best-effort)
        try
        {
            await _audit.LogAsync(
                userId,
                "SUBMIT_ACCOUNT_APPEAL",
                "KhieuNaiTaiKhoan",
                appeal.MaKhieuNai,
                "SUCCESS",
                $"Gửi đơn {AccountAppealRules.RequestTypeLabel(normalizedType, lockCount)} (Lần khóa {lockCount}).");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ghi nhật ký kiểm toán cho đơn khiếu nại {MaKhieuNai} thất bại.", appeal.MaKhieuNai);
        }

        return SubmitAccountAppealResult.Ok(appeal.MaKhieuNai);
    }
}
