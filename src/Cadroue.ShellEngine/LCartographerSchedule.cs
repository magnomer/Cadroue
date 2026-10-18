using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LCartographerSchedule
{
    private static readonly HashSet<Guid> lCartographerScheduledBatches = new();
    private static readonly HashSet<Guid> lCartographerReleased = new();
    private static bool lCartographerWatching;

    public static LScheduleContract? LCartographerScheduleContract { get; set; }

    public static Func<IReadOnlyList<LWorkItem>, bool>? LCartographerLockSeam { get; set; }

    internal static IReadOnlyList<LWorkItem> LCartographerScheduleRead() =>
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
        lCartographerSchedule.LScheduleChanged += LCartographerScheduleHandle;
        LCartographer.LCartographerDispatch(lCartographerSchedule.LScheduleRecords);
    }

    private static void LCartographerScheduleHandle(LScheduleContract lCartographerSchedule)
    {
        Guid[] lCartographerLiveBatches = lCartographerSchedule.LScheduleRecords
            .Select(lCartographerItem => lCartographerItem.LWorkBatchId)
            .Where(lCartographerBatch => lCartographerBatch != Guid.Empty)
            .Distinct()
            .ToArray();

        if (LStationEngine.LStationPost is { } lCartographerPost)
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
        LCartographerSourcesClaim(lCartographerSchedule);
        LCartographerBatchesUpdate(lCartographerSchedule, lCartographerLiveBatches);
        LCartographer.LCartographerDispatch(lCartographerSchedule.LScheduleRecords);
    }

    private static void LCartographerSourcesClaim(LScheduleContract lCartographerSchedule)
    {
        LWorkItem[] lCartographerActive = lCartographerSchedule.LScheduleRecords
            .Where(LCartographerActiveCheck)
            .ToArray();
        if (lCartographerActive.Length > 0)
        {
            LCartographerLockSeam?.Invoke(lCartographerActive);
        }
    }

    private static void LCartographerSourcesRelease(LScheduleContract lCartographerSchedule)
    {
        if (LCartographer.LCartographerDeliverySeam is not { } lCartographerSeam)
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
            if (LCartographerActiveCheck(lCartographerItem))
            {
                lCartographerReleased.Remove(lCartographerItem.LWorkId);
                continue;
            }

            bool lCartographerBatchActive = lCartographerHeld.TryGetValue(
                lCartographerItem.LWorkBatchId, out HashSet<string>? lCartographerBatchPaths);
            if (lCartographerReleased.Contains(lCartographerItem.LWorkId)
                || (lCartographerBatchActive && lCartographerItem.LWorkRelayTarget != Guid.Empty))
            {
                continue;
            }

            string[] lCartographerSources = LCartographerSourcesRead(lCartographerItem).ToArray();
            string[] lCartographerFree = lCartographerSources
                .Where(lCartographerPath => lCartographerBatchPaths?.Contains(lCartographerPath) != true)
                .ToArray();
            lCartographerUnlocks.AddRange(lCartographerFree
                .Select(lCartographerPath => (lCartographerPath, lCartographerItem.LWorkBatchId, lCartographerItem)));
            if (lCartographerFree.Length == lCartographerSources.Length)
            {
                lCartographerReleased.Add(lCartographerItem.LWorkId);
            }
        }

        lCartographerReleased.IntersectWith(
            lCartographerRecords.Select(lCartographerItem => lCartographerItem.LWorkId));

        if (lCartographerUnlocks.Count > 0)
        {
            lCartographerSeam.LCartographerSourceUnlock(lCartographerUnlocks);
        }
    }

    private static bool LCartographerActiveCheck(LWorkItem lCartographerItem) =>
        lCartographerItem.LWorkRunState.LWorkStateCurrent
            is LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning;

    internal static IEnumerable<string> LCartographerSourcesRead(LWorkItem lCartographerItem) =>
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

        LCartographer.LCartographerDeliverySeam?.LCartographerBatchEvict(lCartographerRemovedBatches);
        LCartographer.LCartographerDeliveredRemove(lCartographerSchedule.LScheduleRecords
            .Select(lCartographerItem => lCartographerItem.LWorkId)
            .ToHashSet());
        foreach (Guid lCartographerBatch in lCartographerRemovedBatches)
        {
            LCartographerPlanStore.LCartographerPlanDelete(lCartographerBatch);
        }
    }
}
