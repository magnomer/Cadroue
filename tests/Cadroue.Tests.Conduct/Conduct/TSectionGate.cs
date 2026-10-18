using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TSectionGate
{
    private readonly TFlowPort tSectionFlowPort = new();
    private readonly CFlow tSectionFlow;
    private readonly CSection tSectionGate;
    private int tSectionChanges;
    private int tSectionRowTells;
    private int tSectionSelectTells;

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
    public void FlowAttach_SilentStore_GateTellsOnce()
    {
        tSectionFlow.CFlowCommandSet(true);
        tSectionFlow.CFlowAttach(null!, "full:a", TimeSpan.Zero);

        Assert.Equal(1, tSectionChanges);
    }

    [Fact]
    public void SectionReset_SilentStore_GateTellsOnce()
    {
        tSectionGate.CSectionReset();

        Assert.Equal(1, tSectionChanges);
    }

    [Fact]
    public void EditableSet_AnswersWhetherTheSwitchTurned()
    {
        Assert.True(tSectionGate.CSectionEditable);
        Assert.False(tSectionGate.CSectionEditableSet(true));
        Assert.True(tSectionGate.CSectionEditableSet(false));
        Assert.False(tSectionGate.CSectionEditable);
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

    private void TSectionRowsSync()
    {
        TSectionPortRead().TSectionCount = 2;
        TSectionPortRead().TSectionSelected = [1];
        TSectionPortRead().TSectionRows =
        [
            ("", "p", "", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), 2, false),
            ("b", "", "", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(4), 0, true)
        ];
        tSectionGate.CSectionList.CSectionRowsSync();
        tSectionGate.CSectionList.CSectionRowsChanged += () => tSectionRowTells++;
        tSectionGate.CSectionList.CSectionSelectApplied += () => tSectionSelectTells++;
    }

    [Fact]
    public void RowsSync_HeldRows_AnswerFlagsAffixesSpanAndSelection()
    {
        Assert.Null(tSectionGate.CSectionList.CSectionHeldCount);
        TSectionRowsSync();

        IReadOnlyList<CSectionRow> rows = tSectionGate.CSectionList.CSectionRowsRead();

        Assert.Equal(2, tSectionGate.CSectionList.CSectionHeldCount);
        Assert.True(rows[0].CSectionRowUnnamed);
        Assert.True(rows[0].CSectionRowPrefixed);
        Assert.False(rows[0].CSectionRowSuffixed);
        Assert.Equal(["p"], rows[0].CSectionRowAffixes);
        Assert.Equal(TimeSpan.FromSeconds(2), rows[0].CSectionRowSpan);
        Assert.False(rows[0].CSectionRowSelected);
        Assert.Equal("b", rows[1].CSectionRowName);
        Assert.Equal(TimeSpan.Zero, rows[1].CSectionRowSpan);
        Assert.True(rows[1].CSectionRowHidden);
        Assert.True(rows[1].CSectionRowSelected);
        Assert.True(tSectionGate.CSectionList.CSectionSelectedCheck(1));
        Assert.Equal(0, tSectionRowTells);
    }

    [Fact]
    public void RowsUpdate_SameRowsTellSelection_ChangedRowsTellRebuild()
    {
        TSectionRowsSync();

        tSectionGate.CSectionList.CSectionRowsUpdate();
        TSectionPortRead().TSectionRows = [("c", "", "", TimeSpan.Zero, TimeSpan.FromSeconds(1), 1, false)];
        tSectionGate.CSectionList.CSectionRowsUpdate();

        Assert.Equal((1, 1), (tSectionRowTells, tSectionSelectTells));
        Assert.Equal(1, tSectionGate.CSectionList.CSectionHeldCount);
    }

    [Fact]
    public void Rename_DoublePressWhileEditable_SelectsAndOpensTheEditor()
    {
        TSectionRowsSync();
        CSectionEdit edit = tSectionGate.CSectionList.CSectionEdit;

        Assert.False(tSectionGate.CSectionList.CSectionRenameHandle(0, 1));
        tSectionGate.CSectionEditableSet(false);
        Assert.False(tSectionGate.CSectionList.CSectionRenameHandle(0, 2));
        tSectionGate.CSectionEditableSet(true);
        Assert.False(tSectionGate.CSectionList.CSectionRenameHandle(-1, 2));
        Assert.Null(edit.CSectionEditIndex);
        Assert.True(tSectionGate.CSectionList.CSectionRenameHandle(0, 2));

        Assert.Equal(0, edit.CSectionEditIndex);
        Assert.True(tSectionGate.CSectionList.CSectionRowsRead()[0].CSectionRowEditing);
        Assert.False(tSectionGate.CSectionList.CSectionRowsRead()[1].CSectionRowEditing);
        Assert.Equal(["select:0"], TSectionPortRead().TSectionCalls);
        Assert.Equal(1, tSectionRowTells);
    }

    [Fact]
    public void EditableOff_DropsTheEditWithoutNaming()
    {
        TSectionRowsSync();
        tSectionGate.CSectionList.CSectionRenameHandle(1, 2);

        tSectionGate.CSectionList.CSectionEditableHandle(true);
        Assert.Equal(1, tSectionGate.CSectionList.CSectionEdit.CSectionEditIndex);
        tSectionGate.CSectionList.CSectionEditableHandle(false);
        tSectionGate.CSectionList.CSectionEdit.CSectionEditCommit();

        Assert.Null(tSectionGate.CSectionList.CSectionEdit.CSectionEditIndex);
        Assert.Equal(["select:1"], TSectionPortRead().TSectionCalls);
    }

    [Fact]
    public void ToggleSeekAndSort_CommitThePendingEditFirst()
    {
        TSectionLoad();
        TSectionRowsSync();
        TSectionPortRead().TSectionBound = TimeSpan.FromSeconds(3);
        tSectionGate.CSectionList.CSectionRenameHandle(0, 2);
        tSectionGate.CSectionList.CSectionToggleHandle(1);

        Assert.False(tSectionGate.CSectionList.CSectionSeekHandle(0, 1, false));
        Assert.False(tSectionGate.CSectionList.CSectionSeekHandle(-1, 2, false));
        Assert.True(tSectionGate.CSectionList.CSectionSeekHandle(1, 2, true));
        tSectionGate.CSectionList.CSectionSortRun();

        Assert.Equal(["select:0", "name:0::p:", "toggle:1", "seek:1:True", "sort"], TSectionPortRead().TSectionCalls);
        Assert.Equal(TimeSpan.FromSeconds(3), tSectionFlow.CFlowCursor);
    }

    [Fact]
    public void Drag_PastTheThreshold_MovesLive_ReleaseTellsRebuild()
    {
        TSectionRowsSync();
        CSectionDrag drag = tSectionGate.CSectionList.CSectionDrag;
        List<string> tells = [];
        drag.CSectionDragStarted += index => tells.Add($"start:{index}");
        drag.CSectionDragMoved += (source, insert) => tells.Add($"moved:{source}:{insert}");
        drag.CSectionDragEnd += index => tells.Add($"end:{index}");

        Assert.False(drag.CSectionDragPrepare(0, 2, 5, 5));
        Assert.True(drag.CSectionDragPrepare(0, 1, 5, 5));
        Assert.False(drag.CSectionDragMove(6, 6, 4, 4, true, [0, 30], [30, 30]));
        Assert.False(drag.CSectionDragMove(6, 50, 4, 4, false, [0, 30], [30, 30]));
        tSectionGate.CSectionEditableSet(false);
        Assert.False(drag.CSectionDragMove(6, 50, 4, 4, true, [0, 30], [30, 30]));
        tSectionGate.CSectionEditableSet(true);
        Assert.True(drag.CSectionDragMove(6, 50, 4, 4, true, [0, 30], [30, 30]));
        tSectionGate.CSectionList.CSectionRowsUpdate();
        Assert.Equal(1, drag.CSectionDragRead());
        Assert.True(drag.CSectionDragCommit(false, false));

        Assert.False(drag.CSectionDragCommit(false, false));
        Assert.Equal(["start:0", "moved:0:1", "end:1"], tells);
        Assert.Equal(["move:0:2"], TSectionPortRead().TSectionCalls);
        Assert.Equal((1, 0), (tSectionRowTells, tSectionSelectTells));
        Assert.Equal(-1, drag.CSectionDragRead());
    }

    [Fact]
    public void Release_Unmoved_SelectsByModifiers_LostCaptureEndsTheDrag()
    {
        TSectionRowsSync();
        CSectionDrag drag = tSectionGate.CSectionList.CSectionDrag;
        List<int> ends = [];
        drag.CSectionDragEnd += ends.Add;

        foreach ((bool shift, bool control) in new[] { (true, false), (false, true), (false, false) })
        {
            drag.CSectionDragPrepare(1, 1, 0, 0);
            Assert.True(drag.CSectionDragCommit(shift, control));
        }

        drag.CSectionDragPrepare(0, 1, 0, 0);
        drag.CSectionDragReset();

        Assert.Equal(["rangeSelect:1", "selectToggle:1", "select:1"], TSectionPortRead().TSectionCalls);
        Assert.Equal([1, 1, 1, 0], ends);
        Assert.Null(drag.CSectionDragIndex);
    }
}
