using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QFixRow(LFlawKind QFixRowKind, QProcessingRow QFixRowStep);

// The fix steps the tab offers: one row per flaw kind the checkup can find, then the salvage step.
public static class QFixCatalog
{
    private const string QFixSalvageKey = "Salvage";

    public static readonly IReadOnlyList<QFixRow> QFixKinds =
    [
        QFixRowCreate(LFlawKind.LFlawKindTruncation, "Truncation"),
        QFixRowCreate(LFlawKind.LFlawKindIndex, "Index"),
        QFixRowCreate(LFlawKind.LFlawKindContainer, "Container"),
        QFixRowCreate(LFlawKind.LFlawKindTiming, "Timing"),
        QFixRowCreate(LFlawKind.LFlawKindMetadata, "Metadata"),
        QFixRowCreate(LFlawKind.LFlawKindCoded, "Coded"),
        QFixRowCreate(LFlawKind.LFlawKindFraming, "Framing"),
        QFixRowCreate(LFlawKind.LFlawKindConfig, "Config", "Configuration"),
        QFixRowCreate(LFlawKind.LFlawKindTransport, "Transport"),
        QFixRowCreate(LFlawKind.LFlawKindSecondary, "Secondary"),
        QFixRowCreate(LFlawKind.LFlawKindFfvone, "Ffvone"),
    ];

    public static readonly IReadOnlyList<QProcessingRow> QFixRows =
        QFixKinds.Select(lRow => lRow.QFixRowStep)
            .Append(new QProcessingRow(
                QFixSalvageKey, "/PAsset/PPanel/PProcessingFixSalvage.svg", "Processing.Step.Salvage"))
            .ToList();

    private static QFixRow QFixRowCreate(LFlawKind lKind, string lName, string? lIcon = null) =>
        new(lKind, new QProcessingRow(
            lName, $"/PAsset/PPanel/PProcessingFix{lIcon ?? lName}.svg", $"Processing.Step.{lName}"));
}
