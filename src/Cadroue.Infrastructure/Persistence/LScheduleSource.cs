using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed partial class LSchedule
{
    private static readonly LDepotFolder[] lScheduleDurationFolders =
    {
        LDepotFolder.LDepotFolderScheduled,
        LDepotFolder.LDepotFolderRunning,
        LDepotFolder.LDepotFolderCancelled
    };

    public void LScheduleDurationSet(Guid lWorkId, TimeSpan lWorkDuration)
    {
        if (lWorkDuration <= TimeSpan.Zero)
        {
            return;
        }

        if (lScheduleLiveItems.TryGetValue(lWorkId, out LWorkItem? lWorkClaimed) && lWorkClaimed.LWorkEnd <= TimeSpan.Zero)
        {
            lWorkClaimed.LWorkEnd = lWorkDuration;
        }

        foreach (LWorkItem lWorkItem in lScheduleItems)
        {
            if (lWorkItem.LWorkId != lWorkId || lWorkItem.LWorkEnd > TimeSpan.Zero)
            {
                continue;
            }

            lWorkItem.LWorkEnd = lWorkDuration;
            LScheduleItemRaise(lWorkItem, LScheduleNotice.LScheduleNoticeStatus);
            break;
        }

        LScheduleDurationPersist(lWorkId, lWorkDuration);
    }

    // Source figures measured once when the file is added to the worklist. Duration is only
    // filled when still unknown (a planned range already set is authoritative); the media
    // snapshot, source bytes, and per-input merge bytes are always recorded so the job run
    // never re-measures the source and a deleted source keeps its figures.
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

        if (lScheduleLiveItems.TryGetValue(lWorkId, out LWorkItem? lWorkClaimed))
        {
            LScheduleApply(lWorkClaimed);
        }

        foreach (LWorkItem lWorkItem in lScheduleItems)
        {
            if (lWorkItem.LWorkId != lWorkId)
            {
                continue;
            }

            LScheduleApply(lWorkItem);
            LScheduleItemRaise(lWorkItem, LScheduleNotice.LScheduleNoticeStatus);
            break;
        }

        LScheduleSourcePersist(lWorkId, lWorkDuration, lWorkSourceMedia, lWorkSourceBytes, lWorkMergeBytes);
    }

    private static void LScheduleSourcePersist(
        Guid lWorkId,
        TimeSpan lWorkDuration,
        LWorkMedia? lWorkSourceMedia,
        long? lWorkSourceBytes,
        IReadOnlyList<long> lWorkMergeBytes)
    {
        foreach (LDepotFolder lDepotFolder in lScheduleDurationFolders)
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath) || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
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

    private static void LScheduleDurationPersist(Guid lWorkId, TimeSpan lWorkDuration)
    {
        foreach (LDepotFolder lDepotFolder in lScheduleDurationFolders)
        {
            string lDepotFilePath = LDepot.LDepotFileRead(lDepotFolder, lWorkId);
            if (!File.Exists(lDepotFilePath) || LScheduleStore.LScheduleRecordRead(lDepotFilePath) is not { } lWorkRecord)
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
}
