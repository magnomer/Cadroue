using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LScheduleTransition : LScheduleTransitionContract
{
    private readonly LSchedule lScheduleTransitionOwner;

    public LScheduleTransition(LSchedule lSchedule)
    {
        lScheduleTransitionOwner = lSchedule;
    }

    public void LScheduleCommit(LWorkItem lWorkItem, bool lScheduleSucceeded, string lScheduleMessage)
    {
        lScheduleTransitionOwner.LScheduleLiveRemove(lWorkItem.LWorkId);
        LWorkState lScheduleState = lWorkItem.LWorkRunState.LWorkStateCurrent;
        LDepotFolder lScheduleTarget = LScheduleFolderResolve(lScheduleState, lScheduleSucceeded);

        var lWorkRecord = LWorkDocument.LWorkRecordCreate(lWorkItem);
        lWorkRecord.LWorkStateName = lScheduleState.ToString();
        lWorkRecord.LWorkMessage = lScheduleMessage;
        lWorkRecord.LWorkProgress = lScheduleSucceeded ? 1 : lWorkItem.LWorkRunState.LWorkProgress;
        if (!LScheduleStore.LScheduleRecordMove(lWorkRecord, LDepotFolder.LDepotFolderRunning, lScheduleTarget))
        {
            LTraceLog.LTraceWarningRecord(
                $"Schedule: work '{lWorkItem.LWorkOutputName}' [{LSchedule.LScheduleIdShorten(lWorkItem.LWorkId)}] " +
                $"could not be filed as {lScheduleTarget}; it stays running and is retried on the next scan");
            return;
        }

        LTraceLog.LTraceInfoRecord(
            $"Schedule: work '{lWorkItem.LWorkOutputName}' [{LSchedule.LScheduleIdShorten(lWorkItem.LWorkId)}] " +
            $"committed as {lScheduleState}");
    }

    public bool LScheduleItemCancel(LWorkItem lWorkItem)
    {
        lScheduleTransitionOwner.LScheduleLiveRemove(lWorkItem.LWorkId);
        LSchedulePartialRemove(LWorkDocument.LWorkRecordCreate(lWorkItem));

        LDepotFolder lCancelSource = lWorkItem.LWorkRunState.LWorkStateCurrent == LWorkState.LWorkStateRunning
            ? LDepotFolder.LDepotFolderRunning
            : LDepotFolder.LDepotFolderScheduled;
        var lWorkRecord = LWorkDocument.LWorkRecordCreate(lWorkItem);
        LScheduleLease.LScheduleOwnerClear(lWorkRecord);
        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateCancelled);
        lWorkRecord.LWorkProgress = 0;
        lWorkRecord.LWorkMessage = string.Empty;
        if (!LScheduleStore.LScheduleRecordMove(lWorkRecord, lCancelSource, LDepotFolder.LDepotFolderCancelled))
        {
            LTraceLog.LTraceWarningRecord(
                $"Schedule: work '{lWorkItem.LWorkOutputName}' [{LSchedule.LScheduleIdShorten(lWorkItem.LWorkId)}] " +
                "could not be cancelled");
            return false;
        }

        lScheduleTransitionOwner.LScheduleLoad();
        LTraceLog.LTraceInfoRecord(
            $"Schedule: work '{lWorkItem.LWorkOutputName}' " +
            $"[{LSchedule.LScheduleIdShorten(lWorkItem.LWorkId)}] cancelled");
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

            LSchedulePartialRemove(lWorkRecord);
            LScheduleLease.LScheduleOwnerClear(lWorkRecord);
            lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStatePending);
            lWorkRecord.LWorkAttemptCount = 0;
            lWorkRecord.LWorkRecoverCount = 0;
            lWorkRecord.LWorkRetryCount = 0;
            lWorkRecord.LWorkProgress = 0;
            lWorkRecord.LWorkMessage = string.Empty;
            lWorkRecord.LWorkCreateTime = DateTimeOffset.Now;
            lWorkRecord.LWorkStartTime = null;
            lWorkRecord.LWorkFinishTime = null;
            lWorkRecord.LWorkOutputBytes = null;
            lWorkRecord.LWorkOutputMedia = null;
            if (!LScheduleStore.LScheduleRecordMove(lWorkRecord, lDepotFolder, LDepotFolder.LDepotFolderScheduled))
            {
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LSchedule.LScheduleIdShorten(lWorkId)}] " +
                    "could not be reset");
                return false;
            }

            lScheduleTransitionOwner.LScheduleLoad();
            LTraceLog.LTraceInfoRecord(
                $"Schedule: work '{lWorkRecord.LWorkOutputName}' [{LSchedule.LScheduleIdShorten(lWorkId)}] " +
                "reset to pending");
            return true;
        }

        return false;
    }

    internal static void LSchedulePartialRemove(LWorkDocument lWorkRecord)
    {
        if (string.IsNullOrWhiteSpace(lWorkRecord.LWorkOutputPath)
            || lWorkRecord.LWorkMergeSources.Prepend(lWorkRecord.LWorkSourcePath).Any(lScheduleSource =>
                string.Equals(lScheduleSource, lWorkRecord.LWorkOutputPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        try
        {
            if (File.Exists(lWorkRecord.LWorkOutputPath))
            {
                File.Delete(lWorkRecord.LWorkOutputPath);
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException)
        {
        }
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
