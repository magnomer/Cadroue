using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Conduct;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TLineage
{
    private readonly TLineagePort tLineagePort = new();
    private readonly CLineage tLineageArea;

    public TLineage()
    {
        tLineageArea = new CAtelier(new TUsherPort(), tLineagePort, new TWorkspacePort(), new TPlacementPort())
            .CAtelierLineage;
    }

    [Fact]
    public void LineageRead_Ids_AsksThePortOnceWithTheIds()
    {
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid()];
        tLineageArea.CLineageRead(ids);
        IReadOnlyList<Guid> asked = Assert.Single(tLineagePort.TLineageCalls);
        Assert.Equal(ids, asked);
    }

    [Fact]
    public void LineageRead_EmptyAnswer_ReturnsEmptyLists()
    {
        CLineageAnswer answer = tLineageArea.CLineageRead([]);
        Assert.Empty(answer.CLineageEntries);
        Assert.Empty(answer.CLineageBatches);
        Assert.Empty(answer.CLineageSteps);
    }

    [Fact]
    public void LineageRead_Entry_CopiesEveryField()
    {
        Guid batch = Guid.NewGuid();
        Guid lineage = Guid.NewGuid();
        Guid[] ids = [Guid.NewGuid(), Guid.NewGuid()];
        tLineagePort.TLineageAnswer = TInterface.TLineageAnswerCreate(
            [
                TInterface.TLineageGroupCreate(batch, lineage, "subject", "name", ids, 1200),
                TInterface.TLineageGroupCreate(batch, lineage, "other", null, [], null),
            ],
            [],
            []);

        CLineageAnswer answer = tLineageArea.CLineageRead([]);

        Assert.Equal(
            [
                (batch, lineage, "subject", (string?)"name", string.Join(",", ids), (long?)1200),
                (batch, lineage, "other", null, "", null),
            ],
            answer.CLineageEntries.Select(entry => (
                entry.CLineageEntryBatch,
                entry.CLineageEntryLineage,
                entry.CLineageEntrySubject,
                entry.CLineageEntryName,
                string.Join(",", entry.CLineageEntryIds),
                entry.CLineageEntryOrigin)));
    }

    [Fact]
    public void LineageRead_Batch_CopiesEveryField()
    {
        Guid id = Guid.NewGuid();
        Guid stage = Guid.NewGuid();
        DateTimeOffset earliest = DateTimeOffset.UnixEpoch.AddDays(3);
        tLineagePort.TLineageAnswer = TInterface.TLineageAnswerCreate(
            [],
            [
                TInterface.TLineageBatchCreate(
                    id, new HashSet<Guid> { stage }, 3, earliest, ["a", "b"], ["c"], 1000, 400, 2, 1,
                    [stage, id], true),
                TInterface.TLineageBatchCreate(
                    id, new HashSet<Guid>(), 0, default, [], [], null, null, 0, 0, [], false),
            ],
            []);

        CLineageAnswer answer = tLineageArea.CLineageRead([]);

        Assert.Equal(
            [
                (id, stage.ToString(), 3, earliest, "a,b", "c", (long?)1000, (long?)400, 2, 1, $"{stage},{id}", true),
                (id, "", 0, default(DateTimeOffset), "", "", null, null, 0, 0, "", false),
            ],
            answer.CLineageBatches.Select(batch => (
                batch.CLineageBatchId,
                string.Join(",", batch.CLineageBatchStages),
                batch.CLineageBatchInitial,
                batch.CLineageBatchEarliest,
                string.Join(",", batch.CLineageBatchSources),
                string.Join(",", batch.CLineageBatchOutputs),
                batch.CLineageSourceTotal,
                batch.CLineageOutputTotal,
                batch.CLineageSourceCount,
                batch.CLineageOutputCount,
                string.Join(",", batch.CLineageBatchIds),
                batch.CLineageBatchCompleted)));
    }

    [Fact]
    public void LineageBatchRead_Batch_CopiesOneRecord()
    {
        Guid id = Guid.NewGuid();
        tLineagePort.TLineageBatch = TInterface.TLineageBatchCreate(
            id, new HashSet<Guid>(), 1, default, ["a"], ["b", "c"], 10, 20, 1, 2, [id], true);

        CLineageBatch batch = tLineageArea.CLineageBatchRead(id);

        Assert.Equal([[id]], tLineagePort.TLineageCalls);
        Assert.Equal(
            (id, "a", "b,c", (long?)10, (long?)20, 1, 2, id.ToString(), true),
            (batch.CLineageBatchId,
                string.Join(",", batch.CLineageBatchSources),
                string.Join(",", batch.CLineageBatchOutputs),
                batch.CLineageSourceTotal,
                batch.CLineageOutputTotal,
                batch.CLineageSourceCount,
                batch.CLineageOutputCount,
                string.Join(",", batch.CLineageBatchIds),
                batch.CLineageBatchCompleted));
    }

    [Fact]
    public void LineageRead_Step_CopiesEveryField()
    {
        Guid id = Guid.NewGuid();
        Guid batch = Guid.NewGuid();
        Guid relay = Guid.NewGuid();
        tLineagePort.TLineageAnswer = TInterface.TLineageAnswerCreate(
            [],
            [],
            [
                TInterface.TLineageStepCreate(id, batch, relay, "tab", "Split", true, "name", 900, 0.5, "mkv", "mp4"),
                TInterface.TLineageStepCreate(id, batch, relay, "", "Merge", false, null, null, null, "", ""),
            ]);

        CLineageAnswer answer = tLineageArea.CLineageRead([]);

        Assert.Equal(
            [
                (id, batch, relay, "tab", "Split", true, (string?)"name", (long?)900, (double?)0.5, "mkv", "mp4"),
                (id, batch, relay, "", "Merge", false, null, null, null, "", ""),
            ],
            answer.CLineageSteps.Select(step => (
                step.CLineageStepId,
                step.CLineageStepBatch,
                step.CLineageStepRelay,
                step.CLineageStepTab,
                step.CLineageStepKind,
                step.CLineageStepSubject,
                step.CLineageStepName,
                step.CLineageStepSource,
                step.CLineageStepRatio,
                step.CLineageSourceExtension,
                step.CLineageOutputExtension)));
    }

    [Fact]
    public void LineageTitleRead_Facts_HandsTheThreeFactsToThePortOnce()
    {
        Guid batch = Guid.NewGuid();
        Guid relay = Guid.NewGuid();
        Assert.Equal($"{batch}|{relay}|tab.title", tLineageArea.CLineageTitleRead(batch, relay, "tab"));
        Assert.Equal([$"{batch}|{relay}|tab"], tLineagePort.TLineageTitles);
        Assert.Empty(tLineagePort.TLineageCalls);
    }
}
