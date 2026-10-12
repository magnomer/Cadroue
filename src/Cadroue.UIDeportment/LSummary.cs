using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

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

    public static LSummary LSummaryRead(LRoster lRoster)
    {
        IReadOnlyList<LWorkItem> lBatchItems = lRoster.LRosterBatchRead();
        CLineageBatch lBatch = lRoster.LRosterSummaryRead();
        long? lSourceBytes = lBatch.CLineageSourceTotal;
        long? lOutputBytes = lBatch.CLineageOutputTotal;
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
                    LRosterFormatter.LRosterMebiFormat(lSourceBytes),
                    LRosterFormatter.LRosterMebiFormat(lOutputBytes),
                    LRosterDetail.LRosterLinePlain)
            ],
            LSummarySourceCount = LSummaryFilesFormat(lBatch.CLineageSourceCount),
            LSummaryOutputCount = LSummaryFilesFormat(lBatch.CLineageOutputCount),
            LSummarySources = lBatch.CLineageBatchSources,
            LSummaryOutputs = lBatch.CLineageBatchOutputs
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
            if (LRosterFormatter.LRosterSpentRead(lWorkItem) is { } lSpent)
            {
                lSpentTotal += lSpent;
                lAnySpent = true;
            }
        }

        if (!lAnySpent || lSpentTotal.TotalSeconds <= 0)
        {
            return null;
        }

        return LRosterFormatter.LRosterMeterFormat(
            LRosterFormatter.LRosterElapsedFormat(lSpentTotal),
            LRosterFormatter.LRosterSpeedFormat(lSpentTotal, lOutputBytes));
    }

    public static string LSummaryFilesFormat(int lCount) =>
        lCount == 1
            ? LLocalization.LLocalizationTextRead("Roster.Summary.FileOne")
            : LLocalization.LLocalizationFormat("Roster.Summary.FileMany", lCount);
}
