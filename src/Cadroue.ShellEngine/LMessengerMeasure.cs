using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
    // Each added item's byte size is read natively and recorded the instant it is added — a cheap
    // Windows file-length read that never waits on anything. Its media figures (probe, keyframe
    // interval, loudness) are then deferred to LSubsidiary, the single serial ffmpeg/ffprobe
    // measurement worker, which yields to running jobs; until it reaches an item those rows show
    // "Measuring". Byte size never waits behind a running job, so it appears at once.
    private static Task LMessengerSourceResolve(IReadOnlyList<LWorkItem> lMessengerItems)
    {
        if (LMessengerScheduleSource?.Invoke() is { } lMessengerSchedule)
        {
            foreach (LWorkItem lMessengerItem in lMessengerItems)
            {
                LMessengerBytesSet(lMessengerSchedule, lMessengerItem);
            }
        }

        LSubsidiary.LSubsidiarySourceDefer(lMessengerItems);
        return Task.CompletedTask;
    }

    private static void LMessengerBytesSet(LScheduleContract lMessengerSchedule, LWorkItem lMessengerItem)
    {
        if (lMessengerItem.LWorkMergeSources.Count > 1)
        {
            var lMessengerMergeBytes = new List<long>(lMessengerItem.LWorkMergeSources.Count);
            foreach (string lMessengerSource in lMessengerItem.LWorkMergeSources)
            {
                lMessengerMergeBytes.Add(LScout.LScoutBytesRead(lMessengerSource) ?? 0);
            }

            lMessengerSchedule.LScheduleBytesSet(lMessengerItem.LWorkId, null, lMessengerMergeBytes);
            return;
        }

        lMessengerSchedule.LScheduleBytesSet(
            lMessengerItem.LWorkId, LScout.LScoutBytesRead(lMessengerItem.LWorkSourcePath), Array.Empty<long>());
    }

    // Abort all background measurement (Clear all): drops everything still queued in LSubsidiary and
    // kills the in-flight ffprobe/ffmpeg child at once.
    public static void LMessengerMeasureCancel() => LSubsidiary.LSubsidiaryCancel();
}
