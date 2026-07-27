using System.Collections.ObjectModel;

namespace Cadroue.Core;

public sealed class LSchedule
{
    private readonly ObservableCollection<LWorkItem> lScheduleItems = new();

    public LSchedule()
    {
        LScheduleRecords = new ReadOnlyObservableCollection<LWorkItem>(lScheduleItems);
    }

    public static LSchedule LScheduleCurrent { get; } = new();

    public ReadOnlyObservableCollection<LWorkItem> LScheduleRecords { get; }

    public event Action<LSchedule>? LScheduleChange;

    public bool LScheduleRunning { get; private set; }

    public int LScheduleDoneCount =>
        lScheduleItems.Count(lWorkItem => lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateDone);

    public void LScheduleStart()
    {
        if (LScheduleRunning)
        {
            return;
        }

        LScheduleRunning = true;
        LScheduleChange?.Invoke(this);
    }

    public void LSchedulePause()
    {
        if (!LScheduleRunning)
        {
            return;
        }

        LScheduleRunning = false;
        LScheduleChange?.Invoke(this);
    }

    public void LScheduleReload()
    {
        LDepotIndex.LDepotIndexEnsure();

        var lScheduleLoaded = new List<LWorkItem>();
        foreach (LDepotFolder lDepotFolder in Enum.GetValues<LDepotFolder>())
        {
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder))
            {
                if (LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
                {
                    continue;
                }

                LWorkItem lWorkItem = lWorkRecord.LWorkItemCreate();
                lWorkItem.LWorkStateCurrent = LScheduleStateRead(lDepotFolder, lWorkItem.LWorkStateCurrent);
                lScheduleLoaded.Add(lWorkItem);
            }
        }

        lScheduleItems.Clear();
        foreach (LWorkItem lWorkItem in lScheduleLoaded.OrderBy(lItem => lItem.LWorkCreateTime))
        {
            lScheduleItems.Add(lWorkItem);
        }

        LScheduleChange?.Invoke(this);
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
            if (LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderScheduled))
            {
                lScheduleAddedCount++;
            }
        }

        LScheduleReload();
        return lScheduleAddedCount;
    }

    public LWorkItem? LScheduleClaim()
    {
        LDepotIndex.LDepotIndexEnsure();

        var lScheduleCandidates = new List<LWorkRecord>();
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderScheduled))
        {
            if (LScheduleRecordRead(lDepotFilePath) is { } lWorkRecord)
            {
                lScheduleCandidates.Add(lWorkRecord);
            }
        }

        IEnumerable<LWorkRecord> lScheduleOrdered = lScheduleCandidates
            .OrderByDescending(lWorkRecord => lWorkRecord.Priority == nameof(LWorkPriority.LWorkPriorityHigh))
            .ThenBy(lWorkRecord => lWorkRecord.CreateTime);

        foreach (LWorkRecord lWorkRecord in lScheduleOrdered)
        {
            if (!LScheduleMove(lWorkRecord.WorkId, LDepotFolder.LDepotFolderScheduled, LDepotFolder.LDepotFolderRunning))
            {
                continue;
            }

            lWorkRecord.State = nameof(LWorkState.LWorkStateRunning);
            lWorkRecord.OwnerProcessId = Environment.ProcessId;
            LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderRunning);
            return lWorkRecord.LWorkItemCreate();
        }

        return null;
    }

    public void LScheduleComplete(LWorkItem lWorkItem, bool lScheduleSucceeded, string lScheduleMessage)
    {
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

    public int LScheduleCancel()
    {
        LScheduleRunning = false;
        LDepotIndex.LDepotIndexEnsure();

        int lScheduleReleasedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            if (LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || lWorkRecord.OwnerProcessId != Environment.ProcessId)
            {
                continue;
            }

            if (!LScheduleMove(lWorkRecord.WorkId, LDepotFolder.LDepotFolderRunning, LDepotFolder.LDepotFolderScheduled))
            {
                continue;
            }

            lWorkRecord.State = nameof(LWorkState.LWorkStatePending);
            lWorkRecord.OwnerProcessId = 0;
            lWorkRecord.Progress = 0;
            LScheduleRecordWrite(lWorkRecord, LDepotFolder.LDepotFolderScheduled);
            lScheduleReleasedCount++;
        }

        LScheduleReload();
        return lScheduleReleasedCount;
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

    public void LScheduleClear()
    {
        foreach (LDepotFolder lDepotFolder in new[] { LDepotFolder.LDepotFolderDone, LDepotFolder.LDepotFolderFailed })
        {
            foreach (string lDepotFilePath in LDepot.LDepotFilesRead(lDepotFolder).ToArray())
            {
                try
                {
                    File.Delete(lDepotFilePath);
                }
                catch (IOException)
                {
                }
            }
        }

        LDepotIndex.LDepotIndexRebuild();
        LScheduleReload();
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
