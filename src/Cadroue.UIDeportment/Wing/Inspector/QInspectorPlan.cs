using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QInspectorRow(
    int QInspectorRowIndex,
    string QInspectorRowLabel,
    string QInspectorRowUnit,
    string QInspectorRowPattern,
    double QInspectorRowLeast,
    double QInspectorRowMost);

public sealed record QInspectorChoice(
    IReadOnlyList<string> QInspectorChoiceNames,
    int QInspectorChoiceIndex,
    int QInspectorChoiceCustom,
    string QInspectorChoiceText);

public static class QInspectorPlan
{
    public static QInspectorRow QInspectorRowCreate(
        int lIndex, string lLabelKey, string lUnit, string lFormat, double lLeast, double lMost) =>
        new(lIndex, QLocalizationCatalog.QLocalizationTextRead(lLabelKey), lUnit, lFormat, lLeast, lMost);

    // A gate's preset rows as a combo: each row's wording, the row the gate marked chosen, and its last row as custom.
    public static QInspectorChoice QInspectorChoiceBuild(CChoicePlan lPlan)
    {
        List<string> lNames = lPlan.CChoiceRows
            .Select(static lRow => lRow.CChoiceBasis is { } lBasis
                ? QLocalizationCatalog.QLocalizationFormat(
                    lRow.CChoiceKey, QLocalizationCatalog.QLocalizationTextRead(lBasis))
                : QLocalizationCatalog.QLocalizationTextRead(lRow.CChoiceKey))
            .ToList();
        int lCustom = lPlan.CChoiceRows.Count - 1;
        return new QInspectorChoice(
            lNames, lPlan.CChoiceSelectedIndex, lCustom, lNames[lCustom]);
    }
}
