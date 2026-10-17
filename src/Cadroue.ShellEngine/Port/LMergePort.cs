namespace Cadroue.ShellEngine;

public interface LMergePort
{
    // Records the skipped groups, then queues the groups to run; answers how many work items were queued.
    int LMergeRun(
        string lPresetName,
        IReadOnlyList<(string LMergeName, IReadOnlyList<string> LMergePaths)> lGroups,
        IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab,
        IReadOnlyDictionary<string, Guid> lRelays);

    void LMergeSkipRecord(IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped);

    void LMergeHeldRecord(Guid lSourceTab);
}
