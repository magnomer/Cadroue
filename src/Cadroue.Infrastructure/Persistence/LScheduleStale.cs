using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal static class LScheduleStale
{
    internal static int LScheduleStaleClaim()
    {
        LDepotIndex.LDepotIndexCreate();

        int lScheduleReclaimedCount = 0;
        foreach (string lDepotFilePath in LDepot.LDepotFilesRead(LDepotFolder.LDepotFolderRunning).ToArray())
        {
            if (LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || !LSentinel.LSentinelStaleCheck(lWorkRecord))
            {
                continue;
            }

            LSchedule.LSchedulePartialRemove(lWorkRecord);
            string lSchedulePhase = LSchedulePhaseFormat(lWorkRecord.LWorkPhaseName);

            if (lWorkRecord.LWorkRecoverCount >= LSchedule.LScheduleAttemptLimit)
            {
                if (LScheduleFailedSet(
                        lWorkRecord,
                        $"Stopped unexpectedly while {lSchedulePhase} on attempt {lWorkRecord.LWorkRecoverCount}. " +
                        "Not retried again."))
                {
                    lScheduleReclaimedCount++;
                    LTraceLog.LTraceWarningRecord(
                        $"Schedule: work '{lWorkRecord.LWorkOutputName}' " +
                        $"[{LSchedule.LScheduleIdShorten(lWorkRecord.LWorkId)}] " +
                        $"failed: stopped unexpectedly while {lSchedulePhase} " +
                        $"after {lWorkRecord.LWorkRecoverCount} recovery attempt(s)");
                }

                continue;
            }

            lWorkRecord.LWorkRecoverCount++;
            if (LSchedule.LScheduleRecordRelease(
                    lWorkRecord,
                    $"Recovered after an unexpected stop while {lSchedulePhase} " +
                    $"(attempt {lWorkRecord.LWorkRecoverCount})."))
            {
                lScheduleReclaimedCount++;
                LTraceLog.LTraceWarningRecord(
                    $"Schedule: work '{lWorkRecord.LWorkOutputName}' " +
                    $"[{LSchedule.LScheduleIdShorten(lWorkRecord.LWorkId)}] " +
                    $"returned to the queue: stopped unexpectedly while {lSchedulePhase} " +
                    $"(attempt {lWorkRecord.LWorkRecoverCount} of {LSchedule.LScheduleAttemptLimit})");
            }
        }

        return lScheduleReclaimedCount;
    }

    private static string LSchedulePhaseFormat(string lWorkPhase) =>
        Enum.TryParse(lWorkPhase, out LWorkPhase lSchedulePhase) && lSchedulePhase == LWorkPhase.LWorkPhaseEncoding
            ? "being processed"
            : "starting";

    private static bool LScheduleFailedSet(LWorkDocument lWorkRecord, string lScheduleMessage)
    {
        LSchedule.LScheduleOwnerClear(lWorkRecord);
        lWorkRecord.LWorkStateName = nameof(LWorkState.LWorkStateFailed);
        lWorkRecord.LWorkMessage = lScheduleMessage;
        return LScheduleStore.LScheduleRecordMove(
            lWorkRecord,
            LDepotFolder.LDepotFolderRunning,
            LDepotFolder.LDepotFolderFailed);
    }
}
