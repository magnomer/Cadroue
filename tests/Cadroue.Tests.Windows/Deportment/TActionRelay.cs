using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Cartographer")]
public sealed class TActionRelay
{
    private static QStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static (QStripTab TStripTab, QAction TAction) TActionTabAdd(QStrip strip, string key, bool auto)
    {
        QStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate(key));
        QAction action = TInterface.TActionCreate();
        TInterface.TActionRelayAttach(action, strip, tab);
        TInterface.TActionAutoSet(action, auto);
        return (tab, action);
    }

    [Fact]
    public void Accept_DeliversToAutoTab_OnlyWhenArmed()
    {
        QStrip strip = TStripBuild();
        TInterface.TActionStripAttach(strip);
        (QStripTab armed, QAction armedAction) = TActionTabAdd(strip, "Edit", true);
        (QStripTab idle, QAction idleAction) = TActionTabAdd(strip, "Convert", false);
        List<string> armedItems = [];
        List<string> idleItems = [];
        TInterface.TActionItemsAttach(armedAction, paths => armedItems.AddRange(paths));
        TInterface.TActionItemsAttach(idleAction, paths => idleItems.AddRange(paths));

        TInterface.TActionAccept(armed.QStripTabId, @"C:\media\a.mp4", Guid.NewGuid());
        TInterface.TActionAccept(idle.QStripTabId, @"C:\media\b.mp4", Guid.NewGuid());
        TInterface.TActionAccept(Guid.NewGuid(), @"C:\media\c.mp4", Guid.NewGuid());

        Assert.Equal([@"C:\media\a.mp4"], armedItems);
        Assert.Empty(idleItems);
    }

    [Fact]
    public void Accept_SkipsMergeTab_EvenWhenArmed()
    {
        QStrip strip = TStripBuild();
        TInterface.TActionStripAttach(strip);
        (QStripTab merge, QAction mergeAction) = TActionTabAdd(strip, "Merge", true);
        List<string> items = [];
        TInterface.TActionItemsAttach(mergeAction, paths => items.AddRange(paths));

        TInterface.TActionAccept(merge.QStripTabId, @"C:\media\a.mp4", Guid.NewGuid());

        Assert.Empty(items);
    }

    [Fact]
    public void CohortRun_NeedsAutoAndAnAddedItem()
    {
        QStrip strip = TStripBuild();
        (QStripTab tab, QAction action) = TActionTabAdd(strip, "Merge", false);
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
        QStrip strip = TStripBuild();
        (QStripTab source, QAction action) = TActionTabAdd(strip, "Split", false);
        (QStripTab target, _) = TActionTabAdd(strip, "Edit", false);
        TInterface.TActionRelaySelect(action, target.QStripTabId);
        Assert.Equal(target.QStripTabTitle, action.QActionFaceText);

        Assert.True(TInterface.TStripNameSet(strip, target, "Renamed"));

        Assert.Equal("Renamed", action.QActionFaceText);
        Assert.Equal(target.QStripTabId, TInterface.TActionRouteRead(action).TActionTarget);
        Assert.Equal(source.QStripTabId, TInterface.TActionRouteRead(action).TActionSource);
    }
}
