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

        TInterface.TListSuccessorSet(list, @"C:\b.mp4");
        TInterface.TDocketPathsRemove(docket, @"C:\b.mp4");
        TInterface.TListRemovedApply(list, @"C:\b.mp4");
        TInterface.TListSuccessorSelect(list);
        TInterface.TListSuccessorReset(list);

        Assert.Equal(@"C:\c.mp4", list.LListPathCurrent);

        TInterface.TListSuccessorSet(list, @"C:\c.mp4");
        TInterface.TDocketPathsRemove(docket, @"C:\c.mp4");
        TInterface.TListRemovedApply(list, @"C:\c.mp4");
        TInterface.TListSuccessorSelect(list);

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
        TInterface.TListRemovedApply(list, @"C:\c.mp4");
        TInterface.TListSuccessorSelect(list);

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
        TInterface.TListRemovedApply(list, @"C:\b.mp4");
        TInterface.TListSuccessorSelect(list);

        Assert.Null(list.LListPathCurrent);
        Assert.Empty(TInterface.TListSelectionRead(list));
        Assert.Equal([null], notices);
    }
}
