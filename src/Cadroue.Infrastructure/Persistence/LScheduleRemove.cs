using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LSchedule
{
    public bool LScheduleRemove(Guid lWorkId)
    {
        bool lScheduleRemoved = LScheduleFileRemove(lWorkId) == LScheduleRemoval.LScheduleRemovalRemoved;
        if (lScheduleRemoved)
        {
            LScheduleLoad();
            LTraceLog.LTraceInfoRecord($"Schedule: work [{LScheduleIdShorten(lWorkId)}] removed");
        }

        return lScheduleRemoved;
    }

    public IReadOnlyList<Guid> LScheduleRemovableRead(IEnumerable<Guid> lWorkIds)
    {
        var lScheduleStates = new Dictionary<Guid, LWorkState>();
        foreach (LWorkItem lWorkItem in lScheduleItems)
        {
            lScheduleStates[lWorkItem.LWorkId] = lWorkItem.LWorkStateCurrent;
        }

        return LScheduleRemovableResolve(lWorkIds, lScheduleStates);
    }

    internal static IReadOnlyList<Guid> LScheduleRemovableResolve(
        IEnumerable<Guid> lWorkIds,
        IReadOnlyDictionary<Guid, LWorkState> lScheduleStates) =>
        lWorkIds
            .Where(lWorkId => lScheduleStates.TryGetValue(lWorkId, out LWorkState lWorkState)
                && lWorkState != LWorkState.LWorkStateRunning)
            .ToArray();

    public IReadOnlyDictionary<Guid, LScheduleRemoval> LScheduleBatchRemove(IEnumerable<Guid> lWorkIds)
    {
        var lScheduleOutcomes = new Dictionary<Guid, LScheduleRemoval>();
        foreach (Guid lWorkId in lWorkIds)
        {
            lScheduleOutcomes[lWorkId] = LScheduleFileRemove(lWorkId);
        }

        int lScheduleRemovedCount = lScheduleOutcomes.Values.Count(
            lScheduleOutcome => lScheduleOutcome == LScheduleRemoval.LScheduleRemovalRemoved);
        if (lScheduleRemovedCount > 0)
        {
            LScheduleLoad();
            LTraceLog.LTraceInfoRecord($"Schedule: removed {lScheduleRemovedCount} work item(s)");
        }

        if (lScheduleRemovedCount < lScheduleOutcomes.Count)
        {
            LTraceLog.LTraceWarningRecord(
                $"Schedule: {lScheduleOutcomes.Count - lScheduleRemovedCount} of {lScheduleOutcomes.Count} " +
                "requested work item(s) stayed in the worklist");
        }

        return lScheduleOutcomes;
    }

    private LScheduleRemoval LScheduleFileRemove(Guid lWorkId)
    {
        LScheduleRemoval lScheduleOutcome = LScheduleRemoval.LScheduleRemovalMissing;
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath))
            {
                continue;
            }

            if (lDepotFolder == LDepotFolder.LDepotFolderRunning
                && LScheduleStore.LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord
                && LSentinel.LSentinelOwnerCheck(
                    lWorkRecord.LWorkOwnerProcess, lWorkRecord.LWorkOwnerStamp, lWorkRecord.LWorkOwnerRunner))
            {
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkId)}] " +
                    "was not removed: it is running under a live owner");
                return LScheduleRemoval.LScheduleRemovalHeld;
            }

            try
            {
                File.Delete(lDepotFilePath);
                lScheduleOutcome = LScheduleRemoval.LScheduleRemovalRemoved;
            }
            catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
            {
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: work [{LScheduleIdShorten(lWorkId)}] could not be removed from {lDepotFolder}: " +
                    lException.Message);
                return LScheduleRemoval.LScheduleRemovalBlocked;
            }
        }

        if (lScheduleOutcome == LScheduleRemoval.LScheduleRemovalRemoved)
        {
            LDepotIndex.LDepotIndexRemove(lWorkId);
        }

        return lScheduleOutcome;
    }

    public int LScheduleDoneClear()
    {
        int lScheduleClearedCount = LScheduleFolderClear(new[] { LDepotFolder.LDepotFolderDone });
        LTraceLog.LTraceInfoRecord($"Schedule: cleared {lScheduleClearedCount} completed work item(s)");
        return lScheduleClearedCount;
    }

    public int LScheduleAllClear()
    {
        int lScheduleClearedCount = LScheduleStaleClear();
        lScheduleClearedCount += LScheduleFolderClear(new[]
        {
            LDepotFolder.LDepotFolderScheduled,
            LDepotFolder.LDepotFolderDone,
            LDepotFolder.LDepotFolderFailed,
            LDepotFolder.LDepotFolderCancelled
        });
        LTraceLog.LTraceInfoRecord($"Schedule: cleared {lScheduleClearedCount} work item(s) (all)");
        return lScheduleClearedCount;
    }

    private int LScheduleStaleClear()
    {
        int lScheduleClearedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            LWorkRecord? lWorkRecord = LScheduleStore.LScheduleRecordRead(lDepotFilePath);
            if (lWorkRecord is null || !LScheduleScopeMatch(lWorkRecord) || !LSentinel.LSentinelStaleCheck(lWorkRecord))
            {
                continue;
            }

            try
            {
                File.Delete(lDepotFilePath);
                lScheduleClearedCount++;
            }
            catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
            {
            }
        }

        return lScheduleClearedCount;
    }

    private int LScheduleFolderClear(IReadOnlyList<LDepotFolder> lDepotFolders)
    {
        int lScheduleClearedCount = 0;
        foreach (LDepotFolder lDepotFolder in lDepotFolders)
        {
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder).ToArray())
            {
                if (LScheduleStore.LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord
                    && !LScheduleScopeMatch(lWorkRecord))
                {
                    continue;
                }

                try
                {
                    File.Delete(lDepotFilePath);
                    lScheduleClearedCount++;
                }
                catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
                {
                }
            }
        }

        LDepotIndex.LDepotIndexRebuild();
        LDepotIndex.LDepotIndexCompact();
        LScheduleLoad();
        return lScheduleClearedCount;
    }
}
