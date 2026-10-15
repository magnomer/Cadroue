
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
