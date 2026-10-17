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
        Func<LWorkItem, Guid> lLineageRead,
        Func<string, Guid> lFileRead,
        Func<string, string> lPathResolve,
        Func<string, string> lNameRead,
        Func<string, string> lExtensionRead)
    {
        return LScheduleLineageResolve(
            LScheduleLookup.LScheduleRecordsRead(lSchedule, lIds),
            lLineageRead,
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
            .Select(lBatchId => LScheduleLineageSummary.LScheduleBatchCreate(
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
        LScheduleContract? lSchedule,
        Guid lBatchId,
        Func<LWorkItem, Guid> lLineageRead,
        Func<string, string> lPathResolve)
    {
        if (lSchedule is null)
        {
            return LScheduleLineageSummary.LScheduleBatchCreate(LScheduleKeysRead(lPathResolve, []), lBatchId, [], []);
        }

        IReadOnlyList<LWorkItem> lBatchItems = LScheduleLookup.LScheduleBatchRead(lSchedule, lBatchId);
        return LScheduleLineageSummary.LScheduleBatchCreate(
            LScheduleKeysRead(lPathResolve, lBatchItems),
            lBatchId,
            lBatchItems,
            LScheduleIdsRead(lBatchItems, lLineageRead));
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
                && lWorkItem.LWorkRunState.LWorkOutputBytes is { } lOutput)
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

    private static bool LScheduleKeyMatch(
        IReadOnlyDictionary<string, string> lPathKeys, string lLeftPath, string lRightPath) =>
        LScheduleLineageSummary.LScheduleKeyFind(lPathKeys, lLeftPath) is { } lLeftKey
        && LScheduleLineageSummary.LScheduleKeyFind(lPathKeys, lRightPath) is { } lRightKey
        && string.Equals(lLeftKey, lRightKey, StringComparison.OrdinalIgnoreCase);
}
