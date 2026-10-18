using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Xunit;

namespace Cadroue.Tests;

public sealed class TRosterCardGate
{
    private static readonly Guid TRosterBatch = Guid.NewGuid();
    private static readonly Guid TRosterOther = Guid.NewGuid();
    private static readonly Guid TRosterRunner = Guid.NewGuid();
    private static readonly Guid TRosterFirst = Guid.NewGuid();
    private static readonly Guid TRosterSecond = Guid.NewGuid();
    private static readonly Guid TRosterThird = Guid.NewGuid();
    private static readonly Guid TRosterForeign = Guid.NewGuid();

    private readonly TRosterCardPort tRosterCardPort = new();
    private readonly CRosterCard tRosterCardGate;

    public TRosterCardGate()
    {
        CAtelier atelier = TAtelier.TAtelierCreate(port => port with { LAtelierRosterCard = tRosterCardPort });
        tRosterCardGate = atelier.CAtelierRosterCreate().CRosterCard;
    }

    [Fact]
    public void Atelier_NoRosterCardPort_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TAtelier.TAtelierCreate(port => port with { LAtelierRosterCard = null! }));
    }

    [Fact]
    public void RosterCardRead_Batch_AnswersItsFilesInLineageOrderFromOneEngineCall()
    {
        TRosterRowsAdd(TRosterFirst, TRosterSecond, TRosterThird);

        (CLineageBatch batch, IReadOnlyList<CRosterFile> files) = tRosterCardGate.CRosterCardRead(
            TRosterBatch, TRosterAnswerCreate(), TRosterRunner, id => id == TRosterThird);

        Assert.Equal(TRosterBatch, batch.CLineageBatchId);
        Assert.Equal(
            [$"{TRosterRunner}:{TRosterFirst},{TRosterSecond},{TRosterThird}"], tRosterCardPort.TRosterCardCalls);
        Assert.Equal(2, files.Count);
        Assert.True(files[0].CRosterFileStage);
        Assert.False(files[1].CRosterFileStage);
        Assert.Equal(TRosterFirst, files[0].CRosterFileRows[0].CRosterRowId);
        Assert.Equal(TRosterSecond, files[0].CRosterFileRows[1].CRosterRowId);
        Assert.False(files[0].CRosterFileRows[0].CRosterRowLast);
        Assert.True(files[0].CRosterFileRows[1].CRosterRowLast);
        Assert.True(files[0].CRosterFileRows[1].CRosterRowStage);
        Assert.False(files[0].CRosterFileRows[0].CRosterRowSelected);
        Assert.True(files[1].CRosterFileRows[0].CRosterRowSelected);
        Assert.Equal(TRosterThird, files[1].CRosterFileRows[0].CRosterRowStep.CLineageStepId);
    }

    [Fact]
    public void RosterCardRead_StepTheScheduleLacks_IsLeftOut()
    {
        TRosterRowsAdd(TRosterFirst, TRosterThird);

        (_, IReadOnlyList<CRosterFile> files) =
            tRosterCardGate.CRosterCardRead(TRosterBatch, TRosterAnswerCreate(), TRosterRunner, _ => false);

        CRosterRow row = Assert.Single(files[0].CRosterFileRows);
        Assert.Equal(TRosterFirst, row.CRosterRowId);
        Assert.False(row.CRosterRowLast);
        Assert.True(files[0].CRosterFileStage);
    }

    [Fact]
    public void RosterCardRead_Rows_AnswerWordingKeysAndLabels()
    {
        tRosterCardPort.TRosterCardRows.Add(Conduct.TInterface.TRosterRowCreate(
            TRosterFirst, true, TimeSpan.FromSeconds(3), false, 0, "Done", false, false, false));
        tRosterCardPort.TRosterCardRows.Add(Conduct.TInterface.TRosterRowCreate(
            TRosterSecond, false, TimeSpan.Zero, true, 0, "Running", true, true, true));
        tRosterCardPort.TRosterCardRows.Add(Conduct.TInterface.TRosterRowCreate(
            TRosterThird, false, TimeSpan.Zero, false, 0.5, "Failed", true, false, true));

        (_, IReadOnlyList<CRosterFile> files) =
            tRosterCardGate.CRosterCardRead(TRosterBatch, TRosterAnswerCreate(), TRosterRunner, _ => false);
        CRosterRow done = files[0].CRosterFileRows[0];
        CRosterRow running = files[0].CRosterFileRows[1];
        CRosterRow failed = files[1].CRosterFileRows[0];

        Assert.Equal("Roster.Priority.High", done.CRosterRowPriority);
        Assert.Equal("Roster.Priority.Normal", running.CRosterRowPriority);
        Assert.Equal(TimeSpan.FromSeconds(3), done.CRosterRowLength);
        Assert.Equal("-", done.CRosterRowProgress);
        Assert.StartsWith("0", running.CRosterRowProgress);
        Assert.StartsWith("50", failed.CRosterRowProgress);
        Assert.Equal("Roster.State.Done", done.CRosterRowState);
        Assert.Equal("Done", done.CRosterRowTone);
        Assert.Equal("Running", running.CRosterRowTone);
        Assert.Null(done.CRosterRowOwner);
        Assert.Equal("Roster.Owner.ThisTab", running.CRosterRowOwner);
        Assert.Equal("Roster.Owner.OtherTab", failed.CRosterRowOwner);
    }

    [Fact]
    public void RosterCardRead_ClaimedInAnotherProcess_AnswersOtherWindow()
    {
        tRosterCardPort.TRosterCardRows.Add(Conduct.TInterface.TRosterRowCreate(
            TRosterFirst, false, TimeSpan.Zero, true, 0, "Running", true, false, false));

        (_, IReadOnlyList<CRosterFile> files) =
            tRosterCardGate.CRosterCardRead(TRosterBatch, TRosterAnswerCreate(), TRosterRunner, _ => false);

        Assert.Equal("Roster.Owner.OtherWindow", Assert.Single(files[0].CRosterFileRows).CRosterRowOwner);
    }

    private void TRosterRowsAdd(params Guid[] ids)
    {
        foreach (Guid id in ids)
        {
            tRosterCardPort.TRosterCardRows.Add(Conduct.TInterface.TRosterRowCreate(
                id, false, TimeSpan.Zero, false, 0, "Pending", false, false, false));
        }
    }

    private static CLineageAnswer TRosterAnswerCreate() =>
        new(
            [
                new CLineageEntry(TRosterBatch, Guid.NewGuid(), "one", "one", [TRosterFirst, TRosterSecond], null),
                new CLineageEntry(TRosterOther, Guid.NewGuid(), "foreign", null, [TRosterForeign], null),
                new CLineageEntry(TRosterBatch, Guid.NewGuid(), "two", "two", [TRosterThird], null)
            ],
            [
                TRosterBatchCreate(TRosterOther, [TRosterForeign]),
                TRosterBatchCreate(TRosterBatch, [TRosterFirst, TRosterSecond, TRosterThird])
            ],
            [
                TRosterStepCreate(TRosterFirst),
                TRosterStepCreate(TRosterSecond),
                TRosterStepCreate(TRosterThird),
                TRosterStepCreate(TRosterForeign)
            ]);

    private static CLineageBatch TRosterBatchCreate(Guid id, IReadOnlyList<Guid> ids) =>
        new(id, new HashSet<Guid> { TRosterSecond }, 2, DateTimeOffset.UnixEpoch, [], [], null, null, 0, 0, ids, false);

    private static CLineageStep TRosterStepCreate(Guid id) =>
        new(id, TRosterBatch, Guid.Empty, string.Empty, "Edit", true, null, null, null, string.Empty, string.Empty);
}
