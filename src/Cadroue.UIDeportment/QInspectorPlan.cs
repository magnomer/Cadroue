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
    private const string QInspectorCustomToken = "Custom";
    private const string QInspectorCustomKey = "Inspector.Common.Custom";
    private const string QInspectorCustomBased = "Inspector.Common.PresetCustom";

    public static QInspectorRow QInspectorRowCreate(
        int lIndex, string lLabelKey, string lUnit, string lFormat, double lLeast, double lMost) =>
        new(lIndex, QLocalizationCatalog.QLocalizationTextRead(lLabelKey), lUnit, lFormat, lLeast, lMost);

    public static QInspectorChoice QInspectorChoiceRead(
        IReadOnlyList<string> lTokens, Func<string, string> lKeyRead, string? lToken, string? lMatch)
    {
        string lText = lMatch is null && lToken is not null
            ? QLocalizationCatalog.QLocalizationFormat(
                QInspectorCustomBased, QLocalizationCatalog.QLocalizationTextRead(lKeyRead(lToken)))
            : QLocalizationCatalog.QLocalizationTextRead(QInspectorCustomKey);
        List<string> lNames = lTokens
            .Select(lEntry => QLocalizationCatalog.QLocalizationTextRead(lKeyRead(lEntry)))
            .ToList();
        lNames.Add(lText);
        int lIndex = lMatch is null ? -1 : lTokens.ToList().IndexOf(lMatch);
        return new QInspectorChoice(lNames, lIndex < 0 ? lTokens.Count : lIndex, lTokens.Count, lText);
    }

    // A gate's preset rows as a combo: each row's wording, the row the gate marked chosen, and its last row as custom.
    public static QInspectorChoice QInspectorChoiceBuild(IReadOnlyList<CChoice> lRows)
    {
        List<string> lNames = lRows
            .Select(static lRow => lRow.CChoiceBasis is { } lBasis
                ? QLocalizationCatalog.QLocalizationFormat(
                    lRow.CChoiceKey, QLocalizationCatalog.QLocalizationTextRead(lBasis))
                : QLocalizationCatalog.QLocalizationTextRead(lRow.CChoiceKey))
            .ToList();
        int lCustom = lRows.Count - 1;
        int lChosen = lRows.ToList().FindIndex(lRow => lRow.CChoiceChosen);
        return new QInspectorChoice(lNames, lChosen, lCustom, lNames[lCustom]);
    }

    public static string? QInspectorChoiceResolve(
        IReadOnlyList<string> lTokens, int lIndex, string? lToken, string? lMatch)
    {
        if (lIndex < 0 || lIndex >= lTokens.Count)
        {
            return null;
        }

        string lSelected = lTokens[lIndex];
        return lSelected == QInspectorCustomToken || (lSelected == lToken && lMatch == lSelected)
            ? null
            : lSelected;
    }
}
