using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TListSelection
{
    private static (LDocket, QList) TListBuild(params string[] paths)
    {
        QList list = TInterfaceList.TListCreate();
        TInterfaceList.TDocketPathsAdd(list.QListDocket, paths);
        return (list.QListDocket, list);
    }

    [Fact]
    public void Click_SelectsOne_AnchorsThere()
    {
        (_, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        List<string?> notices = [];
        TInterfaceList.TListPathAttach(list, notices.Add);

        TInterfaceList.TListPressSelect(list, @"C:\b.mp4", false, false);

        Assert.Equal(@"C:\b.mp4", list.QListPathCurrent);
        Assert.Equal(@"C:\b.mp4", list.QListGate.CListPathAnchor);
        Assert.Equal([@"C:\b.mp4"], TInterfaceList.TListSelectionRead(list));
        Assert.Equal([@"C:\b.mp4"], notices);
    }

    [Fact]
    public void ShiftClick_SelectsRangeFromAnchor_InDocketOrder()
    {
        (_, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4", @"C:\d.mp4");
        TInterfaceList.TListPressSelect(list, @"C:\c.mp4", false, false);

        TInterfaceList.TListPressSelect(list, @"C:\a.mp4", true, false);

        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4"], TInterfaceList.TListSelectionRead(list));
        Assert.Equal(@"C:\a.mp4", list.QListPathCurrent);
        Assert.Equal(@"C:\c.mp4", list.QListGate.CListPathAnchor);
    }

    [Fact]
    public void ControlClick_TogglesMembership_CurrentFallsBackToLastSelected()
    {
        (_, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        TInterfaceList.TListPressSelect(list, @"C:\a.mp4", false, false);
        TInterfaceList.TListPressSelect(list, @"C:\c.mp4", false, true);

        Assert.Equal([@"C:\a.mp4", @"C:\c.mp4"], TInterfaceList.TListSelectionRead(list));
        Assert.Equal(@"C:\c.mp4", list.QListPathCurrent);

        TInterfaceList.TListPressSelect(list, @"C:\c.mp4", false, true);

        Assert.Equal([@"C:\a.mp4"], TInterfaceList.TListSelectionRead(list));
        Assert.Equal(@"C:\a.mp4", list.QListPathCurrent);
    }

    [Fact]
    public void PressOnMultiSelection_KeepsSetUntilRelease_ThenCollapses()
    {
        (_, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        TInterfaceList.TListPressSelect(list, @"C:\a.mp4", false, false);
        TInterfaceList.TListPressSelect(list, @"C:\c.mp4", true, false);

        TInterfaceList.TListPressSelect(list, @"C:\b.mp4", false, false);

        Assert.Equal(@"C:\b.mp4", list.QListGate.CListPressPath);
        Assert.Equal(3, TInterfaceList.TListSelectionRead(list).Count);

        TInterfaceList.TListReleaseSelect(list);

        Assert.Null(list.QListGate.CListPressPath);
        Assert.Equal([@"C:\b.mp4"], TInterfaceList.TListSelectionRead(list));
    }

    [Fact]
    public void SelectAll_WithoutSelection_CurrentIsLast_KeepsCurrentOtherwise()
    {
        (_, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        TInterfaceList.TListSelect(list, null);

        TInterfaceList.TListAllSelect(list);

        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4"], TInterfaceList.TListSelectionRead(list));
        Assert.Equal(@"C:\b.mp4", list.QListPathCurrent);

        TInterfaceList.TListSelect(list, @"C:\a.mp4");
        TInterfaceList.TListAllSelect(list);

        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4"], TInterfaceList.TListSelectionRead(list));
        Assert.Equal(@"C:\a.mp4", list.QListPathCurrent);
    }

    [Fact]
    public void RemoveCurrent_SelectsNextSurvivor_ThenPreviousAtEnd()
    {
        (LDocket docket, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        TInterfaceList.TListSelect(list, @"C:\b.mp4");
        List<IReadOnlyList<string>> cleared = [];
        TInterfaceList.TListClearAttach(list, cleared.Add);

        TInterfaceList.TListRemove(list);

        Assert.Equal(@"C:\c.mp4", list.QListPathCurrent);
        Assert.Equal([@"C:\a.mp4", @"C:\c.mp4"], TInterface.TDocketPathsRead(docket));
        Assert.Equal([[@"C:\b.mp4"]], cleared);

        TInterfaceList.TListRemove(list);

        Assert.Equal(@"C:\a.mp4", list.QListPathCurrent);
    }

    [Fact]
    public void RemoveOther_KeepsCurrent_DropsAnchorWhenGone()
    {
        (LDocket docket, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        TInterfaceList.TListSelect(list, @"C:\c.mp4");
        TInterfaceList.TListPressSelect(list, @"C:\a.mp4", false, true);
        List<string?> notices = [];
        TInterfaceList.TListPathAttach(list, notices.Add);

        TInterfaceList.TDocketPathsRemove(docket, @"C:\c.mp4");

        Assert.Equal(@"C:\a.mp4", list.QListPathCurrent);
        Assert.Equal(@"C:\a.mp4", list.QListGate.CListPathAnchor);
        Assert.Equal([@"C:\a.mp4"], TInterfaceList.TListSelectionRead(list));
        Assert.Empty(notices);
    }

    [Fact]
    public void RemoveCurrentFromOutside_ClearsSelection()
    {
        (LDocket docket, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        TInterfaceList.TListSelect(list, @"C:\b.mp4");
        List<string?> notices = [];
        TInterfaceList.TListPathAttach(list, notices.Add);

        TInterfaceList.TDocketPathsRemove(docket, @"C:\b.mp4");

        Assert.Null(list.QListPathCurrent);
        Assert.Empty(TInterfaceList.TListSelectionRead(list));
        Assert.Equal([null], notices);
    }

    [Fact]
    public void Remove_SkipsLockedSelection_ClearKeepsLocked()
    {
        (LDocket docket, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        TInterfaceList.TDocketDeliveredAdd(docket, @"C:\locked.mp4", true);
        List<bool> locks = [];
        TInterfaceList.TListLockAttach(list, locks.Add);

        TInterfaceList.TListSelect(list, @"C:\locked.mp4");
        Assert.True(TInterfaceList.TListLockCheck(list));
        Assert.Contains(true, locks);

        TInterfaceList.TListRemove(list);
        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4", @"C:\locked.mp4"], TInterface.TDocketPathsRead(docket));

        TInterfaceList.TListClear(list);
        Assert.Equal([@"C:\locked.mp4"], TInterface.TDocketPathsRead(docket));
        Assert.False(list.QListEmpty);
    }

    [Fact]
    public void Cards_GroupLockedByBatch_StateKeysFollowSelection()
    {
        (LDocket docket, QList list) = TListBuild(@"C:\a.mp4");
        TInterfaceList.TDocketDeliveredAdd(docket, @"C:\x.mp4", true);
        TInterfaceList.TListSelect(list, @"C:\x.mp4");

        IReadOnlyList<QListCard> cards = TInterfaceList.TListCardsRead(list);

        Assert.Equal(2, cards.Count);
        Assert.False(cards[0].QListCardLocked);
        Assert.Equal("a.mp4", cards[0].QListCardRows[0].QListRowName);
        Assert.Equal("Plain", cards[0].QListCardRows[0].QListRowState);
        Assert.True(cards[0].QListCardRows[0].QListRowLast);
        Assert.True(cards[1].QListCardLocked);
        Assert.Equal("LockedSelected", cards[1].QListCardRows[0].QListRowState);
        Assert.False(cards[1].QListCardRows[0].QListRowLast);

        TInterfaceList.TListSelect(list, @"C:\a.mp4");

        Assert.Equal("Selected", TInterfaceList.TListStateRead(list, @"C:\a.mp4"));
        Assert.Equal("Locked", TInterfaceList.TListStateRead(list, @"C:\x.mp4"));
    }

    [Fact]
    public void Press_LockedRowSelectsWithoutCapture_DragNeedsThreshold()
    {
        (LDocket docket, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        TInterfaceList.TDocketDeliveredAdd(docket, @"C:\x.mp4", true);

        Assert.False(TInterfaceList.TListPressHandle(list, @"C:\x.mp4", false, false, 0, 0, 0, 0));
        Assert.Equal(@"C:\x.mp4", list.QListPathCurrent);
        Assert.False(TInterfaceList.TListDragResolve(list, 20, 20, 4, true));

        Assert.True(TInterfaceList.TListPressHandle(list, @"C:\a.mp4", false, false, 10, 10, 3, 4));
        TInterfaceList.TListPressSelect(list, @"C:\b.mp4", true, false);
        Assert.False(TInterfaceList.TListDragResolve(list, 12, 12, 4, true));
        Assert.False(TInterfaceList.TListDragResolve(list, 30, 30, 4, false));
        Assert.True(TInterfaceList.TListDragResolve(list, 30, 30, 4, true));
        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4"], list.QListDrag.QListDragPaths);
        TInterface.TGhostPointSet(list.QListDrag.QListGhost, 13, 14);
        Assert.Equal(10, list.QListDrag.QListGhost.QGhostLeft);
        Assert.Null(list.QListGate.CListDragPath);
        Assert.False(TInterfaceList.TListDragResolve(list, 60, 60, 4, true));

        Assert.True(TInterfaceList.TListPressHandle(list, @"C:\a.mp4", false, false, 0, 0, 0, 0));
        TInterfaceList.TListReleaseHandle(list);
        Assert.Null(list.QListGate.CListDragPath);
        Assert.Equal([@"C:\a.mp4"], TInterfaceList.TListSelectionRead(list));
    }

    [Fact]
    public void KeyRun_ControlA_SelectsAll_OtherKeysIgnored()
    {
        (_, QList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");

        Assert.False(TInterfaceList.TListKeyRun(list, "A", false));
        Assert.False(TInterfaceList.TListKeyRun(list, "B", true));
        Assert.True(TInterfaceList.TListKeyRun(list, "A", true));
        Assert.Equal(2, TInterfaceList.TListSelectionRead(list).Count);
    }

    [Fact]
    public async Task PathsAdd_KeepsExistingMediaOnly_SelectsFirstAdded()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "list-add", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string clip = Path.Combine(folder, "clip.mp4");
        File.WriteAllBytes(clip, []);
        try
        {
            (LDocket docket, QList list) = TListBuild();
            List<IReadOnlyList<LDocketEntry>> added = [];
            TInterfaceList.TListItemsAttach(list, added.Add);

            int count = await TInterfaceList.TListPathsAdd(list, clip, Path.Combine(folder, "missing.mp4"));

            Assert.Equal(1, count);
            Assert.Equal([clip], TInterface.TDocketPathsRead(docket));
            Assert.Equal(clip, list.QListPathCurrent);
            Assert.Single(added);

            TInterfaceList.TListDialogAdd(list, false, [clip]);
            TInterfaceList.TListDialogAdd(list, null, [clip]);

            Assert.Single(added);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
