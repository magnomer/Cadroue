using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Application;

public static class LScheduleLineagePlan
{
    public static LScheduleLineageAnswer LScheduleLineageResolve(
        LScheduleContract lSchedule,
        IReadOnlyList<Guid> lIds,
        Func<string, Guid> lFileRead,
        Func<string, string> lPathResolve,
        Func<string, string> lNameRead,
        Func<string, string> lExtensionRead)
    {
        return LScheduleLineageResolve(
            LScheduleLookup.LScheduleRecordsRead(lSchedule, lIds),
            lSchedule.LScheduleLineageRead,
            lFileRead,
            lPathResolve,
            lNameRead,
            lExtensionRead);
    }

    public static LScheduleLineageAnswer LScheduleLineageResolve(
        IReadOnlyList<LWorkItem> lWorkItems,
        Func<LWorkItem, Guid> lLineageRead,
        Func<string, Guid> lFileRead,
        Func<string, string> lPathResolve,
        Func<string, string> lNameRead,
        Func<string, string> lExtensionRead)
    {
        var lOrder = new List<(Guid, Guid)>();
        var lIndex = new Dictionary<(Guid, Guid), List<LWorkItem>>();
        var lSubjects = new Dictionary<(Guid, Guid), string>();
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            Guid lLineageId = lLineageRead(lWorkItem);
            (Guid, Guid) lKey = (lWorkItem.LWorkBatchId, lLineageId);
            if (!lIndex.TryGetValue(lKey, out List<LWorkItem>? lItems))
            {
                lItems = new List<LWorkItem>();
                lIndex[lKey] = lItems;
                lSubjects[lKey] = LScheduleSubjectRead(lFileRead, lWorkItem, lLineageId);
                lOrder.Add(lKey);
            }

            lItems.Add(lWorkItem);
        }

        IReadOnlyDictionary<string, string> lPathKeys = LScheduleKeysRead(lPathResolve, lWorkItems);
        LScheduleLineageEntry[] lEntries = lOrder
            .Select(lKey => new LScheduleLineageEntry(
                lKey.Item1,
                lKey.Item2,
                lSubjects[lKey],
                LScheduleNameRead(lNameRead, lSubjects[lKey]),
                lIndex[lKey].Select(lWorkItem => lWorkItem.LWorkId).ToArray(),
                LScheduleOriginRead(lPathKeys, lKey.Item1, lSubjects[lKey], lWorkItems)))
            .ToArray();
        LScheduleLineageBatch[] lBatches = lWorkItems
            .Select(lWorkItem => lWorkItem.LWorkBatchId)
            .Distinct()
            .Select(lBatchId => LScheduleBatchCreate(
                lPathKeys,
                lBatchId,
                lWorkItems.Where(lWorkItem => lWorkItem.LWorkBatchId == lBatchId).ToArray(),
                lEntries
                    .Where(lEntry => lEntry.LScheduleEntryBatch == lBatchId)
                    .SelectMany(lEntry => lEntry.LScheduleEntryIds)
                    .ToArray()))
            .ToArray();
        LScheduleLineageStep[] lSteps = lEntries
            .SelectMany(lEntry => lIndex[(lEntry.LScheduleEntryBatch, lEntry.LScheduleEntryLineage)]
                .Select(lWorkItem => LScheduleLineageStep.LScheduleStepCreate(
                    lNameRead, lExtensionRead, lWorkItem,
                    LScheduleKeyMatch(lPathKeys, lWorkItem.LWorkOutputPath, lEntry.LScheduleEntrySubject),
                    lEntry.LScheduleEntryOrigin)))
            .ToArray();
        return new LScheduleLineageAnswer(lEntries, lBatches, lSteps);
    }

    public static LScheduleLineageBatch LScheduleBatchResolve(
        LScheduleContract? lSchedule, Guid lBatchId, Func<string, string> lPathResolve)
    {
        if (lSchedule is null)
        {
            return LScheduleBatchCreate(LScheduleKeysRead(lPathResolve, []), lBatchId, [], []);
        }

        IReadOnlyList<LWorkItem> lBatchItems = LScheduleLookup.LScheduleBatchRead(lSchedule, lBatchId);
        return LScheduleBatchCreate(
            LScheduleKeysRead(lPathResolve, lBatchItems),
            lBatchId,
            lBatchItems,
            LScheduleIdsRead(lBatchItems, lSchedule.LScheduleLineageRead));
    }

    private static Guid[] LScheduleIdsRead(IReadOnlyList<LWorkItem> lBatchItems, Func<LWorkItem, Guid> lLineageRead) =>
        lBatchItems
            .GroupBy(lLineageRead)
            .SelectMany(lLineage => lLineage.Select(lWorkItem => lWorkItem.LWorkId))
            .ToArray();

    private static string LScheduleSubjectRead(Func<string, Guid> lFileRead, LWorkItem lLineageFirst, Guid lLineageId)
    {
        if (lLineageFirst.LWorkKind == LWorkKind.LWorkKindMerge
            || lFileRead(lLineageFirst.LWorkOutputPath) == lLineageId)
        {
            return lLineageFirst.LWorkOutputPath;
        }

        return lLineageFirst.LWorkSourcePath;
    }

    internal static string? LScheduleNameRead(Func<string, string> lNameRead, string lFilePath) =>
        string.IsNullOrWhiteSpace(lFilePath) ? null : lNameRead(lFilePath);

    private static long? LScheduleOriginRead(
        IReadOnlyDictionary<string, string> lPathKeys,
        Guid lBatchId,
        string lSubject,
        IReadOnlyList<LWorkItem> lWorkItems)
    {
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            if (lWorkItem.LWorkBatchId != lBatchId)
            {
                continue;
            }

            if (LScheduleKeyMatch(lPathKeys, lWorkItem.LWorkOutputPath, lSubject)
                && lWorkItem.LWorkOutputBytes is { } lOutput)
            {
                return lOutput;
            }

            if (LScheduleKeyMatch(lPathKeys, lWorkItem.LWorkSourcePath, lSubject)
                && lWorkItem.LWorkSourceBytes is { } lSource)
            {
                return lSource;
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<string, string> LScheduleKeysRead(
        Func<string, string> lPathResolve, IReadOnlyList<LWorkItem> lWorkItems)
    {
        var lPaths = new List<string>();
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            lPaths.Add(lWorkItem.LWorkOutputPath);
            lPaths.Add(lWorkItem.LWorkSourcePath);
            lPaths.AddRange(lWorkItem.LWorkMergeSources);
        }

        var lPathKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string lPath in lPaths)
        {
            if (lPath is not null && !lPathKeys.ContainsKey(lPath))
            {
                lPathKeys[lPath] = lPathResolve(lPath);
            }
        }

        return lPathKeys;
    }

    private static string? LScheduleKeyFind(IReadOnlyDictionary<string, string> lPathKeys, string lPath) =>
        !string.IsNullOrWhiteSpace(lPath) && lPathKeys.TryGetValue(lPath, out string? lKey) ? lKey : null;

    private static bool LScheduleKeyMatch(
        IReadOnlyDictionary<string, string> lPathKeys, string lLeftPath, string lRightPath) =>
        LScheduleKeyFind(lPathKeys, lLeftPath) is { } lLeftKey
        && LScheduleKeyFind(lPathKeys, lRightPath) is { } lRightKey
        && string.Equals(lLeftKey, lRightKey, StringComparison.OrdinalIgnoreCase);

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

    private static LScheduleLineageBatch LScheduleBatchCreate(
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
