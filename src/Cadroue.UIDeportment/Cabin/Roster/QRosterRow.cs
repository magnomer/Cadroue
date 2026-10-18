using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QRosterRow
{
    public const string QRosterShadeSelected = "Select";
    public const string QRosterShadeStage = "Stage";
    public const string QRosterShadePlain = "Plain";

    public required Guid QRosterRowId { get; init; }

    public required string QRosterRowStep { get; init; }

    public required string QRosterRowPriority { get; init; }

    public required string QRosterRowLength { get; init; }

    public required string QRosterRowProgress { get; init; }

    public required string QRosterRowRatio { get; init; }

    public required string QRosterRowState { get; init; }

    public required string QRosterRowKey { get; init; }

    public required string QRosterRowOwner { get; init; }

    public required bool QRosterRowLast { get; init; }

    public required bool QRosterRowStage { get; init; }

    public required string QRosterRowShade { get; init; }

    // Paints the gate's row: its wording keys through the catalog, its span and lineage step in the roster's words.
    public static QRosterRow QRosterRowCreate(CLineage lLineage, CRosterRow lRow, string lShade)
    {
        return new QRosterRow
        {
            QRosterRowId = lRow.CRosterRowId,
            QRosterRowStep = QLineage.QLineageStepFormat(lLineage, lRow.CRosterRowStep),
            QRosterRowPriority = QLocalizationCatalog.QLocalizationTextRead(lRow.CRosterRowPriority),
            QRosterRowLength = QRosterSpanFormat(lRow.CRosterRowLength),
            QRosterRowProgress = lRow.CRosterRowProgress,
            QRosterRowRatio = QLineage.QLineageRatioFormat(lRow.CRosterRowStep),
            QRosterRowState = QLocalizationCatalog.QLocalizationTextRead(lRow.CRosterRowState),
            QRosterRowKey = lRow.CRosterRowTone,
            QRosterRowOwner = lRow.CRosterRowOwner is { } lOwner
                ? QLocalizationCatalog.QLocalizationTextRead(lOwner)
                : "-",
            QRosterRowLast = lRow.CRosterRowLast,
            QRosterRowStage = lRow.CRosterRowStage,
            QRosterRowShade = lShade
        };
    }

    // The row's click: the roster's selection takes this row's step with the held modifier keys.
    public void QRosterRowSelect(QRoster lRoster, bool lRange, bool lToggle) =>
        lRoster.QRosterSelection.QRosterStepSelect(QRosterRowId, lRange, lToggle);

    // The items the row's context menu offers as it opens.
    public IReadOnlyList<QRosterItem> QRosterRowOpen(QRoster lRoster, QStrip? lStrip) =>
        lRoster.QRosterMenu.QRosterMenuRead(QRosterRowId, lStrip);

    public static string QRosterShadeResolve(bool lSelected, bool lCardSelected, bool lStage)
    {
        if (lSelected)
        {
            return QRosterShadeSelected;
        }

        return !lCardSelected && lStage ? QRosterShadeStage : QRosterShadePlain;
    }

    public static string QRosterSpanFormat(TimeSpan lSpan) => $"{lSpan:hh\\:mm\\:ss}";
}
