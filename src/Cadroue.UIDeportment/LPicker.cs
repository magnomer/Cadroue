using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed record LPickerItem(string LPickerItemToken, string LPickerItemLabel, bool LPickerItemChecked);

public static class LPicker
{
    public static IReadOnlyList<LPickerItem> LPickerItemsCreate(
        IReadOnlyList<string> lTokens,
        IReadOnlyList<string> lSelected) =>
        lTokens
            .Select(lToken => new LPickerItem(lToken, lToken, lSelected.Contains(lToken, StringComparer.Ordinal)))
            .ToArray();

    public static IReadOnlyList<LPickerItem> LPickerItemsCreate(
        IReadOnlyList<LLocalizationChoice> lChoices,
        IReadOnlyList<string> lSelected) =>
        lChoices
            .Select(lChoice => new LPickerItem(
                lChoice.LLocalizationChoiceToken,
                lChoice.ToString(),
                lSelected.Contains(lChoice.LLocalizationChoiceToken, StringComparer.Ordinal)))
            .ToArray();

    public static (string LPickerText, bool LPickerEmpty) LPickerSummaryResolve(
        IReadOnlyList<string> lLabels,
        string lEmptyText) =>
        lLabels.Count == 0 ? (lEmptyText, true) : (string.Join(", ", lLabels), false);
}
