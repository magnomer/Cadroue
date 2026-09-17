using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TRosterSelection
{
    private static LRoster TRosterBuild(IReadOnlyList<Guid> ids)
    {
        LRoster roster = TInterface.TRosterCreate();
        foreach (Guid id in ids)
        {
            TInterface.TRosterOrderAdd(roster, id);
        }

        return roster;
    }

    private static Guid[] TRosterIdsCreate(int count) =>
        Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();

    [Fact]
    public void StepSelect_Plain_ReplacesSelection()
    {
        Guid[] ids = TRosterIdsCreate(3);
        LRoster roster = TRosterBuild(ids);

        TInterface.TRosterStepSelect(roster, ids[0], false, false);
        TInterface.TRosterStepSelect(roster, ids[2], false, false);

        Assert.False(TInterface.TRosterSelectedCheck(roster, ids[0]));
        Assert.True(TInterface.TRosterSelectedCheck(roster, ids[2]));
        Assert.Equal(ids[2], roster.LRosterCurrentId);
    }

    [Fact]
    public void StepSelect_RangeAndToggle()
    {
        Guid[] ids = TRosterIdsCreate(4);
        LRoster roster = TRosterBuild(ids);

        TInterface.TRosterStepSelect(roster, ids[3], false, false);
        TInterface.TRosterStepSelect(roster, ids[1], true, false);
        Assert.Equal(3, roster.LRosterSelectedIds.Count);
        Assert.False(TInterface.TRosterSelectedCheck(roster, ids[0]));
        Assert.Equal(ids[3], roster.LRosterCurrentId);

        TInterface.TRosterStepSelect(roster, ids[2], false, true);
        Assert.False(TInterface.TRosterSelectedCheck(roster, ids[2]));
        Assert.Equal(ids[2], roster.LRosterCurrentId);
    }

    [Fact]
    public void CardSelect_ClearsSteps_AndStepClearsCard()
    {
        Guid[] ids = TRosterIdsCreate(2);
        LRoster roster = TRosterBuild(ids);
        Guid batch = Guid.NewGuid();

        TInterface.TRosterStepSelect(roster, ids[0], false, false);
        TInterface.TRosterCardSelect(roster, batch);
        Assert.Equal(batch, roster.LRosterCardId);
        Assert.Empty(roster.LRosterSelectedIds);
        Assert.Equal(Guid.Empty, roster.LRosterCurrentId);

        TInterface.TRosterStepSelect(roster, ids[1], false, false);
        Assert.Equal(Guid.Empty, roster.LRosterCardId);
    }

    [Fact]
    public void StaleRemove_DropsMissingIdsAndBatches()
    {
        Guid[] ids = TRosterIdsCreate(3);
        LRoster roster = TRosterBuild(ids);
        Guid batch = Guid.NewGuid();
        TInterface.TRosterStepSelect(roster, ids[1], false, false);
        Assert.True(TInterface.TRosterCollapseToggle(roster, batch));
        TInterface.TRosterCardSelect(roster, batch);

        TInterface.TRosterOrderClear(roster);
        TInterface.TRosterOrderAdd(roster, ids[0]);
        TInterface.TRosterStaleRemove(roster, Array.Empty<Guid>());

        Assert.Equal(Guid.Empty, roster.LRosterCardId);
        Assert.Equal(Guid.Empty, roster.LRosterCurrentId);
        Assert.Empty(roster.LRosterSelectedIds);
        Assert.False(TInterface.TRosterCollapsedCheck(roster, batch));
        Assert.True(TInterface.TRosterOrderMatch(roster, [ids[0]]));
        Assert.True(TInterface.TRosterCloseSet(roster));
        Assert.False(TInterface.TRosterCloseSet(roster));
    }
}
