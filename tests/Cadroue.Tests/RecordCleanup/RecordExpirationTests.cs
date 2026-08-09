using System;

using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class RecordExpirationTests
{
    private static readonly DateTime Now = new(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Expired_FortyDaysOld_ThirtyDayBudget_True()
    {
        Assert.True(TInterface.RetentionExpiredCheck(Now.AddDays(-40), Now, 30));
    }

    [Fact]
    public void Expired_TenDaysOld_ThirtyDayBudget_False()
    {
        Assert.False(TInterface.RetentionExpiredCheck(Now.AddDays(-10), Now, 30));
    }

    [Fact]
    public void Expired_ZeroDays_NeverExpired()
    {
        Assert.False(TInterface.RetentionExpiredCheck(Now.AddDays(-40), Now, 0));
    }

    [Fact]
    public void Expired_NegativeDays_NeverExpired()
    {
        Assert.False(TInterface.RetentionExpiredCheck(Now.AddDays(-40), Now, -5));
    }
}
