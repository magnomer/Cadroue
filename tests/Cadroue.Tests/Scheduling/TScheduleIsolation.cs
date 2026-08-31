using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TScheduleIsolation
{
    [Fact]
    public void ForeignSignet_IsNotClaimed()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();

        schedule.TSignetSet(ownerA);
        schedule.TWorkCreate("isolated");

        schedule.TSignetSet(ownerB);
        Assert.False(schedule.TSignetClaimCheck());
    }

    [Fact]
    public void OwnSignet_IsClaimed()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();

        schedule.TSignetSet(ownerA);
        Guid workId = schedule.TWorkCreate("owned");

        Assert.Equal(workId, schedule.TSignetClaimRead());
    }

    [Fact]
    public void LegacySignet_IsClaimableByAnySignet()
    {
        using var schedule = new TScheduleSignet();

        schedule.TSignetSet(Guid.Empty);
        Guid workId = schedule.TWorkCreate("legacy");

        schedule.TSignetSet(Guid.NewGuid());
        Assert.Equal(workId, schedule.TSignetClaimRead());
    }

    [Fact]
    public void DisplayedRecords_ExcludeForeignSignetByDefault()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();

        schedule.TSignetSet(ownerA);
        Guid foreignId = schedule.TWorkCreate("foreign");

        schedule.TSignetSet(ownerB);
        Guid ownId = schedule.TWorkCreate("own");
        schedule.TSignetSharedSet(false);

        IReadOnlyList<Guid> displayed = schedule.TSignetDisplayRead();
        Assert.Contains(ownId, displayed);
        Assert.DoesNotContain(foreignId, displayed);
    }

    [Fact]
    public void DisplayedRecords_IncludeForeignSignetWhenShared()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();

        schedule.TSignetSet(ownerA);
        Guid foreignId = schedule.TWorkCreate("foreign");

        schedule.TSignetSet(ownerB);
        Guid ownId = schedule.TWorkCreate("own");
        schedule.TSignetSharedSet(true);

        IReadOnlyList<Guid> displayed = schedule.TSignetDisplayRead();
        Assert.Contains(ownId, displayed);
        Assert.Contains(foreignId, displayed);
    }

    [Fact]
    public void DisplayedRecords_AlwaysIncludeLegacySignet()
    {
        using var schedule = new TScheduleSignet();
        Guid ownerA = Guid.NewGuid();
        Guid ownerB = Guid.NewGuid();

        schedule.TSignetSet(Guid.Empty);
        Guid legacyId = schedule.TWorkCreate("legacy");

        schedule.TSignetSet(ownerA);
        Guid foreignId = schedule.TWorkCreate("foreign");

        schedule.TSignetSet(ownerB);
        schedule.TSignetSharedSet(false);

        IReadOnlyList<Guid> displayed = schedule.TSignetDisplayRead();
        Assert.Contains(legacyId, displayed);
        Assert.DoesNotContain(foreignId, displayed);
    }
}
