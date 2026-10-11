using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record LLineageEntry(
    Guid LLineageEntryBatch,
    Guid LLineageEntryId,
    string LLineageEntrySubject,
    IReadOnlyList<LWorkItem> LLineageEntryItems,
    long? LLineageEntryOrigin);

public static class LLineage
{
    private readonly record struct LLineageKey(Guid LLineageKeyBatch, Guid LLineageKeyLineage);

    public static IReadOnlyList<LLineageEntry> LLineageRead(
        CUsher lUsher,
        IReadOnlyList<LWorkItem> lWorkItems,
        Func<LWorkItem, Guid> lLineageRead)
    {
        var lOrder = new List<LLineageKey>();
        var lIndex = new Dictionary<LLineageKey, List<LWorkItem>>();
        var lSubjects = new Dictionary<LLineageKey, string>();
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            Guid lLineageId = lLineageRead(lWorkItem);
            var lKey = new LLineageKey(lWorkItem.LWorkBatchId, lLineageId);
            if (!lIndex.TryGetValue(lKey, out List<LWorkItem>? lItems))
            {
                lItems = new List<LWorkItem>();
                lIndex[lKey] = lItems;
                lSubjects[lKey] = LLineageSubjectRead(lWorkItem, lLineageId);
                lOrder.Add(lKey);
            }

            lItems.Add(lWorkItem);
        }

