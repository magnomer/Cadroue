using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TLineageEntries
{
    [Fact]
    public void Read_GroupsAThreeStageBatchIntoOneEntry_WithTitleStepsAndRatios()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        TScheduleWork third = schedule.TWorkCreate(batch, "three", parent: second);
        first.TWorkItem.LWorkSourceBytes = 1000;
        first.TWorkItem.LWorkRunState.LWorkOutputBytes = 800;
        second.TWorkItem.LWorkRunState.LWorkOutputBytes = 400;
        third.TWorkItem.LWorkRunState.LWorkOutputBytes = 200;
        LWorkItem[] items = [first.TWorkItem, second.TWorkItem, third.TWorkItem];
        schedule.TScheduleAdd(first);
        schedule.TScheduleAdd(second);
        schedule.TScheduleAdd(third);

        CLineageAnswer lineage = TInterfaceRoster.TRosterLineageRead(schedule.TScheduleRead(), items);
        CLineageEntry entry = Assert.Single(lineage.CLineageEntries);

        Assert.Equal(batch, entry.CLineageEntryBatch);
        Assert.Equal(3, entry.CLineageEntryIds.Count);
        Assert.Equal(first.TWorkItem.LWorkSourcePath, entry.CLineageEntrySubject);
        Assert.Equal(1000, entry.CLineageEntryOrigin);
        Assert.Equal(
            new double?[] { 0.8, 0.4, 0.2 },
            lineage.CLineageSteps.Select(step => step.CLineageStepRatio));
        Assert.NotEmpty(TInterfaceRoster.TLineageTitleFormat(entry));
        Assert.Equal(1, TInterfaceRoster.TLineageInitialRead(schedule.TScheduleRead(), items));
        Assert.Equal(new[] { second.TWorkId }, TInterfaceRoster.TLineageStageRead(schedule.TScheduleRead(), items));
        Assert.Equal(1, lineage.CLineageBatches.Single().CLineageBatchInitial);
        Assert.Equal(new[] { second.TWorkId }, lineage.CLineageBatches.Single().CLineageBatchStages);

        string[] steps = items
            .Select(item => TInterfaceRoster.TLineageStepFormat(lineage, item))
            .ToArray();
        Assert.All(steps, Assert.NotEmpty);
        Assert.Equal(steps[0], steps[1]);
        Assert.StartsWith("80", TInterfaceRoster.TLineageRatioFormat(lineage, first.TWorkItem));
        Assert.StartsWith("40", TInterfaceRoster.TLineageRatioFormat(lineage, second.TWorkItem));
        Assert.StartsWith("20", TInterfaceRoster.TLineageRatioFormat(lineage, third.TWorkItem));
        first.TWorkItem.LWorkSourceBytes = null;
        Assert.Equal("-", TInterfaceRoster.TLineageRatioFormat(
            TInterfaceRoster.TRosterLineageRead(schedule.TScheduleRead(), items), third.TWorkItem));
    }

    [Fact]
    public void Read_CarriesTheEntryNameAndEachStepFact()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        Guid firstRelay = Guid.NewGuid();
        Guid secondRelay = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        first.TWorkItem.LWorkSourceBytes = 1000;
        first.TWorkItem.LWorkRunState.LWorkOutputBytes = 800;
        first.TWorkItem.LWorkRelaySource = firstRelay;
        first.TWorkItem.LWorkTab = "First tab";
        schedule.TScheduleAdd(first);
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        second.TWorkItem.LWorkRunState.LWorkOutputBytes = 400;
        second.TWorkItem.LWorkRelaySource = secondRelay;
        second.TWorkItem.LWorkTab = "Second tab";
        schedule.TScheduleAdd(second);

        CLineageAnswer lineage = TInterfaceRoster.TRosterLineageRead(
            schedule.TScheduleRead(), [first.TWorkItem, second.TWorkItem]);

        CLineageEntry entry = Assert.Single(lineage.CLineageEntries);
        Assert.Equal("one.source", entry.CLineageEntryName);
        Assert.Equal(new[] { first.TWorkId, second.TWorkId }, entry.CLineageEntryIds);
        Assert.Equal(
            new[] { first.TWorkId, second.TWorkId }, lineage.CLineageSteps.Select(step => step.CLineageStepId));
        Assert.All(lineage.CLineageSteps, step => Assert.Null(step.CLineageStepName));
        Assert.All(lineage.CLineageSteps, step => Assert.False(step.CLineageStepSubject));
        Assert.All(
            lineage.CLineageSteps, step => Assert.Equal(nameof(LWorkKind.LWorkKindEdit), step.CLineageStepKind));
        Assert.Equal(new long?[] { 1000, null }, lineage.CLineageSteps.Select(step => step.CLineageStepSource));
        Assert.All(lineage.CLineageSteps, step => Assert.Equal(batch, step.CLineageStepBatch));
        Assert.Equal(new[] { firstRelay, secondRelay }, lineage.CLineageSteps.Select(step => step.CLineageStepRelay));
        Assert.Equal(new[] { "First tab", "Second tab" }, lineage.CLineageSteps.Select(step => step.CLineageStepTab));
        Assert.Equal(new double?[] { 0.8, 0.4 }, lineage.CLineageSteps.Select(step => step.CLineageStepRatio));
        Assert.Equal(
            first.TWorkItem.LWorkCreateTime,
            TInterfaceRoster.TRosterLineageRead(schedule.TScheduleRead(), [second.TWorkItem, first.TWorkItem])
                .CLineageBatches.Single().CLineageBatchEarliest);

        TScheduleWork split = schedule.TWorkCreate(Guid.NewGuid(), "three", kind: LWorkKind.LWorkKindSplit);
        schedule.TScheduleAdd(split);
        CLineageStep splitStep = Assert.Single(
            TInterfaceRoster.TRosterLineageRead(schedule.TScheduleRead(), [split.TWorkItem]).CLineageSteps);
        Assert.False(splitStep.CLineageStepSubject);
        Assert.Equal(nameof(LWorkKind.LWorkKindSplit), splitStep.CLineageStepKind);
        Assert.Equal("three.output", splitStep.CLineageStepName);
    }

    [Fact]
    public void Read_TwoSpellingsOfOnePath_JoinOnThePortKey()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        string output = first.TWorkItem.LWorkOutputPath;
        TInterfaceRoster.TWorkOutputSet(
            first.TWorkItem,
            Path.Combine(Path.GetDirectoryName(output)!, "spelled", "..", Path.GetFileName(output)));
        schedule.TScheduleAdd(first, second);

        CLineageBatch record = Assert.Single(
            TInterfaceRoster.TRosterLineageRead(
                schedule.TScheduleRead(), [first.TWorkItem, second.TWorkItem]).CLineageBatches);

        Assert.Equal(new[] { first.TWorkItem.LWorkSourcePath }, record.CLineageBatchSources);
        Assert.Equal(new[] { second.TWorkItem.LWorkOutputPath }, record.CLineageBatchOutputs);
    }

    [Fact]
    public void Read_SeparatesBatches_AndKeepsFirstSeenOrder()
    {
        using var schedule = new TSchedule();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(first, "one");
        TScheduleWork two = schedule.TWorkCreate(second, "two");
        TScheduleWork three = schedule.TWorkCreate(first, "three", parent: one);
        schedule.TScheduleAdd(one, two, three);

        IReadOnlyList<CLineageEntry> lineages = TInterfaceRoster.TRosterLineageRead(
            schedule.TScheduleRead(), [one.TWorkItem, two.TWorkItem, three.TWorkItem]).CLineageEntries;

        Assert.Equal(2, lineages.Count);
        Assert.Equal(first, lineages[0].CLineageEntryBatch);
        Assert.Equal(2, lineages[0].CLineageEntryIds.Count);
        Assert.Equal(second, lineages[1].CLineageEntryBatch);
        Assert.Null(lineages[1].CLineageEntryOrigin);
    }

    [Fact]
    public void Read_BatchIds_FollowTheEntryOrder_AndTheRosterKeepsIt()
    {
        using var schedule = new TSchedule();
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(first, "one");
        TScheduleWork two = schedule.TWorkCreate(first, "two");
        TScheduleWork three = schedule.TWorkCreate(second, "three");
        TScheduleWork four = schedule.TWorkCreate(first, "four", parent: one);
        schedule.TScheduleAdd(one, two, three, four);

        IReadOnlyList<CLineageBatch> batches = TInterfaceRoster.TRosterLineageRead(
            schedule.TScheduleRead(), [one.TWorkItem, two.TWorkItem, three.TWorkItem, four.TWorkItem]).CLineageBatches;

        Assert.Equal(new[] { first, second }, batches.Select(batch => batch.CLineageBatchId));
        Assert.Equal(new[] { one.TWorkId, four.TWorkId, two.TWorkId }, batches[0].CLineageBatchIds);
        Assert.Equal(new[] { three.TWorkId }, batches[1].CLineageBatchIds);
        Assert.Equal(
            new[] { one.TWorkId, four.TWorkId, two.TWorkId, three.TWorkId },
            TInterfaceRoster.TRosterIdsRead(schedule.TScheduleRead()));
    }

    [Fact]
    public void Read_BatchCompleted_OnlyWhenNoItemIsPendingOrRunning()
    {
        using var schedule = new TSchedule();
        Guid running = Guid.NewGuid();
        Guid finished = Guid.NewGuid();
        Guid pending = Guid.NewGuid();
        TScheduleWork one = schedule.TWorkCreate(running, "one");
        TScheduleWork two = schedule.TWorkCreate(running, "two");
        TScheduleWork three = schedule.TWorkCreate(finished, "three");
        TScheduleWork four = schedule.TWorkCreate(finished, "four");
        TScheduleWork five = schedule.TWorkCreate(pending, "five");
        one.TWorkItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateDone;
        two.TWorkItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateRunning;
        three.TWorkItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateDone;
        four.TWorkItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStateDone;
        five.TWorkItem.LWorkRunState.LWorkStateCurrent = LWorkState.LWorkStatePending;
        schedule.TScheduleAdd(one, two, three, four, five);

        IReadOnlyList<CLineageBatch> batches = TInterfaceRoster.TRosterLineageRead(
            schedule.TScheduleRead(),
            [one.TWorkItem, two.TWorkItem, three.TWorkItem, four.TWorkItem, five.TWorkItem]).CLineageBatches;

        Assert.Equal(new[] { false, true, false }, batches.Select(batch => batch.CLineageBatchCompleted));
    }

    [Fact]
    public void InitialRead_TwoSpellingsOfOneOutput_CountAsOnePath()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        string output = first.TWorkItem.LWorkOutputPath;
        TInterfaceRoster.TWorkOutputSet(
            first.TWorkItem,
            Path.Combine(Path.GetDirectoryName(output)!, "spelled", "..", Path.GetFileName(output)));
        schedule.TScheduleAdd(first, second);

        Assert.Equal(
            1, TInterfaceRoster.TLineageInitialRead(schedule.TScheduleRead(), [first.TWorkItem, second.TWorkItem]));
    }

    [Fact]
    public void InitialRead_BlankInput_HasNoPathAndCountsNothing()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TInterfaceRoster.TWorkOutputSet(first.TWorkItem, " ");
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        schedule.TScheduleAdd(second);

        Assert.Equal(0, TInterfaceRoster.TLineageInitialRead(schedule.TScheduleRead(), [second.TWorkItem]));
    }
}
