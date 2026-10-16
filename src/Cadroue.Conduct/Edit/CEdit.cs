using System;
using System.Collections.Generic;
using System.Linq;
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
    }

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
        if (!presetValid)
        {
            return (CEditVerdict.CEditVerdictMissing, null, Guid.Empty);
        }

        if (!presetSupported)
        {
            return (CEditVerdict.CEditVerdictIncompatible, null, Guid.Empty);
        }

        if (path is null)
        {
            return (CEditVerdict.CEditVerdictEmpty, null, Guid.Empty);
        }

        foreach ((string itemPath, Guid itemBatch, bool itemLocked) in items)
        {
            if (string.Equals(itemPath, path, StringComparison.OrdinalIgnoreCase))
            {
                return itemLocked
                    ? (CEditVerdict.CEditVerdictEmpty, null, Guid.Empty)
                    : (CEditVerdict.CEditVerdictRan, itemPath, itemBatch);
            }
        }

        return (CEditVerdict.CEditVerdictEmpty, null, Guid.Empty);
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
        if (!presetValid)
        {
            return CEditVerdict.CEditVerdictMissing;
        }

        if (!presetSupported)
        {
            return CEditVerdict.CEditVerdictIncompatible;
        }

        _cEditPort.LEditAllRun(
            presetName,
            items
                .Where(item => !item.CEditLocked
                    && (paths is null || paths.Contains(item.CEditPath, StringComparer.OrdinalIgnoreCase)))
                .Select(item => (item.CEditPath, item.CEditBatch))
                .ToArray(),
            relayTarget,
            sourceTab);
        return CEditVerdict.CEditVerdictRan;
    }
}
