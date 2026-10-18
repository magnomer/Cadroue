using System.Globalization;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QRosterFile(
    string QRosterFileTitle,
    string QRosterFileShade,
    IReadOnlyList<QRosterRow> QRosterFileRows)
{
    public bool QRosterFileMatch(QRosterFile lOther) =>
        QRosterFileTitle == lOther.QRosterFileTitle
        && QRosterFileShade == lOther.QRosterFileShade
        && QRosterFileRows.SequenceEqual(lOther.QRosterFileRows);
}

public sealed record QRosterEntry(
    QRosterCard QRosterEntryCard,
    string QRosterEntryKind,
    string QRosterEntryFrame,
    QRosterFile? QRosterEntryFile,
    QRosterRow? QRosterEntryRow)
{
    public const string QRosterKindHead = "Head";
    public const string QRosterKindFile = "File";
    public const string QRosterKindRow = "Row";
    public const string QRosterFrameWhole = "Whole";
    public const string QRosterFrameTop = "Top";
    public const string QRosterFrameMiddle = "Middle";
    public const string QRosterFrameBottom = "Bottom";

    public static IReadOnlyList<QRosterEntry> QRosterEntriesCreate(QRosterCard lCard)
    {
        QRosterEntry[] lBody = lCard.QRosterCardCollapsed
            ? []
            : lCard.QRosterCardFiles
                .SelectMany(lFile => lFile.QRosterFileRows
                    .Select(lRow => new QRosterEntry(lCard, QRosterKindRow, QRosterFrameMiddle, lFile, lRow))
                    .Prepend(new QRosterEntry(lCard, QRosterKindFile, QRosterFrameMiddle, lFile, null)))
                .ToArray();
        var lHead = new QRosterEntry(
            lCard, QRosterKindHead, lBody.Length == 0 ? QRosterFrameWhole : QRosterFrameTop, null, null);
        return lBody.Length == 0
            ? [lHead]
            : [lHead, .. lBody[..^1], lBody[^1] with { QRosterEntryFrame = QRosterFrameBottom }];
    }
}

public sealed record QRosterCard
{
    public const string QRosterCardSelected = "Select";
    public const string QRosterCardDone = "Done";
    public const string QRosterCardPlain = "Plain";

    public required Guid QRosterCardBatch { get; init; }

    public required string QRosterCardTitle { get; init; }

    public required string QRosterCardKey { get; init; }

    public required string QRosterCardBody { get; init; }

    public required bool QRosterCardCollapsed { get; init; }

    public required string QRosterCardTooltip { get; init; }

    public required IReadOnlyList<QRosterFile> QRosterCardFiles { get; init; }

    public bool QRosterCardMatch(QRosterCard lOther) =>
        QRosterCardBatch == lOther.QRosterCardBatch
        && QRosterCardTitle == lOther.QRosterCardTitle
        && QRosterCardKey == lOther.QRosterCardKey
        && QRosterCardBody == lOther.QRosterCardBody
        && QRosterCardCollapsed == lOther.QRosterCardCollapsed
        && QRosterCardTooltip == lOther.QRosterCardTooltip
        && QRosterCardFiles.Count == lOther.QRosterCardFiles.Count
        && QRosterCardFiles.Zip(lOther.QRosterCardFiles).All(lPair => lPair.First.QRosterFileMatch(lPair.Second));

    // The gate answers the batch and its files in one call; the card picks its brushes and wording from that answer.
    public static QRosterCard QRosterCardCreate(
        CLineage lLineage,
        CRosterCard lGate,
        Guid lBatchId,
        CLineageAnswer lAnswer,
        Guid lRunner,
        bool lSelected,
        bool lCollapsed,
        Func<Guid, bool> lSelectedCheck)
    {
        (CLineageBatch lRecord, IReadOnlyList<CRosterFile> lFiles) =
            lGate.CRosterCardRead(lBatchId, lAnswer, lRunner, lSelectedCheck);
        bool lCompleted = lRecord.CLineageBatchCompleted;
        return new QRosterCard
        {
            QRosterCardBatch = lBatchId,
            QRosterCardTitle = QRosterTitleFormat(lRecord.CLineageBatchEarliest, lRecord.CLineageBatchInitial),
            QRosterCardKey = lSelected ? QRosterCardSelected : lCompleted ? QRosterCardDone : QRosterCardPlain,
            QRosterCardBody = lCompleted ? QRosterCardDone : lSelected ? QRosterCardSelected : QRosterCardPlain,
            QRosterCardCollapsed = lCollapsed,
            QRosterCardTooltip = QLocalizationCatalog.QLocalizationTextRead(
                lCollapsed ? "Roster.Card.Expand" : "Roster.Card.Collapse"),
            QRosterCardFiles = lFiles.Select(lFile => QRosterFileCreate(lLineage, lFile, lSelected)).ToArray()
        };
    }

    public static string QRosterTitleFormat(DateTimeOffset lEarliest, int lInitialCount)
    {
        string lTitle = lEarliest.LocalDateTime.ToString(
            "yyyy-MM-dd tt h:mm", CultureInfo.CurrentUICulture);
        return lInitialCount == 1
            ? QLocalizationCatalog.QLocalizationFormat("Roster.Card.One", lTitle)
            : QLocalizationCatalog.QLocalizationFormat("Roster.Card.Many", lTitle, lInitialCount);
    }

    private static QRosterFile QRosterFileCreate(CLineage lLineage, CRosterFile lFile, bool lCardSelected) =>
        new(
            QLineage.QLineageTitleFormat(lFile.CRosterFileEntry),
            QRosterRow.QRosterShadeResolve(false, lCardSelected, lFile.CRosterFileStage),
            lFile.CRosterFileRows.Select(lRow => QRosterRow.QRosterRowCreate(
                lLineage,
                lRow,
                QRosterRow.QRosterShadeResolve(
                    lRow.CRosterRowSelected,
                    lCardSelected,
                    lRow.CRosterRowStage))).ToArray());
}
