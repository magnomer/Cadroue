using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Application;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// Records each docket edit with the index of the docket it reached (its order in TRelayDockets) and every trace; each
// answer is set by the test.
internal sealed class TRelayPort : LRelayPort
{
    public List<string> TRelayCalls { get; } = [];

    public List<LDocket> TRelayDockets { get; } = [];

    public bool TRelayEmpty { get; set; } = true;

    public int TRelayScanned { get; set; } = 1;

    public bool TRelayListed { get; set; }

    public int TRelayAdded { get; set; } = 1;

    public Func<LDocket, bool> TRelayClaimed { get; set; } = _ => true;

    public int TRelayReleased { get; set; }

    public (IReadOnlyList<(string, Guid)>, IReadOnlyList<string>) TRelayDeparted { get; set; } = ([], []);

    public bool TRelayAttached { get; private set; }

    public void LRelayAttach(
        Func<Guid, string, Guid, bool> lIntake,
        Func<Guid, string, Guid, bool> lTrack,
        Action<Guid, IReadOnlyList<string>, bool> lDrop,
        Action<Guid, string, Guid> lArrive,
        Action<IReadOnlyList<Guid>> lEvict,
        Action<IReadOnlyList<(string LRelayPath, Guid LRelayBatch, Guid LRelayTab)>> lUnlock,
        Func<IReadOnlyList<(Guid LRelayTab, IReadOnlyList<(string, Guid)> LRelayLocks)>, bool> lClaim,
        Func<Guid, string, Guid, bool> lDeliver) => TRelayAttached = true;

    public bool LRelayEmptyCheck()
    {
        TRelayCalls.Add("empty");
        return TRelayEmpty;
    }

    public int LRelayPathsRemove(LDocket lDocket, IReadOnlyList<string> lPaths)
    {
        TRelayCalls.Add($"remove {TRelayIndexRead(lDocket)} {string.Join(",", lPaths)}");
        return lPaths.Count;
    }

    public int LRelayRelease(LDocket lDocket, IReadOnlyList<(string, Guid)> lReleases)
    {
        TRelayCalls.Add($"release {TRelayIndexRead(lDocket)} {string.Join(",", lReleases.Select(item => item.Item1))}");
        return TRelayReleased;
    }

    public bool LRelayClaim(LDocket lDocket, IReadOnlyList<(string, Guid)> lLocks)
    {
        TRelayCalls.Add($"claim {TRelayIndexRead(lDocket)} {string.Join(",", lLocks.Select(item => item.Item1))}");
        return TRelayClaimed(lDocket);
    }

    public (IReadOnlyList<(string, Guid)> LRelayOrphans, IReadOnlyList<string> LRelayDelivered) LRelayDepartedRead(
        LDocket lDocket, IReadOnlySet<Guid> lBatches)
    {
        TRelayCalls.Add($"departed {TRelayIndexRead(lDocket)} {lBatches.Count}");
        return TRelayDeparted;
    }

    public (int LRelayScanned, bool LRelayListed, int LRelayAdded) LRelayDeliveredAdd(
        LDocket lDocket, string lPath, Guid lBatch, bool lLocked)
    {
        TRelayCalls.Add($"deliver {TRelayIndexRead(lDocket)} {lPath} {(lLocked ? "locked" : "open")}");
        return (TRelayScanned, TRelayListed, TRelayAdded);
    }

    public void LRelayInfoRecord(string lMessage) => TRelayCalls.Add($"info {lMessage}");

    public void LRelayWarningRecord(string lMessage) => TRelayCalls.Add($"warning {lMessage}");

    private int TRelayIndexRead(LDocket lDocket) => TRelayDockets.IndexOf(lDocket);
}
