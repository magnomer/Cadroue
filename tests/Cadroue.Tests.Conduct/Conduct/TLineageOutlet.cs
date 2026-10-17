using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TLineageOutlet
{
    [Fact]
    public void LineageResolve_SmallSchedule_ReturnsThePlanAnswer()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        TScheduleWork first = schedule.TWorkCreate(batch, "one");
        TScheduleWork second = schedule.TWorkCreate(batch, "two", parent: first);
        first.TWorkItem.LWorkSourceBytes = 1000;
        first.TWorkItem.LWorkRunState.LWorkOutputBytes = 800;
        second.TWorkItem.LWorkRunState.LWorkOutputBytes = 400;
        schedule.TScheduleAdd(first, second);
        Guid[] ids = [second.TWorkId, first.TWorkId];

        LLineageAnswer answer = TInterface.TLineageResolve(schedule.TScheduleRead(), ids);
        LScheduleLineageAnswer plan = TInterface.TLineagePlanRead(schedule.TScheduleRead(), ids);

        Assert.Equal(
            plan.LScheduleLineageEntries.Select(entry => (
                entry.LScheduleEntryBatch,
                entry.LScheduleEntryLineage,
                entry.LScheduleEntrySubject,
                entry.LScheduleEntryName,
                string.Join(",", entry.LScheduleEntryIds),
                entry.LScheduleEntryOrigin)),
            answer.LLineageGroups.Select(group => (
                group.LLineageGroupBatch,
                group.LLineageGroupLineage,
                group.LLineageGroupSubject,
                group.LLineageGroupName,
                string.Join(",", group.LLineageGroupIds),
                group.LLineageGroupOrigin)));
        Assert.Equal(
            plan.LScheduleLineageBatches.Select(item => (
                item.LScheduleBatchId,
                string.Join(",", item.LScheduleBatchStages.Order()),
                item.LScheduleBatchInitial,
                item.LScheduleBatchEarliest,
                string.Join("|", item.LScheduleBatchSources),
                string.Join("|", item.LScheduleBatchOutputs),
                item.LScheduleSourceTotal,
                item.LScheduleOutputTotal,
                item.LScheduleSourceCount,
                item.LScheduleOutputCount,
                string.Join(",", item.LScheduleBatchIds),
                item.LScheduleBatchCompleted)),
            answer.LLineageBatches.Select(item => (
                item.LLineageBatchId,
                string.Join(",", item.LLineageBatchStages.Order()),
                item.LLineageBatchInitial,
                item.LLineageBatchEarliest,
                string.Join("|", item.LLineageBatchSources),
                string.Join("|", item.LLineageBatchOutputs),
                item.LLineageSourceTotal,
                item.LLineageOutputTotal,
                item.LLineageSourceCount,
                item.LLineageOutputCount,
                string.Join(",", item.LLineageBatchIds),
                item.LLineageBatchCompleted)));
        Assert.Equal(
            plan.LScheduleLineageSteps.Select(step => (
                step.LScheduleStepId,
                step.LScheduleStepBatch,
                step.LScheduleStepRelay,
                step.LScheduleStepTab,
                step.LScheduleStepKind.ToString(),
                step.LScheduleStepSubject,
                step.LScheduleStepName,
                step.LScheduleStepSource,
                step.LScheduleStepRatio,
                step.LScheduleSourceExtension,
                step.LScheduleOutputExtension)),
            answer.LLineageSteps.Select(step => (
                step.LLineageStepId,
                step.LLineageStepBatch,
                step.LLineageStepRelay,
                step.LLineageStepTab,
                step.LLineageStepKind,
                step.LLineageStepSubject,
                step.LLineageStepName,
                step.LLineageStepSource,
                step.LLineageStepRatio,
                step.LLineageSourceExtension,
                step.LLineageOutputExtension)));
        Assert.Equal(2, answer.LLineageSteps.Count);
    }

    [Fact]
    public void LineageTitleRead_StepFacts_MatchTheTitleOfTheWorkItem_AfterTheRecordIsRemoved()
    {
        using var schedule = new TSchedule();
        TScheduleWork work = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(work);
        LLineageStep step = Assert.Single(
            TInterface.TLineageResolve(schedule.TScheduleRead(), [work.TWorkId]).LLineageSteps);
        string expected = TAtelier.TAtelierCreate(port =>
                port with { LAtelierLineage = TInterface.TLineageOutletCreate() })
            .CAtelierLineage
            .CLineageTitleRead(work.TWorkItem.LWorkBatchId, work.TWorkItem.LWorkRelaySource, work.TWorkItem.LWorkTab);

        schedule.TScheduleAllClear();

        Assert.Equal(expected, TInterface.TLineageTitleRead(step));
    }

    [Fact]
    public void LineagePlanRead_RepeatedPath_AsksThePathOnce_AndTheFirstKeyWins()
    {
        using var schedule = new TSchedule();
        TScheduleWork first = schedule.TWorkCreate(Guid.NewGuid(), "one");
        TScheduleWork second = schedule.TWorkDeriveCreate(first, "two");
        schedule.TScheduleAdd(first, second);
        var calls = new List<string>();

        LScheduleLineageAnswer plan = TInterface.TLineagePlanRead(
            schedule.TScheduleRead(),
            [first.TWorkId, second.TWorkId],
            path =>
            {
                calls.Add(path);
                return path + "#" + calls.Count;
            });

        Assert.Equal(
            [first.TWorkItem.LWorkOutputPath, first.TWorkItem.LWorkSourcePath, second.TWorkItem.LWorkOutputPath],
            calls);
        Assert.Equal([first.TWorkItem.LWorkSourcePath], plan.LScheduleLineageBatches.Single().LScheduleBatchSources);
    }

    [Fact]
    public void LineagePlanRead_NullPath_SkipsItAndResolvesTheRest()
    {
        using var schedule = new TSchedule();
        TScheduleWork merge = schedule.TWorkMergeCreate(Guid.NewGuid(), "merge", ["a", null!, "b"]);
        schedule.TScheduleAdd(merge);
        var calls = new List<string>();

        LScheduleLineageAnswer plan = TInterface.TLineagePlanRead(
            schedule.TScheduleRead(),
            [merge.TWorkId],
            path =>
            {
                calls.Add(path);
                return path + ".path";
            });

        Assert.Equal(
            [merge.TWorkItem.LWorkOutputPath, merge.TWorkItem.LWorkSourcePath, "a", "b"],
            calls);
        Assert.Equal(["a", "b"], plan.LScheduleLineageBatches.Single().LScheduleBatchSources);
    }

    [Fact]
    public void LineagePlanRead_StepPaths_AskTheExtensionForSourceThenOutput()
    {
        using var schedule = new TSchedule();
        TScheduleWork work = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(work);
        var calls = new List<string>();

        LScheduleLineageStep step = Assert.Single(TInterface.TLineageExtensionRead(
            schedule.TScheduleRead(),
            [work.TWorkId],
            path =>
            {
                calls.Add(path);
                return path + ".extension";
            }).LScheduleLineageSteps);

        Assert.Equal([work.TWorkItem.LWorkSourcePath, work.TWorkItem.LWorkOutputPath], calls);
        Assert.Equal(work.TWorkItem.LWorkSourcePath + ".extension", step.LScheduleSourceExtension);
        Assert.Equal(work.TWorkItem.LWorkOutputPath + ".extension", step.LScheduleOutputExtension);
    }

    [Fact]
    public void LineageResolve_StepPaths_AskTheExtensionSeam()
    {
        using var schedule = new TSchedule();
        TScheduleWork work = schedule.TWorkCreate(Guid.NewGuid(), "one");
        schedule.TScheduleAdd(work);
        Func<string, string> previous = LUsherTextEngine.LUsherExtensionSeam;
        LUsherTextEngine.LUsherExtensionSeam = path => path + ".extension";
        try
        {
            LLineageStep step = Assert.Single(
                TInterface.TLineageResolve(schedule.TScheduleRead(), [work.TWorkId]).LLineageSteps);
            Assert.Equal(work.TWorkItem.LWorkSourcePath + ".extension", step.LLineageSourceExtension);
            Assert.Equal(work.TWorkItem.LWorkOutputPath + ".extension", step.LLineageOutputExtension);
        }
        finally
        {
            LUsherTextEngine.LUsherExtensionSeam = previous;
        }
    }
}
