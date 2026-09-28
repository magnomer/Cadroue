using System.Collections;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal static class PSCombo
{
    internal static ComboBox PSComboBuild(string pSelected, params string[] pItems) =>
        PSComboShapeBuild(
            pItems,
            pItems.Intersect(Enumerable.Repeat(pSelected, 1)).Concat(pItems).FirstOrDefault(),
            PSComboIdleHandle);

    internal static ComboBox PSComboBuild(int pIndex, Action<int> pChoice, params string[] pItems) =>
        PSComboShapeBuild(pItems, pItems.ElementAtOrDefault(pIndex), pChoice);

    internal static ComboBox PSComboBuild(string pSelected, params LLocalizationChoice[] pItems) =>
        PSComboShapeBuild(
            pItems,
            pItems.Where(pItem => string.Equals(pItem.LLocalizationChoiceToken, pSelected, StringComparison.Ordinal))
                .Concat(pItems).FirstOrDefault(),
            PSComboIdleHandle);

    internal static string PSComboTextRead(System.Windows.Controls.Primitives.Selector pCombo) =>
        LLocalizationChoice.LLocalizationChoiceRead(pCombo.SelectedItem);

    private static ComboBox PSComboShapeBuild(IEnumerable pItems, object? pSelected, Action<int> pChoice)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            SelectedItem = pSelected,
            MinWidth = 260,
            Height = PSField.PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.SelectionChanged += (_, _) => pChoice(pCombo.SelectedIndex);
        return pCombo;
    }

    private static void PSComboIdleHandle(int pIndex)
    {
    }
}
