using System;

using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class LRetentionTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

    // ---- Expiry ----

    [Fact]
    public void Expired_FortyDaysOld_ThirtyDayBudget_True()
    {
        Assert.True(LRetention.LRetentionExpiredCheck(Now.AddDays(-40), Now, 30));
    }

    [Fact]
    public void Expired_TenDaysOld_ThirtyDayBudget_False()
    {
        Assert.False(LRetention.LRetentionExpiredCheck(Now.AddDays(-10), Now, 30));
    }

    [Fact]
    public void Expired_ZeroDays_NeverExpired()
    {
        Assert.False(LRetention.LRetentionExpiredCheck(Now.AddDays(-40), Now, 0));
    }

    [Fact]
    public void Expired_NegativeDays_NeverExpired()
    {
        Assert.False(LRetention.LRetentionExpiredCheck(Now.AddDays(-40), Now, -5));
    }

    // ---- Exclusion ----

    [Theory]
    [InlineData("scheduled/x.json")]
    [InlineData("running/y.json")]
    [InlineData("palettes/set.json")]
    [InlineData("work.db")]
    [InlineData("work.db-wal")]
    [InlineData("work.db-shm")]
    public void Excluded_ProtectedPaths_True(string path)
    {
        Assert.True(LRetention.LRetentionExcludedCheck(path));
    }

    [Theory]
    [InlineData("done/z.json")]
    [InlineData("audiowork/a.mp4")]
    [InlineData("relayplans/p.json")]
    public void Excluded_OrdinaryPaths_False(string path)
    {
        Assert.False(LRetention.LRetentionExcludedCheck(path));
    }

    [Fact]
    public void Excluded_CaseInsensitiveRoot_True()
    {
        Assert.True(LRetention.LRetentionExcludedCheck("Scheduled/x.json"));
    }

    [Fact]
    public void Excluded_Backslash_Root_True()
    {
        Assert.True(LRetention.LRetentionExcludedCheck("running\\y.json"));
    }
}
