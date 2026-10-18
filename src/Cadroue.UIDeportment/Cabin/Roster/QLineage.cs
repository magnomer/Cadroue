using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public static class QLineage
{
    private static readonly IReadOnlyDictionary<string, string> QLineageKinds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(LWorkKind.LWorkKindEdit)] = "Roster.Kind.Edit",
            [nameof(LWorkKind.LWorkKindFix)] = "Roster.Kind.Fix",
            [nameof(LWorkKind.LWorkKindAudio)] = "Roster.Kind.Audio",
            [nameof(LWorkKind.LWorkKindConvert)] = "Roster.Kind.Convert",
            [nameof(LWorkKind.LWorkKindMerge)] = "Roster.Kind.Merge"
        };

    public static string QLineageStepFormat(CLineage lLineage, CLineageStep lStep)
    {
        if (lStep.CLineageStepKind == nameof(LWorkKind.LWorkKindSplit) && !lStep.CLineageStepSubject)
        {
            return QLocalizationCatalog.QLocalizationFormat(
                "Roster.Lineage.Split", QLineageNameFormat(lStep.CLineageStepName));
        }

        string lTabName = lLineage.CLineageTitleRead(
            lStep.CLineageStepBatch, lStep.CLineageStepRelay, lStep.CLineageStepTab);
        return QLocalizationCatalog.QLocalizationFormat(
            "Roster.Lineage.Step",
            string.IsNullOrWhiteSpace(lTabName) ? QLineageKindRead(lStep.CLineageStepKind) : lTabName);
    }

    private static string QLineageKindRead(string lKind) =>
        QLocalizationCatalog.QLocalizationTextRead(QLineageKinds.GetValueOrDefault(lKind, "Roster.Kind.Split"));

    public static string QLineageRatioFormat(CLineageStep lStep) =>
        lStep.CLineageStepRatio is { } lRatio ? $"{lRatio:P1}" : "-";

    public static string QLineageTitleFormat(CLineageEntry lEntry)
    {
        string lLineageName = QLineageNameFormat(lEntry.CLineageEntryName);
        return lEntry.CLineageEntryIds.Count == 1
            ? QLocalizationCatalog.QLocalizationFormat("Roster.Lineage.One", lLineageName)
            : QLocalizationCatalog.QLocalizationFormat("Roster.Lineage.Many", lLineageName,
                lEntry.CLineageEntryIds.Count);
    }

    private static string QLineageNameFormat(string? lName) =>
        lName ?? QLocalizationCatalog.QLocalizationTextRead("Roster.Lineage.Unknown");
}
