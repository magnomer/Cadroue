using Cadroue.Application;
using Cadroue.Core;
using static Cadroue.UIDeportment.LRosterFormat;

namespace Cadroue.UIDeportment;

public sealed class LSummary
{
    public required IReadOnlyList<string> LSummaryMeters { get; init; }

    public required IReadOnlyList<LRosterBar> LSummaryBars { get; init; }

    public required IReadOnlyList<LRosterCompareRow> LSummaryCompares { get; init; }

    public required string LSummarySourceCount { get; init; }

    public required string LSummaryOutputCount { get; init; }

    public required IReadOnlyList<string> LSummarySources { get; init; }

    public required IReadOnlyList<string> LSummaryOutputs { get; init; }

    public static LSummary LSummaryRead(IReadOnlyList<LWorkItem> lBatchItems)
    {
        (long? lSourceBytes, long? lOutputBytes) = LSummarySizeRead(lBatchItems);
        (IReadOnlyList<string> lSources, IReadOnlyList<string> lOutputs) = LSummaryPathsRead(lBatchItems);
        string? lMeter = LSummaryMeterFormat(lBatchItems, lOutputBytes);
        return new LSummary
        {
            LSummaryMeters = lMeter is null ? [] : [lMeter],
            LSummaryBars = LRosterDetail.LRosterBarsCreate(lSourceBytes, lOutputBytes),
            LSummaryCompares =
            [
                LSummaryCompareCreate(
                    LLocalization.LLocalizationTextRead("Roster.Section.Source"),
                    LLocalization.LLocalizationTextRead("Roster.Section.Output"),
                    LRosterDetail.LRosterLineHead),
                LSummaryCompareCreate(
                    LRosterMebiFormat(lSourceBytes), LRosterMebiFormat(lOutputBytes), LRosterDetail.LRosterLinePlain)
            ],
            LSummarySourceCount = LSummaryFilesFormat(lSources.Count),
            LSummaryOutputCount = LSummaryFilesFormat(lOutputs.Count),
            LSummarySources = lSources,
            LSummaryOutputs = lOutputs
        };
    }

    private static LRosterCompareRow LSummaryCompareCreate(string lSource, string lOutput, string lKey) =>
        new(new LRosterLine(lSource, lKey), new LRosterLine(lOutput, lKey));

    public static string? LSummaryMeterFormat(IReadOnlyList<LWorkItem> lBatchItems, long? lOutputBytes)
    {
        TimeSpan lSpentTotal = TimeSpan.Zero;
        bool lAnySpent = false;
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            if (LRosterSpentRead(lWorkItem) is { } lSpent)
            {
                lSpentTotal += lSpent;
                lAnySpent = true;
            }
        }

        if (!lAnySpent || lSpentTotal.TotalSeconds <= 0)
        {
            return null;
        }

        return $"{LRosterElapsedFormat(lSpentTotal)} / {LRosterSpeedFormat(lSpentTotal, lOutputBytes)}";
    }

    public static string LSummaryFilesFormat(int lCount) =>
        lCount == 1
            ? LLocalization.LLocalizationTextRead("Roster.Summary.FileOne")
            : LLocalization.LLocalizationFormat("Roster.Summary.FileMany", lCount);

    private static IReadOnlyList<string> LSummaryInputsRead(LWorkItem lWorkItem) =>
        lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge
            ? lWorkItem.LWorkMergeSources
            : [lWorkItem.LWorkSourcePath];

    private static HashSet<string> LSummaryOutputsRead(IReadOnlyList<LWorkItem> lBatchItems)
    {
        var lOutputKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            if (LLineage.LLineagePathRead(lWorkItem.LWorkOutputPath) is { } lOutputKey)
            {
                lOutputKeys.Add(lOutputKey);
            }
        }

        return lOutputKeys;
    }

    public static (IReadOnlyList<string>, IReadOnlyList<string>) LSummaryPathsRead(
        IReadOnlyList<LWorkItem> lBatchItems)
    {
        HashSet<string> lOutputKeys = LSummaryOutputsRead(lBatchItems);
        var lConsumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSeenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSources = new List<string>();
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            foreach (string lInput in LSummaryInputsRead(lWorkItem))
            {
                if (LLineage.LLineagePathRead(lInput) is not { } lInputKey)
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
            if (LLineage.LLineagePathRead(lWorkItem.LWorkOutputPath) is { } lOutputKey
                && !lConsumed.Contains(lOutputKey)
                && lSeenOutputs.Add(lOutputKey))
            {
                lOutputs.Add(lWorkItem.LWorkOutputPath);
            }
        }

        return (lSources, lOutputs);
    }

    public static (long? lSourceTotal, long? lOutputTotal) LSummarySizeRead(IReadOnlyList<LWorkItem> lBatchItems)
    {
        HashSet<string> lOutputKeys = LSummaryOutputsRead(lBatchItems);
        var lConsumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lInputBytes = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
        foreach (LWorkItem lWorkItem in lBatchItems)
        {
            bool lMerge = lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge;
            IReadOnlyList<string> lInputs = LSummaryInputsRead(lWorkItem);
            for (int lIndex = 0; lIndex < lInputs.Count; lIndex++)
            {
                if (LLineage.LLineagePathRead(lInputs[lIndex]) is not { } lInputKey)
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
                || LLineage.LLineagePathRead(lWorkItem.LWorkOutputPath) is not { } lOutputKey
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
