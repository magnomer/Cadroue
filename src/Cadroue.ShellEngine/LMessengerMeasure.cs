using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LMessenger
{
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

    public static void LMessengerMeasureCancel() => LSubsidiary.LSubsidiaryCancel();
}
