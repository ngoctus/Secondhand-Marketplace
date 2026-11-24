using System.Text.Json;
using SecondHandMarketplace.Web.Services.Moderation;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>Kiểm thử phần quy tắc thuần (không database) của C2.</summary>
public class ModerationDecisionTests
{
    private static KeywordMatch Kw(int id, string phrase, string level, string field = "Ten", int? pos = 0) =>
        new(id, phrase, "NHOM", level, field, pos);

    [Fact]
    public void Evaluate_NoKeywords_IsNoneAndApproveSuggestion()
    {
        var o = ModerationDecision.Evaluate(Array.Empty<KeywordMatch>());
        Assert.Equal("NONE", o.Level);
        Assert.Equal("LOW", o.MucRuiRo);          // rủi ro thấp (khác với mức từ khóa LOW)
        Assert.Equal("APPROVE", o.DeXuat);
        Assert.Equal("[]", o.DauHieuJson);
    }

    [Fact]
    public void Evaluate_OnlyLow_NeedsReview()
    {
        var o = ModerationDecision.Evaluate(new[] { Kw(1, "a", "LOW") });
        Assert.Equal("LOW", o.Level);
        Assert.Equal("MEDIUM", o.MucRuiRo);
        Assert.Equal("NEEDS_REVIEW", o.DeXuat);
    }

    [Fact]
    public void Evaluate_HighAndLow_IsHighAndRejectSuggestion()
    {
        var o = ModerationDecision.Evaluate(new[] { Kw(1, "a", "LOW"), Kw(2, "b", "HIGH") });
        Assert.Equal("HIGH", o.Level);
        Assert.Equal("HIGH", o.MucRuiRo);
        Assert.Equal("REJECT", o.DeXuat);
        Assert.Equal("HIGH", o.Matches[0].Level);  // HIGH xếp trước
    }

    [Fact]
    public void Evaluate_UnknownLevel_IsTreatedAsLowNotHigh()
    {
        var o = ModerationDecision.Evaluate(new[] { Kw(1, "a", "MEDIUM") });
        Assert.Equal("LOW", o.Level);
    }

    [Fact]
    public void Evaluate_SameKeywordSameFieldTwice_IsDeduplicated()
    {
        var o = ModerationDecision.Evaluate(new[] { Kw(1, "a", "HIGH", "Ten", 5), Kw(1, "a", "HIGH", "Ten", 2), Kw(1, "a", "HIGH", "MoTa", 3) });
        Assert.Equal(2, o.Matches.Count);
        Assert.Equal(2, o.Matches.First(m => m.Field == "Ten").Position);
    }

    [Fact]
    public void EvidenceJson_IsValidJsonKeepsVietnameseAndRoundTrips()
    {
        var o = ModerationDecision.Evaluate(new[] { new KeywordMatch(7, "hàng giả", "HANG_GIA", "HIGH", "MoTa", 12) });
        using var doc = JsonDocument.Parse(o.DauHieuJson);   // ném lỗi nếu không phải JSON hợp lệ
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Contains("hàng giả", o.DauHieuJson);

        var back = ModerationDecision.ParseEvidence(o.DauHieuJson);
        var m = Assert.Single(back);
        Assert.Equal((7, "hàng giả", "HANG_GIA", "HIGH", "MoTa", 12), (m.KeywordId, m.Phrase, m.Group, m.Level, m.Field, m.Position));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("không phải json")]
    [InlineData("{\"a\":1}")]
    [InlineData("[{\"loai\":\"GIA_BAT_THUONG\",\"bang_chung\":\"x\",\"do_tin_cay\":0.6}]")]
    public void ParseEvidence_ToleratesNullInvalidAndLegacyShapes(string? json)
    {
        Assert.Empty(ModerationDecision.ParseEvidence(json));
    }

    [Fact]
    public void Hash_IsStableAndChangesWithFindings()
    {
        var a = ModerationDecision.ComputeHash(new[] { Kw(1, "a", "LOW"), Kw(2, "b", "LOW") });
        var b = ModerationDecision.ComputeHash(new[] { Kw(2, "b", "LOW"), Kw(1, "a", "LOW") });
        var c = ModerationDecision.ComputeHash(new[] { Kw(1, "a", "LOW") });
        Assert.Equal(64, a.Length);
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Theory]
    [InlineData("Bán hàng GIẢ giá rẻ", "hàng giả", 4)]
    [InlineData("hàng giả", "hang gia", 0)]
    [InlineData("không có gì", "hàng giả", null)]
    [InlineData("hàng giả", "", null)]
    public void FindPosition_UsesNormalizedWordBoundaries(string text, string phrase, int? expected)
    {
        var normalizedPhrase = SecondHandMarketplace.Web.Common.TextHelper.ChuanHoa(phrase);
        Assert.Equal(expected, ModerationDecision.FindPosition(text, normalizedPhrase));
    }

    [Theory]
    // open, status, level, violationRecorded, allowed
    [InlineData(true, "DONE", "NONE", false, true)]
    [InlineData(true, "DONE", "LOW", false, true)]             // LOW: Admin được duyệt (không vi phạm)
    [InlineData(true, "DONE", "HIGH", true, false)]            // HIGH
    [InlineData(true, "DONE", "HIGH", false, false)]
    [InlineData(true, "QUEUED", null, false, false)]           // QUEUED
    [InlineData(true, "FAILED", null, false, false)]           // FAILED
    [InlineData(true, "NONE", null, false, false)]             // chưa có dòng phân tích
    [InlineData(true, "DONE", null, false, false)]             // DONE cũ chưa quét từ khóa
    [InlineData(true, "DONE", "LOW", true, false)]             // đã ghi vi phạm cho lượt này
    [InlineData(false, "DONE", "NONE", false, false)]          // lượt đã có quyết định
    public void CanApprove_Matrix(bool open, string status, string? level, bool violation, bool allowed)
    {
        var (ok, reason) = ModerationDecision.CanApprove(open, status, level, violation);
        Assert.Equal(allowed, ok);
        Assert.Equal(!allowed, reason is not null);
    }

