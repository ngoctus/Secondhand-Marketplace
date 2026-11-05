using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Services.Moderation;

public sealed partial class ForbiddenKeywordService(AppDbContext db, IAuditLogService auditLog) : IForbiddenKeywordService
{
    private const int MaxPhrase = 100;   // TuKhoaCam.CumTu nvarchar(100)
    private const int MaxGroup = 30;     // TuKhoaCam.Nhom varchar(30)
    private const int MaxNote = 500;     // TuKhoaCam.GhiChu nvarchar(500)

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex GroupPattern();

    public async Task<IReadOnlyList<ForbiddenKeywordItem>> ListAsync(string? search = null, string? level = null,
        bool? active = null, CancellationToken cancellationToken = default)
    {
        var query = db.TuKhoaCams.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(level))
        {
            var normalized = level.Trim().ToUpperInvariant();
            query = query.Where(k => k.MucDo == normalized);
        }
        if (active is not null) query = query.Where(k => k.DangApDung == active);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(k => k.CumTu.Contains(term) || (k.Nhom != null && k.Nhom.Contains(term)));
        }

        var rows = await query
            .OrderByDescending(k => k.MucDo == ModerationPolicy.KeywordHigh)
            .ThenBy(k => k.CumTu)
            .ToListAsync(cancellationToken);

        return rows.Select(k => new ForbiddenKeywordItem(k.MaTuKhoaCam, k.CumTu, k.CumTuChuanHoa, k.Nhom,
            ModerationDecision.NormalizeLevel(k.MucDo), k.GhiChu, k.DangApDung, k.NgayTao)).ToList();
    }

    public async Task<(bool Success, string? Error, int? Id)> AddAsync(string phrase, string? group, string level,
        string? note, int? actorId, CancellationToken cancellationToken = default)
    {
        phrase = phrase?.Trim() ?? "";
        var error = Validate(phrase, ref group, level, ref note, out var normalizedLevel);
        if (error is not null) return (false, error, null);

        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

            // Tận dụng sp_ThemTuKhoaCam: chuẩn hóa bằng fn_ChuanHoaChuoi và báo lỗi trùng (50111) / không hợp lệ (50110).
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC dbo.sp_ThemTuKhoaCam @CumTu = {phrase}, @Nhom = {group}, @GhiChu = {note}", cancellationToken);

            // Cột MucDo có DEFAULT 'LOW'; đặt đúng mức Admin chọn trong cùng transaction.
            var created = await db.TuKhoaCams.OrderByDescending(k => k.MaTuKhoaCam)
                .FirstOrDefaultAsync(k => k.CumTu == phrase, cancellationToken);
            if (created is null) return (false, "Không thể thêm từ khóa.", null);

            created.MucDo = normalizedLevel;
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await auditLog.LogAsync(actorId, "ADD_FORBIDDEN_KEYWORD", "TuKhoaCam", created.MaTuKhoaCam, "SUCCESS",
                null, $"{{\"cum_tu\":\"{Escape(phrase)}\",\"muc_do\":\"{normalizedLevel}\"}}");
            return (true, null, created.MaTuKhoaCam);
        }
        catch (SqlException ex) when (ex.Number == 50111)
        {
            return (false, "Từ khóa đã tồn tại.", null);
        }
        catch (SqlException ex) when (ex.Number == 50110)
        {
            return (false, "Từ khóa không hợp lệ.", null);
        }
    }

    public async Task<(bool Success, string? Error)> SetActiveAsync(int id, bool active, int? actorId,
        CancellationToken cancellationToken = default)
    {
        var keyword = await db.TuKhoaCams.SingleOrDefaultAsync(k => k.MaTuKhoaCam == id, cancellationToken);
        if (keyword is null) return (false, "Không tìm thấy từ khóa.");
        if (keyword.DangApDung == active) return (true, null);

        keyword.DangApDung = active;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.LogAsync(actorId, active ? "ENABLE_FORBIDDEN_KEYWORD" : "DISABLE_FORBIDDEN_KEYWORD", "TuKhoaCam", id);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, string? group, string level, string? note,
        int? actorId, CancellationToken cancellationToken = default)
    {
        var error = Validate("x", ref group, level, ref note, out var normalizedLevel);
        if (error is not null) return (false, error);

        var keyword = await db.TuKhoaCams.SingleOrDefaultAsync(k => k.MaTuKhoaCam == id, cancellationToken);
        if (keyword is null) return (false, "Không tìm thấy từ khóa.");

        keyword.Nhom = group;
        keyword.MucDo = normalizedLevel;
        keyword.GhiChu = note;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.LogAsync(actorId, "UPDATE_FORBIDDEN_KEYWORD", "TuKhoaCam", id, "SUCCESS", null,
            $"{{\"muc_do\":\"{normalizedLevel}\"}}");
        return (true, null);
    }

    private static string? Validate(string phrase, ref string? group, string level, ref string? note, out string normalizedLevel)
    {
        normalizedLevel = (level ?? "").Trim().ToUpperInvariant();
        if (phrase.Length == 0) return "Từ khóa không được để trống.";
        if (phrase.Length > MaxPhrase) return $"Từ khóa tối đa {MaxPhrase} ký tự.";
        if (normalizedLevel is not (ModerationPolicy.KeywordHigh or ModerationPolicy.KeywordLow))
            return "Mức độ phải là LOW hoặc HIGH.";

        group = string.IsNullOrWhiteSpace(group) ? null : group.Trim().ToUpperInvariant();
        if (group is not null)
        {
            if (group.Length > MaxGroup) return $"Nhóm tối đa {MaxGroup} ký tự.";
            if (!GroupPattern().IsMatch(group)) return "Nhóm chỉ gồm chữ không dấu, số và dấu gạch dưới (ví dụ HANG_GIA).";
        }

        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note is not null && note.Length > MaxNote) return $"Ghi chú tối đa {MaxNote} ký tự.";
        return null;
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
