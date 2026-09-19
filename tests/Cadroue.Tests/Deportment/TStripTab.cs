using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TStripTab
{
    private static LStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static LStripTab TStripTabAdd(LStrip strip, string key)
    {
        LStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        return tab;
    }

    [Fact]
    public void Add_NumbersSameKind_AndSelectsFirst()
    {
        LStrip strip = TStripBuild();
        List<LStripTab?> selections = [];
        TInterface.TStripSelectAttach(strip, tab => selections.Add(tab));

        LStripTab first = TStripTabAdd(strip, "Split");
        LStripTab second = TStripTabAdd(strip, "Split");
        LStripTab edit = TStripTabAdd(strip, "Edit");

        Assert.Equal("Split 1", first.LStripTabTitle);
        Assert.Equal("Split 2", second.LStripTabTitle);
        Assert.Equal("Edit", edit.LStripTabTitle);
        Assert.Same(first, strip.LStripSelected);
        Assert.True(first.LStripTabSelected);
        Assert.False(second.LStripTabSelected);
        Assert.Equal([first], selections);
    }

    [Fact]
    public void Remove_SelectedTab_SelectsNextAndRenumbers()
    {
        LStrip strip = TStripBuild();
        LStripTab first = TStripTabAdd(strip, "Split");
        LStripTab second = TStripTabAdd(strip, "Split");
        LStripTab third = TStripTabAdd(strip, "Split");
        TInterface.TStripSelect(strip, second);

        TInterface.TStripRemove(strip, second);

        Assert.Same(third, strip.LStripSelected);
        Assert.Equal("Split 1", first.LStripTabTitle);
        Assert.Equal("Split 2", third.LStripTabTitle);

        TInterface.TStripRemove(strip, third);
        Assert.Same(first, strip.LStripSelected);
        Assert.Equal("Split", first.LStripTabTitle);

        TInterface.TStripRemove(strip, first);
        Assert.Null(strip.LStripSelected);
    }

    [Fact]
    public void NameSet_ResolvesDuplicates_AndResetsToStandard()
    {
        LStrip strip = TStripBuild();
        LStripTab first = TStripTabAdd(strip, "Split");
        LStripTab second = TStripTabAdd(strip, "Split");

        Assert.True(TInterface.TStripNameSet(strip, first, "  Intro "));
        Assert.Equal("Intro", first.LStripTabTitle);
        Assert.Equal("Split", second.LStripTabTitle);

        Assert.True(TInterface.TStripNameSet(strip, second, "Intro"));
        Assert.Equal("Intro 2", second.LStripTabTitle);

        Assert.False(TInterface.TStripNameSet(strip, first, "Split"));
        Assert.Equal(string.Empty, first.LStripTabCustom);
        Assert.Equal("Split", first.LStripTabTitle);
    }

    [Fact]
    public void Separator_HidesAroundSelectedAndHovered()
    {
        LStrip strip = TStripBuild();
        LStripTab a = TStripTabAdd(strip, "Split");
        LStripTab b = TStripTabAdd(strip, "Edit");
        LStripTab c = TStripTabAdd(strip, "Audio");
        LStripTab d = TStripTabAdd(strip, "Merge");

        Assert.False(a.LStripTabSeparator);
        Assert.True(b.LStripTabSeparator);
        Assert.True(c.LStripTabSeparator);
        Assert.False(d.LStripTabSeparator);

        TInterface.TStripHoverSet(strip, d);
        Assert.False(c.LStripTabSeparator);

        TInterface.TStripHoverClear(strip, a);
        Assert.False(c.LStripTabSeparator);

        TInterface.TStripHoverClear(strip, null);
        Assert.True(c.LStripTabSeparator);
    }

    [Fact]
    public void Move_ReordersAndClamps()
    {
        LStrip strip = TStripBuild();
        LStripTab a = TStripTabAdd(strip, "Split");
        LStripTab b = TStripTabAdd(strip, "Edit");
        LStripTab c = TStripTabAdd(strip, "Audio");

        Assert.True(TInterface.TStripMove(strip, a, 9));
        Assert.Equal([b, c, a], strip.LStripTabs);
        Assert.False(TInterface.TStripMove(strip, a, 2));
    }

    [Fact]
    public void Suspend_DefersTitlesAndSelection()
    {
        LStrip strip = TStripBuild();
        TInterface.TStripUpdateSuspend(strip);
        LStripTab first = TStripTabAdd(strip, "Split");
        LStripTab second = TStripTabAdd(strip, "Split");

        Assert.Null(strip.LStripSelected);
        Assert.Equal("Split", second.LStripTabTitle);

        TInterface.TStripUpdateResume(strip);
        TInterface.TStripTitleUpdate(strip);

        Assert.Equal("Split 1", first.LStripTabTitle);
        Assert.Equal("Split 2", second.LStripTabTitle);
    }

    [Fact]
    public void KeyResolve_FallsBackToSplit()
    {
        Assert.Equal("Edit", TInterface.TStripKeyResolve("Edit"));
        Assert.Equal("Split", TInterface.TStripKeyResolve("Unknown"));
        Assert.Equal("Split", TInterface.TStripKeyCreate("Bogus").LStripTabKey);
        Assert.Equal("Worklist", TInterface.TStripKeyCreate("Worklist").LStripTabKey);
    }

    [Fact]
    public void Close_RaisesCloseNotice_ThenRemovesTab()
    {
        LStrip strip = TStripBuild();
        LStripTab first = TStripTabAdd(strip, "Split");
        LStripTab second = TStripTabAdd(strip, "Edit");
        List<LStripTab> added = [];
        List<LStripTab> closed = [];
        TInterface.TStripAddAttach(strip, added.Add);
        TInterface.TStripCloseAttach(strip, closed.Add);

        Assert.True(TInterface.TStripCloseConfirm(strip, second));
        Assert.True(TInterface.TStripCloseConfirm(strip, null));
        TInterface.TStripClose(strip, second);
        TInterface.TStripClose(strip, second);

        Assert.Equal([second], closed);
        Assert.Equal([first], strip.LStripTabs);

        LStripTab third = TStripTabAdd(strip, "Fix");
        Assert.Equal([third], added);
        TInterface.TStripAllClose(strip);
        Assert.Equal([second, first, third], closed);
        Assert.Empty(strip.LStripTabs);
        Assert.Null(strip.LStripSelected);
    }

    [Fact]
    public void RelayRead_SkipsSourceAndTabsWithoutList()
    {
        LStrip strip = TStripBuild();
        LStripTab source = TStripTabAdd(strip, "Split");
        LStripTab listed = TStripTabAdd(strip, "Convert");
        LStripTab bare = TStripTabAdd(strip, "Worklist");
        TInterface.TStripWorkspaceAttach(source, TInterface.TPresetInitialCreate("Split"), TInterface.TDocketCreate());
        TInterface.TStripWorkspaceAttach(
            listed, TInterface.TPresetInitialCreate("Convert"), TInterface.TDocketCreate());
        TInterface.TStripWorkspaceAttach(bare, TInterface.TPresetInitialCreate("Worklist"), null);

        Assert.Equal([listed], TInterface.TStripRelayRead(strip, source.LStripTabId));
        Assert.False(bare.LStripTabBusy);
    }

    [Fact]
    public void PendingSet_RaisesTabChangeOnce()
    {
        LStrip strip = TStripBuild();
        LStripTab tab = TStripTabAdd(strip, "Split");
        List<LStripTab> changes = [];
        TInterface.TStripTabAttach(strip, changes.Add);

        TInterface.TStripPendingSet(strip, tab, true);
        TInterface.TStripPendingSet(strip, tab, true);
        Assert.True(tab.LStripTabPending);
        Assert.Equal([tab], changes);

        TInterface.TStripPendingSet(strip, tab, false);
        Assert.False(tab.LStripTabPending);
        Assert.Equal(2, changes.Count);
    }
}
