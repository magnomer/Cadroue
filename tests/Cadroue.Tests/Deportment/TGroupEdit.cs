using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TGroupEdit
{
    [Fact]
    public void PathsInsert_SkipsDuplicate_LandsContiguous()
    {
        LGroupSelection selection = TInterface.TGroupSelectionCreate();
        LGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4", @"C:\b.mp4", @"C:\c.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        bool accepted = TInterface.TGroupPathsInsert(group, 0, [@"C:\B.MP4", @"C:\x.mp4", @"C:\y.mp4"], 1);

        Assert.True(accepted);
        Assert.Equal(
            [@"C:\a.mp4", @"C:\x.mp4", @"C:\y.mp4", @"C:\b.mp4", @"C:\c.mp4"],
            group.LGroupRecords[0].LGroupRecordPaths);
        Assert.Equal(1, rebuilds);
    }

    [Fact]
    public void PathsInsert_AllDuplicates_AcceptsWithoutNotice()
    {
        LGroupSelection selection = TInterface.TGroupSelectionCreate();
        LGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4", @"C:\b.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        bool accepted = TInterface.TGroupPathsInsert(group, 0, [@"C:\a.mp4", @"C:\b.mp4"], 0);

        Assert.True(accepted);
        Assert.Equal([@"C:\a.mp4", @"C:\b.mp4"], group.LGroupRecords[0].LGroupRecordPaths);
        Assert.Equal(0, rebuilds);
        Assert.False(TInterface.TGroupPathsInsert(group, 3, [@"C:\z.mp4"], 0));
        Assert.False(TInterface.TGroupPathsInsert(group, 0, [], 0));
    }

    [Fact]
    public void EditStart_MarksIndex_CommitAppliesAndClears()
    {
        LGroupSelection selection = TInterface.TGroupSelectionCreate();
        LGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        TInterface.TGroupEditStart(group, 0);
        Assert.Equal(0, group.LGroupEditingIndex);
        Assert.Equal(1, rebuilds);

        TInterface.TGroupEditStart(group, 0);
        Assert.Equal(1, rebuilds);

        Assert.True(TInterface.TGroupNameCommit(group, "  Two "));
        Assert.Null(group.LGroupEditingIndex);
        Assert.Equal("Two", group.LGroupRecords[0].LGroupRecordName);
        Assert.Equal(2, rebuilds);

        Assert.False(TInterface.TGroupNameCommit(group, "Three"));
        Assert.Equal("Two", group.LGroupRecords[0].LGroupRecordName);
        Assert.Equal(2, rebuilds);
    }

    [Fact]
    public void Commit_RejectedName_ClosesEditorKeepsName()
    {
        LGroupSelection selection = TInterface.TGroupSelectionCreate();
        LGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        TInterface.TGroupEditStart(group, 0);
        Assert.False(TInterface.TGroupNameCommit(group, "   "));
        Assert.Null(group.LGroupEditingIndex);
        Assert.Equal("One", group.LGroupRecords[0].LGroupRecordName);
        Assert.Equal(2, rebuilds);

        TInterface.TGroupEditStart(group, 0);
        TInterface.TGroupEditCancel(group);
        Assert.Null(group.LGroupEditingIndex);
        Assert.Equal("One", group.LGroupRecords[0].LGroupRecordName);
        Assert.Equal(4, rebuilds);

        TInterface.TGroupEditCancel(group);
        Assert.Equal(4, rebuilds);
    }

    [Fact]
    public void NameSet_RaisesOnlyOnAcceptedChange()
    {
        LGroupSelection selection = TInterface.TGroupSelectionCreate();
        LGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        Assert.False(TInterface.TGroupNameSet(group, 0, "One"));
        Assert.False(TInterface.TGroupNameSet(group, 0, ""));
        Assert.False(TInterface.TGroupNameSet(group, 2, "Two"));
        Assert.Equal(0, rebuilds);

        Assert.True(TInterface.TGroupNameSet(group, 0, "Two"));
        Assert.Equal("Two", group.LGroupRecords[0].LGroupRecordName);
        Assert.Equal(1, rebuilds);
    }

    [Fact]
    public void RecordsChange_ClearsEditing()
    {
        LGroupSelection selection = TInterface.TGroupSelectionCreate();
        LGroup group = TInterface.TGroupCreate(selection);
        TInterface.TGroupAdd(group, [@"C:\a.mp4"], "One");
        TInterface.TGroupAdd(group, [@"C:\b.mp4"], "Two");
        int rebuilds = 0;
        TInterface.TGroupAttach(group, () => rebuilds++);

        TInterface.TGroupEditStart(group, 1);
        TInterface.TGroupRemove(group, 0);

        Assert.Null(group.LGroupEditingIndex);
        Assert.Equal(2, rebuilds);
        Assert.False(TInterface.TGroupNameCommit(group, "Three"));
        Assert.Equal("Two", group.LGroupRecords[0].LGroupRecordName);
        Assert.Equal(2, rebuilds);
    }
}
