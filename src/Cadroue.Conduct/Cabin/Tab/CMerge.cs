using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The merge runs' decisions: whether the preset lets a run go, which groups run, which are skipped for files still in
// the worklist, and the batch each unlocked source arrived with. One per atelier.
public sealed class CMerge
{
    private readonly LMergePort _cMergePort;

    internal CMerge(LMergePort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cMergePort = port;
    }

    // Every runnable group runs at the given priority. Without a valid preset nothing runs and false is answered;
    // otherwise the port is asked once, even with no group to run.
    public bool CMergeRun(
        bool presetValid,
        string presetName,
        IReadOnlyList<(string CMergeName, IReadOnlyList<string> CMergePaths)> groups,
        IReadOnlyList<(string CMergePath, Guid CMergeBatch, bool CMergeLocked)> items,
        bool urgent,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(items);
        if (!presetValid)
        {
            return false;
        }

        (var runs, var skips) = CMergeGroupsResolve(groups, items, Guid.Empty);
        _cMergePort.LMergeRun(presetName, runs, skips, urgent, relayTarget, sourceTab, CMergeRelaysRead(items));
        return true;
    }

    // A relayed cohort: given a cohort, only groups whose every path arrived in it run, at normal priority; the count
    // the port queued is answered. Without a valid preset the hold is recorded and zero is answered.
    public int CMergeCohortRun(
        bool presetValid,
        string presetName,
        IReadOnlyList<(string CMergeName, IReadOnlyList<string> CMergePaths)> groups,
        IReadOnlyList<(string CMergePath, Guid CMergeBatch, bool CMergeLocked)> items,
        Guid cohort,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(items);
        if (!presetValid)
        {
            _cMergePort.LMergeHeldRecord(sourceTab);
            return 0;
        }

        (var runs, var skips) = CMergeGroupsResolve(groups, items, cohort);
        return _cMergePort.LMergeRun(
            presetName, runs, skips, false, relayTarget, sourceTab, CMergeRelaysRead(items));
    }

    // The paths a run would take now, group by group; the skipped groups are recorded as a run records them.
    public IReadOnlyList<string> CMergeEligibleRead(
        IReadOnlyList<(string CMergeName, IReadOnlyList<string> CMergePaths)> groups,
        IReadOnlyList<(string CMergePath, Guid CMergeBatch, bool CMergeLocked)> items)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(items);
        (var runs, var skips) = CMergeGroupsResolve(groups, items, Guid.Empty);
        _cMergePort.LMergeSkipRecord(skips);
        return runs.SelectMany(run => run.LMergePaths).ToArray();
    }

    // In group order: outside a non-empty cohort a group is passed over unrecorded; a group holding a locked file is
    // skipped and recorded; an empty group is passed over; every other group runs.
    private static (
        IReadOnlyList<(string LMergeName, IReadOnlyList<string> LMergePaths)> CMergeRuns,
        IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> CMergeSkips)
        CMergeGroupsResolve(
            IReadOnlyList<(string CMergeName, IReadOnlyList<string> CMergePaths)> groups,
            IReadOnlyList<(string CMergePath, Guid CMergeBatch, bool CMergeLocked)> items,
            Guid cohort)
    {
        var runs = new List<(string, IReadOnlyList<string>)>();
        var skips = new List<(string, int, int)>();
        foreach ((string name, IReadOnlyList<string> paths) in groups)
        {
            if (cohort != Guid.Empty && !paths.All(path => CDocket.CDocketFind(items, path)?.CDocketBatch == cohort))
            {
                continue;
            }

            int locked = paths.Count(path => CDocket.CDocketFind(items, path)?.CDocketLocked == true);
            if (locked > 0)
            {
                skips.Add((name, locked, paths.Count));
                continue;
            }

            if (paths.Count > 0)
            {
                runs.Add((name, paths));
            }
        }

        return (runs, skips);
    }

    // Each unlocked item's batch by its path, ignoring case; a later item replaces an earlier one.
    private static IReadOnlyDictionary<string, Guid> CMergeRelaysRead(
        IReadOnlyList<(string CMergePath, Guid CMergeBatch, bool CMergeLocked)> items)
    {
        var relays = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach ((string path, Guid batch) in CDocket.CDocketUnlockedRead(items, null))
        {
            relays[path] = batch;
        }

        return relays;
    }
}
