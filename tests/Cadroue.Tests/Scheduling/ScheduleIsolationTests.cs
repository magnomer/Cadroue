using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class ScheduleIsolationTests
{
    [Fact]
    public void ForeignSignet_IsNotClaimed()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();

        schedule.SignetSet(ownerA);
        schedule.WorkCreate("isolated");

        schedule.SignetSet(ownerB);
        Assert.False(schedule.ClaimFound());
    }

    [Fact]
    public void OwnSignet_IsClaimed()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();

        schedule.SignetSet(ownerA);
        Guid workId = schedule.WorkCreate("owned");

        Assert.Equal(workId, schedule.ClaimId());
    }

    [Fact]
    public void LegacySignet_IsClaimableByAnySignet()
    {
        using var schedule = new TScheduleSignet();

        schedule.SignetSet(Guid.Empty);
        Guid workId = schedule.WorkCreate("legacy");

        schedule.SignetSet(Guid.NewGuid());
        Assert.Equal(workId, schedule.ClaimId());
    }

    [Fact]
    public void DisplayedRecords_ExcludeForeignSignetByDefault()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();

        schedule.SignetSet(ownerA);
        Guid foreignId = schedule.WorkCreate("foreign");

        schedule.SignetSet(ownerB);
        Guid ownId = schedule.WorkCreate("own");
        schedule.SharedSet(false);

        IReadOnlyList<Guid> displayed = schedule.DisplayedRead();
        Assert.Contains(ownId, displayed);
        Assert.DoesNotContain(foreignId, displayed);
    }

    [Fact]
    public void DisplayedRecords_IncludeForeignSignetWhenShared()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();

        schedule.SignetSet(ownerA);
        Guid foreignId = schedule.WorkCreate("foreign");

        schedule.SignetSet(ownerB);
        Guid ownId = schedule.WorkCreate("own");
        schedule.SharedSet(true);

        IReadOnlyList<Guid> displayed = schedule.DisplayedRead();
        Assert.Contains(ownId, displayed);
        Assert.Contains(foreignId, displayed);
    }

    [Fact]
    public void DisplayedRecords_AlwaysIncludeLegacySignet()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();

        schedule.SignetSet(Guid.Empty);
        Guid legacyId = schedule.WorkCreate("legacy");

        schedule.SignetSet(ownerA);
        Guid foreignId = schedule.WorkCreate("foreign");

        schedule.SignetSet(ownerB);
        schedule.SharedSet(false);

        IReadOnlyList<Guid> displayed = schedule.DisplayedRead();
        Assert.Contains(legacyId, displayed);
        Assert.DoesNotContain(foreignId, displayed);
    }
}
