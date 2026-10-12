using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TGroupEdit
{
    [Fact]
    public void PathsInsert_SkipsDuplicate_LandsContiguous()
    {
        LGroupSelection selection = Interface.TInterface.TGroupSelectionCreate();
        QGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        bool accepted = TInterface.TGroupPathsInsert(group, 0, [@"C:\B.MP4", @"C:\x.mp4", @"C:\y.mp4"], 1);

        Assert.True(accepted);
        Assert.Equal(
            [@"C:\a.mp4", @"C:\x.mp4", @"C:\y.mp4", @"C:\b.mp4", @"C:\c.mp4"],
            group.QGroupRecords[0].QGroupRecordPaths);
        Assert.Equal(1, rebuilds);
    }

    [Fact]
    public void PathsInsert_AllDuplicates_AcceptsWithoutNotice()
    {
        LGroupSelection selection = Interface.TInterface.TGroupSelectionCreate();
        QGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4", @"C:\b.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        bool accepted = TInterface.TGroupPathsInsert(group, 0, [@"C:\a.mp4", @"C:\b.mp4"], 0);

        Assert.True(accepted);
        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4"], group.QGroupRecords[0].QGroupRecordPaths);
        Assert.Equal(0, rebuilds);
        Assert.False(TInterface.TGroupPathsInsert(group, 3, [@"C:\z.mp4"], 0));
        Assert.False(TInterface.TGroupPathsInsert(group, 0, [], 0));
    }

    [Fact]
    public void EditStart_MarksIndex_CommitAppliesAndClears()
    {
        LGroupSelection selection = Interface.TInterface.TGroupSelectionCreate();
        QGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        TInterface.TGroupEditStart(group, 0);
        Assert.Equal(0, group.QGroupName.QGroupEditingIndex);
        Assert.Equal(1, rebuilds);

        TInterface.TGroupEditStart(group, 0);
        Assert.Equal(1, rebuilds);

        Assert.True(TInterface.TGroupNameCommit(group, "  Two "));
        Assert.Null(group.QGroupName.QGroupEditingIndex);
        Assert.Equal("Two", group.QGroupRecords[0].QGroupRecordName);
        Assert.Equal(2, rebuilds);

        Assert.False(TInterface.TGroupNameCommit(group, "Three"));
        Assert.Equal("Two", group.QGroupRecords[0].QGroupRecordName);
        Assert.Equal(2, rebuilds);
    }

    [Fact]
    public void Commit_RejectedName_ClosesEditorKeepsName()
    {
        LGroupSelection selection = Interface.TInterface.TGroupSelectionCreate();
        QGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        TInterface.TGroupEditStart(group, 0);
        Assert.False(TInterface.TGroupNameCommit(group, "   "));
        Assert.Null(group.QGroupName.QGroupEditingIndex);
        Assert.Equal("One", group.QGroupRecords[0].QGroupRecordName);
        Assert.Equal(2, rebuilds);

        TInterface.TGroupEditStart(group, 0);
        TInterface.TGroupEditCancel(group);
        Assert.Null(group.QGroupName.QGroupEditingIndex);
        Assert.Equal("One", group.QGroupRecords[0].QGroupRecordName);
        Assert.Equal(4, rebuilds);

        TInterface.TGroupEditCancel(group);
        Assert.Equal(4, rebuilds);
    }

    [Fact]
    public void NameSet_RaisesOnlyOnAcceptedChange()
    {
        LGroupSelection selection = Interface.TInterface.TGroupSelectionCreate();
        QGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        Assert.False(TInterface.TGroupNameSet(group, 0, "One"));
        Assert.False(TInterface.TGroupNameSet(group, 0, ""));
        Assert.False(TInterface.TGroupNameSet(group, 2, "Two"));
        Assert.Equal(0, rebuilds);

        Assert.True(TInterface.TGroupNameSet(group, 0, "Two"));
        Assert.Equal("Two", group.QGroupRecords[0].QGroupRecordName);
        Assert.Equal(1, rebuilds);
    }

    [Fact]
    public void RecordsChange_ClearsEditing()
    {
        LGroupSelection selection = Interface.TInterface.TGroupSelectionCreate();
        QGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        TInterface.TGroupAdd(group, [@"C:\b.mp4"], "Two");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        TInterface.TGroupEditStart(group, 1);
        TInterface.TGroupRemove(group, 0);

        Assert.Null(group.QGroupName.QGroupEditingIndex);
        Assert.Equal(2, rebuilds);
        Assert.False(TInterface.TGroupNameCommit(group, "Three"));
        Assert.Equal("Two", group.QGroupRecords[0].QGroupRecordName);
        Assert.Equal(2, rebuilds);
    }

    [Fact]
    public void Cards_NumberFiles_NameThem_MarkTheEditingCard()
    {
        QGroup group = TInterface.TGroupCreate(Interface.TInterface.TGroupSelectionCreate());
        TInterface.TGroupAdd(group, [@"C:\a (1).mp4", @"C:\a (2).mp4"], "One");
        TInterface.TGroupAdd(group, [@"C:\b.mp4"], "Two");
        TInterface.TGroupEditStart(group, 1);

        IReadOnlyList<QGroupCard> cards = TInterface.TGroupCardsRead(group);

        Assert.Equal(2, cards.Count);
        Assert.False(cards[0].QGroupCardEditing);
        Assert.Equal("2", cards[0].QGroupCardFiles[1].QGroupFileNumber);
        Assert.Equal("a (2).mp4", cards[0].QGroupCardFiles[1].QGroupFileName);
        Assert.Equal(0, cards[0].QGroupCardFiles[1].QGroupFileGroup);
        Assert.True(cards[1].QGroupCardEditing);
        Assert.False(group.QGroupEmpty);
    }

    [Fact]
    public void Toggles_MirrorTheSelection_RunChangesIt()
    {
        LGroupSelection selection = Interface.TInterface.TGroupSelectionCreate();
        QGroup group = TInterface.TGroupCreate(selection);
        int faces = 0;
        TInterface.TGroupFaceAttach(group, () => faces++);

        Assert.True(TInterface.TGroupModeRead(group).QGroupToggleLeft.QGroupSideActive);
        TInterface.TGroupToggleRun(group, "Mode", true);
        Assert.True(selection.LGroupAuto);
        Assert.True(TInterface.TGroupModeRead(group).QGroupToggleRight.QGroupSideActive);
        Assert.Equal(1, faces);

        IReadOnlyList<QGroupSwitch> switches = TInterface.TGroupSwitchesRead(group);
        Assert.Equal(2, switches.Count);
        TInterface.TGroupToggleRun(group, "Strict", true);
        Assert.False(selection.LGroupStrict);
        TInterface.TGroupToggleRun(group, "Name", false);
        Assert.True(TInterface.TGroupSwitchesRead(group)[1].QGroupToggleLeft.QGroupSideActive);
        Assert.Equal(3, faces);
    }

    [Fact]
    public void AutoUpdate_GroupsTheAttachedSource_OnlyInAutoMode()
    {
        LGroupSelection selection = Interface.TInterface.TGroupSelectionCreate();
        QGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupSourceAttach(group, () => [@"C:\a (1).mp4", @"C:\a (2).mp4", @"C:\b (1).mp4"]);

        TInterface.TGroupAutoUpdate(group);
        Assert.Empty(group.QGroupRecords);

        TInterface.TGroupToggleRun(group, "Mode", true);
        Assert.Contains(group.QGroupRecords, record => record.QGroupRecordPaths.Count == 2);
    }

    [Fact]
    public void Drops_RouteFilesOut_MoveOrInsertIntoCards_AddNewGroupsOnThePanel()
    {
        QGroup group = TInterface.TGroupCreate(Interface.TInterface.TGroupSelectionCreate());
        TInterface.TGroupAdd(group, [@"C:\a.mp4", @"C:\b.mp4"], "One");
        List<IReadOnlyList<string>> requests = [];
        TInterface.TGroupRequestAttach(group, requests.Add);

        Assert.True(TInterface.TGroupCardAccept(group, 0, 0, [@"C:\new.mp4"], null, null, null));
        Assert.Equal([[@"C:\new.mp4"]], requests);
        Assert.True(TInterface.TGroupCardAccept(group, 0, 0, null, null, null, [@"C:\c.mp4"]));
        Assert.Equal([@"C:\c.mp4", @"C:\a.mp4", @"C:\b.mp4"], group.QGroupRecords[0].QGroupRecordPaths);
        Assert.True(TInterface.TGroupCardAccept(group, 0, 3, null, 0, @"C:\c.mp4", null));
        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4"], group.QGroupRecords[0].QGroupRecordPaths);

        Assert.True(TInterface.TGroupPanelAccept(group, true, null, null, null, [@"C:\z.mp4"]));
        Assert.Single(group.QGroupRecords);
        Assert.True(TInterface.TGroupPanelAccept(group, false, null, 0, @"C:\c.mp4", null));
        Assert.Equal(2, group.QGroupRecords.Count);
        Assert.Equal([@"C:\c.mp4"], group.QGroupRecords[1].QGroupRecordPaths);
        Assert.False(TInterface.TGroupPanelAccept(group, false, null, null, null, []));
        Assert.True(TInterface.TGroupPanelAccept(group, false, [@"C:\x.mp4"], null, null, null));
        Assert.Equal(2, requests.Count);
    }

    [Fact]
    public void Drag_OpensOnPress_StartsAfterThreshold_InsertResolvesByRowMiddle()
    {
        QGroup group = TInterface.TGroupCreate(Interface.TInterface.TGroupSelectionCreate());
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        List<string> opened = [];
        TInterface.TGroupOpenAttach(group, opened.Add);

        TInterface.TGroupPressHandle(group, 0, @"C:\a.mp4", 10, 10);

        Assert.Equal([@"C:\a.mp4"], opened);
        Assert.Equal(0, group.QGroupDrag.QGroupDragIndex);
        Assert.False(TInterface.TGroupDragResolve(group, 12, 12, 4, true));
        Assert.False(TInterface.TGroupDragResolve(group, 30, 30, 4, false));
        Assert.True(TInterface.TGroupDragResolve(group, 30, 30, 4, true));

        TInterface.TGroupDragClear(group);
        Assert.Equal(-1, group.QGroupDrag.QGroupDragIndex);
        Assert.False(TInterface.TGroupDragResolve(group, 30, 30, 4, true));

        Assert.Equal(0, TInterface.TGroupInsertResolve(10, [0, 30], [30, 30]));
        Assert.Equal(1, TInterface.TGroupInsertResolve(20, [0, 30], [30, 30]));
        Assert.Equal(2, TInterface.TGroupInsertResolve(70, [0, 30], [30, 30]));
    }

    [Fact]
    public void KeyRun_ReturnCommits_EscapeCancels_LabelNeedsDoubleClick()
    {
        QGroup group = TInterface.TGroupCreate(Interface.TInterface.TGroupSelectionCreate());
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");

        Assert.False(TInterface.TGroupLabelHandle(group, 0, 1));
        Assert.True(TInterface.TGroupLabelHandle(group, 0, 2));
        Assert.False(TInterface.TGroupKeyRun(group, "A", "x"));
        Assert.True(TInterface.TGroupKeyRun(group, "Return", "Two"));
        Assert.Equal("Two", group.QGroupRecords[0].QGroupRecordName);

        TInterface.TGroupLabelHandle(group, 0, 2);
        Assert.True(TInterface.TGroupKeyRun(group, "Escape", "Three"));
        Assert.Equal("Two", group.QGroupRecords[0].QGroupRecordName);
        Assert.Null(group.QGroupName.QGroupEditingIndex);
    }
}
