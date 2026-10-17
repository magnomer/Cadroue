using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The merge dispatch: the preset's encoding is read by its name, the group records and the priority are built here,
// and every warning text is worded here for the warning sink the host hands in; which groups run is the gate's.
public sealed class LMergeOutlet : LMergePort
{
    private readonly Action<string> _lMergeOutletWarning;

    public LMergeOutlet(Action<string> lWarning)
    {
        ArgumentNullException.ThrowIfNull(lWarning);
        _lMergeOutletWarning = lWarning;
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
        return LMessenger.LMessengerMergeDescribe(
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
            _lMergeOutletWarning(
                $"Merge skipped group '{lName}': "
                + $"{lLocked} of {lCount} file(s) still in the worklist");
        }
    }

    public void LMergeHeldRecord(Guid lSourceTab) =>
        _lMergeOutletWarning(
            "Merge held relayed files in tab "
            + $"'{LMessenger.LMessengerTitleSource?.Invoke(lSourceTab) ?? string.Empty}': "
            + "no valid export preset is selected");
}
