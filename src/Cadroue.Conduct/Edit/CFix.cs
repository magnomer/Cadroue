using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The fix runs' decisions: whether the preset lets a run go, then which docket items run. One per atelier; the clinic
// plan's saves are CFixPlan's.
public sealed class CFix
{
    private readonly LFixPort _cFixPort;

    internal CFix(LFixPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cFixPort = port;
        CFixPlan = new CFixPlan(port);
    }

    // The clinic plan's saves, over the same port.
    public CFixPlan CFixPlan { get; }

    // The single run: the first item whose path is the current one, ignoring case, runs while unlocked. Without a valid
    // preset nothing runs and false is answered; otherwise the port is asked once, even with nothing picked.
    public bool CFixRun(
        bool presetValid,
        string presetName,
        string? path,
        IReadOnlyList<(string CFixPath, Guid CFixBatch, bool CFixLocked)> items,
        bool urgent,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (!presetValid)
        {
            return false;
        }

        _cFixPort.LFixRun(
            presetName,
            CDocket.CDocketFind(items, path) is { CDocketLocked: false } item
                ? [(item.CDocketPath, item.CDocketBatch)]
                : [],
            urgent,
            relayTarget,
            sourceTab);
        return true;
    }

    // Every unlocked item runs, or, given paths, every unlocked item whose path is among them, in docket order, at
    // normal priority. Without a valid preset nothing runs and false is answered.
    public bool CFixAllRun(
        bool presetValid,
        string presetName,
        IReadOnlyList<(string CFixPath, Guid CFixBatch, bool CFixLocked)> items,
        IReadOnlyList<string>? paths,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (!presetValid)
        {
            return false;
        }

        _cFixPort.LFixRun(presetName, CDocket.CDocketUnlockedRead(items, paths), false, relayTarget, sourceTab);
        return true;
    }
}
