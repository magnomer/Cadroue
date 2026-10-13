using Cadroue.Core;

namespace Cadroue.Application;

internal static class LScheduleLineageSummary
{
    internal static string? LScheduleKeyFind(IReadOnlyDictionary<string, string> lPathKeys, string lPath) =>
        !string.IsNullOrWhiteSpace(lPath) && lPathKeys.TryGetValue(lPath, out string? lKey) ? lKey : null;

    private static IReadOnlyList<string> LScheduleInputsRead(LWorkItem lWorkItem) =>
        lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
            ? lWorkItem.LWorkMergeSources
            : [lWorkItem.LWorkSourcePath];

    private static HashSet<string> LScheduleKeysCreate(
        IReadOnlyDictionary<string, string> lPathKeys, IEnumerable<string> lPaths)
    {
        var lKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string lPath in lPaths)
        {
            if (LScheduleKeyFind(lPathKeys, lPath) is { } lKey)
            {
                lKeys.Add(lKey);
            }
        }

        return lKeys;
    }

    internal static LScheduleLineageBatch LScheduleBatchCreate(
        IReadOnlyDictionary<string, string> lPathKeys,
        Guid lBatchId,
        IReadOnlyList<LWorkItem> lBatchItems,
        IReadOnlyList<Guid> lIds)
    {
        (IReadOnlyList<string> lSources, IReadOnlyList<string> lOutputs) = LSchedulePathsRead(lPathKeys, lBatchItems);
        (long? lSourceTotal, long? lOutputTotal) = LScheduleSizeRead(lPathKeys, lBatchItems);
        return new LScheduleLineageBatch(
            lBatchId,
            LScheduleStageRead(lPathKeys, lBatchItems),
            LScheduleInitialRead(lPathKeys, lBatchItems),
            lBatchItems.Select(lWorkItem => lWorkItem.LWorkCreateTime).DefaultIfEmpty().Min(),
            lSources,
            lOutputs,
            lSourceTotal,
            lOutputTotal,
            lSources.Count,
            lOutputs.Count,
            lIds,
            lBatchItems.All(lWorkItem => lWorkItem.LWorkStateCurrent is not
                (LWorkState.LWorkStatePending or LWorkState.LWorkStateRunning)));
    }

    private static HashSet<Guid> LScheduleStageRead(
        IReadOnlyDictionary<string, string> lPathKeys, IReadOnlyList<LWorkItem> lBatchItems)
    {
        HashSet<string> lConsumed = LScheduleKeysCreate(lPathKeys, lBatchItems.SelectMany(LScheduleInputsRead));
        HashSet<string> lProduced =
            LScheduleKeysCreate(lPathKeys, lBatchItems.Select(lWorkItem => lWorkItem.LWorkOutputPath));
        var lStageIds = new HashSet<Guid>();
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            if (LScheduleKeyFind(lPathKeys, lWorkItem.LWorkOutputPath) is { } lOutputKey
                && lConsumed.Contains(lOutputKey)
                && LScheduleInputsRead(lWorkItem).Any(
                    lInput => LScheduleKeyFind(lPathKeys, lInput) is { } lInputKey && lProduced.Contains(lInputKey)))
            {
                lStageIds.Add(lWorkItem.LWorkId);
            }
        }

        return lStageIds;
    }

    private static int LScheduleInitialRead(
        IReadOnlyDictionary<string, string> lPathKeys, IReadOnlyList<LWorkItem> lBatchItems)
    {
        HashSet<string> lOutputs =
            LScheduleKeysCreate(lPathKeys, lBatchItems.Select(lWorkItem => lWorkItem.LWorkOutputPath));
        HashSet<string> lInputs = LScheduleKeysCreate(lPathKeys, lBatchItems.SelectMany(LScheduleInputsRead));
        lInputs.ExceptWith(lOutputs);
        return lInputs.Count;
    }

    private static (IReadOnlyList<string>, IReadOnlyList<string>) LSchedulePathsRead(
        IReadOnlyDictionary<string, string> lPathKeys, IReadOnlyList<LWorkItem> lBatchItems)
    {
        HashSet<string> lOutputKeys =
            LScheduleKeysCreate(lPathKeys, lBatchItems.Select(lWorkItem => lWorkItem.LWorkOutputPath));
        var lConsumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSeenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSources = new List<string>();
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            foreach (string lInput in LScheduleInputsRead(lWorkItem))
            {
                if (LScheduleKeyFind(lPathKeys, lInput) is not { } lInputKey)
                {
                    continue;
                }

                lConsumed.Add(lInputKey);
                if (!lOutputKeys.Contains(lInputKey) && lSeenSources.Add(lInputKey))
                {
                    lSources.Add(lInput);
                }
            }
        }

        var lSeenOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lOutputs = new List<string>();
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            if (LScheduleKeyFind(lPathKeys, lWorkItem.LWorkOutputPath) is { } lOutputKey
                && !lConsumed.Contains(lOutputKey)
                && lSeenOutputs.Add(lOutputKey))
            {
                lOutputs.Add(lWorkItem.LWorkOutputPath);
            }
        }

        return (lSources, lOutputs);
    }

    private static (long? lSourceTotal, long? lOutputTotal) LScheduleSizeRead(
        IReadOnlyDictionary<string, string> lPathKeys, IReadOnlyList<LWorkItem> lBatchItems)
    {
        HashSet<string> lOutputKeys =
            LScheduleKeysCreate(lPathKeys, lBatchItems.Select(lWorkItem => lWorkItem.LWorkOutputPath));
        var lConsumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lInputBytes = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            bool lMerge = lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge;
            IReadOnlyList<string> lInputs = LScheduleInputsRead(lWorkItem);
            for (int lIndex = 0; lIndex < lInputs.Count; lIndex++)
            {
                if (LScheduleKeyFind(lPathKeys, lInputs[lIndex]) is not { } lInputKey)
                {
                    continue;
                }

                lConsumed.Add(lInputKey);
                if (!lInputBytes.ContainsKey(lInputKey))
                {
                    lInputBytes[lInputKey] = lMerge
                        ? lIndex < lWorkItem.LWorkMergeBytes.Count && lWorkItem.LWorkMergeBytes[lIndex] > 0
                            ? lWorkItem.LWorkMergeBytes[lIndex]
                            : null
                        : lWorkItem.LWorkSourceBytes;
                }
            }
        }

        long lSourceTotal = 0;
        bool lSourceAny = false, lSourceOk = true;
        foreach ((string lInputKey, long? lInputByte) in lInputBytes)
        {
            if (lOutputKeys.Contains(lInputKey))
            {
                continue;
            }

            lSourceAny = true;
            if (lInputByte is { } lInputWhole)
            {
                lSourceTotal += lInputWhole;
            }
            else
            {
                lSourceOk = false;
            }
        }

        long lOutputTotal = 0;
        bool lOutputAny = false;
        var lSeenOutputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            if (lWorkItem.LWorkStateCurrent != LWorkState.LWorkStateDone
                || LScheduleKeyFind(lPathKeys, lWorkItem.LWorkOutputPath) is not { } lOutputKey
                || lConsumed.Contains(lOutputKey)
                || !lSeenOutputs.Add(lOutputKey)
                || lWorkItem.LWorkOutputBytes is not { } lOutputWhole)
            {
                continue;
            }

            lOutputAny = true;
            lOutputTotal += lOutputWhole;
        }

        return (
            lSourceAny && lSourceOk ? lSourceTotal : null,
            lOutputAny ? lOutputTotal : null);
    }
}
