using System.Collections.ObjectModel;

namespace Cadroue.Core;

public sealed partial class LSchedule
{
    private readonly ObservableCollection<LWorkItem> lScheduleItems = new();
    private readonly Dictionary<Guid, LWorkItem> lScheduleLiveItems = new();

    public LSchedule()
    {
        LScheduleRecords = new ReadOnlyObservableCollection<LWorkItem>(lScheduleItems);
    }

    public static LSchedule LScheduleCurrent { get; } = new();

    public ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    public event Action<LSchedule>? LScheduleChange;

    public int LScheduleDoneCount =>
        lScheduleItems.Count(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone);

    public void LScheduleChangeRaise() => LScheduleChange?.Invoke(this);

    public void LScheduleReload()
    {
        LDepotIndex.LDepotIndexEnsure();
        LScheduleStaleClaim();

        var lScheduleLoaded = new List<LWorkItem>();
        var lScheduleRunningIds = new HashSet<Guid>();
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder))
            {
                if (LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
                {
                    continue;
                }

                if (lDepotFolder == LDepotFolder.LDepotFolderRunning
                    && lScheduleLiveItems.TryGetValue(lWorkRecord.WorkId, out LWorkItem? lWorkLive))
                {
                    lScheduleRunningIds.Add(lWorkRecord.WorkId);
                    lScheduleLoaded.Add(lWorkLive);
                    continue;
                }

                LWorkItem lWorkItem = lWorkRecord.LWorkItemCreate();
                lWorkItem.LWorkStateCurrent = LScheduleStateRead(lDepotFolder, lWorkItem.LWorkStateCurrent);
                lScheduleLoaded.Add(lWorkItem);
            }
        }

        foreach (Guid lScheduleLiveId in lScheduleLiveItems.Keys.ToArray())
        {
            if (!lScheduleRunningIds.Contains(lScheduleLiveId))
            {
                lScheduleLiveItems.Remove(lScheduleLiveId);
            }
        }

        lScheduleItems.Clear();
        foreach (LWorkItem lWorkItem in lScheduleLoaded.OrderBy(lItem => lItem.LWorkCreateTime))
        {
            lScheduleItems.Add(lWorkItem);
        }

        LScheduleChange?.Invoke(this);
    }

    public void LScheduleDurationSet(Guid lWorkId, TimeSpan lWorkDuration)
    {
        if (lWorkDuration <= TimeSpan.Zero)
        {
            return;
        }

        foreach (LWorkItem lWorkItem in lScheduleItems)
        {
            if (lWorkItem.LWorkId != lWorkId || lWorkItem.LWorkEnd > TimeSpan.Zero)
            {
                continue;
            }

            lWorkItem.LWorkEnd = lWorkDuration;
            break;
        }

        string lDepotFilePath = LDepot.LDepotFilePathRead(LDepotFolder.LDepotFolderScheduled, lWorkId);
        if (!File.Exists(lDepotFilePath) || LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
        {
            return;
        }

        lWorkRecord.EndTicks = lWorkDuration.Ticks;
        LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderScheduled);
    }

    public int LScheduleAdd(IReadOnlyList<LWorkItem> lWorkItems)
    {
        if (lWorkItems.Count == 0)
        {
            return 0;
        }

        LDepotIndex.LDepotIndexEnsure();
        int lScheduleAddedCount = 0;
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
            if (!LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
            {
                continue;
            }

            lScheduleAddedCount++;
            lScheduleItems.Add(lWorkItem);
        }

        if (lScheduleAddedCount > 0)
        {
            LScheduleChange?.Invoke(this);
        }

        return lScheduleAddedCount;
    }

    public void LScheduleComplete(LWorkItem lWorkItem, bool lScheduleSucceeded, string lScheduleMessage)
    {
        lScheduleLiveItems.Remove(lWorkItem.LWorkId);
        LDepotFolder lScheduleTarget = lScheduleSucceeded
            ? LDepotFolder.LDepotFolderDone
            : LDepotFolder.LDepotFolderFailed;

        LScheduleMove(lWorkItem.LWorkId, LDepotFolder.LDepotFolderRunning, lScheduleTarget);

        var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
        lWorkRecord.State = lScheduleSucceeded
            ? nameof(LWorkState.LWorkStateDone)
            : nameof(LWorkState.LWorkStateFailed);
        lWorkRecord.Message = lScheduleMessage;
        lWorkRecord.Progress = lScheduleSucceeded ? 1 : lWorkItem.LWorkProgress;
        lWorkRecord.OwnerProcessId = 0;
        LScheduleRecordWrite(lWorkRecord, lScheduleTarget);
    }

    public bool LScheduleRemove(Guid lWorkId)
    {
        bool lScheduleRemoved = false;
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            string lDepotFilePath = LDepot.LDepotFilePathRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath))
            {
                continue;
            }

            try
            {
                File.Delete(lDepotFilePath);
                lScheduleRemoved = true;
            }
            catch (IOException)
            {
            }
        }

        if (lScheduleRemoved)
        {
            LDepotIndex.LDepotIndexRemove(lWorkId);
            LScheduleReload();
        }

        return lScheduleRemoved;
    }

    public int LScheduleDoneClear() =>
        LScheduleFolderClear(new[] { LDepotFolder.LDepotFolderDone });

    public int LScheduleAllClear() =>
        LScheduleFolderClear(new[]
        {
            LDepotFolder.LDepotFolderScheduled,
            LDepotFolder.LDepotFolderDone,
            LDepotFolder.LDepotFolderFailed
        });

    private int LScheduleFolderClear(IReadOnlyList<LDepotFolder> lDepotFolders)
    {
        int lScheduleClearedCount = 0;
        foreach (LDepotFolder lDepotFolder in lDepotFolders)
        {
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder).ToArray())
            {
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
        LScheduleReload();
        return lScheduleClearedCount;
    }

    public IReadOnlyList<LWorkItem> LSchedulePendingRead() =>
        lScheduleItems
            .Where(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStatePending)
            .ToArray();

    public bool LSchedulePendingExist() =>
        LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled).Any();

    private static LWorkState LScheduleStateRead(LDepotFolder lDepotFolder, LWorkState lScheduleFileState) =>
        lDepotFolder switch
        {
            LDepotFolder.LDepotFolderRunning => LWorkState.LWorkStateRunning,
            LDepotFolder.LDepotFolderDone => LWorkState.LWorkStateDone,
            LDepotFolder.LDepotFolderFailed => LWorkState.LWorkStateFailed,
            _ => lScheduleFileState == LWorkState.LWorkStateRunning ? LWorkState.LWorkStatePending : lScheduleFileState
        };

    private static LWorkRecord? LScheduleRecordRead(string lDepotFilePath)
    {
        try
        {
            return LWorkRecord.LWorkRecordParse(File.ReadAllText(lDepotFilePath));
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static bool LScheduleRecordWrite(LWorkRecord lWorkRecord, LDepotFolder lDepotFolder)
    {
        try
        {
            File.WriteAllText(
                LDepot.LDepotFilePathRead(lDepotFolder, lWorkRecord.WorkId),
                lWorkRecord.LWorkRecordJsonCreate());
            LDepotIndex.LDepotIndexSet(lWorkRecord, lDepotFolder);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static bool LScheduleMove(Guid lWorkId, LDepotFolder lDepotFrom, LDepotFolder lDepotTo)
    {
        string lDepotFromPath = LDepot.LDepotFilePathRead(lDepotFrom, lWorkId);
        string lDepotToPath = LDepot.LDepotFilePathRead(lDepotTo, lWorkId);

        try
        {
            File.Move(lDepotFromPath, lDepotToPath, overwrite: false);
            return true;
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
