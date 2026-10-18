using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The merge dispatch: the preset's encoding is read by its name, the group records and the priority are built here,
// and every warning text is worded here for the rig's warning; which groups run is the gate's.
public sealed class LMergeFacade : LMergePort
{
    private readonly LRigTrace _lMergeFacadeTrace;

    internal LMergeFacade(LRigTrace lTrace)
    {
        ArgumentNullException.ThrowIfNull(lTrace);
        _lMergeFacadeTrace = lTrace;
    }

    public int LMergeRun(
        string lPresetName,
        IReadOnlyList<(string LMergeName, IReadOnlyList<string> LMergePaths)> lGroups,
        IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab,
        IReadOnlyDictionary<string, Guid> lRelays)
    {
        LMergeSkipRecord(lSkipped);
        return LMessengerMerge.LMessengerMergeDescribe(
            lUrgent ? LWorkPriority.LWorkPriorityHigh : LWorkPriority.LWorkPriorityNormal,
            lGroups.Select(lGroup => new LWorkGroup(lGroup.LMergeName, lGroup.LMergePaths)).ToArray(),
            LPresetSelection.LPresetEncodingRead(lPresetName),
            lRelayTarget,
            lSourceTab,
            lRelays);
    }

    public void LMergeSkipRecord(IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped)
    {
        foreach ((string lName, int lLocked, int lCount) in lSkipped)
        {
            _lMergeFacadeTrace.LRigTraceWarning(
                $"Merge skipped group '{lName}': "
                + $"{lLocked} of {lCount} file(s) still in the worklist",
                null);
        }
    }

    public void LMergeHeldRecord(Guid lSourceTab) =>
        _lMergeFacadeTrace.LRigTraceWarning(
            "Merge held relayed files in tab "
            + $"'{LMessenger.LMessengerTitleSource?.Invoke(lSourceTab) ?? string.Empty}': "
            + "no valid export preset is selected",
            null);
}
