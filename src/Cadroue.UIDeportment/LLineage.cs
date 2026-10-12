using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public static class LLineage
{
    private static readonly IReadOnlyDictionary<string, string> LLineageKinds =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(LWorkKind.LWorkKindEdit)] = "Roster.Kind.Edit",
            [nameof(LWorkKind.LWorkKindFix)] = "Roster.Kind.Fix",
            [nameof(LWorkKind.LWorkKindAudio)] = "Roster.Kind.Audio",
            [nameof(LWorkKind.LWorkKindConvert)] = "Roster.Kind.Convert",
            [nameof(LWorkKind.LWorkKindMerge)] = "Roster.Kind.Merge"
        };

    public static string LLineageStepFormat(CLineage lLineage, CLineageStep lStep)
    {
        if (lStep.CLineageStepKind == nameof(LWorkKind.LWorkKindSplit) && !lStep.CLineageStepSubject)
        {
            return LLocalization.LLocalizationFormat(
                "Roster.Lineage.Split", LLineageNameFormat(lStep.CLineageStepName));
        }

        string lTabName = lLineage.CLineageTitleRead(
            lStep.CLineageStepBatch, lStep.CLineageStepRelay, lStep.CLineageStepTab);
        return LLocalization.LLocalizationFormat(
            "Roster.Lineage.Step",
            string.IsNullOrWhiteSpace(lTabName) ? LLineageKindRead(lStep.CLineageStepKind) : lTabName);
    }

    private static string LLineageKindRead(string lKind) =>
        LLocalization.LLocalizationTextRead(LLineageKinds.GetValueOrDefault(lKind, "Roster.Kind.Split"));

    public static string LLineageRatioFormat(CLineageStep lStep) =>
        lStep.CLineageStepRatio is { } lRatio ? $"{lRatio:P1}" : "-";

    public static string LLineageTitleFormat(CLineageEntry lEntry)
    {
        string lLineageName = LLineageNameFormat(lEntry.CLineageEntryName);
        return lEntry.CLineageEntryIds.Count == 1
            ? LLocalization.LLocalizationFormat("Roster.Lineage.One", lLineageName)
            : LLocalization.LLocalizationFormat("Roster.Lineage.Many", lLineageName, lEntry.CLineageEntryIds.Count);
    }

    private static string LLineageNameFormat(string? lName) =>
        lName ?? LLocalization.LLocalizationTextRead("Roster.Lineage.Unknown");
}
