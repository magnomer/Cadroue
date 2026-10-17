using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The relay's engine side: the delivery seams, each work item turned into the tab it came from and the paths it holds,
// the docket edits, and the media scan and the traces the host hands in.
public sealed class LRelayOutlet : LRelayPort
{
    private readonly Func<string, IReadOnlyList<string>> _lRelayOutletScanner;
    private readonly Func<string, bool> _lRelayOutletMedia;
    private readonly Action<string> _lRelayOutletInfo;
    private readonly Action<string, string?> _lRelayOutletWarning;

    public LRelayOutlet(
        Func<string, IReadOnlyList<string>> lScan,
        Func<string, bool> lMedia,
        Action<string> lInfo,
        Action<string, string?> lWarning)
    {
        ArgumentNullException.ThrowIfNull(lScan);
        ArgumentNullException.ThrowIfNull(lMedia);
        ArgumentNullException.ThrowIfNull(lInfo);
        ArgumentNullException.ThrowIfNull(lWarning);
        _lRelayOutletScanner = lScan;
        _lRelayOutletMedia = lMedia;
        _lRelayOutletInfo = lInfo;
        _lRelayOutletWarning = lWarning;
    }

    public void LRelayAttach(
        Func<Guid, string, Guid, bool> lIntake,
        Func<Guid, string, Guid, bool> lTrack,
        Action<Guid, IReadOnlyList<string>, bool> lDrop,
        Action<Guid, string, Guid> lArrive,
        Action<IReadOnlyList<Guid>> lEvict,
        Action<IReadOnlyList<(string LRelayPath, Guid LRelayBatch, Guid LRelayTab)>> lUnlock,
        Func<IReadOnlyList<(Guid LRelayTab, IReadOnlyList<(string, Guid)> LRelayLocks)>, bool> lClaim,
        Func<Guid, string, Guid, bool> lDeliver)
    {
        LCartographer.LCartographerLockSeam = lAccepted =>
            lClaim(lAccepted.Select(lItem => (LRelayTabRead(lItem), LRelayLocksRead(lItem))).ToArray());
        LCartographer.LCartographerDeliverySeam = new LCartographerDelivery(
            lIntake,
            lTrack,
            (lItem, lForce) => lDrop(LRelayTabRead(lItem), LRelayDropRead(lItem), lForce),
            lArrive,
            lEvict,
            lUnlocks => lUnlock(lUnlocks
                .Select(lUnlockItem => (
                    lUnlockItem.PListPath, lUnlockItem.PListBatch, LRelayTabRead(lUnlockItem.PListOwner)))
                .ToArray()),
            _lRelayOutletMedia);
        LMessenger.LMessengerDeliverSource = lDeliver;
    }

    public bool LRelayEmptyCheck() => LPreference.LPreferenceStateCurrent.LPreferenceRelayEmpty;

    public int LRelayPathsRemove(LDocket lDocket, IReadOnlyList<string> lPaths) => lDocket.LDocketPathsRemove(lPaths);

    public int LRelayRelease(LDocket lDocket, IReadOnlyList<(string, Guid)> lReleases) =>
        lDocket.LDocketRelease(lReleases);

    public bool LRelayClaim(LDocket lDocket, IReadOnlyList<(string, Guid)> lLocks) => lDocket.LDocketClaim(lLocks);

    public (IReadOnlyList<(string, Guid)> LRelayOrphans, IReadOnlyList<string> LRelayDelivered) LRelayDepartedRead(
        LDocket lDocket, IReadOnlySet<Guid> lBatches)
    {
        LDocketEntry[] lDeparted = lDocket.LDocketItemsRead()
            .Where(lItem => lBatches.Contains(lItem.LDocketEntryBatch))
            .ToArray();
        (string, Guid)[] lOrphans = lDeparted
            .Where(lItem => lItem.LDocketEntryLocked && !lItem.LDocketEntryDelivered)
            .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch))
            .ToArray();
        string[] lDelivered = lDeparted
            .Where(lItem => lItem.LDocketEntryDelivered)
            .Select(lItem => lItem.LDocketEntryPath)
            .ToArray();
        return (lOrphans, lDelivered);
    }

    public (int LRelayScanned, bool LRelayListed, int LRelayAdded) LRelayDeliveredAdd(
        LDocket lDocket, string lPath, Guid lBatch, bool lLocked)
    {
        IReadOnlyList<string> lScanned = _lRelayOutletScanner(lPath);
        if (lScanned.Count == 0)
        {
            return (0, false, 0);
        }

        bool lListed = lDocket.LDocketItemFind(lPath) is not null;
        return (lScanned.Count, lListed, lDocket.LDocketDeliveredAdd(lScanned, lBatch, lLocked));
    }

    public void LRelayInfoRecord(string lMessage) => _lRelayOutletInfo(lMessage);

    // A relay warning carries no detail.
    public void LRelayWarningRecord(string lMessage) => _lRelayOutletWarning(lMessage, null);

    // The tab the work came from: its relay source, or that stage's original tab when the plan names it as a stage;
    // Guid.Empty when the work came from no tab.
    private static Guid LRelayTabRead(LWorkItem lItem)
    {
        Guid lSourceTab = lItem.LWorkRelaySource;
        if (lSourceTab == Guid.Empty)
        {
            return Guid.Empty;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lItem.LWorkBatchId, out LCartographerPlanDocument lPlan)
            && lPlan.LCartographerStages.FirstOrDefault(lStage => lStage.LCartographerStageId == lSourceTab)
                is { } lSourceStage)
        {
            lSourceTab = lSourceStage.LCartographerOriginalTab;
        }

        return lSourceTab;
    }

    // The work's source path and its merge sources.
    private static IReadOnlyList<string> LRelayDropRead(LWorkItem lItem)
    {
        var lDropPaths = new List<string> { lItem.LWorkSourcePath };
        lDropPaths.AddRange(lItem.LWorkMergeSources);
        return lDropPaths;
    }

    // The work's source path and each merge source, locked to its batch.
    private static IReadOnlyList<(string, Guid)> LRelayLocksRead(LWorkItem lItem)
    {
        var lLocks = new List<(string, Guid)> { (lItem.LWorkSourcePath, lItem.LWorkBatchId) };
        foreach (string lMergeSource in lItem.LWorkMergeSources)
        {
            lLocks.Add((lMergeSource, lItem.LWorkBatchId));
        }

        return lLocks;
    }
}
