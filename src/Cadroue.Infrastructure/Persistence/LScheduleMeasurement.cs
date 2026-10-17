using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LScheduleMeasurement : LScheduleMeasurementContract
{
    private static readonly LDepotFolder[] lScheduleSourceFolders =
    {
        LDepotFolder.LDepotFolderScheduled,
        LDepotFolder.LDepotFolderRunning,
        LDepotFolder.LDepotFolderCancelled
    };

    private static readonly LDepotFolder[] lScheduleOutputFolders =
    {
        LDepotFolder.LDepotFolderDone,
        LDepotFolder.LDepotFolderFailed
    };

    private readonly LSchedule lScheduleMeasurementOwner;

    public LScheduleMeasurement(LSchedule lSchedule)
    {
        lScheduleMeasurementOwner = lSchedule;
    }

    public void LScheduleDurationSet(Guid lWorkId, TimeSpan lWorkDuration)
    {
        if (lWorkDuration <= TimeSpan.Zero)
        {
            return;
        }

        if (lScheduleMeasurementOwner.LScheduleLiveRead(lWorkId) is { } lWorkClaimed
            && lWorkClaimed.LWorkEnd <= TimeSpan.Zero)
        {
            lWorkClaimed.LWorkEnd = lWorkDuration;
        }

        foreach (LWorkItem lWorkItem in lScheduleMeasurementOwner.LScheduleRecords)
        {
            if (lWorkItem.LWorkId != lWorkId || lWorkItem.LWorkEnd > TimeSpan.Zero)
            {
                continue;
            }

            lWorkItem.LWorkEnd = lWorkDuration;
            lScheduleMeasurementOwner.LScheduleItemRaise(lWorkItem, LScheduleNotice.LScheduleNoticeStatus);
            break;
        }

        LScheduleDurationPersist(lWorkId, lWorkDuration);
    }

    public void LScheduleSourceSet(
        Guid lWorkId,
        TimeSpan lWorkDuration,
        LWorkMedia? lWorkSourceMedia,
        long? lWorkSourceBytes,
        IReadOnlyList<long> lWorkMergeBytes)
    {
        bool lScheduleDurationKnown = lWorkDuration > TimeSpan.Zero;
        void LScheduleApply(LWorkItem lWorkItem)
        {
            lWorkItem.LWorkSourceMeasured = true;
            if (lScheduleDurationKnown && lWorkItem.LWorkEnd <= TimeSpan.Zero)
            {
                lWorkItem.LWorkEnd = lWorkDuration;
            }

            if (lWorkSourceMedia is not null)
            {
                lWorkItem.LWorkSourceMedia = lWorkSourceMedia;
            }

            if (lWorkSourceBytes is not null)
            {
                lWorkItem.LWorkSourceBytes = lWorkSourceBytes;
            }

            if (lWorkMergeBytes.Count > 0)
            {
                lWorkItem.LWorkMergeBytes = lWorkMergeBytes;
            }
        }

        if (lScheduleMeasurementOwner.LScheduleLiveRead(lWorkId) is { } lWorkClaimed)
        {
            LScheduleApply(lWorkClaimed);
        }

        foreach (LWorkItem lWorkItem in lScheduleMeasurementOwner.LScheduleRecords)
        {
            if (lWorkItem.LWorkId != lWorkId)
            {
                continue;
            }

            LScheduleApply(lWorkItem);
            lScheduleMeasurementOwner.LScheduleItemRaise(lWorkItem, LScheduleNotice.LScheduleNoticeStatus);
            break;
        }

        LScheduleSourcePersist(lWorkId, lWorkDuration, lWorkSourceMedia, lWorkSourceBytes, lWorkMergeBytes);
    }

    public void LScheduleBytesSet(Guid lWorkId, long? lWorkSourceBytes, IReadOnlyList<long> lWorkMergeBytes)
    {
        void LScheduleApply(LWorkItem lWorkItem)
        {
            if (lWorkSourceBytes is not null)
            {
                lWorkItem.LWorkSourceBytes = lWorkSourceBytes;
            }

            if (lWorkMergeBytes.Count > 0)
            {
                lWorkItem.LWorkMergeBytes = lWorkMergeBytes;
            }
        }

        if (lScheduleMeasurementOwner.LScheduleLiveRead(lWorkId) is { } lWorkClaimed)
        {
            LScheduleApply(lWorkClaimed);
        }

        foreach (LWorkItem lWorkItem in lScheduleMeasurementOwner.LScheduleRecords)
        {
            if (lWorkItem.LWorkId != lWorkId)
            {
                continue;
            }

            LScheduleApply(lWorkItem);
            lScheduleMeasurementOwner.LScheduleItemRaise(lWorkItem, LScheduleNotice.LScheduleNoticeStatus);
            break;
        }

        LScheduleBytesPersist(lWorkId, lWorkSourceBytes, lWorkMergeBytes);
    }

    public void LScheduleLoudnessSet(Guid lWorkId, double lWorkLoudness)
    {
        foreach (LWorkItem lWorkItem in lScheduleMeasurementOwner.LScheduleRecords)
        {
            if (lWorkItem.LWorkId != lWorkId)
            {
                continue;
            }

            if (lWorkItem.LWorkRunState.LWorkOutputMedia is { } lWorkOutputMedia)
            {
                lWorkItem.LWorkRunState.LWorkOutputMedia = lWorkOutputMedia with { LWorkMediaLoudness = lWorkLoudness };
            }

            lScheduleMeasurementOwner.LScheduleItemRaise(lWorkItem, LScheduleNotice.LScheduleNoticeStatus);
            break;
        }

        LScheduleLoudnessPersist(lWorkId, lWorkLoudness);
    }

    private static void LScheduleDurationPersist(Guid lWorkId, TimeSpan lWorkDuration)
    {
        foreach (LDepotFolder lDepotFolder in lScheduleSourceFolders)
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath)
                || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
            {
                continue;
            }

            if (lWorkRecord.LWorkEndTicks > 0)
            {
                return;
            }

            lWorkRecord.LWorkEndTicks = lWorkDuration.Ticks;
            LScheduleStore.LScheduleRecordSave(lWorkRecord, lDepotFolder);
            return;
        }
    }

    private static void LScheduleSourcePersist(
        Guid lWorkId,
        TimeSpan lWorkDuration,
        LWorkMedia? lWorkSourceMedia,
        long? lWorkSourceBytes,
        IReadOnlyList<long> lWorkMergeBytes)
    {
        foreach (LDepotFolder lDepotFolder in lScheduleSourceFolders)
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath)
                || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
            {
                continue;
            }

            lWorkRecord.LWorkSourceMeasured = true;
            if (lWorkDuration > TimeSpan.Zero && lWorkRecord.LWorkEndTicks <= 0)
            {
                lWorkRecord.LWorkEndTicks = lWorkDuration.Ticks;
            }

            if (lWorkSourceMedia is not null)
            {
                lWorkRecord.LWorkSourceMedia = lWorkSourceMedia;
            }

            if (lWorkSourceBytes is not null)
            {
                lWorkRecord.LWorkSourceBytes = lWorkSourceBytes;
            }

            if (lWorkMergeBytes.Count > 0)
            {
                lWorkRecord.LWorkMergeBytes = lWorkMergeBytes.ToList();
            }

            LScheduleStore.LScheduleRecordSave(lWorkRecord, lDepotFolder);
            return;
        }
    }

    private static void LScheduleBytesPersist(Guid lWorkId, long? lWorkSourceBytes, IReadOnlyList<long> lWorkMergeBytes)
    {
        foreach (LDepotFolder lDepotFolder in lScheduleSourceFolders)
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath)
                || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
            {
                continue;
            }

            if (lWorkSourceBytes is not null)
            {
                lWorkRecord.LWorkSourceBytes = lWorkSourceBytes;
            }

            if (lWorkMergeBytes.Count > 0)
            {
                lWorkRecord.LWorkMergeBytes = lWorkMergeBytes.ToList();
            }

            LScheduleStore.LScheduleRecordSave(lWorkRecord, lDepotFolder);
            return;
        }
    }

    private static void LScheduleLoudnessPersist(Guid lWorkId, double lWorkLoudness)
    {
        foreach (LDepotFolder lDepotFolder in lScheduleOutputFolders)
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath)
                || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord
                || lWorkRecord.LWorkOutputMedia is not { } lWorkRecordMedia)
            {
                continue;
            }

            lWorkRecord.LWorkOutputMedia = lWorkRecordMedia with { LWorkMediaLoudness = lWorkLoudness };
            LScheduleStore.LScheduleRecordSave(lWorkRecord, lDepotFolder);
            return;
        }
    }
}
