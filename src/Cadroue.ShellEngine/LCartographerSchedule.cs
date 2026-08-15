using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.ShellEngine;

public static partial class LCartographer
{
    private static readonly HashSet<Guid> lCartographerScheduledBatches = new();
    private static bool lCartographerWatching;

    public static LScheduleContract? LCartographerScheduleContract { get; set; }

    public static Action<IReadOnlyList<LWorkItem>, Guid>? LCartographerLockSeam { get; set; }

    private static IReadOnlyList<LWorkItem> LCartographerScheduleRead() =>
        LCartographerScheduleContract?.LScheduleRecords ?? (IReadOnlyList<LWorkItem>)Array.Empty<LWorkItem>();

    public static void LCartographerStart()
    {
        if (lCartographerWatching || LCartographerScheduleContract is not { } lCartographerSchedule)
        {
            return;
        }

        lCartographerWatching = true;
        lCartographerScheduledBatches.UnionWith(lCartographerSchedule.LScheduleRecords
            .Select(lCartographerItem => lCartographerItem.LWorkBatchId)
            .Where(lCartographerBatch => lCartographerBatch != Guid.Empty));
        lCartographerSchedule.LScheduleChange += LCartographerScheduleHandle;
        LCartographerDispatch(lCartographerSchedule.LScheduleRecords);
    }

    public static int LCartographerAccept(
        IReadOnlyList<LWorkItem> lCartographerItems,
        Guid lCartographerTarget = default,
        Guid lCartographerSource = default,
        LCartographerPlanRecord? lCartographerPreparedPlan = null)
    {
        if (lCartographerItems.Count == 0 || LCartographerScheduleContract is not { } lCartographerSchedule)
        {
            return 0;
        }

        LCartographerRelaySet(lCartographerItems, lCartographerTarget, lCartographerSource, lCartographerPreparedPlan);

        IReadOnlyList<LWorkItem> lCartographerAccepted = lCartographerSchedule.LScheduleAcceptedAdd(lCartographerItems);
        LCartographerLockSeam?.Invoke(lCartographerAccepted, lCartographerSource);
        return lCartographerAccepted.Count;
    }

    public static LCartographerPlanRecord? LCartographerPlanPrepare(Guid lCartographerTarget) =>
        lCartographerTarget == Guid.Empty || lCartographerTarget == LCartographerFinishTarget
            ? null
            : LCartographerPlanCreate(Guid.Empty, lCartographerTarget);

    private static void LCartographerScheduleHandle(LScheduleContract lCartographerSchedule)
    {
        Guid[] lCartographerLiveBatches = lCartographerSchedule.LScheduleRecords
            .Select(lCartographerItem => lCartographerItem.LWorkBatchId)
            .Where(lCartographerBatch => lCartographerBatch != Guid.Empty)
            .Distinct()
            .ToArray();

        if (LStation.LStationPost is { } lCartographerPost)
        {
            lCartographerPost(() => LCartographerScheduleApply(lCartographerSchedule, lCartographerLiveBatches));
            return;
        }

        LCartographerScheduleApply(lCartographerSchedule, lCartographerLiveBatches);
    }

    private static void LCartographerScheduleApply(
        LScheduleContract lCartographerSchedule,
        IReadOnlyCollection<Guid> lCartographerLiveBatches)
    {
        LCartographerSourcesRelease(lCartographerSchedule);
        LCartographerBatchesUpdate(lCartographerSchedule, lCartographerLiveBatches);
        LCartographerDispatch(lCartographerSchedule.LScheduleRecords);
    }

    private static void LCartographerSourcesRelease(LScheduleContract lCartographerSchedule)
    {
        if (LCartographerDeliverySeam is not { } lCartographerSeam)
        {
            return;
        }

        var lCartographerUnlocks = new List<(string PListPath, Guid PListBatch)>();
        foreach (LWorkItem lCartographerItem in lCartographerSchedule.LScheduleRecords)
        {
            if (lCartographerItem.LWorkRelayTarget != Guid.Empty
                || lCartographerItem.LWorkStateCurrent is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning)
            {
                continue;
            }

            lCartographerUnlocks.Add((lCartographerItem.LWorkSourcePath, lCartographerItem.LWorkBatchId));
            lCartographerUnlocks.AddRange(lCartographerItem.LWorkMergeSources.Select(
                lCartographerPath => (lCartographerPath, lCartographerItem.LWorkBatchId)));
        }

        if (lCartographerUnlocks.Count > 0)
        {
            lCartographerSeam.LCartographerSourceUnlock(lCartographerUnlocks.Distinct().ToArray());
        }
    }

    private static void LCartographerBatchesUpdate(
        LScheduleContract lCartographerSchedule,
        IReadOnlyCollection<Guid> lCartographerLiveBatches)
    {
        Guid[] lCartographerRemovedBatches = lCartographerScheduledBatches
            .Where(lCartographerBatch => !lCartographerLiveBatches.Contains(lCartographerBatch))
            .ToArray();

        lCartographerScheduledBatches.Clear();
        lCartographerScheduledBatches.UnionWith(lCartographerLiveBatches);
        if (lCartographerRemovedBatches.Length == 0)
        {
            return;
        }

        LCartographerDeliverySeam?.LCartographerBatchEvict(lCartographerRemovedBatches);
        LCartographerDeliveredRemove(lCartographerSchedule.LScheduleRecords
            .Select(lCartographerItem => lCartographerItem.LWorkId)
            .ToHashSet());
    }
}
