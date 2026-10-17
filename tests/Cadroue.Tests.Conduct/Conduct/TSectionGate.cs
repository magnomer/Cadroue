using System;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TSectionGate
{
    private readonly TFlowPort tSectionFlowPort = new();
    private readonly CFlow tSectionFlow;
    private readonly CSection tSectionGate;
    private int tSectionChanges;

    public TSectionGate()
    {
        tSectionFlow = TAtelier.TAtelierCreate(port => port with { LAtelierFlow = tSectionFlowPort })
            .CAtelierWorkspaceCreate()
            .CWorkspaceFlow;
        tSectionGate = tSectionFlow.CFlowSection;
        tSectionGate.CSectionChanged += () => tSectionChanges++;
    }

    private TSectionPort TSectionPortRead() => tSectionFlowPort.TFlowSection;

    private void TSectionLoad()
    {
        tSectionFlow.CFlowCommandSet(true);
        tSectionFlow.CFlowAttach(null!, "full:a", TimeSpan.FromSeconds(5));
        tSectionFlow.CFlowPaletteApply(4);
        TSectionPortRead().TSectionCalls.Clear();
    }

    [Fact]
    public void FlowCreate_Section_TracesTheFlowsSourceText()
    {
        Assert.Same(tSectionGate, tSectionFlow.CFlowSection);
        Assert.Equal("(no media)", TSectionPortRead().TSectionSource!());
    }

    [Fact]
    public void SectionChanged_PortTells_GateTellsOnce()
    {
        TSectionPortRead().TSectionRaise();

        Assert.Equal(1, tSectionChanges);
    }

    [Fact]
    public void SectionEmpty_CountAndSelection_AnswerTogether()
    {
        Assert.True(tSectionGate.CSectionEmpty);

        TSectionPortRead().TSectionSelection = 0;
        Assert.False(tSectionGate.CSectionEmpty);

        TSectionPortRead().TSectionSelection = null;
        TSectionPortRead().TSectionCount = 1;
        Assert.False(tSectionGate.CSectionEmpty);
    }

    [Fact]
    public void FlowAttach_Accepted_StartsTheSectionsOverOnTheSource()
    {
        Assert.False(tSectionFlow.CFlowAttach(null!, "full:a", TimeSpan.Zero));
        tSectionFlow.CFlowCommandSet(true);
        Assert.True(tSectionFlow.CFlowAttach(null!, "full:a", TimeSpan.Zero));
        tSectionGate.CSectionLoad();
        tSectionGate.CSectionReset();

        Assert.Equal(["attach:full:a", "load:60", "attach:null"], TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionAdd_NotEditableOrNoSource_DoesNotRun()
    {
        tSectionGate.CSectionAdd(true);
        tSectionGate.CSectionStartSet(true);
        tSectionGate.CSectionEndSet(true);
        TSectionLoad();
        tSectionGate.CSectionAdd(false);
        tSectionGate.CSectionStartSet(false);
        tSectionGate.CSectionEndSet(false);
        tSectionGate.CSectionDivide(false);

        Assert.Empty(TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionAdd_EditableWithSource_HandsCursorDurationAndPalette()
    {
        TSectionLoad();

        tSectionGate.CSectionAdd(true);
        tSectionGate.CSectionStartSet(true);
        tSectionGate.CSectionEndSet(true);
        tSectionGate.CSectionDivide(true);

        Assert.Equal(["add:5:60:4", "start:5:60:4", "end:5:4", "divide:5:4"], TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionDivide_EditableWithoutSource_Runs()
    {
        tSectionGate.CSectionDivide(true);

        Assert.Equal(["divide:0:1"], TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionSeek_CommandsStoppedOrOutsideTheSections_AnswersNull()
    {
        TSectionPortRead().TSectionCount = 2;
        Assert.Null(tSectionGate.CSectionSeek(0, false));

        TSectionLoad();
        Assert.Null(tSectionGate.CSectionSeek(-1, false));
        Assert.Null(tSectionGate.CSectionSeek(2, false));

        Assert.Empty(TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionSeek_HeldSection_AnswersTheEnginesBound()
    {
        TSectionLoad();
        TSectionPortRead().TSectionCount = 2;
        TSectionPortRead().TSectionBound = TimeSpan.FromSeconds(9);

        Assert.Equal(TimeSpan.FromSeconds(9), tSectionGate.CSectionSeek(1, true));
        Assert.Equal(["seek:1:True"], TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionMoveAndSort_NotEditable_RefuseWithoutRunning()
    {
        Assert.False(tSectionGate.CSectionMove(false, 0, 1));
        Assert.False(tSectionGate.CSectionSort(false));
        tSectionGate.CSectionToggle(false, 0);

        Assert.Empty(TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionMoveAndSort_Editable_AnswerTheEngine()
    {
        TSectionPortRead().TSectionAnswer = false;

        Assert.False(tSectionGate.CSectionMove(true, 0, 1));
        Assert.False(tSectionGate.CSectionSort(true));
        tSectionGate.CSectionToggle(true, 0);

        Assert.Equal(["move:0:1", "sort", "toggle:0"], TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionNameSet_OutsideTheSectionsOrNotEditable_DoesNotRun()
    {
        TSectionPortRead().TSectionCount = 1;

        tSectionGate.CSectionNameSet(true, 1, "a", null, null);
        tSectionGate.CSectionNameSet(true, -1, "a", null, null);
        tSectionGate.CSectionNameSet(false, 0, "a", null, null);
        tSectionGate.CSectionNameSet(true, 0, "a", "p", "s");

        Assert.Equal(["name:0:a:p:s"], TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionCombineApply_NoRangeOrUnloaded_Refuses()
    {
        Assert.False(tSectionGate.CSectionCombineApply([], [], []));

        TSectionLoad();
        tSectionFlow.CFlowUnloadSet();
        Assert.False(tSectionGate.CSectionCombineApply([], [], []));

        Assert.Empty(TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionCombineApply_HeldRange_HandsDurationAndPalette()
    {
        TSectionLoad();

        Assert.True(tSectionGate.CSectionCombineApply([], [], []));
        Assert.Equal(["combine:60:4"], TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void SectionConfirmedRuns_HandTheReadSelectionOn()
    {
        tSectionGate.CSectionSelect(1);
        tSectionGate.CSectionSelectToggle(2);
        tSectionGate.CSectionRangeSelect(3);
        tSectionGate.CSectionDelete([1, 2], 7);
        tSectionGate.CSectionClear(3, 7);

        Assert.Equal(
            ["select:1", "selectToggle:2", "rangeSelect:3", "delete:1,2:7", "clear:3:7"],
            TSectionPortRead().TSectionCalls);
    }
}
