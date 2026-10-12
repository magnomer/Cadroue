using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Cartographer")]
public sealed class TActionFace
{
    private static QStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static QStripTab TStripTabAdd(QStrip strip, string key)
    {
        QStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate(key));
        return tab;
    }

    [Fact]
    public void AutoSet_ReadsCheckedOnly()
    {
        QAction action = TInterface.TActionCreate();

        TInterface.TActionAutoSet(action, null);
        Assert.False(action.QActionAutoRelay);

        TInterface.TActionAutoSet(action, true);
        Assert.True(action.QActionAutoRelay);
    }

    [Fact]
    public void RelayAttach_HoldsTabAndAnswersStripSeams()
    {
        QStrip strip = TStripBuild();
        QStripTab source = TStripTabAdd(strip, "Split");
        QAction action = TInterface.TActionCreate();

        TInterface.TActionRelayAttach(action, strip, source);

        Assert.Same(action, source.QStripTabAction);
        Assert.Equal(source.QStripTabId, TInterface.TActionRouteRead(action).TActionSource);
        Assert.False(TInterface.TStripRelayCheck(source));

        TInterface.TActionAutoSet(action, true);
        Assert.True(TInterface.TStripRelayCheck(source));
    }

    [Fact]
    public void RelaySelect_ShowsTabFace_RefusesSelf()
    {
        QStrip strip = TStripBuild();
        QStripTab source = TStripTabAdd(strip, "Split");
        QStripTab target = TStripTabAdd(strip, "Edit");
        QAction action = TInterface.TActionCreate();
        TInterface.TActionRelayAttach(action, strip, source);
        int faces = 0;
        TInterface.TActionFaceAttach(action, () => faces++);

        Assert.False(action.QActionFaceShown);
        Assert.Equal(string.Empty, action.QActionFaceIcon);
        Assert.Equal([target.QStripTabId], TInterface.TActionOptionsRead(action).Select(o => o.QActionOptionId));

        TInterface.TActionRelaySelect(action, target.QStripTabId);

        Assert.Equal(target.QStripTabId, TInterface.TActionRouteRead(action).TActionTarget);
        Assert.True(action.QActionFaceShown);
        Assert.Equal("Edit", action.QActionFaceIcon);
        Assert.Equal(target.QStripTabTitle, action.QActionFaceText);
        Assert.Equal(1, faces);

        TInterface.TActionRelaySelect(action, target.QStripTabId);
        Assert.Equal(1, faces);

        TInterface.TActionRelaySelect(action, source.QStripTabId);
        Assert.Equal(target.QStripTabId, TInterface.TActionRouteRead(action).TActionTarget);

        TInterface.TActionRelaySelect(action, LCartographer.LCartographerFinishTarget);
        Assert.Equal(LCartographer.LCartographerFinishTarget, TInterface.TActionRouteRead(action).TActionTarget);
        Assert.False(action.QActionFaceShown);
    }

    [Fact]
    public void RelayApply_UnknownTab_FallsBackToNone()
    {
        QStrip strip = TStripBuild();
        QStripTab source = TStripTabAdd(strip, "Split");
        QAction action = TInterface.TActionCreate();
        TInterface.TActionRelayAttach(action, strip, source);

        TInterface.TActionRelayApply(action, Guid.NewGuid());

        Assert.Equal(Guid.Empty, TInterface.TActionRouteRead(action).TActionTarget);
        Assert.False(action.QActionFaceShown);
    }

    [Fact]
    public void MenuRead_WrapsOptionsWithNoneAndFinish()
    {
        QStrip strip = TStripBuild();
        QStripTab source = TStripTabAdd(strip, "Split");
        QStripTab target = TStripTabAdd(strip, "Convert");
        QAction action = TInterface.TActionCreate();
        TInterface.TActionRelayAttach(action, strip, source);

        IReadOnlyList<QActionOption> menu = TInterface.TActionMenuRead(action);

        Assert.Equal(
            [Guid.Empty, target.QStripTabId, LCartographer.LCartographerFinishTarget],
            menu.Select(o => o.QActionOptionId));
        Assert.Equal(string.Empty, menu[0].QActionOptionKey);
        Assert.Equal("Convert", menu[1].QActionOptionKey);
    }

    [Fact]
    public void Runs_GateOnEligible_RaiseEmptyOtherwise()
    {
        QAction action = TInterface.TActionCreate();
        List<LWorkPriority> runs = [];
        int alls = 0;
        int empties = 0;
        List<string> items = [];
        TInterface.TActionRunAttach(action, runs.Add);
        TInterface.TActionAllAttach(action, () => alls++);
        TInterface.TActionEmptyAttach(action, () => empties++);
        TInterface.TActionItemsAttach(action, paths => items.AddRange(paths));
        TInterface.TActionEligibleAttach(action, () => []);

        TInterface.TActionAllRun(action);
        TInterface.TActionHighRun(action);
        TInterface.TActionListRun(action);

        Assert.Empty(runs);
        Assert.Equal(0, alls);
        Assert.Equal(3, empties);

        TInterface.TActionEligibleAttach(action, () => ["a.mp4"]);
        TInterface.TActionAllRun(action);
        TInterface.TActionHighRun(action);
        TInterface.TActionListRun(action);

        Assert.Equal(1, alls);
        Assert.Equal([LWorkPriority.LWorkPriorityHigh, LWorkPriority.LWorkPriorityNormal], runs);
        Assert.Empty(items);
    }

    [Fact]
    public void ListRun_WithSelection_SendsItems()
    {
        QList list = TInterfaceList.TListCreate();
        LDocket docket = list.QListDocket;
        TInterfaceList.TDocketPathsAdd(docket, @"C:\media\a.mp4", @"C:\media\b.mp4");
        TInterfaceList.TListPressSelect(list, @"C:\media\b.mp4", false, false);
        QAction action = TInterface.TActionCreate();
        TInterface.TActionListAttach(action, list);
        List<string> items = [];
        List<LWorkPriority> runs = [];
        TInterface.TActionItemsAttach(action, paths => items.AddRange(paths));
        TInterface.TActionRunAttach(action, runs.Add);

        TInterface.TActionListRun(action);

        Assert.Equal([@"C:\media\b.mp4"], items);
        Assert.Empty(runs);
    }
}
