using SecondHandMarketplace.Web.Services.Accounts;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class AccountAppealRulesTests
{
    [Theory]
    [InlineData(0, true, 1)]
    [InlineData(1, true, 1)]
    [InlineData(2, true, 2)]
    [InlineData(0, false, 0)]
    [InlineData(2, false, 2)]
    public void NormalizeLockCount_HandlesVariousStates(int lockCount, bool isLocked, int expected)
    {
        var result = AccountAppealRules.NormalizeLockCount(lockCount, isLocked);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    public void IsUnlockRequestAllowed_OnlyAllowsTier1AndTier2(int effectiveLockCount, bool expected)
    {
        var result = AccountAppealRules.IsUnlockRequestAllowed(effectiveLockCount);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(AccountAppealRules.TypeUnlockRequest, 1, false)]
    [InlineData(AccountAppealRules.TypeUnlockRequest, 2, true)]
    [InlineData(AccountAppealRules.TypeUnlockRequest, 3, false)]
    [InlineData(AccountAppealRules.TypeAppeal, 1, false)]
    [InlineData(AccountAppealRules.TypeAppeal, 2, false)]
    public void RequiresUnlockFee_OnlyAppliesToTier2UnlockRequest(string type, int lockCount, bool expected)
    {
        var result = AccountAppealRules.RequiresUnlockFee(type, lockCount);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildTransferContent_GeneratesCorrectFormat()
    {
        var content = AccountAppealRules.BuildTransferContent(12, 2);
        Assert.Equal("MOKHOA U12 L2", content);
    }

    [Fact]
    public void EvaluateCycle_NoRejectedAppeals_ReturnsFullAttempts()
    {
        var now = DateTime.UtcNow;
        var result = AccountAppealRules.EvaluateCycle(new List<DateTime>(), 2, 4320, now);

        Assert.Equal(0, result.AttemptsUsedInCurrentCycle);
        Assert.Equal(2, result.AttemptsRemainingInCurrentCycle);
        Assert.False(result.IsInCooldown);
        Assert.Null(result.CooldownEndsAtUtc);
    }

    [Fact]
    public void EvaluateCycle_OneRejectedAppeal_ReturnsOneRemainingAttempt()
    {
        var now = DateTime.UtcNow;
        var rejectedList = new List<DateTime> { now.AddMinutes(-30) };
        var result = AccountAppealRules.EvaluateCycle(rejectedList, 2, 4320, now);

        Assert.Equal(1, result.AttemptsUsedInCurrentCycle);
        Assert.Equal(1, result.AttemptsRemainingInCurrentCycle);
        Assert.False(result.IsInCooldown);
        Assert.Null(result.CooldownEndsAtUtc);
    }

    [Fact]
    public void EvaluateCycle_TwoRejectedAppeals_WithinCooldown_TriggersCooldown()
    {
        var now = DateTime.UtcNow;
        var lastRejected = now.AddMinutes(-60);
        var rejectedList = new List<DateTime> { now.AddMinutes(-120), lastRejected };
        var result = AccountAppealRules.EvaluateCycle(rejectedList, 2, 4320, now);

        Assert.Equal(2, result.AttemptsUsedInCurrentCycle);
        Assert.Equal(0, result.AttemptsRemainingInCurrentCycle);
        Assert.True(result.IsInCooldown);
        Assert.NotNull(result.CooldownEndsAtUtc);
        Assert.Equal(lastRejected.AddMinutes(4320), result.CooldownEndsAtUtc.Value);
    }

    [Fact]
    public void EvaluateCycle_TwoRejectedAppeals_AfterCooldown_OpensNewCycle()
    {
        var now = DateTime.UtcNow;
        var lastRejected = now.AddMinutes(-4400); // Past 4320 minutes (3 days)
        var rejectedList = new List<DateTime> { now.AddMinutes(-5000), lastRejected };
        var result = AccountAppealRules.EvaluateCycle(rejectedList, 2, 4320, now);

        Assert.Equal(0, result.AttemptsUsedInCurrentCycle);
        Assert.Equal(2, result.AttemptsRemainingInCurrentCycle);
        Assert.False(result.IsInCooldown);
        Assert.Null(result.CooldownEndsAtUtc);
    }

    [Fact]
    public void SplitAndCombineCooldownMinutes_RoundTripCorrectly()
    {
        const int originalMinutes = 1575; // 1 day, 2 hours, 15 minutes
        var (d, h, m) = AccountAppealRules.SplitCooldownMinutes(originalMinutes);

        Assert.Equal(1, d);
        Assert.Equal(2, h);
        Assert.Equal(15, m);

        var combined = AccountAppealRules.CombineCooldownMinutes(d, h, m);
        Assert.Equal(originalMinutes, combined);
    }

    [Fact]
    public void FormatCooldownDuration_ReturnsReadableString()
    {
        var text = AccountAppealRules.FormatCooldownDuration(4320);
        Assert.Equal("3 ngày", text);

        var complex = AccountAppealRules.FormatCooldownDuration(1575);
        Assert.Equal("1 ngày 2 giờ 15 phút", complex);
    }

    [Fact]
    public void FormatRemainingTime_HandlesZeroAndSubMinute()
    {
        Assert.Equal("0 phút", AccountAppealRules.FormatRemainingTime(TimeSpan.Zero));
        Assert.Equal("dưới 1 phút", AccountAppealRules.FormatRemainingTime(TimeSpan.FromSeconds(25)));
    }
}
