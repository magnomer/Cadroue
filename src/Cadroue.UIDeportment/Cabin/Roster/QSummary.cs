using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QSummary
{
    public required IReadOnlyList<string> QSummaryMeters { get; init; }

    public required IReadOnlyList<QRosterBar> QSummaryBars { get; init; }

    public required IReadOnlyList<QRosterCompareRow> QSummaryCompares { get; init; }

    public required string QSummarySourceCount { get; init; }

    public required string QSummaryOutputCount { get; init; }

    public required IReadOnlyList<string> QSummarySources { get; init; }

    public required IReadOnlyList<string> QSummaryOutputs { get; init; }

    public static QSummary QSummaryRead(QRoster lRoster)
    {
        CLineageBatch lBatch = lRoster.QRosterSummaryRead();
        long? lSourceBytes = lBatch.CLineageSourceTotal;
        long? lOutputBytes = lBatch.CLineageOutputTotal;
        string? lMeter = QSummaryMeterFormat(lBatch.CLineageBatchSpent, lOutputBytes);
        return new QSummary
        {
            QSummaryMeters = lMeter is null ? [] : [lMeter],
            QSummaryBars = QRosterDetail.QRosterBarsCreate(lSourceBytes, lOutputBytes),
            QSummaryCompares =
            [
                QSummaryCompareCreate(
                    QLocalizationCatalog.QLocalizationTextRead("Roster.Section.Source"),
                    QLocalizationCatalog.QLocalizationTextRead("Roster.Section.Output"),
                    QRosterDetail.QRosterLineHead),
                QSummaryCompareCreate(
                    QRosterFormatter.QRosterMebiFormat(lSourceBytes),
                    QRosterFormatter.QRosterMebiFormat(lOutputBytes),
                    QRosterDetail.QRosterLinePlain)
            ],
            QSummarySourceCount = QSummaryFilesFormat(lBatch.CLineageSourceCount),
            QSummaryOutputCount = QSummaryFilesFormat(lBatch.CLineageOutputCount),
            QSummarySources = lBatch.CLineageBatchSources,
            QSummaryOutputs = lBatch.CLineageBatchOutputs
        };
    }

    private static QRosterCompareRow QSummaryCompareCreate(string lSource, string lOutput, string lKey) =>
        new(new QRosterLine(lSource, lKey), new QRosterLine(lOutput, lKey));

    // The meter shows the gate's spent time for the batch; no spent time shows no meter.
    public static string? QSummaryMeterFormat(TimeSpan? lSpent, long? lOutputBytes) =>
        lSpent is { } lSpentTotal
            ? QRosterFormatter.QRosterMeterFormat(
                QRosterFormatter.QRosterElapsedFormat(lSpentTotal),
                QRosterFormatter.QRosterSpeedFormat(lSpentTotal, lOutputBytes))
            : null;

    public static string QSummaryFilesFormat(int lCount) =>
        lCount == 1
            ? QLocalizationCatalog.QLocalizationTextRead("Roster.Summary.FileOne")
            : QLocalizationCatalog.QLocalizationFormat("Roster.Summary.FileMany", lCount);
}
