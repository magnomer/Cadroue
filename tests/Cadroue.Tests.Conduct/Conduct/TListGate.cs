using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TListGate
{
    private readonly TListPort tListPort = new();
    private readonly CList tListGate;
    private readonly List<string?> tListPaths = [];
    private readonly List<bool> tListLocks = [];
    private int tListEntries;

    public TListGate()
    {
        tListGate = TAtelier.TAtelierCreate(port => port with { LAtelierList = tListPort.TListCreate })
            .CAtelierListCreate();
        tListGate.CListPathChanged += tListPaths.Add;
        tListGate.CListLockChanged += tListLocks.Add;
        tListGate.CListItemsAdded += _ => tListEntries++;
    }

    [Fact]
    public void ListCreate_NullPort_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            TAtelier.TAtelierCreate(port => port with { LAtelierList = null! }));

    [Fact]
    public void ListCreate_HandsTheUsherNameReaderToThePort()
    {
        Assert.NotNull(tListPort.TListName);
        Assert.True(tListGate.CListEmpty);
    }

    [Fact]
    public void ListAdd_SelectsFirstAddedPath_ThenPassesEntriesOn()
    {
        tListPort.TListPathsAdd(false, "C:/a.mp4", "C:/b.mp4");

        Assert.Equal(["C:/a.mp4"], tListGate.CListSelectionRead());
        Assert.Equal("C:/a.mp4", tListGate.CListPathCurrent);
        Assert.Equal("C:/a.mp4", tListGate.CListPathAnchor);
        Assert.Equal(1, tListEntries);
        Assert.False(tListGate.CListEmpty);
    }

    [Fact]
    public async Task ListPathsAdd_HandsRequestedPathsToOneEngineCall()
    {
        int added = await tListGate.CListPathsAdd(new HashSet<string> { "C:/a.mp4" });

        Assert.Equal(1, added);
        Assert.Equal(["add C:/a.mp4"], tListPort.TListCalls);
    }

    [Fact]
    public void ListPressSelect_ShiftSelectsRangeFromAnchor()
    {
        tListPort.TListPathsAdd(false, "C:/a.mp4", "C:/b.mp4", "C:/c.mp4");

        tListGate.CListPressSelect("C:/c.mp4", true, false);

        Assert.Equal(["C:/a.mp4", "C:/b.mp4", "C:/c.mp4"], tListGate.CListSelectionRead());
        Assert.Equal("C:/c.mp4", tListGate.CListPathCurrent);
    }

    [Fact]
    public void ListPressSelect_ControlTogglesAndPlainPressOnSeveralWaitsForRelease()
    {
        tListPort.TListPathsAdd(false, "C:/a.mp4", "C:/b.mp4");
        tListGate.CListPressSelect("C:/b.mp4", false, true);

        tListGate.CListPressSelect("C:/a.mp4", false, false);

        Assert.Equal("C:/a.mp4", tListGate.CListPressPath);
        Assert.Equal(2, tListGate.CListSelectionRead().Count);
        tListGate.CListReleaseSelect();
        Assert.Equal(["C:/a.mp4"], tListGate.CListSelectionRead());
        Assert.Null(tListGate.CListPressPath);
    }

    [Fact]
    public void ListRemove_MovesCurrentToNextLeft()
    {
        tListPort.TListPathsAdd(false, "C:/a.mp4", "C:/b.mp4", "C:/c.mp4");
        tListGate.CListSelect("C:/b.mp4");

        tListGate.CListRemove();

        Assert.Equal(["remove C:/b.mp4"], tListPort.TListCalls);
        Assert.Equal("C:/c.mp4", tListGate.CListPathCurrent);
        Assert.Equal(["C:/c.mp4"], tListGate.CListSelectionRead());
    }

    [Fact]
    public void ListRemove_SkipsLocked_FallsBackToThePathBefore()
    {
        tListPort.TListPathsAdd(true, "C:/locked.mp4");
        tListPort.TListPathsAdd(false, "C:/a.mp4", "C:/b.mp4");
        tListGate.CListAllSelect();

        tListGate.CListRemove();

        Assert.Equal(["remove C:/a.mp4,C:/b.mp4"], tListPort.TListCalls);
        Assert.Equal("C:/locked.mp4", tListGate.CListPathCurrent);
        Assert.Equal(["C:/locked.mp4"], tListGate.CListSelectionRead());
    }

    [Fact]
    public void ListClear_RemovesUnlockedOnly_NothingToRemoveMakesNoCall()
    {
        tListPort.TListPathsAdd(true, "C:/locked.mp4");
        tListGate.CListClear();
        Assert.Empty(tListPort.TListCalls);

        tListPort.TListPathsAdd(false, "C:/a.mp4");
        tListGate.CListClear();

        Assert.Equal(["remove C:/a.mp4"], tListPort.TListCalls);
    }

    [Fact]
    public void ListLockCheck_FollowsCurrentPath()
    {
        tListPort.TListPathsAdd(true, "C:/locked.mp4");

        Assert.True(tListGate.CListLockCheck());
        Assert.True(tListLocks[^1]);
        tListGate.CListSelect(null);
        Assert.False(tListGate.CListLockCheck());
        Assert.Null(tListPaths[^1]);
    }

    [Fact]
    public void ListDrag_LockedPressStartsNone_UnlockedCarriesSelection()
    {
        tListPort.TListPathsAdd(true, "C:/locked.mp4");
        Assert.False(tListGate.CListPressHandle("C:/locked.mp4", false, false, 0, 0));

        tListPort.TListPathsAdd(false, "C:/a.mp4", "C:/b.mp4");
        tListGate.CListAllSelect();
        Assert.True(tListGate.CListPressHandle("C:/a.mp4", false, false, 10, 10));
        Assert.False(tListGate.CListDragResolve(12, 12, 4, 4, true));
        Assert.True(tListGate.CListDragResolve(10, 14, 4, 4, true));

        Assert.Equal(["C:/a.mp4", "C:/b.mp4"], tListGate.CListDragPaths);
        Assert.Null(tListGate.CListDragPath);
        Assert.Null(tListGate.CListPressPath);
    }

    [Fact]
    public void ListListedSelect_UnlistedPathIsIgnored()
    {
        tListGate.CListListedSelect("C:/a.mp4");
        Assert.Null(tListGate.CListPathCurrent);

        tListPort.TListPathsAdd(false, "C:/a.mp4", "C:/b.mp4");
        tListGate.CListListedSelect("C:/B.MP4");
        Assert.Equal("C:/B.MP4", tListGate.CListPathCurrent);
    }

    [Fact]
    public void ListClose_CancelsThroughThePort()
    {
        tListGate.CListClose();

        Assert.Equal(["close"], tListPort.TListCalls);
    }
}