    [Fact]
    public void LockThreshold_IsCentralizedAtThree()
    {
        Assert.Equal(3, ModerationPolicy.ViolationLockThreshold);
    }

    // ---------------------------------------------------------------- C-01 Hybrid Merge & Green Lane

    [Fact]
    public void MergeWithAi_NoneAndAiApprove_IsHybridAndGreenLane()
    {
        var kw = ModerationDecision.Evaluate(Array.Empty<KeywordMatch>());
        var ai = AiModerationResult.Ok("LOW", "APPROVE", "Sản phẩm hợp lệ.", Array.Empty<AiModerationSignal>(), "gemini-3.1-flash-lite");

        var merged = ModerationDecision.MergeWithAi(kw, ai, aiAttempted: true);

        Assert.Equal("NONE", merged.Level);
        Assert.Equal("LOW", merged.MucRuiRo);
        Assert.Equal("APPROVE", merged.DeXuat);
        Assert.Equal("hybrid:gemini-3.1-flash-lite", merged.ModelVersion);
        Assert.True(ModerationPolicy.IsGreenLane("DONE", merged.Level, merged.MucRuiRo, merged.DeXuat, merged.ModelVersion));
    }

    [Fact]
    public void MergeWithAi_NoneAndAiHighRisk_KeepsKeywordLevelNone_MergesSignalsAndIsNotGreenLane()
    {
        var kw = ModerationDecision.Evaluate(Array.Empty<KeywordMatch>());
        var signals = new[]
        {
            new AiModerationSignal("BAT_THUONG_GIA", "HIGH", "Gia", "iPhone 15 giá 300.000đ bất thường."),
            new AiModerationSignal("NGOAI_SAN", "MEDIUM", "MoTa", "Chứa số Zalo yêu cầu cọc ngoài sàn.")
        };
        var ai = AiModerationResult.Ok("HIGH", "REJECT", "Nghi ngờ lừa đảo giá ảo.", signals, "gemini-3.1-flash-lite");

        var merged = ModerationDecision.MergeWithAi(kw, ai, aiAttempted: true);

        Assert.Equal("NONE", merged.Level);
        Assert.Equal("HIGH", merged.MucRuiRo);
        Assert.Equal("REJECT", merged.DeXuat);
        Assert.False(ModerationPolicy.IsGreenLane("DONE", merged.Level, merged.MucRuiRo, merged.DeXuat, merged.ModelVersion));

        // JSON hợp lệ, ParseEvidence không bị vỡ, ParseAiSignals đọc đủ 2 tín hiệu
        Assert.Empty(ModerationDecision.ParseEvidence(merged.DauHieuJson));
        var parsedSignals = ModerationDecision.ParseAiSignals(merged.DauHieuJson);
        Assert.Equal(2, parsedSignals.Count);
        Assert.Contains(parsedSignals, s => s.Group == "BAT_THUONG_GIA" && s.Field == "Gia");
        Assert.Contains(parsedSignals, s => s.Group == "NGOAI_SAN" && s.Field == "MoTa");
    }

    [Fact]
    public void MergeWithAi_LowKeywordAndAiApprove_RespectsSafetyFloor()
    {
        var kw = ModerationDecision.Evaluate(new[] { Kw(1, "replica", "LOW") });
        var ai = AiModerationResult.Ok("LOW", "APPROVE", "Có vẻ ổn.", Array.Empty<AiModerationSignal>(), "gemini-3.1-flash-lite");

        var merged = ModerationDecision.MergeWithAi(kw, ai, aiAttempted: true);

        Assert.Equal("LOW", merged.Level);
        Assert.Equal("MEDIUM", merged.MucRuiRo);        // không bị hạ xuống LOW
        Assert.Equal("NEEDS_REVIEW", merged.DeXuat);    // không bị hạ xuống APPROVE
        Assert.False(ModerationPolicy.IsGreenLane("DONE", merged.Level, merged.MucRuiRo, merged.DeXuat, merged.ModelVersion));
        Assert.Single(ModerationDecision.ParseEvidence(merged.DauHieuJson));
    }

    [Fact]
    public void MergeWithAi_WhenAiFails_FallsBackToKeywordV1Fallback_AndNotGreenLane()
    {
        var kw = ModerationDecision.Evaluate(Array.Empty<KeywordMatch>());
        var failed = AiModerationResult.Fail("HTTP 429 Too Many Requests", "gemini-3.1-flash-lite");

        var merged = ModerationDecision.MergeWithAi(kw, failed, aiAttempted: true);

        Assert.Equal("NONE", merged.Level);
        Assert.Equal("LOW", merged.MucRuiRo);
        Assert.Equal("APPROVE", merged.DeXuat);
        Assert.Equal(ModerationPolicy.ModelVersionFallback, merged.ModelVersion);
        Assert.True(ModerationPolicy.IsFallbackModel(merged.ModelVersion));
        Assert.False(ModerationPolicy.IsGreenLane("DONE", merged.Level, merged.MucRuiRo, merged.DeXuat, merged.ModelVersion));
    }
}
