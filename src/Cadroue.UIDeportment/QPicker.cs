using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed record QPickerItem(string QPickerItemToken, string QPickerItemLabel, bool QPickerItemChecked);

public static class QPicker
{
    public static IReadOnlyList<QPickerItem> QPickerItemsCreate(
        IReadOnlyList<string> lTokens,
        IReadOnlyList<string> lSelected) =>
        lTokens
            .Select(lToken => new QPickerItem(lToken, lToken, lSelected.Contains(lToken, StringComparer.Ordinal)))
            .ToArray();

    public static IReadOnlyList<QPickerItem> QPickerItemsCreate(
        IReadOnlyList<LLocalizationChoice> lChoices,
        IReadOnlyList<string> lSelected) =>
        lChoices
            .Select(lChoice => new QPickerItem(
                lChoice.LLocalizationChoiceToken,
                lChoice.ToString(),
                lSelected.Contains(lChoice.LLocalizationChoiceToken, StringComparer.Ordinal)))
            .ToArray();

    public static (string QPickerText, bool QPickerEmpty) QPickerSummaryResolve(
        IReadOnlyList<string> lLabels,
        string lEmptyText) =>
        lLabels.Count == 0 ? (lEmptyText, true) : (string.Join(", ", lLabels), false);
}
