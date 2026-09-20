using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TListSelection
{
    private static (LDocket, LList) TListBuild(params string[] paths)
    {
        LDocket docket = TInterface.TDocketCreate();
        TInterface.TDocketPathsAdd(docket, paths);
        return (docket, TInterface.TListCreate(docket));
    }

    [Fact]
    public void Click_SelectsOne_AnchorsThere()
    {
        (_, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        List<string?> notices = [];
        TInterface.TListPathAttach(list, notices.Add);

        TInterface.TListPressSelect(list, @"C:\b.mp4", false, false);

        Assert.Equal(@"C:\b.mp4", list.LListPathCurrent);
        Assert.Equal(@"C:\b.mp4", list.LListPathAnchor);
        Assert.Equal([@"C:\b.mp4"], TInterface.TListSelectionRead(list));
        Assert.Equal([@"C:\b.mp4"], notices);
    }

    [Fact]
    public void ShiftClick_SelectsRangeFromAnchor_InDocketOrder()
    {
        (_, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4", @"C:\d.mp4");
        TInterface.TListPressSelect(list, @"C:\c.mp4", false, false);

        TInterface.TListPressSelect(list, @"C:\a.mp4", true, false);

        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4"], TInterface.TListSelectionRead(list));
        Assert.Equal(@"C:\a.mp4", list.LListPathCurrent);
        Assert.Equal(@"C:\c.mp4", list.LListPathAnchor);
    }

    [Fact]
    public void ControlClick_TogglesMembership_CurrentFallsBackToLastSelected()
    {
        (_, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        TInterface.TListPressSelect(list, @"C:\a.mp4", false, false);
        TInterface.TListPressSelect(list, @"C:\c.mp4", false, true);

        Assert.Equal([@"C:\a.mp4", @"C:\c.mp4"], TInterface.TListSelectionRead(list));
        Assert.Equal(@"C:\c.mp4", list.LListPathCurrent);

        TInterface.TListPressSelect(list, @"C:\c.mp4", false, true);

        Assert.Equal([@"C:\a.mp4"], TInterface.TListSelectionRead(list));
        Assert.Equal(@"C:\a.mp4", list.LListPathCurrent);
    }

    [Fact]
    public void PressOnMultiSelection_KeepsSetUntilRelease_ThenCollapses()
    {
        (_, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        TInterface.TListPressSelect(list, @"C:\a.mp4", false, false);
        TInterface.TListPressSelect(list, @"C:\c.mp4", true, false);

        TInterface.TListPressSelect(list, @"C:\b.mp4", false, false);

        Assert.Equal(@"C:\b.mp4", list.LListPressPath);
        Assert.Equal(3, TInterface.TListSelectionRead(list).Count);

        TInterface.TListReleaseSelect(list);

        Assert.Null(list.LListPressPath);
        Assert.Equal([@"C:\b.mp4"], TInterface.TListSelectionRead(list));
    }

    [Fact]
    public void SelectAll_WithoutSelection_CurrentIsLast_KeepsCurrentOtherwise()
    {
        (_, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");

        TInterface.TListAllSelect(list);

        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4"], TInterface.TListSelectionRead(list));
        Assert.Equal(@"C:\b.mp4", list.LListPathCurrent);

        TInterface.TListSelect(list, @"C:\a.mp4");
        TInterface.TListAllSelect(list);

        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4"], TInterface.TListSelectionRead(list));
        Assert.Equal(@"C:\a.mp4", list.LListPathCurrent);
    }

    [Fact]
    public void RemoveCurrent_SelectsNextSurvivor_ThenPreviousAtEnd()
    {
        (LDocket docket, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        TInterface.TListSelect(list, @"C:\b.mp4");
        List<IReadOnlyList<string>> cleared = [];
        TInterface.TListClearAttach(list, cleared.Add);

        TInterface.TListRemove(list);

        Assert.Equal(@"C:\c.mp4", list.LListPathCurrent);
        Assert.Equal([@"C:\a.mp4", @"C:\c.mp4"], TInterface.TDocketPathsRead(docket));
        Assert.Equal([[@"C:\b.mp4"]], cleared);

        TInterface.TListRemove(list);

        Assert.Equal(@"C:\a.mp4", list.LListPathCurrent);
    }

    [Fact]
    public void RemoveOther_KeepsCurrent_DropsAnchorWhenGone()
    {
        (LDocket docket, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4");
        TInterface.TListSelect(list, @"C:\c.mp4");
        TInterface.TListPressSelect(list, @"C:\a.mp4", false, true);
        List<string?> notices = [];
        TInterface.TListPathAttach(list, notices.Add);

        TInterface.TDocketPathsRemove(docket, @"C:\c.mp4");

        Assert.Equal(@"C:\a.mp4", list.LListPathCurrent);
        Assert.Equal(@"C:\a.mp4", list.LListPathAnchor);
        Assert.Equal([@"C:\a.mp4"], TInterface.TListSelectionRead(list));
        Assert.Empty(notices);
    }

    [Fact]
    public void RemoveCurrentFromOutside_ClearsSelection()
    {
        (LDocket docket, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        TInterface.TListSelect(list, @"C:\b.mp4");
        List<string?> notices = [];
        TInterface.TListPathAttach(list, notices.Add);

        TInterface.TDocketPathsRemove(docket, @"C:\b.mp4");

        Assert.Null(list.LListPathCurrent);
        Assert.Empty(TInterface.TListSelectionRead(list));
        Assert.Equal([null], notices);
    }

    [Fact]
    public void Remove_SkipsLockedSelection_ClearKeepsLocked()
    {
        (LDocket docket, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        TInterface.TDocketDeliveredAdd(docket, @"C:\locked.mp4", true);
        List<bool> locks = [];
        TInterface.TListLockAttach(list, locks.Add);

        TInterface.TListSelect(list, @"C:\locked.mp4");
        Assert.True(TInterface.TListLockCheck(list));
        Assert.Contains(true, locks);

        TInterface.TListRemove(list);
        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4", @"C:\locked.mp4"], TInterface.TDocketPathsRead(docket));

        TInterface.TListClear(list);
        Assert.Equal([@"C:\locked.mp4"], TInterface.TDocketPathsRead(docket));
        Assert.False(list.LListEmpty);
    }

    [Fact]
    public void Cards_GroupLockedByBatch_StateKeysFollowSelection()
    {
        (LDocket docket, LList list) = TListBuild(@"C:\a.mp4");
        TInterface.TDocketDeliveredAdd(docket, @"C:\x.mp4", true);
        TInterface.TListSelect(list, @"C:\x.mp4");

        IReadOnlyList<LListCard> cards = TInterface.TListCardsRead(list);

        Assert.Equal(2, cards.Count);
        Assert.False(cards[0].LListCardLocked);
        Assert.Equal("a.mp4", cards[0].LListCardRows[0].LListRowName);
        Assert.Equal("Plain", cards[0].LListCardRows[0].LListRowState);
        Assert.True(cards[0].LListCardRows[0].LListRowLast);
        Assert.True(cards[1].LListCardLocked);
        Assert.Equal("LockedSelected", cards[1].LListCardRows[0].LListRowState);
        Assert.False(cards[1].LListCardRows[0].LListRowLast);

        TInterface.TListSelect(list, @"C:\a.mp4");

        Assert.Equal("Selected", TInterface.TListStateRead(list, @"C:\a.mp4"));
        Assert.Equal("Locked", TInterface.TListStateRead(list, @"C:\x.mp4"));
    }

    [Fact]
    public void Press_LockedRowSelectsWithoutCapture_DragNeedsThreshold()
    {
        (LDocket docket, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");
        TInterface.TDocketDeliveredAdd(docket, @"C:\x.mp4", true);

        Assert.False(TInterface.TListPressHandle(list, @"C:\x.mp4", false, false, 0, 0, 0, 0));
        Assert.Equal(@"C:\x.mp4", list.LListPathCurrent);
        Assert.False(TInterface.TListDragResolve(list, 20, 20, 4, true));

        Assert.True(TInterface.TListPressHandle(list, @"C:\a.mp4", false, false, 10, 10, 3, 4));
        TInterface.TListPressSelect(list, @"C:\b.mp4", true, false);
        Assert.False(TInterface.TListDragResolve(list, 12, 12, 4, true));
        Assert.False(TInterface.TListDragResolve(list, 30, 30, 4, false));
        Assert.True(TInterface.TListDragResolve(list, 30, 30, 4, true));
        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4"], list.LListDrag.LListDragPaths);
        Assert.Equal(3, list.LListDrag.LListGrabX);
        Assert.Null(list.LListDrag.LListDragPath);
        Assert.False(TInterface.TListDragResolve(list, 60, 60, 4, true));

        Assert.True(TInterface.TListPressHandle(list, @"C:\a.mp4", false, false, 0, 0, 0, 0));
        TInterface.TListReleaseHandle(list);
        Assert.Null(list.LListDrag.LListDragPath);
        Assert.Equal([@"C:\a.mp4"], TInterface.TListSelectionRead(list));
    }

    [Fact]
    public void KeyRun_ControlA_SelectsAll_OtherKeysIgnored()
    {
        (_, LList list) = TListBuild(@"C:\a.mp4", @"C:\b.mp4");

        Assert.False(TInterface.TListKeyRun(list, "A", false));
        Assert.False(TInterface.TListKeyRun(list, "B", true));
        Assert.True(TInterface.TListKeyRun(list, "A", true));
        Assert.Equal(2, TInterface.TListSelectionRead(list).Count);
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
            (LDocket docket, LList list) = TListBuild();
            List<IReadOnlyList<LDocketEntry>> added = [];
            TInterface.TListItemsAttach(list, added.Add);

            int count = await TInterface.TListPathsAdd(list, clip, Path.Combine(folder, "missing.mp4"));

            Assert.Equal(1, count);
            Assert.Equal([clip], TInterface.TDocketPathsRead(docket));
            Assert.Equal(clip, list.LListPathCurrent);
            Assert.Single(added);

            TInterface.TListDialogAdd(list, false, [clip]);
            TInterface.TListDialogAdd(list, null, [clip]);

            Assert.Single(added);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
