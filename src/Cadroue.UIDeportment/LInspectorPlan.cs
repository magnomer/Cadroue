using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed record LInspectorRow(
    int LInspectorRowIndex,
    string LInspectorRowLabel,
    string LInspectorRowUnit,
    string LInspectorRowPattern,
    double LInspectorRowLeast,
    double LInspectorRowMost);

public sealed record LInspectorChoice(
    IReadOnlyList<string> LInspectorChoiceNames,
    int LInspectorChoiceIndex,
    int LInspectorChoiceCustom,
    string LInspectorChoiceText);

public static class LInspectorPlan
{
    private const string LInspectorCustomToken = "Custom";
    private const string LInspectorCustomKey = "Inspector.Common.Custom";
    private const string LInspectorCustomBased = "Inspector.Common.PresetCustom";

    public static LInspectorRow LInspectorRowCreate(
        int lIndex, string lLabelKey, string lUnit, string lFormat, double lLeast, double lMost) =>
        new(lIndex, LLocalization.LLocalizationTextRead(lLabelKey), lUnit, lFormat, lLeast, lMost);

    public static LInspectorChoice LInspectorChoiceRead(
        IReadOnlyList<string> lTokens, Func<string, string> lKeyRead, string? lToken, string? lMatch)
    {
        string lText = lMatch is null && lToken is not null
            ? LLocalization.LLocalizationFormat(
                LInspectorCustomBased, LLocalization.LLocalizationTextRead(lKeyRead(lToken)))
            : LLocalization.LLocalizationTextRead(LInspectorCustomKey);
        List<string> lNames = lTokens
            .Select(lEntry => LLocalization.LLocalizationTextRead(lKeyRead(lEntry)))
            .ToList();
        lNames.Add(lText);
        int lIndex = lMatch is null ? -1 : lTokens.ToList().IndexOf(lMatch);
        return new LInspectorChoice(lNames, lIndex < 0 ? lTokens.Count : lIndex, lTokens.Count, lText);
    }

    public static string? LInspectorChoiceResolve(
        IReadOnlyList<string> lTokens, int lIndex, string? lToken, string? lMatch)
    {
        if (lIndex < 0 || lIndex >= lTokens.Count)
        {
            return null;
        }

        string lSelected = lTokens[lIndex];
        return lSelected == LInspectorCustomToken || (lSelected == lToken && lMatch == lSelected)
            ? null
            : lSelected;
    }
}
