using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using SecondHandMarketplace.Web.Common;

namespace SecondHandMarketplace.Web.Services.Moderation;

/// <summary>Kết quả đánh giá thuần (không đụng database) của một lần quét từ khóa.</summary>
public sealed record ModerationOutcome(
    string Level,
    IReadOnlyList<KeywordMatch> Matches,
    string MucRuiRo,
    string DeXuat,
    string GiaiThich,
    string DauHieuJson,
    string FindingsHash);

/// <summary>Quy tắc HIGH/LOW dạng hàm thuần để dễ kiểm thử.</summary>
public static class ModerationDecision
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Giữ nguyên tiếng Việt trong JSON lưu DB (vẫn là JSON hợp lệ cho ISJSON).
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    /// <summary>Chỉ "HIGH" là HIGH; giá trị lạ coi là LOW (không tự từ chối khi dữ liệu không rõ).</summary>
    public static string NormalizeLevel(string? level) =>
        string.Equals(level?.Trim(), ModerationPolicy.KeywordHigh, StringComparison.OrdinalIgnoreCase)
            ? ModerationPolicy.KeywordHigh
            : ModerationPolicy.KeywordLow;

    public static ModerationOutcome Evaluate(IEnumerable<KeywordMatch> found)
    {
        // Mỗi (từ khóa, trường) chỉ một dòng; HIGH xếp trước.
        var matches = found
            .Select(m => m with { Level = NormalizeLevel(m.Level) })
            .GroupBy(m => (m.KeywordId, m.Field))
            .Select(g => g.OrderBy(m => m.Position ?? int.MaxValue).First())
            .OrderByDescending(m => m.Level == ModerationPolicy.KeywordHigh)
            .ThenBy(m => m.Field, StringComparer.Ordinal)
            .ThenBy(m => m.KeywordId)
            .ToList();

        var level = matches.Any(m => m.Level == ModerationPolicy.KeywordHigh) ? ModerationPolicy.LevelHigh
                  : matches.Count > 0 ? ModerationPolicy.LevelLow
                  : ModerationPolicy.LevelNone;

        // PhanTichAI.MucRuiRo / DeXuat: ràng buộc DONE bắt buộc có đủ hai cột.
        var (risk, advice) = level switch
        {
            ModerationPolicy.LevelHigh => ("HIGH", "REJECT"),
            ModerationPolicy.LevelLow => ("MEDIUM", "NEEDS_REVIEW"),
            _ => ("LOW", "APPROVE")
        };

        return new ModerationOutcome(level, matches, risk, advice, BuildExplanation(level, matches),
            BuildEvidenceJson(matches), ComputeHash(matches));
    }

    /// <summary>JSON mảng "dấu hiệu" cùng kiểu seed (snake_case): loai, tu_khoa_id, tu_khoa, nhom, muc_do, truong, vi_tri, bang_chung.</summary>
    public static string BuildEvidenceJson(IReadOnlyList<KeywordMatch> matches)
    {
        var items = matches.Select(m => new Dictionary<string, object?>
        {
            ["loai"] = "TU_KHOA_CAM",
            ["tu_khoa_id"] = m.KeywordId,
            ["tu_khoa"] = m.Phrase,
            ["nhom"] = m.Group,
            ["muc_do"] = m.Level,
            ["truong"] = m.Field,
            ["vi_tri"] = m.Position,
            ["bang_chung"] = $"{(m.Field == "Ten" ? "Tên" : "Mô tả")} chứa từ khóa cấm mức {m.Level}: \"{m.Phrase}\""
        }).ToList();
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    /// <summary>Đọc lại DauHieu; chịu được dữ liệu cũ khác định dạng (bỏ qua phần tử không phải từ khóa cấm).</summary>
    public static IReadOnlyList<KeywordMatch> ParseEvidence(string? json)
    {
        var result = new List<KeywordMatch>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object) continue;
                if (!e.TryGetProperty("loai", out var loai) || loai.GetString() != "TU_KHOA_CAM") continue;
                var id = e.TryGetProperty("tu_khoa_id", out var idEl) && idEl.TryGetInt32(out var idVal) ? idVal : 0;
                var phrase = e.TryGetProperty("tu_khoa", out var p) ? p.GetString() ?? "" : "";
                var group = e.TryGetProperty("nhom", out var g) && g.ValueKind == JsonValueKind.String ? g.GetString() : null;
                var lvl = e.TryGetProperty("muc_do", out var l) ? l.GetString() : null;
                var field = e.TryGetProperty("truong", out var f) ? f.GetString() ?? "" : "";
                int? pos = e.TryGetProperty("vi_tri", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var pv) ? pv : null;
                result.Add(new KeywordMatch(id, phrase, group, NormalizeLevel(lvl), field, pos));
            }
        }
        catch (JsonException)
        {
            result.Clear();
        }
        return result;
    }

    /// <summary>Băm SHA-256 (hex thường, 64 ký tự khớp char(64)) của tập phát hiện; đổi khi tập từ khóa/mức/trường đổi.</summary>
    public static string ComputeHash(IReadOnlyList<KeywordMatch> matches)
    {
        var canonical = string.Join(";", matches
            .OrderBy(m => m.KeywordId).ThenBy(m => m.Field, StringComparer.Ordinal)
            .Select(m => $"{m.KeywordId}|{m.Level}|{m.Field}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    /// <summary>Vị trí (chỉ số ký tự) của cụm đã chuẩn hóa trong văn bản sau khi chuẩn hóa, khớp theo ranh giới từ như fn_TimTuKhoaCam.</summary>
    public static int? FindPosition(string? text, string? normalizedPhrase)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(normalizedPhrase)) return null;
        var padded = " " + TextHelper.ChuanHoa(text) + " ";
        var idx = padded.IndexOf(" " + normalizedPhrase.Trim() + " ", StringComparison.Ordinal);
        return idx < 0 ? null : idx;
    }

    /// <summary>
    /// Có được duyệt không. Phản chiếu trigger trg_LuotKiemDuyet_ChanDuyetViPham (database/14_kiemduyet_tukhoa_c2.sql);
    /// trigger mới là chốt chặn cuối cùng. LOW được phép duyệt (Admin duyệt = không vi phạm).
    /// </summary>
    public static (bool Allowed, string? Reason) CanApprove(
        bool turnIsOpen, string? analysisStatus, string? level, bool violationRecorded)
    {
        if (!turnIsOpen) return (false, "Lượt kiểm duyệt này đã có quyết định.");
        if (analysisStatus != ModerationPolicy.AnalysisDone || level is null)
            return (false, "Sản phẩm chưa được phân tích từ khóa xong (QUEUED/FAILED hoặc chưa quét).");
        if (level == ModerationPolicy.LevelHigh || violationRecorded)
            return (false, "Sản phẩm có từ khóa cấm mức HIGH hoặc đã bị ghi nhận vi phạm.");
        return (true, null);
    }

    public static string BuildTermsText(IEnumerable<KeywordMatch> matches, string? onlyLevel = null) =>
        string.Join(", ", matches
            .Where(m => onlyLevel is null || m.Level == onlyLevel)
            .Select(m => m.Phrase)
            .Distinct(StringComparer.OrdinalIgnoreCase));

    private static string BuildExplanation(string level, IReadOnlyList<KeywordMatch> matches)
    {
        var text = level switch
        {
            ModerationPolicy.LevelHigh =>
                $"Phát hiện từ khóa cấm mức HIGH: {BuildTermsText(matches, ModerationPolicy.KeywordHigh)}. Hệ thống tự động từ chối và ghi nhận 1 vi phạm.",
            ModerationPolicy.LevelLow =>
                $"Phát hiện từ khóa cấm mức LOW: {BuildTermsText(matches)}. Chờ Admin xem xét: duyệt thì không vi phạm, từ chối thì ghi 1 vi phạm.",
            _ => "Không phát hiện từ khóa cấm trong tên và mô tả."
        };
        return text.Length <= 2000 ? text : text[..2000];
    }
}
