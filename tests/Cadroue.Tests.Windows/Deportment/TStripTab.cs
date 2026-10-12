using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TStripTab
{
    private static QStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static QStripTab TStripTabAdd(QStrip strip, string key)
    {
        QStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        return tab;
    }

    [Fact]
    public void Add_NumbersSameKind_AndSelectsFirst()
    {
        QStrip strip = TStripBuild();
        List<QStripTab?> selections = [];
        TInterface.TStripSelectAttach(strip, tab => selections.Add(tab));

        QStripTab first = TStripTabAdd(strip, "Split");
        QStripTab second = TStripTabAdd(strip, "Split");
        QStripTab edit = TStripTabAdd(strip, "Edit");

        Assert.Equal("Split 1", first.QStripTabTitle);
        Assert.Equal("Split 2", second.QStripTabTitle);
        Assert.Equal("Edit", edit.QStripTabTitle);
        Assert.Same(first, strip.QStripSelected);
        Assert.True(first.QStripTabSelected);
        Assert.False(second.QStripTabSelected);
        Assert.Equal([first], selections);
    }

    [Fact]
    public void Remove_SelectedTab_SelectsNextAndRenumbers()
    {
        QStrip strip = TStripBuild();
        QStripTab first = TStripTabAdd(strip, "Split");
        QStripTab second = TStripTabAdd(strip, "Split");
        QStripTab third = TStripTabAdd(strip, "Split");
        TInterface.TStripSelect(strip, second);

        TInterface.TStripRemove(strip, second);

        Assert.Same(third, strip.QStripSelected);
        Assert.Equal("Split 1", first.QStripTabTitle);
        Assert.Equal("Split 2", third.QStripTabTitle);

        TInterface.TStripRemove(strip, third);
        Assert.Same(first, strip.QStripSelected);
        Assert.Equal("Split", first.QStripTabTitle);

        TInterface.TStripRemove(strip, first);
        Assert.Null(strip.QStripSelected);
    }

    [Fact]
    public void NameSet_ResolvesDuplicates_AndResetsToStandard()
    {
        QStrip strip = TStripBuild();
        QStripTab first = TStripTabAdd(strip, "Split");
        QStripTab second = TStripTabAdd(strip, "Split");

        Assert.True(TInterface.TStripNameSet(strip, first, "  Intro "));
        Assert.Equal("Intro", first.QStripTabTitle);
        Assert.Equal("Split", second.QStripTabTitle);

        Assert.True(TInterface.TStripNameSet(strip, second, "Intro"));
        Assert.Equal("Intro 2", second.QStripTabTitle);

        Assert.False(TInterface.TStripNameSet(strip, first, "Split"));
        Assert.Equal(string.Empty, first.QStripTabCustom);
        Assert.Equal("Split", first.QStripTabTitle);
    }

    [Fact]
    public void Separator_HidesAroundSelectedAndHovered()
    {
        QStrip strip = TStripBuild();
        QStripTab a = TStripTabAdd(strip, "Split");
        QStripTab b = TStripTabAdd(strip, "Edit");
        QStripTab c = TStripTabAdd(strip, "Audio");
        QStripTab d = TStripTabAdd(strip, "Merge");

        Assert.False(a.QStripTabSeparator);
        Assert.True(b.QStripTabSeparator);
        Assert.True(c.QStripTabSeparator);
        Assert.False(d.QStripTabSeparator);

        TInterface.TStripHoverSet(strip, d);
        Assert.False(c.QStripTabSeparator);

        TInterface.TStripHoverClear(strip, a);
        Assert.False(c.QStripTabSeparator);

        TInterface.TStripHoverClear(strip, null);
        Assert.True(c.QStripTabSeparator);
    }

    [Fact]
    public void Move_ReordersAndClamps()
    {
        QStrip strip = TStripBuild();
        QStripTab a = TStripTabAdd(strip, "Split");
        QStripTab b = TStripTabAdd(strip, "Edit");
        QStripTab c = TStripTabAdd(strip, "Audio");

        Assert.True(TInterface.TStripMove(strip, a, 9));
        Assert.Equal([b, c, a], strip.QStripTabs);
        Assert.False(TInterface.TStripMove(strip, a, 2));
    }

    [Fact]
    public void Suspend_DefersTitlesAndSelection()
    {
        QStrip strip = TStripBuild();
        TInterface.TStripUpdateSuspend(strip);
        QStripTab first = TStripTabAdd(strip, "Split");
        QStripTab second = TStripTabAdd(strip, "Split");

        Assert.Null(strip.QStripSelected);
        Assert.Equal("Split", second.QStripTabTitle);

        TInterface.TStripUpdateResume(strip);
        TInterface.TStripTitleUpdate(strip);

        Assert.Equal("Split 1", first.QStripTabTitle);
        Assert.Equal("Split 2", second.QStripTabTitle);
    }

    [Fact]
    public void KeyResolve_FallsBackToSplit()
    {
        Assert.Equal("Edit", TInterface.TStripKeyResolve("Edit"));
        Assert.Equal("Split", TInterface.TStripKeyResolve("Unknown"));
        Assert.Equal("Split", TInterface.TStripKeyCreate("Bogus").QStripTabKey);
        Assert.Equal("Worklist", TInterface.TStripKeyCreate("Worklist").QStripTabKey);
    }

    [Fact]
    public void Close_RaisesCloseNotice_ThenRemovesTab()
    {
        QStrip strip = TStripBuild();
        QStripTab first = TStripTabAdd(strip, "Split");
        QStripTab second = TStripTabAdd(strip, "Edit");
        List<QStripTab> added = [];
        List<QStripTab> closed = [];
        TInterface.TStripAddAttach(strip, added.Add);
        TInterface.TStripCloseAttach(strip, closed.Add);

        Assert.True(TInterface.TStripCloseConfirm(strip, second));
        Assert.True(TInterface.TStripCloseConfirm(strip, null));
        TInterface.TStripClose(strip, second);
        TInterface.TStripClose(strip, second);

        Assert.Equal([second], closed);
        Assert.Equal([first], strip.QStripTabs);

        QStripTab third = TStripTabAdd(strip, "Fix");
        Assert.Equal([third], added);
        TInterface.TStripAllClose(strip);
        Assert.Equal([second, first, third], closed);
        Assert.Empty(strip.QStripTabs);
        Assert.Null(strip.QStripSelected);
    }

    [Fact]
    public void RelayRead_SkipsSourceAndTabsWithoutList()
    {
        using var schedule = new TSchedule();
        TInterfaceRoster.TStationAttach(schedule.TScheduleRead());
        QStrip strip = TStripBuild();
        QStripTab source = TStripTabAdd(strip, "Split");
        QStripTab listed = TStripTabAdd(strip, "Convert");
        QStripTab bare = TStripTabAdd(strip, "Worklist");
        TInterface.TStripWorkspaceAttach(source, TInterface.TPresetInitialCreate("Split"));
        TInterface.TStripWorkspaceAttach(listed, TInterface.TPresetInitialCreate("Convert"));
        TInterface.TStripWorkspaceAttach(bare, TInterface.TPresetInitialCreate("Worklist"));

        try
        {
            Assert.Equal([listed], TInterface.TStripRelayRead(strip, source.QStripTabId));
            Assert.False(bare.QStripTabBusy);
        }
        finally
        {
            TInterface.TWorkspaceClose(bare.QStripTabWorkspace);
        }
    }

    [Fact]
    public void PendingSet_RaisesTabChangeOnce()
    {
        QStrip strip = TStripBuild();
        QStripTab tab = TStripTabAdd(strip, "Split");
        List<QStripTab> changes = [];
        TInterface.TStripTabAttach(strip, changes.Add);

        TInterface.TStripPendingSet(strip, tab, true);
        TInterface.TStripPendingSet(strip, tab, true);
        Assert.True(tab.QStripTabPending);
        Assert.Equal([tab], changes);

        TInterface.TStripPendingSet(strip, tab, false);
        Assert.False(tab.QStripTabPending);
        Assert.Equal(2, changes.Count);
    }
}
