using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The relay across every tab: a delivered file lands in its target tab's list, work claims and frees its source files
// in the tab it came from, and a batch that leaves the worklist takes its delivered files along. Each tab is keyed by
// its id. The window hands the open tabs as one read, each with its list (none for a tab without one), and the title
// a trace names a tab by; the dockets are reached only through the relay port, and every trace leaves through it.
public sealed class CRelay
{
    private readonly LRelayPort _cRelayPort;
    private readonly CUsher _cRelayUsher;
    private Func<IReadOnlyList<(Guid CRelayTab, CList? CRelayList)>>? _cRelayTabs;
    private Func<Guid, string>? _cRelayTitle;
    private Action<Guid, string, Guid>? _cRelayArrival;

    internal CRelay(LRelayPort port, CUsher usher)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(usher);
        _cRelayPort = port;
        _cRelayUsher = usher;
    }

    // The open tabs, read at every call, each tab's title by id, and the tab side's take of an arrival. From here on
    // the engine's delivery seams reach this gate; before it, the relay finds no tab.
    public void CRelayAttach(
        Func<IReadOnlyList<(Guid CRelayTab, CList? CRelayList)>> tabs,
        Func<Guid, string> title,
        Action<Guid, string, Guid> arrival)
    {
        _cRelayTabs = tabs;
        _cRelayTitle = title;
        _cRelayArrival = arrival;
        _cRelayPort.LRelayAttach(
            CRelayDeliveredAdd,
            CRelayDeliveredCommit,
            CRelayDeliveredRemove,
            arrival,
            CRelayBatchRemove,
            CRelaySourceRelease,
            CRelaySourceClaim,
            CRelayDeliverRun);
    }

    // Hands a delivered file to the tab for the user: it lands unlocked.
    public bool CRelayDeliveredAdd(Guid tab, string path, Guid batch) => CRelayDeliveredApply(tab, path, batch, false);

    // Registers a delivered file an automatic stage will process: it lands locked to its batch.
    public bool CRelayDeliveredCommit(Guid tab, string path, Guid batch) =>
        CRelayDeliveredApply(tab, path, batch, true);

    // After delivery the source files leave the tab they came from, when the relay empties its sources or the drop is
    // forced.
    public void CRelayDeliveredRemove(Guid source, IReadOnlyList<string> paths, bool force)
    {
        if (!force && !_cRelayPort.LRelayEmptyCheck())
        {
            return;
        }

        if (CRelayTabFind(CRelayTabsRead(), source)?.CRelayList is not { } list)
        {
            return;
        }

        int drained = _cRelayPort.LRelayPathsRemove(list.CListDocket, paths);
        if (drained > 0)
        {
            _cRelayPort.LRelayInfoRecord(
                $"Relay removed {drained} source file(s) from tab '{CRelayTitleRead(source)}' after delivery");
        }
    }

    // In every tab, the batches' locked sources left unfinished are unlocked and their delivered files removed.
    public void CRelayBatchRemove(IReadOnlyList<Guid> batches)
    {
        if (_cRelayTabs is null)
        {
            return;
        }

        var removed = batches.ToHashSet();
        foreach ((Guid tab, CList? list) in CRelayTabsRead())
        {
            if (list is null)
            {
                continue;
            }

            (IReadOnlyList<(string, Guid)> orphans, IReadOnlyList<string> delivered) =
                _cRelayPort.LRelayDepartedRead(list.CListDocket, removed);
            CRelayDepartedRemove(tab, list, orphans, delivered);
        }
    }

    // Each source is unlocked in the tab it came from; a source whose tab is gone is unlocked in every tab.
    public void CRelaySourceRelease(IReadOnlyList<(string CRelayPath, Guid CRelayBatch, Guid CRelayTab)> unlocks)
    {
        if (unlocks.Count == 0 || _cRelayTabs is null)
        {
            return;
        }

        IReadOnlyList<(Guid CRelayTab, CList? CRelayList)> open = CRelayTabsRead();
        var owned = new List<(CList?, (string, Guid)[])>();
        var orphans = new List<(string, Guid)>();
        foreach (var group in unlocks.GroupBy(unlock => CRelayTabFind(open, unlock.CRelayTab)))
        {
            (string, Guid)[] releases = group
                .Select(unlock => (unlock.CRelayPath, unlock.CRelayBatch))
                .Distinct()
                .ToArray();
            if (group.Key is { } found)
            {
                owned.Add((found.CRelayList, releases));
                continue;
            }

            orphans.AddRange(releases);
        }

        CRelayReleasesApply(open, owned, orphans);
    }

    // Each work item's locks are claimed in the tab it came from; false when another owner already holds one.
    public bool CRelaySourceClaim(IReadOnlyList<(Guid CRelayTab, IReadOnlyList<(string, Guid)> CRelayLocks)> accepted)
    {
        if (_cRelayTabs is null)
        {
            return true;
        }

        var claims = new List<(CList, (string, Guid)[])>();
        IReadOnlyList<(Guid CRelayTab, CList? CRelayList)> open = CRelayTabsRead();
        foreach (var group in accepted.GroupBy(item => CRelayTabFind(open, item.CRelayTab)))
        {
            if (group.Key?.CRelayList is not { } list)
            {
                continue;
            }

            claims.Add((list, group.SelectMany(item => item.CRelayLocks).Distinct().ToArray()));
        }

        bool claimed = true;
        foreach ((CList list, (string, Guid)[] locks) in claims)
        {
            claimed &= _cRelayPort.LRelayClaim(list.CListDocket, locks);
        }

        return claimed;
    }

    // A funnel's delivery: the file lands for the user, then the tab side takes its arrival.
    public bool CRelayDeliverRun(Guid target, string path, Guid cohort)
    {
        if (!CRelayDeliveredAdd(target, path, cohort))
        {
            return false;
        }

        _cRelayArrival?.Invoke(target, path, cohort);
        return true;
    }

    private static (Guid CRelayTab, CList? CRelayList)? CRelayTabFind(
        IReadOnlyList<(Guid CRelayTab, CList? CRelayList)> open, Guid tab)
    {
        foreach ((Guid CRelayTab, CList? CRelayList) entry in open)
        {
            if (entry.CRelayTab == tab)
            {
                return entry;
            }
        }

        return null;
    }

    private static string CRelayLeafRead(string path) => path[(path.LastIndexOfAny(['\\', '/', ':']) + 1)..];

    private IReadOnlyList<(Guid CRelayTab, CList? CRelayList)> CRelayTabsRead() => _cRelayTabs?.Invoke() ?? [];

    private string CRelayTitleRead(Guid tab) => _cRelayTitle?.Invoke(tab) ?? string.Empty;

    private void CRelayDepartedRemove(
        Guid tab, CList list, IReadOnlyList<(string, Guid)> orphans, IReadOnlyList<string> delivered)
    {
        int released = orphans.Count == 0 ? 0 : _cRelayPort.LRelayRelease(list.CListDocket, orphans);
        if (released > 0)
        {
            _cRelayPort.LRelayInfoRecord(
                $"Relay unlocked {released} source file(s) in tab '{CRelayTitleRead(tab)}' "
                + "after their work left the worklist unfinished");
        }

        if (delivered.Count == 0)
        {
            return;
        }

        _cRelayPort.LRelayPathsRemove(list.CListDocket, delivered);
        _cRelayPort.LRelayInfoRecord(
            $"Relay removed {delivered.Count} delivered file(s) from tab '{CRelayTitleRead(tab)}' "
            + "after their batch left the worklist");
    }

    private void CRelayReleasesApply(
        IReadOnlyList<(Guid CRelayTab, CList? CRelayList)> open,
        IReadOnlyList<(CList?, (string, Guid)[])> owned,
        List<(string, Guid)> orphans)
    {
        foreach ((CList? list, (string, Guid)[] releases) in owned)
        {
            if (list is not null)
            {
                _cRelayPort.LRelayRelease(list.CListDocket, releases);
            }
        }

        if (orphans.Count == 0)
        {
            return;
        }

        foreach ((Guid _, CList? list) in open)
        {
            if (list is not null)
            {
                _cRelayPort.LRelayRelease(list.CListDocket, orphans);
            }
        }
    }

    private bool CRelayDeliveredApply(Guid tab, string path, Guid batch, bool locked)
    {
        if (_cRelayTabs is null)
        {
            _cRelayPort.LRelayWarningRecord($"Relay skipped '{CRelayLeafRead(path)}': the destination tab is gone");
            return false;
        }

        string name = _cRelayUsher.CUsherNameRead(path);
        if (CRelayTabFind(CRelayTabsRead(), tab)?.CRelayList is not { } list)
        {
            _cRelayPort.LRelayWarningRecord($"Relay skipped '{name}': the destination tab is gone");
            return false;
        }

        (int scanned, bool listed, int added) = _cRelayPort.LRelayDeliveredAdd(list.CListDocket, path, batch, locked);
        if (scanned == 0)
        {
            _cRelayPort.LRelayWarningRecord($"Relay skipped '{name}': the destination tab rejected the output");
            return false;
        }

        if (added == 0)
        {
            _cRelayPort.LRelayWarningRecord(
                $"Relay skipped '{name}': tab '{CRelayTitleRead(tab)}' still holds it for other work");
            return false;
        }

        _cRelayPort.LRelayInfoRecord(listed
            ? $"Relay took over '{name}' already listed in tab '{CRelayTitleRead(tab)}'"
            : $"Relay added '{name}' to tab '{CRelayTitleRead(tab)}'");
        return true;
    }
}
