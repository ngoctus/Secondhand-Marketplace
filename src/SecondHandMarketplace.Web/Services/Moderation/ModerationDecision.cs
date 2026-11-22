using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using SecondHandMarketplace.Web.Common;

namespace SecondHandMarketplace.Web.Services.Moderation;

/// <summary>Kết quả đánh giá thuần (không đụng database) của một lần quét từ khóa (+ AI Tầng 2 nếu có).</summary>
public sealed record ModerationOutcome(
    string Level,
    IReadOnlyList<KeywordMatch> Matches,
    string MucRuiRo,
    string DeXuat,
    string GiaiThich,
    string DauHieuJson,
    string FindingsHash,
    string ModelVersion = ModerationPolicy.ModelVersion);

/// <summary>Quy tắc HIGH/LOW và hợp nhất Tầng 2 AI dạng hàm thuần để dễ kiểm thử.</summary>
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
            ModerationPolicy.LevelHigh => (ModerationPolicy.RiskHigh, ModerationPolicy.AdviceReject),
            ModerationPolicy.LevelLow => (ModerationPolicy.RiskMedium, ModerationPolicy.AdviceNeedsReview),
            _ => (ModerationPolicy.RiskLow, ModerationPolicy.AdviceApprove)
        };

        return new ModerationOutcome(level, matches, risk, advice, BuildExplanation(level, matches),
            BuildEvidenceJson(matches), ComputeHash(matches), ModerationPolicy.ModelVersion);
    }

    /// <summary>
    /// Hợp nhất kết quả Tầng 1 (từ khóa) và Tầng 2 (LLM) theo các quy tắc an toàn C-01:
    /// - HIGH Tầng 1: không hợp nhất (đã tự từ chối bằng keyword-v1).
    /// - Không gọi Tầng 2 (aiAttempted = false): giữ nguyên kết quả keyword-v1.
    /// - Tầng 2 lỗi/timeout/thiếu key: fallback an toàn về kết quả Tầng 1 với ModelVersion = keyword-v1-fallback.
    /// - Tầng 2 thành công: giữ nguyên MucDoTuKhoa Tầng 1 (NONE/LOW), chỉ nâng (không hạ) MucRuiRo &amp; DeXuat,
    ///   gộp JSON dấu hiệu (TU_KHOA_CAM + AI_CANH_BAO), lưu ModelVersion = hybrid:&lt;model&gt;.
    /// </summary>
    public static ModerationOutcome MergeWithAi(
        ModerationOutcome keywordOutcome,
        AiModerationResult? aiResult,
        bool aiAttempted)
    {
        if (keywordOutcome.Level == ModerationPolicy.LevelHigh || !aiAttempted)
            return keywordOutcome;

        if (aiResult is null || !aiResult.Succeeded)
        {
            var fallbackNote = Truncate(
                $"{keywordOutcome.GiaiThich} (AI ngữ cảnh tạm không phản hồi — đang dùng kết quả quét từ khóa cơ bản.)",
                2000);
            return keywordOutcome with
            {
                GiaiThich = fallbackNote,
                ModelVersion = ModerationPolicy.ModelVersionFallback
            };
        }

        var mergedRisk = MaxRisk(keywordOutcome.MucRuiRo, aiResult.RiskLevel);
        var mergedAdvice = MaxRecommendation(keywordOutcome.DeXuat, aiResult.Recommendation);
        var signals = aiResult.Signals ?? Array.Empty<AiModerationSignal>();

        var explanation = keywordOutcome.Level == ModerationPolicy.LevelLow
            ? $"{keywordOutcome.GiaiThich} | AI phân tích: {aiResult.Explanation}"
            : $"AI phân tích: {aiResult.Explanation}";

        return new ModerationOutcome(
            Level: keywordOutcome.Level,
            Matches: keywordOutcome.Matches,
            MucRuiRo: mergedRisk,
            DeXuat: mergedAdvice,
            GiaiThich: Truncate(explanation, 2000),
            DauHieuJson: BuildEvidenceJson(keywordOutcome.Matches, signals),
            FindingsHash: ComputeHash(keywordOutcome.Matches, signals),
            ModelVersion: BuildHybridModelVersion(aiResult.ModelName));
    }

    /// <summary>JSON mảng "dấu hiệu" cùng kiểu seed (snake_case): gồm phần tử TU_KHOA_CAM và (nếu có) AI_CANH_BAO.</summary>
    public static string BuildEvidenceJson(
        IReadOnlyList<KeywordMatch> matches,
        IReadOnlyList<AiModerationSignal>? aiSignals = null)
    {
        var items = matches.Select(m => new Dictionary<string, object?>
        {
            ["loai"] = ModerationPolicy.EvidenceTypeKeyword,
            ["tu_khoa_id"] = m.KeywordId,
            ["tu_khoa"] = m.Phrase,
            ["nhom"] = m.Group,
            ["muc_do"] = m.Level,
            ["truong"] = m.Field,
            ["vi_tri"] = m.Position,
            ["bang_chung"] = $"{(m.Field == "Ten" ? "Tên" : "Mô tả")} chứa từ khóa cấm mức {m.Level}: \"{m.Phrase}\""
        }).ToList();

        if (aiSignals is { Count: > 0 })
        {
            foreach (var s in aiSignals)
            {
                items.Add(new Dictionary<string, object?>
                {
                    ["loai"] = ModerationPolicy.EvidenceTypeAi,
                    ["nhom"] = s.Group,
                    ["muc_do"] = s.Level,
                    ["truong"] = s.Field,
                    ["bang_chung"] = s.Evidence
                });
            }
        }

        return JsonSerializer.Serialize(items, JsonOptions);
    }

    /// <summary>Đọc lại DauHieu phần từ khóa cấm (TU_KHOA_CAM); chịu được dữ liệu cũ và phần tử AI_CANH_BAO.</summary>
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
                if (!e.TryGetProperty("loai", out var loai) || loai.GetString() != ModerationPolicy.EvidenceTypeKeyword) continue;
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

    /// <summary>Đọc lại danh sách cảnh báo AI (loai = "AI_CANH_BAO") từ JSON PhanTichAI.DauHieu.</summary>
    public static IReadOnlyList<AiModerationSignal> ParseAiSignals(string? json)
    {
        var result = new List<AiModerationSignal>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return result;
            foreach (var e in doc.RootElement.EnumerateArray())
            {
                if (e.ValueKind != JsonValueKind.Object) continue;
                if (!e.TryGetProperty("loai", out var loai) || loai.GetString() != ModerationPolicy.EvidenceTypeAi) continue;
                var group = e.TryGetProperty("nhom", out var g) && g.ValueKind == JsonValueKind.String ? g.GetString() ?? "SPAM_KHAC" : "SPAM_KHAC";
                var level = e.TryGetProperty("muc_do", out var l) && l.ValueKind == JsonValueKind.String ? l.GetString() ?? ModerationPolicy.RiskMedium : ModerationPolicy.RiskMedium;
                var field = e.TryGetProperty("truong", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString() ?? "MoTa" : "MoTa";
                var evidence = e.TryGetProperty("bang_chung", out var b) && b.ValueKind == JsonValueKind.String ? b.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(evidence)) continue;
                result.Add(new AiModerationSignal(group, level, field, evidence));
            }
        }
        catch (JsonException)
        {
            result.Clear();
        }
        return result;
    }

    /// <summary>Băm SHA-256 (hex thường, 64 ký tự khớp char(64)) của tập phát hiện; đổi khi tập từ khóa/tín hiệu AI đổi.</summary>
    public static string ComputeHash(IReadOnlyList<KeywordMatch> matches, IReadOnlyList<AiModerationSignal>? aiSignals = null)
    {
        var parts = matches
            .OrderBy(m => m.KeywordId).ThenBy(m => m.Field, StringComparer.Ordinal)
            .Select(m => $"{m.KeywordId}|{m.Level}|{m.Field}")
            .ToList();

        if (aiSignals is { Count: > 0 })
        {
            parts.AddRange(aiSignals
                .OrderBy(s => s.Group, StringComparer.Ordinal)
                .ThenBy(s => s.Field, StringComparer.Ordinal)
                .ThenBy(s => s.Evidence, StringComparer.Ordinal)
                .Select(s => $"AI|{s.Group}|{s.Level}|{s.Field}|{s.Evidence}"));
        }

        var canonical = string.Join(";", parts);
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
        return Truncate(text, 2000);
    }

    private static string MaxRisk(string a, string b) =>
        RiskRank(b) > RiskRank(a) ? NormalizeRiskOrDefault(b, a) : a;

    private static string MaxRecommendation(string a, string b) =>
        RecommendationRank(b) > RecommendationRank(a) ? NormalizeRecommendationOrDefault(b, a) : a;

    private static int RiskRank(string? risk) => risk?.Trim().ToUpperInvariant() switch
    {
        ModerationPolicy.RiskHigh => 3,
        ModerationPolicy.RiskMedium => 2,
        ModerationPolicy.RiskLow => 1,
        _ => 0
    };

    private static string NormalizeRiskOrDefault(string? raw, string fallback) => raw?.Trim().ToUpperInvariant() switch
    {
        ModerationPolicy.RiskHigh => ModerationPolicy.RiskHigh,
        ModerationPolicy.RiskMedium => ModerationPolicy.RiskMedium,
        ModerationPolicy.RiskLow => ModerationPolicy.RiskLow,
        _ => fallback
    };

    private static int RecommendationRank(string? advice) => advice?.Trim().ToUpperInvariant() switch
    {
        ModerationPolicy.AdviceReject => 3,
        ModerationPolicy.AdviceNeedsReview => 2,
        ModerationPolicy.AdviceApprove => 1,
        _ => 0
    };

    private static string NormalizeRecommendationOrDefault(string? raw, string fallback) => raw?.Trim().ToUpperInvariant() switch
    {
        ModerationPolicy.AdviceReject => ModerationPolicy.AdviceReject,
        ModerationPolicy.AdviceNeedsReview => ModerationPolicy.AdviceNeedsReview,
        ModerationPolicy.AdviceApprove => ModerationPolicy.AdviceApprove,
        _ => fallback
    };

    private static string BuildHybridModelVersion(string? modelName)
    {
        var clean = string.IsNullOrWhiteSpace(modelName) ? "llm" : modelName.Trim();
        var full = ModerationPolicy.HybridModelPrefix + clean;
        return Truncate(full, ModerationPolicy.MaxModelVersionLength);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
