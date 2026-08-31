using System;

using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class TRetentionExpiration
{
    private static readonly DateTime TRetentionNow = new(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Expired_FortyDaysOld_ThirtyDayBudget_True()
    {
        Assert.True(TInterface.TRetentionExpiredCheck(TRetentionNow.AddDays(-40), TRetentionNow, 30));
    }

    [Fact]
    public void Expired_TenDaysOld_ThirtyDayBudget_False()
    {
        Assert.False(TInterface.TRetentionExpiredCheck(TRetentionNow.AddDays(-10), TRetentionNow, 30));
    }

    [Fact]
    public void Expired_ZeroDays_NeverExpired()
    {
        Assert.False(TInterface.TRetentionExpiredCheck(TRetentionNow.AddDays(-40), TRetentionNow, 0));
    }

    [Fact]
    public void Expired_NegativeDays_NeverExpired()
    {
        Assert.False(TInterface.TRetentionExpiredCheck(TRetentionNow.AddDays(-40), TRetentionNow, -5));
    }
}