        return lOrder
            .Select(lKey => new LLineageEntry(
                lKey.LLineageKeyBatch,
                lKey.LLineageKeyLineage,
                lSubjects[lKey],
                lIndex[lKey],
                LLineageOriginRead(lUsher, lKey.LLineageKeyBatch, lSubjects[lKey], lWorkItems)))
            .ToArray();
    }

    private static string LLineageSubjectRead(LWorkItem lLineageFirst, Guid lLineageId)
    {
        if (lLineageFirst.LWorkKind == LWorkKind.LWorkKindMerge
            || LScheduleLineage.LScheduleFileRead(lLineageFirst.LWorkOutputPath) == lLineageId)
        {
            return lLineageFirst.LWorkOutputPath;
        }

        return lLineageFirst.LWorkSourcePath;
    }

    private static long? LLineageOriginRead(
        CUsher lUsher, Guid lBatchId, string lSubject, IReadOnlyList<LWorkItem> lWorkItems)
    {
        foreach (LWorkItem lWorkItem in lWorkItems)
        {
            if (lWorkItem.LWorkBatchId != lBatchId)
            {
                continue;
            }

            if (LLineageMatch(lUsher, lWorkItem.LWorkOutputPath, lSubject) && lWorkItem.LWorkOutputBytes is { } lOutput)
            {
                return lOutput;
            }

            if (LLineageMatch(lUsher, lWorkItem.LWorkSourcePath, lSubject) && lWorkItem.LWorkSourceBytes is { } lSource)
            {
                return lSource;
            }
        }

        return null;
    }

    public static string LLineageStepFormat(CUsher lUsher, LWorkItem lWorkItem, string lSubject)
    {
        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindSplit
            && !LLineageMatch(lUsher, lWorkItem.LWorkOutputPath, lSubject))
        {
            return LLocalization.LLocalizationFormat(
                "Roster.Lineage.Split", LLineageFileRead(lUsher, lWorkItem.LWorkOutputPath));
        }

        string lTabName = LCartographer.LCartographerTitleRead(lWorkItem);
        return LLocalization.LLocalizationFormat(
            "Roster.Lineage.Step",
            string.IsNullOrWhiteSpace(lTabName) ? LLineageKindRead(lWorkItem.LWorkKind) : lTabName);
    }

    private static string LLineageKindRead(LWorkKind lWorkKind) =>
        LLocalization.LLocalizationTextRead(lWorkKind switch
        {
            LWorkKind.LWorkKindEdit => "Roster.Kind.Edit",
            LWorkKind.LWorkKindFix => "Roster.Kind.Fix",
            LWorkKind.LWorkKindAudio => "Roster.Kind.Audio",
            LWorkKind.LWorkKindConvert => "Roster.Kind.Convert",
            LWorkKind.LWorkKindMerge => "Roster.Kind.Merge",
            _ => "Roster.Kind.Split"
        });

    public static string LLineageRatioFormat(
        CUsher lUsher, LWorkItem lWorkItem, string lSubject, long? lOriginBytes)
    {
        if (LLineageMatch(lUsher, lWorkItem.LWorkOutputPath, lSubject))
        {
            return LLineageRatioFormat(lWorkItem);
        }

        if (lOriginBytes is not { } lOriginWhole || lOriginWhole <= 0 || lWorkItem.LWorkOutputBytes is not { } lOutput)
        {
            return "-";
        }

        return $"{(double)lOutput / lOriginWhole:P1}";
    }

    public static string LLineageRatioFormat(LWorkItem lWorkItem) =>
        LLineageRatioRead(lWorkItem) is { } lRatio ? $"{lRatio:P1}" : "-";

    public static double? LLineageRatioRead(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkOutputBytes is not { } lOutputWhole
            || LLineageSourceRead(lWorkItem) is not { } lSourceWhole
            || lSourceWhole <= 0)
        {
            return null;
        }

        return (double)lOutputWhole / lSourceWhole;
    }

    public static long? LLineageSourceRead(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkMergeSources.Count > 1 && lWorkItem.LWorkMergeBytes.Count > 0)
        {
            long lMergeTotal = lWorkItem.LWorkMergeBytes.Sum();
            if (lMergeTotal > 0)
            {
                return lMergeTotal;
            }
        }

        return lWorkItem.LWorkSourceBytes;
    }

    public static bool LLineageMatch(CUsher lUsher, string lLeftPath, string lRightPath) =>
        LLineagePathRead(lUsher, lLeftPath) is { } lLeftKey
        && LLineagePathRead(lUsher, lRightPath) is { } lRightKey
        && string.Equals(lLeftKey, lRightKey, StringComparison.OrdinalIgnoreCase);

    public static string? LLineagePathRead(CUsher lUsher, string lPath) =>
        string.IsNullOrWhiteSpace(lPath) ? null : lUsher.CUsherPathResolve(lPath);

    public static string LLineageTitleFormat(CUsher lUsher, LLineageEntry lEntry)
    {
        string lLineageName = LLineageFileRead(lUsher, lEntry.LLineageEntrySubject);
        return lEntry.LLineageEntryItems.Count == 1
            ? LLocalization.LLocalizationFormat("Roster.Lineage.One", lLineageName)
            : LLocalization.LLocalizationFormat("Roster.Lineage.Many", lLineageName, lEntry.LLineageEntryItems.Count);
    }

    public static string LLineageFileRead(CUsher lUsher, string lFilePath) =>
        string.IsNullOrWhiteSpace(lFilePath)
            ? LLocalization.LLocalizationTextRead("Roster.Lineage.Unknown")
            : lUsher.CUsherNameRead(lFilePath);

    private static IEnumerable<string> LLineageInputsRead(LWorkItem lWorkItem) =>
        lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
            ? lWorkItem.LWorkMergeSources
            : new[] { lWorkItem.LWorkSourcePath };

    private static HashSet<string> LLineageKeysCreate(CUsher lUsher, IEnumerable<string> lPaths)
    {
        var lKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string lPath in lPaths)
        {
            if (LLineagePathRead(lUsher, lPath) is { } lKey)
            {
                lKeys.Add(lKey);
            }
        }

        return lKeys;
    }

    public static HashSet<Guid> LLineageStageRead(CUsher lUsher, IReadOnlyList<LWorkItem> lBatchItems)
    {
        HashSet<string> lConsumed = LLineageKeysCreate(lUsher, lBatchItems.SelectMany(LLineageInputsRead));
        HashSet<string> lProduced =
            LLineageKeysCreate(lUsher, lBatchItems.Select(lWorkItem => lWorkItem.LWorkOutputPath));
        var lStageIds = new HashSet<Guid>();
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            if (LLineagePathRead(lUsher, lWorkItem.LWorkOutputPath) is { } lOutputKey
                && lConsumed.Contains(lOutputKey)
                && LLineageInputsRead(lWorkItem).Any(
                    lInput => LLineagePathRead(lUsher, lInput) is { } lInputKey && lProduced.Contains(lInputKey)))
            {
                lStageIds.Add(lWorkItem.LWorkId);
            }
        }

        return lStageIds;
    }

    public static int LLineageInitialRead(CUsher lUsher, IReadOnlyList<LWorkItem> lBatchItems)
    {
        HashSet<string> lOutputs =
            LLineageKeysCreate(lUsher, lBatchItems.Select(lWorkItem => lWorkItem.LWorkOutputPath));
        HashSet<string> lInputs = LLineageKeysCreate(lUsher, lBatchItems.SelectMany(LLineageInputsRead));
        lInputs.ExceptWith(lOutputs);
        return lInputs.Count;
    }
}
