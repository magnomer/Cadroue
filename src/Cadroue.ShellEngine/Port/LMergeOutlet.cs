namespace Cadroue.ShellEngine;

// The engine's face for the merge gate: every member forwards to the same-named member of the engine's merge facade.
// It rejects a null engine and keeps it for every forward.
public sealed class LMergeOutlet : LMergePort
{
    private readonly LEngine _lMergeOutletEngine;

    public LMergeOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lMergeOutletEngine = lEngine;
    }

    public int LMergeRun(
        string lPresetName,
        IReadOnlyList<(string LMergeName, IReadOnlyList<string> LMergePaths)> lGroups,
        IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab,
        IReadOnlyDictionary<string, Guid> lRelays) =>
        _lMergeOutletEngine.LEngineMerge.LMergeRun(
            lPresetName, lGroups, lSkipped, lUrgent, lRelayTarget, lSourceTab, lRelays);

    public void LMergeSkipRecord(IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped) =>
        _lMergeOutletEngine.LEngineMerge.LMergeSkipRecord(lSkipped);

    public void LMergeHeldRecord(Guid lSourceTab) => _lMergeOutletEngine.LEngineMerge.LMergeHeldRecord(lSourceTab);
}
