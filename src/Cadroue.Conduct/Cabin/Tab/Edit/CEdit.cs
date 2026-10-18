using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// A run's answer: the preset notice the driver raises, nothing to run, or the run went (for the single run: may go).
public enum CEditVerdict
{
    CEditVerdictMissing,
    CEditVerdictIncompatible,
    CEditVerdictEmpty,
    CEditVerdictRan
}

// The edit runs' decisions: the preset verdict, then which docket items run. The preset owner answers whether its
// selection is valid and supported; the gate decides what that means for a run. One per atelier.
public sealed class CEdit
{
    private readonly LEditPort _cEditPort;

    internal CEdit(LEditPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cEditPort = port;
        CEditAudio = new CEditAudio(port);
    }

    // The audio tab's sidecar saves, over the same port.
    public CEditAudio CEditAudio { get; }

    // The single run: a missing preset before an incompatible one, then the first item whose path is the current one,
    // ignoring case, runs only while unlocked; its path and batch are answered. The driver dispatches that item itself
    // until the edit plan can reach a gate.
    public (CEditVerdict CEditVerdict, string? CEditPath, Guid CEditBatch) CEditRun(
        bool presetValid,
        bool presetSupported,
        string? path,
        IReadOnlyList<(string CEditPath, Guid CEditBatch, bool CEditLocked)> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (CEditPresetRead(presetValid, presetSupported) is { } refusal)
        {
            return (refusal, null, Guid.Empty);
        }

        return CDocket.CDocketFind(items, path) is { CDocketLocked: false } item
            ? (CEditVerdict.CEditVerdictRan, item.CDocketPath, item.CDocketBatch)
            : (CEditVerdict.CEditVerdictEmpty, null, Guid.Empty);
    }

    // Every unlocked item runs, or, given paths, every unlocked item whose path is among them, in docket order. An
    // empty pick still runs, as the engine reports what it queued.
    public CEditVerdict CEditAllRun(
        bool presetValid,
        bool presetSupported,
        string presetName,
        IReadOnlyList<(string CEditPath, Guid CEditBatch, bool CEditLocked)> items,
        IReadOnlyList<string>? paths,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (CEditPresetRead(presetValid, presetSupported) is { } refusal)
        {
            return refusal;
        }

        _cEditPort.LEditAllRun(presetName, CDocket.CDocketUnlockedRead(items, paths), relayTarget, sourceTab);
        return CEditVerdict.CEditVerdictRan;
    }

    // A missing preset is told before an incompatible one; a valid, supported preset refuses nothing.
    private static CEditVerdict? CEditPresetRead(bool presetValid, bool presetSupported) =>
        !presetValid
            ? CEditVerdict.CEditVerdictMissing
            : !presetSupported
                ? CEditVerdict.CEditVerdictIncompatible
                : (CEditVerdict?)null;
}
