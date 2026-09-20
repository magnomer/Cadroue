using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Cartographer")]
public sealed class TActionFace
{
    private static LStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static LStripTab TStripTabAdd(LStrip strip, string key)
    {
        LStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate(key), TInterface.TDocketCreate());
        return tab;
    }

    [Fact]
    public void AutoSet_ReadsCheckedOnly()
    {
        LAction action = TInterface.TActionCreate();

        TInterface.TActionAutoSet(action, null);
        Assert.False(action.LActionAutoRelay);

        TInterface.TActionAutoSet(action, true);
        Assert.True(action.LActionAutoRelay);
    }

    [Fact]
    public void RelayAttach_HoldsTabAndAnswersStripSeams()
    {
        LStrip strip = TStripBuild();
        LStripTab source = TStripTabAdd(strip, "Split");
        LAction action = TInterface.TActionCreate();

        TInterface.TActionRelayAttach(action, strip, source);

        Assert.Same(action, source.LStripTabAction);
        Assert.Equal(source.LStripTabId, action.LActionSourceTab);
        Assert.False(TInterface.TStripRelayCheck(source));

        TInterface.TActionAutoSet(action, true);
        Assert.True(TInterface.TStripRelayCheck(source));
    }

    [Fact]
    public void RelaySelect_ShowsTabFace_RefusesSelf()
    {
        LStrip strip = TStripBuild();
        LStripTab source = TStripTabAdd(strip, "Split");
        LStripTab target = TStripTabAdd(strip, "Edit");
        LAction action = TInterface.TActionCreate();
        TInterface.TActionRelayAttach(action, strip, source);
        int faces = 0;
        TInterface.TActionFaceAttach(action, () => faces++);

        Assert.False(action.LActionFaceShown);
        Assert.Equal(string.Empty, action.LActionFaceIcon);
        Assert.Equal([target.LStripTabId], TInterface.TActionOptionsRead(action).Select(o => o.LActionOptionId));

        TInterface.TActionRelaySelect(action, target.LStripTabId);

        Assert.Equal(target.LStripTabId, action.LActionRelayTarget);
        Assert.True(action.LActionFaceShown);
        Assert.Equal("Edit", action.LActionFaceIcon);
        Assert.Equal(target.LStripTabTitle, action.LActionFaceText);
        Assert.Equal(1, faces);

        TInterface.TActionRelaySelect(action, target.LStripTabId);
        Assert.Equal(1, faces);

        TInterface.TActionRelaySelect(action, source.LStripTabId);
        Assert.Equal(target.LStripTabId, action.LActionRelayTarget);

        TInterface.TActionRelaySelect(action, LCartographer.LCartographerFinishTarget);
        Assert.Equal(LCartographer.LCartographerFinishTarget, action.LActionRelayTarget);
        Assert.False(action.LActionFaceShown);
    }

    [Fact]
    public void RelayApply_UnknownTab_FallsBackToNone()
    {
        LStrip strip = TStripBuild();
        LStripTab source = TStripTabAdd(strip, "Split");
        LAction action = TInterface.TActionCreate();
        TInterface.TActionRelayAttach(action, strip, source);

        TInterface.TActionRelayApply(action, Guid.NewGuid());

        Assert.Equal(Guid.Empty, action.LActionRelayTarget);
        Assert.False(action.LActionFaceShown);
    }

    [Fact]
    public void MenuRead_WrapsOptionsWithNoneAndFinish()
    {
        LStrip strip = TStripBuild();
        LStripTab source = TStripTabAdd(strip, "Split");
        LStripTab target = TStripTabAdd(strip, "Convert");
        LAction action = TInterface.TActionCreate();
        TInterface.TActionRelayAttach(action, strip, source);

        IReadOnlyList<LActionOption> menu = TInterface.TActionMenuRead(action);

        Assert.Equal(
            [Guid.Empty, target.LStripTabId, LCartographer.LCartographerFinishTarget],
            menu.Select(o => o.LActionOptionId));
        Assert.Equal(string.Empty, menu[0].LActionOptionKey);
        Assert.Equal("Convert", menu[1].LActionOptionKey);
    }

    [Fact]
    public void Runs_GateOnEligible_RaiseEmptyOtherwise()
    {
        LAction action = TInterface.TActionCreate();
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
        LDocket docket = TInterface.TDocketCreate();
        TInterface.TDocketPathsAdd(docket, @"C:\media\a.mp4", @"C:\media\b.mp4");
        LList list = TInterface.TListCreate(docket);
        TInterface.TListPressSelect(list, @"C:\media\b.mp4", false, false);
        LAction action = TInterface.TActionCreate();
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
