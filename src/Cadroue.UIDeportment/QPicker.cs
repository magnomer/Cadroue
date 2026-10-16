using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed record QPickerItem(string QPickerItemToken, string QPickerItemLabel, bool QPickerItemChecked);

public static class QPicker
{
    public static IReadOnlyList<QPickerItem> QPickerItemsCreate(
        IReadOnlyList<LLocalizationChoice> lChoices,
        IReadOnlyList<string> lSelected) =>
        lChoices
            .Select(lChoice => new QPickerItem(
                lChoice.LLocalizationChoiceToken,
                lChoice.ToString(),
                lSelected.Contains(lChoice.LLocalizationChoiceToken, StringComparer.Ordinal)))
            .ToArray();

    public static IReadOnlyList<QPickerItem> QPickerItemSet(
        IReadOnlyList<QPickerItem> lItems,
        string lToken,
        bool lChecked) =>
        lItems
            .Select(lItem => string.Equals(lItem.QPickerItemToken, lToken, StringComparison.Ordinal)
                ? lItem with { QPickerItemChecked = lChecked }
                : lItem)
            .ToArray();

    public static IReadOnlyList<string> QPickerSelectionRead(IReadOnlyList<QPickerItem> lItems) =>
        lItems.Where(lItem => lItem.QPickerItemChecked).Select(lItem => lItem.QPickerItemToken).ToArray();

    public static (string QPickerText, bool QPickerEmpty) QPickerSummaryRead(
        IReadOnlyList<QPickerItem> lItems,
        string lEmptyText) =>
        QPickerSummaryResolve(
            lItems.Where(lItem => lItem.QPickerItemChecked).Select(lItem => lItem.QPickerItemLabel).ToArray(),
            lEmptyText);

    public static (string QPickerText, bool QPickerEmpty) QPickerSummaryResolve(
        IReadOnlyList<string> lLabels,
        string lEmptyText) =>
        lLabels.Count == 0 ? (lEmptyText, true) : (string.Join(", ", lLabels), false);
}
