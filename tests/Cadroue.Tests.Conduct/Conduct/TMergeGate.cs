using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TMergeGate
{
    private static readonly Guid TMergeRelay = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TMergeTab = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TMergeBatchA = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TMergeBatchB = new("bbbbbbbb-0000-0000-0000-000000000002");

    private static readonly (string CMergePath, Guid CMergeBatch, bool CMergeLocked)[] TMergeItems =
    [
        (@"C:\a.mp4", TMergeBatchA, false),
        (@"C:\b.mp4", TMergeBatchB, false),
        (@"C:\c.mp4", TMergeBatchA, true),
    ];

    private static readonly (string CMergeName, IReadOnlyList<string> CMergePaths)[] TMergeGroups =
    [
        ("Pair", [@"C:\a.mp4", @"C:\B.MP4"]),
        ("Locked", [@"C:\b.mp4", @"C:\c.mp4"]),
        ("Empty", []),
        ("Single", [@"c:\A.mp4"]),
    ];

    private readonly TMergePort tMergePort = new();
    private readonly CMerge tMergeGate;

    public TMergeGate()
    {
        tMergeGate = TAtelier.TAtelierCreate(port => port with { LAtelierMerge = tMergePort }).CAtelierMerge;
    }

    [Fact]
    public void MergeRun_InvalidPreset_AnswersFalseAndRunsNothing()
    {
        Assert.False(tMergeGate.CMergeRun(false, "Alpha", TMergeGroups, TMergeItems, true, TMergeRelay, TMergeTab));
        Assert.Empty(tMergePort.TMergeCalls);
    }

    [Fact]
    public void MergeRun_ValidPreset_RunsUnlockedGroupsSkipsLockedOnesInOnePortCall()
    {
        Assert.True(tMergeGate.CMergeRun(true, "Alpha", TMergeGroups, TMergeItems, true, TMergeRelay, TMergeTab));

        Assert.Equal(
            [
                @"run Alpha|Pair=C:\a.mp4,C:\B.MP4;Single=c:\A.mp4|skip Locked:1/2|urgent"
                + $@"|{TMergeRelay}|{TMergeTab}|C:\a.mp4@{TMergeBatchA:N},C:\b.mp4@{TMergeBatchB:N}",
            ],
            tMergePort.TMergeCalls);
    }

    [Fact]
    public void MergeRun_NoGroups_StillAsksThePortOnce()
    {
        Assert.True(tMergeGate.CMergeRun(true, "Alpha", [], [], false, TMergeRelay, TMergeTab));

        Assert.Equal([$"run Alpha||skip |normal|{TMergeRelay}|{TMergeTab}|"], tMergePort.TMergeCalls);
    }

    [Fact]
    public void MergeCohortRun_InvalidPreset_RecordsTheHoldAndAnswersZero()
    {
        tMergePort.TMergeAnswer = 5;

        int queued = tMergeGate.CMergeCohortRun(
            false, "Alpha", TMergeGroups, TMergeItems, TMergeBatchA, TMergeRelay, TMergeTab);

        Assert.Equal(0, queued);
        Assert.Equal([$"held {TMergeTab}"], tMergePort.TMergeCalls);
    }

    [Fact]
    public void MergeCohortRun_RunsOnlyGroupsWhollyInTheCohortAtNormalPriority()
    {
        tMergePort.TMergeAnswer = 3;

        int queued = tMergeGate.CMergeCohortRun(
            true, "Alpha", TMergeGroups, TMergeItems, TMergeBatchA, TMergeRelay, TMergeTab);

        Assert.Equal(3, queued);
        Assert.Equal(
            [
                @"run Alpha|Single=c:\A.mp4|skip |normal"
                + $@"|{TMergeRelay}|{TMergeTab}|C:\a.mp4@{TMergeBatchA:N},C:\b.mp4@{TMergeBatchB:N}",
            ],
            tMergePort.TMergeCalls);
    }

    [Fact]
    public void MergeCohortRun_EmptyCohort_FiltersNothing()
    {
        tMergeGate.CMergeCohortRun(true, "Alpha", TMergeGroups, TMergeItems, Guid.Empty, TMergeRelay, TMergeTab);

        Assert.StartsWith(
            @"run Alpha|Pair=C:\a.mp4,C:\B.MP4;Single=c:\A.mp4|skip Locked:1/2|", tMergePort.TMergeCalls[0]);
    }

    [Fact]
    public void MergeEligibleRead_AnswersRunnablePathsAndRecordsTheSkips()
    {
        Assert.Equal(
            [@"C:\a.mp4", @"C:\B.MP4", @"c:\A.mp4"], tMergeGate.CMergeEligibleRead(TMergeGroups, TMergeItems));
        Assert.Equal(["skip Locked:1/2"], tMergePort.TMergeCalls);
    }

    [Fact]
    public void MergeRun_UnknownPath_IsNeitherLockedNorInACohort()
    {
        (string, IReadOnlyList<string>)[] groups = [("Stray", [@"C:\z.mp4"])];

        Assert.Equal([@"C:\z.mp4"], tMergeGate.CMergeEligibleRead(groups, TMergeItems));
        tMergeGate.CMergeCohortRun(true, "Alpha", groups, TMergeItems, TMergeBatchA, TMergeRelay, TMergeTab);

        Assert.StartsWith("run Alpha||skip |", tMergePort.TMergeCalls[1]);
    }

    [Fact]
    public void MergeRun_RelaysIgnoreCase_LaterItemWins()
    {
        (string, Guid, bool)[] items = [(@"C:\a.mp4", TMergeBatchA, false), (@"c:\A.MP4", TMergeBatchB, false)];

        tMergeGate.CMergeRun(true, "Alpha", [], items, false, TMergeRelay, TMergeTab);

        Assert.EndsWith($@"|C:\a.mp4@{TMergeBatchB:N}", tMergePort.TMergeCalls[0]);
    }
}
