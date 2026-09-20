using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Cartographer")]
public sealed class TActionRelay
{
    private static LStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static (LStripTab TStripTab, LAction TAction) TActionTabAdd(LStrip strip, string key, bool auto)
    {
        LStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate(key), TInterface.TDocketCreate());
        LAction action = TInterface.TActionCreate();
        TInterface.TActionRelayAttach(action, strip, tab);
        TInterface.TActionAutoSet(action, auto);
        return (tab, action);
    }

    [Fact]
    public void Accept_DeliversToAutoTab_OnlyWhenArmed()
    {
        LStrip strip = TStripBuild();
        TInterface.TActionStripAttach(strip);
        (LStripTab armed, LAction armedAction) = TActionTabAdd(strip, "Edit", true);
        (LStripTab idle, LAction idleAction) = TActionTabAdd(strip, "Convert", false);
        List<string> armedItems = [];
        List<string> idleItems = [];
        TInterface.TActionItemsAttach(armedAction, paths => armedItems.AddRange(paths));
        TInterface.TActionItemsAttach(idleAction, paths => idleItems.AddRange(paths));

        TInterface.TActionAccept(armed.LStripTabId, @"C:\media\a.mp4", Guid.NewGuid());
        TInterface.TActionAccept(idle.LStripTabId, @"C:\media\b.mp4", Guid.NewGuid());
        TInterface.TActionAccept(Guid.NewGuid(), @"C:\media\c.mp4", Guid.NewGuid());

        Assert.Equal([@"C:\media\a.mp4"], armedItems);
        Assert.Empty(idleItems);
    }

    [Fact]
    public void Accept_SkipsMergeTab_EvenWhenArmed()
    {
        LStrip strip = TStripBuild();
        TInterface.TActionStripAttach(strip);
        (LStripTab merge, LAction mergeAction) = TActionTabAdd(strip, "Merge", true);
        List<string> items = [];
        TInterface.TActionItemsAttach(mergeAction, paths => items.AddRange(paths));

        TInterface.TActionAccept(merge.LStripTabId, @"C:\media\a.mp4", Guid.NewGuid());

        Assert.Empty(items);
    }

    [Fact]
    public void CohortRun_NeedsAutoAndAnAddedItem()
    {
        LStrip strip = TStripBuild();
        (LStripTab tab, LAction action) = TActionTabAdd(strip, "Merge", false);
        int added = 0;
        TInterface.TActionCohortAttach(action, _ => added);
        Guid cohort = Guid.NewGuid();

        Assert.False(TInterface.TActionCohortRun(action, cohort));
        Assert.False(TInterface.TStripCohortRun(tab, cohort));

        TInterface.TActionAutoSet(action, true);
        Assert.False(TInterface.TActionCohortRun(action, cohort));

        added = 2;
        Assert.True(TInterface.TActionCohortRun(action, cohort));
        Assert.True(TInterface.TStripCohortRun(tab, cohort));
    }

    [Fact]
    public void StripTitleChange_RefreshesTargetFromCartographer()
    {
        LStrip strip = TStripBuild();
        (LStripTab source, LAction action) = TActionTabAdd(strip, "Split", false);
        (LStripTab target, _) = TActionTabAdd(strip, "Edit", false);
        TInterface.TActionRelaySelect(action, target.LStripTabId);
        Assert.Equal(target.LStripTabTitle, action.LActionFaceText);

        Assert.True(TInterface.TStripNameSet(strip, target, "Renamed"));

        Assert.Equal("Renamed", action.LActionFaceText);
        Assert.Equal(target.LStripTabId, action.LActionRelayTarget);
        Assert.Equal(source.LStripTabId, action.LActionSourceTab);
    }
}
