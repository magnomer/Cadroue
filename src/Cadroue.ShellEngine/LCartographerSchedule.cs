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

        IReadOnlyList<LWorkItem> lCartographerRecords = lCartographerSchedule.LScheduleRecords;
        var lCartographerHeld = new Dictionary<Guid, HashSet<string>>();
        foreach (LWorkItem lCartographerItem in lCartographerRecords.Where(LCartographerActiveCheck))
        {
            if (!lCartographerHeld.TryGetValue(lCartographerItem.LWorkBatchId, out HashSet<string>? lCartographerPaths))
            {
                lCartographerPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                lCartographerHeld[lCartographerItem.LWorkBatchId] = lCartographerPaths;
            }

            lCartographerPaths.UnionWith(LCartographerSourcesRead(lCartographerItem));
        }

        var lCartographerUnlocks = new List<(string PListPath, Guid PListBatch, LWorkItem PListOwner)>();
        foreach (LWorkItem lCartographerItem in lCartographerRecords)
        {
            bool lCartographerBatchActive = lCartographerHeld.TryGetValue(
                lCartographerItem.LWorkBatchId, out HashSet<string>? lCartographerBatchPaths);
            if (LCartographerActiveCheck(lCartographerItem)
                || (lCartographerBatchActive && lCartographerItem.LWorkRelayTarget != Guid.Empty))
            {
                continue;
            }

            lCartographerUnlocks.AddRange(LCartographerSourcesRead(lCartographerItem)
                .Where(lCartographerPath => lCartographerBatchPaths?.Contains(lCartographerPath) != true)
                .Select(lCartographerPath => (lCartographerPath, lCartographerItem.LWorkBatchId, lCartographerItem)));
        }

        if (lCartographerUnlocks.Count > 0)
        {
            lCartographerSeam.LCartographerSourceUnlock(lCartographerUnlocks);
        }
    }

    private static bool LCartographerActiveCheck(LWorkItem lCartographerItem) =>
        lCartographerItem.LWorkStateCurrent is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning;

    private static IEnumerable<string> LCartographerSourcesRead(LWorkItem lCartographerItem) =>
        lCartographerItem.LWorkMergeSources.Prepend(lCartographerItem.LWorkSourcePath);

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
