using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The roster card's gate: one batch's files and rows, read in one engine call. CRoster holds one per roster.
// It answers which rows a card holds and what each shows; the owner keeps the wording and the brushes.
public sealed class CRosterCard
{
    private readonly LRosterCardPort _cRosterCardPort;

    internal CRosterCard(LRosterCardPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cRosterCardPort = port;
    }

    // The batch's lineage record and its files in lineage order; a row is selected when the owner's check says so.
    public (CLineageBatch, IReadOnlyList<CRosterFile>) CRosterCardRead(
        Guid batch, CLineageAnswer answer, Guid runner, Func<Guid, bool> selected)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(selected);
        CLineageBatch record = answer.CLineageBatches.First(item => item.CLineageBatchId == batch);
        CLineageEntry[] entries = answer.CLineageEntries.Where(entry => entry.CLineageEntryBatch == batch).ToArray();
        var rows = new Dictionary<Guid, LRosterRow>();
        foreach (LRosterRow row in _cRosterCardPort.LRosterCardRead(
                     entries.SelectMany(entry => entry.CLineageEntryIds).ToArray(), runner))
        {
            rows.TryAdd(row.LRosterRowId, row);
        }

        var steps = new Dictionary<Guid, CLineageStep>();
        foreach (CLineageStep step in answer.CLineageSteps)
        {
            steps.TryAdd(step.CLineageStepId, step);
        }

        return (record, entries
            .Select(entry => CRosterFile.CRosterFileCreate(entry, rows, steps, record.CLineageBatchStages, selected))
            .ToArray());
    }
}

// One lineage file on a card: staged when its last step is a stage of the batch.
public sealed record CRosterFile(
    CLineageEntry CRosterFileEntry,
    bool CRosterFileStage,
    IReadOnlyList<CRosterRow> CRosterFileRows)
{
    internal static CRosterFile CRosterFileCreate(
        CLineageEntry entry,
        IReadOnlyDictionary<Guid, LRosterRow> rows,
        IReadOnlyDictionary<Guid, CLineageStep> steps,
        IReadOnlySet<Guid> stages,
        Func<Guid, bool> selected)
    {
        IReadOnlyList<Guid> ids = entry.CLineageEntryIds;
        return new CRosterFile(
            entry,
            ids.Count > 0 && stages.Contains(ids[^1]),
            ids
                .Select((id, index) => (id, last: index == ids.Count - 1))
                .Where(step => rows.ContainsKey(step.id))
                .Select(step => CRosterRow.CRosterRowCreate(
                    rows[step.id], steps[step.id], step.last, stages.Contains(step.id), selected(step.id)))
                .ToArray());
    }
}

// One step row: wording keys for priority, state and owner (no owner when nobody claims it), a ready progress label,
// and the state token the owner picks its brush by.
public sealed record CRosterRow(
    Guid CRosterRowId,
    CLineageStep CRosterRowStep,
    bool CRosterRowLast,
    bool CRosterRowStage,
    bool CRosterRowSelected,
    string CRosterRowPriority,
    TimeSpan CRosterRowLength,
    string CRosterRowProgress,
    string CRosterRowState,
    string CRosterRowTone,
    string? CRosterRowOwner)
{
    internal static CRosterRow CRosterRowCreate(
        LRosterRow row, CLineageStep step, bool last, bool stage, bool selected)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new CRosterRow(
            row.LRosterRowId,
            step,
            last,
            stage,
            selected,
            row.LRosterRowHigh ? "Roster.Priority.High" : "Roster.Priority.Normal",
            row.LRosterRowLength,
            row.LRosterRowRunning || row.LRosterRowProgress > 0 ? $"{row.LRosterRowProgress:P0}" : "-",
            row.LRosterRowToken switch
            {
                "Pending" => "Roster.State.Pending",
                "Running" => "Roster.State.Running",
                "Done" => "Roster.State.Done",
                "Failed" => "Roster.State.Failed",
                "Unresolved" => "Roster.State.Unresolved",
                "Partial" => "Roster.State.Partial",
                "Blocked" => "Roster.State.Blocked",
                "Cancelled" => "Roster.State.Cancelled",
                _ => throw new ArgumentOutOfRangeException(nameof(row), row.LRosterRowToken, "Unknown state token.")
            },
            row.LRosterRowToken,
            !row.LRosterRowClaimed
                ? null
                : row.LRosterRowOwned
                    ? "Roster.Owner.ThisTab"
                    : row.LRosterRowLocal ? "Roster.Owner.OtherTab" : "Roster.Owner.OtherWindow");
    }
}
