using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlowSectionEdit
{
    private static LFlow TFlowBuild(double seconds = 10)
    {
        LFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSectionSet(flow, true);
        TInterface.TFlowSourceSet(
            flow, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), "C:\\media\\clip.mp4");
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(seconds));
        return flow;
    }

    [Fact]
    public void Add_CreatesASectionAtTheCursor_AndRaisesTheChange()
    {
        LFlow flow = TFlowBuild();
        List<int?> selections = [];
        TInterface.TFlowSectionAttach(flow, (sections, select) => selections.Add(select));

        TInterface.TFlowSectionAdd(flow);

        IReadOnlyList<LPiece> sections = TInterface.TFlowSectionsRead(flow);
        Assert.Single(sections);
        Assert.Equal(TimeSpan.FromSeconds(10), sections[0].LPieceOrigin);
        Assert.Equal([0], selections);
        Assert.Equal(0, flow.LFlowSectionIndex);
        Assert.False(TInterface.TFlowEmptyCheck(flow));
    }

    [Fact]
    public void Add_RefusesWithoutEditableOrSource()
    {
        LFlow flow = TFlowBuild();
        TInterface.TFlowEditSet(flow, false);
        TInterface.TFlowSectionAdd(flow);
        Assert.Empty(TInterface.TFlowSectionsRead(flow));

        LFlow bare = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(bare, true);
        TInterface.TFlowSectionAdd(bare);
        Assert.Empty(TInterface.TFlowSectionsRead(bare));
        Assert.True(TInterface.TFlowEmptyCheck(bare));
    }

    [Fact]
    public void Divide_SplitsTheSelectedSection_AndKeepsTheLeftHalfSelected()
    {
        LFlow flow = TFlowBuild();
        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));

        TInterface.TFlowSectionDivide(flow);

        IReadOnlyList<LPiece> sections = TInterface.TFlowSectionsRead(flow);
        Assert.Equal(2, sections.Count);
        Assert.Equal(TimeSpan.FromSeconds(30), sections[0].LPieceEnd);
        Assert.Equal(TimeSpan.FromSeconds(30), sections[1].LPieceOrigin);
        Assert.Equal(0, TInterface.TFlowSelectionRead(flow));
    }

    [Fact]
    public void Delete_WithoutConfirmation_RemovesTheSelectedSections()
    {
        LFlow flow = TFlowBuild();
        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));
        TInterface.TFlowSectionDivide(flow);
        TInterface.TFlowSectionSelect(flow, 1);

        TInterface.TFlowSectionDelete(flow);

        IReadOnlyList<LPiece> sections = TInterface.TFlowSectionsRead(flow);
        Assert.Single(sections);
        Assert.Equal(TimeSpan.FromSeconds(30), sections[0].LPieceEnd);
    }

    [Fact]
    public void Clear_EmptiesTheList_AndSkipsWhenAlreadyEmpty()
    {
        LFlow flow = TFlowBuild();
        List<int> counts = [];
        TInterface.TFlowSectionAttach(flow, (sections, select) => counts.Add(sections.Count));
        TInterface.TFlowSectionClear(flow);
        Assert.Empty(counts);

        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowSectionClear(flow);
        Assert.Equal([1, 0], counts);
        Assert.True(TInterface.TFlowEmptyCheck(flow));
    }

    [Fact]
    public void Toggle_Move_Sort_Name_ChangeTheRecordsOnlyWhileEditable()
    {
        LFlow flow = TFlowBuild();
        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));
        TInterface.TFlowSectionDivide(flow);
        TInterface.TFlowNameSet(flow, 0, "zeta", null, null);
        TInterface.TFlowNameSet(flow, 1, "alpha", "pre", "suf");

        IReadOnlyList<LPiece> named = TInterface.TFlowSectionsRead(flow);
        Assert.Equal("zeta", named[0].LPieceName);
        Assert.Equal("pre", named[1].LPiecePrefix);
        Assert.Equal("suf", named[1].LPieceSuffix);

        Assert.True(TInterface.TFlowSectionSort(flow));
        Assert.Equal("alpha", TInterface.TFlowSectionsRead(flow)[0].LPieceName);
        Assert.False(TInterface.TFlowSectionSort(flow));

        Assert.True(TInterface.TFlowSectionMove(flow, 0, 2));
        Assert.Equal("zeta", TInterface.TFlowSectionsRead(flow)[0].LPieceName);
        Assert.False(TInterface.TFlowSectionMove(flow, 0, 0));

        TInterface.TFlowSectionToggle(flow, 0);
        Assert.True(TInterface.TFlowSectionsRead(flow)[0].LPieceHidden);

        TInterface.TFlowEditSet(flow, false);
        TInterface.TFlowSectionToggle(flow, 0);
        Assert.True(TInterface.TFlowSectionsRead(flow)[0].LPieceHidden);
        Assert.False(TInterface.TFlowSectionMove(flow, 0, 2));
        TInterface.TFlowNameSet(flow, 0, "other", null, null);
        Assert.Equal("zeta", TInterface.TFlowSectionsRead(flow)[0].LPieceName);
    }

    [Fact]
    public void Seek_SelectsTheSection_AndMovesTheCursorToItsBound()
    {
        LFlow flow = TFlowBuild();
        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));
        TInterface.TFlowSectionDivide(flow);
        List<TimeSpan> seeks = [];
        TInterface.TFlowCursorAttach(flow, () => { }, seeks.Add);

        TInterface.TFlowSectionSeek(flow, 1, true);
        Assert.Equal(1, TInterface.TFlowSelectionRead(flow));
        Assert.Equal(TimeSpan.FromSeconds(60), flow.LFlowCursor);

        TInterface.TFlowSectionSeek(flow, 0, false);
        Assert.Equal(TimeSpan.FromSeconds(10), flow.LFlowCursor);
        Assert.Equal([TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10)], seeks);

        TInterface.TFlowSectionSeek(flow, 5, false);
        Assert.Equal(TimeSpan.FromSeconds(10), flow.LFlowCursor);
    }

    [Fact]
    public void StartEnd_MoveTheSelectedBounds()
    {
        LFlow flow = TFlowBuild();
        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(20));
        TInterface.TFlowStartSet(flow);
        Assert.Equal(TimeSpan.FromSeconds(20), TInterface.TFlowSectionsRead(flow)[0].LPieceOrigin);

        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(50));
        TInterface.TFlowEndSet(flow);
        Assert.Equal(TimeSpan.FromSeconds(50), TInterface.TFlowSectionsRead(flow)[0].LPieceEnd);
        Assert.Single(TInterface.TFlowSplitRead(flow));
    }

    [Fact]
    public void Add_RecordsTheSectionLine()
    {
        LFlow flow = TFlowBuild();
        using var observed = new ManualResetEventSlim(false);
        string? line = null;

        void TFlowTraceRead(LTraceEntry entry)
        {
            if (entry.LTraceEntrySummary.Contains("clip.mp4", StringComparison.Ordinal)
                && entry.LTraceEntrySummary.StartsWith("Section added", StringComparison.Ordinal))
            {
                line = entry.LTraceEntrySummary;
                observed.Set();
            }
        }

        TInterface.TTraceAttach(TFlowTraceRead);
        try
        {
            TInterface.TFlowSectionAdd(flow);
            Assert.True(observed.Wait(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            TInterface.TTraceDetach(TFlowTraceRead);
        }

        Assert.Equal("Section added #1 of 1 in 'clip.mp4': unnamed 00:00:10.000-00:01:00.000", line);
    }
}
