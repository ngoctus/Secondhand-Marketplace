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
}
