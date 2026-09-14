using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LSchedule
{
    public void LScheduleCommit(LWorkItem lWorkItem, bool lScheduleSucceeded, string lScheduleMessage)
    {
        lScheduleLiveItems.Remove(lWorkItem.LWorkId);
        LWorkState lScheduleState = lWorkItem.LWorkStateCurrent;
        LDepotFolder lScheduleTarget = LScheduleFolderResolve(lScheduleState, lScheduleSucceeded);

        var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
        lWorkRecord.LWorkStateName = lScheduleState.ToString();
        lWorkRecord.LWorkMessage = lScheduleMessage;
        lWorkRecord.LWorkProgress = lScheduleSucceeded ? 1 : lWorkItem.LWorkProgress;
        if (!LScheduleStore.LScheduleRecordMove(lWorkRecord, LDepotFolder.LDepotFolderRunning, lScheduleTarget))
        {
            LTraceLog.LTraceWarningRecord(
                $"Schedule: work '{lWorkItem.LWorkOutputName}' [{LScheduleIdShorten(lWorkItem.LWorkId)}] " +
                $"could not be filed as {lScheduleTarget}; it stays running and is retried on the next scan");
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Schedule: work '{lWorkItem.LWorkOutputName}' [{LScheduleIdShorten(lWorkItem.LWorkId)}] " +
            $"committed as {lScheduleState}");
    }

    public bool LScheduleItemCancel(LWorkItem lWorkItem)
    {
        lScheduleLiveItems.Remove(lWorkItem.LWorkId);
        LSchedulePartialRemove(LWorkRecord.LWorkRecordCreate(lWorkItem));

        LDepotFolder lCancelSource = lWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning
            ? LDepotFolder.LDepotFolderRunning
            : LDepotFolder.LDepotFolderScheduled;
        var lWorkRecord = LWorkRecord.LWorkRecordCreate(lWorkItem);
        LScheduleOwnerClear(lWorkRecord);
        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateCancelled);
        lWorkRecord.LWorkProgress = 0;
        lWorkRecord.LWorkMessage = string.Empty;
        if (!LScheduleStore.LScheduleRecordMove(lWorkRecord, lCancelSource, LDepotFolder.LDepotFolderCancelled))
        {
            LTraceLog.LTraceWarningRecord(
                $"Schedule: work '{lWorkItem.LWorkOutputName}' [{LScheduleIdShorten(lWorkItem.LWorkId)}] " +
                "could not be cancelled");
            return false;
        }

        LScheduleLoad();
        LTraceLog.LTraceInfoRecord(
            $"Schedule: work '{lWorkItem.LWorkOutputName}' [{LScheduleIdShorten(lWorkItem.LWorkId)}] cancelled");
        return true;
    }

    public bool LScheduleItemReset(Guid lWorkId)
    {
        foreach (LDepotFolder lDepotFolder in new[]
            {
                LDepotFolder.LDepotFolderCancelled,
                LDepotFolder.LDepotFolderFailed
            })
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath)
                || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
            {
                continue;
            }

            LScheduleOwnerClear(lWorkRecord);
            lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStatePending);
            lWorkRecord.LWorkAttemptCount = 0;
            lWorkRecord.LWorkRecoverCount = 0;
            lWorkRecord.LWorkProgress = 0;
            lWorkRecord.LWorkMessage = string.Empty;
            if (!LScheduleStore.LScheduleRecordMove(lWorkRecord, lDepotFolder, LDepotFolder.LDepotFolderScheduled))
            {
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkId)}] " +
                    "could not be reset");
                return false;
            }

            LScheduleLoad();
            LTraceLog.LTraceInfoRecord(
                $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LScheduleIdShorten(lWorkId)}] reset to pending");
            return true;
        }

        return false;
    }

    private static LDepotFolder LScheduleFolderResolve(LWorkState lWorkState, bool lScheduleSucceeded) =>
        lWorkState switch
        {
            LWorkState.LWorkStatePartial => LDepotFolder.LDepotFolderDone,
            LWorkState.LWorkStateUnresolved => LDepotFolder.LDepotFolderFailed,
            LWorkState.LWorkStateBlocked => LDepotFolder.LDepotFolderFailed,
            _ => lScheduleSucceeded ? LDepotFolder.LDepotFolderDone : LDepotFolder.LDepotFolderFailed
        };
}
