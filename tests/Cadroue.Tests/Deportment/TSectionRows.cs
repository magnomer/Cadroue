using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSectionRows
{
    private static (LFlow, LSection) TSectionBuild()
    {
        LFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSectionSet(flow, true);
        TInterface.TFlowSourceSet(
            flow, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), "C:\\media\\clip.mp4");
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(10));
        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));
        TInterface.TFlowSectionDivide(flow);
        TInterface.TFlowNameSet(flow, 1, "beta", "pre", "");
        return (flow, TInterface.TSectionCreate(flow));
    }

    [Fact]
    public void Rows_ComeFromPieces_WithNumbersTimesAffixesAndSelection()
    {
        (LFlow flow, LSection section) = TSectionBuild();
        TInterface.TFlowSectionSelect(flow, 1);

        IReadOnlyList<LSectionRow> rows = TInterface.TSectionRowsRead(section);

        Assert.Equal(2, rows.Count);
        Assert.Equal("1", rows[0].LSectionRowNumber);
        Assert.True(rows[0].LSectionRowUnnamed);
        Assert.Equal("0:10", rows[0].LSectionRowOrigin);
        Assert.Equal("0:30", rows[0].LSectionRowEnd);
        Assert.Equal("  (0:20)", rows[0].LSectionRowSpan);
        Assert.False(rows[0].LSectionRowSelected);
        Assert.Equal("beta", rows[1].LSectionRowText);
        Assert.Equal(["  /  pre"], rows[1].LSectionRowAffixes);
        Assert.True(rows[1].LSectionRowPrefixed);
        Assert.False(rows[1].LSectionRowSuffixed);
        Assert.True(rows[1].LSectionRowSelected);
        Assert.False(rows[1].LSectionRowEditing);
        Assert.False(string.IsNullOrEmpty(TInterface.TSectionTitleRead(section)));
    }

    [Fact]
    public void SameList_AppliesSelectionOnly_ChangedListRebuilds()
    {
        (LFlow flow, LSection section) = TSectionBuild();
        int rebuilds = 0;
        int selects = 0;
        TInterface.TSectionAttach(section, () => rebuilds++, () => selects++);

        TInterface.TFlowSectionSelect(flow, 0);
        Assert.Equal(0, rebuilds);
        Assert.Equal(1, selects);

        TInterface.TFlowNameSet(flow, 0, "alpha", null, null);
        Assert.Equal(1, rebuilds);
        Assert.Equal(1, selects);
    }

    [Fact]
    public void Rename_StartsEditing_CommitAppliesTrimmedAffixes_CancelRestores()
    {
        (LFlow flow, LSection section) = TSectionBuild();
        int rebuilds = 0;
        TInterface.TSectionAttach(section, () => rebuilds++, () => { });

        Assert.False(TInterface.TSectionRenameHandle(section, 0, 1));
        Assert.True(TInterface.TSectionRenameHandle(section, 0, 2));
        Assert.Equal(0, section.LSectionEditIndex);
        Assert.True(TInterface.TSectionRowsRead(section)[0].LSectionRowEditing);
        int serial = section.LSectionEditSerial;

        TInterface.TSectionTextSet(section, " gamma ", "p ", " s");
        Assert.True(TInterface.TSectionKeyRun(section, "Return"));

        LPiece piece = TInterface.TFlowSectionsRead(flow)[0];
        Assert.Equal("gamma", piece.LPieceName);
        Assert.Equal("p", piece.LPiecePrefix);
        Assert.Equal("s", piece.LPieceSuffix);
        Assert.Null(section.LSectionEditIndex);

        TInterface.TSectionBlurHandle(section, serial, false);
        Assert.Equal("gamma", TInterface.TFlowSectionsRead(flow)[0].LPieceName);

        TInterface.TSectionRenameHandle(section, 1, 2);
        TInterface.TSectionTextSet(section, "dropped", "", "");
        Assert.True(TInterface.TSectionKeyRun(section, "Escape"));
        Assert.False(TInterface.TSectionKeyRun(section, "A"));
        Assert.Equal("beta", TInterface.TFlowSectionsRead(flow)[1].LPieceName);
        Assert.True(rebuilds >= 4);
        Assert.True(TInterface.TSectionStepCheck(","));
        Assert.False(TInterface.TSectionStepCheck("a"));
    }

    [Fact]
    public void Blur_CommitsOnlyTheLiveEditor_WhenFocusLeftTheBoxes()
    {
        (LFlow flow, LSection section) = TSectionBuild();
        TInterface.TSectionRenameHandle(section, 0, 2);
        int serial = section.LSectionEditSerial;
        TInterface.TSectionTextSet(section, "kept", "", "");

        TInterface.TSectionBlurHandle(section, serial - 1, false);
        Assert.Equal(0, section.LSectionEditIndex);
        TInterface.TSectionBlurHandle(section, serial, true);
        Assert.Equal(0, section.LSectionEditIndex);
        TInterface.TSectionBlurHandle(section, serial, false);

        Assert.Null(section.LSectionEditIndex);
        Assert.Equal("kept", TInterface.TFlowSectionsRead(flow)[0].LPieceName);
    }

    [Fact]
    public void Drag_MovesAfterThreshold_ReordersThroughTheFlow_ReleaseRebuilds()
    {
        (LFlow flow, LSection section) = TSectionBuild();
        List<int> starts = [];
        List<int> ends = [];
        List<(int, int)> moves = [];
        int rebuilds = 0;
        TInterface.TSectionAttach(section, () => rebuilds++, () => { });
        TInterface.TSectionDragAttach(section, starts.Add, ends.Add, (source, insert) => moves.Add((source, insert)));
        double[] tops = [0, 30];
        double[] heights = [30, 30];

        Assert.False(TInterface.TSectionPressHandle(section, 0, 2, 5, 5));
        Assert.True(TInterface.TSectionPressHandle(section, 0, 1, 5, 5));
        Assert.False(TInterface.TSectionMoveHandle(section, 6, 6, 4, true, tops, heights));
        Assert.False(TInterface.TSectionMoveHandle(section, 6, 50, 4, false, tops, heights));
        Assert.True(TInterface.TSectionMoveHandle(section, 6, 50, 4, true, tops, heights));

        Assert.Equal([0], starts);
        Assert.Equal([(0, 1)], moves);
        Assert.Equal(1, section.LSectionDrag.LSectionDragIndex);
        Assert.Equal("beta", TInterface.TFlowSectionsRead(flow)[0].LPieceName);
        Assert.Equal(0, rebuilds);

        Assert.True(TInterface.TSectionReleaseHandle(section, false, false));
        Assert.Equal([1], ends);
        Assert.Equal(1, rebuilds);
        Assert.Null(section.LSectionDrag.LSectionDragIndex);
        Assert.False(TInterface.TSectionReleaseHandle(section, false, false));
    }

    [Fact]
    public void Release_WithoutMove_SelectsByModifiers_ToggleAndSeekAskTheFlow()
    {
        (LFlow flow, LSection section) = TSectionBuild();
        TInterface.TFlowSectionSelect(flow, 0);

        TInterface.TSectionPressHandle(section, 1, 1, 5, 5);
        Assert.True(TInterface.TSectionReleaseHandle(section, true, false));
        Assert.Equal([0, 1], TInterface.TFlowSelectedRead(flow));

        TInterface.TSectionPressHandle(section, 0, 1, 5, 5);
        Assert.True(TInterface.TSectionReleaseHandle(section, false, true));
        Assert.Equal([1], TInterface.TFlowSelectedRead(flow));

        TInterface.TSectionToggleHandle(section, 1);
        Assert.True(TInterface.TFlowSectionsRead(flow)[1].LPieceHidden);

        Assert.False(TInterface.TSectionSeekHandle(section, 0, 1, false));
        Assert.True(TInterface.TSectionSeekHandle(section, 0, 2, true));
        Assert.Equal(0, TInterface.TFlowSelectionRead(flow));
        Assert.Equal(TimeSpan.FromSeconds(30), flow.LFlowCursor);

        TInterface.TSectionPressHandle(section, 0, 1, 5, 5);
        TInterface.TSectionLostHandle(section);
        Assert.Null(section.LSectionDrag.LSectionDragIndex);
    }

    [Fact]
    public void EditableOff_DisablesCancelsAndClearsTheDrag()
    {
        (LFlow flow, LSection section) = TSectionBuild();
        List<bool> enabled = [];
        TInterface.TSectionEnabledAttach(section, enabled.Add);
        TInterface.TSectionRenameHandle(section, 0, 2);

        TInterface.TFlowEditSet(flow, false);

        Assert.Equal([false], enabled);
        Assert.False(section.LSectionEditable);
        Assert.Null(section.LSectionEditIndex);
        Assert.False(TInterface.TSectionRenameHandle(section, 0, 2));
        Assert.True(TInterface.TSectionPressHandle(section, 0, 1, 5, 5));
        Assert.False(TInterface.TSectionMoveHandle(section, 50, 50, 4, true, [0, 30], [30, 30]));
    }
}
