using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal static class PSCombo
{
    internal static ComboBox PSComboBuild(string pSelected, params string[] pItems)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = PSField.PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.SelectedItem = pItems.Contains(pSelected) ? pSelected : pItems.FirstOrDefault();
        return pCombo;
    }

    internal static ComboBox PSComboBuild(string pSelected, params LLocalizationChoice[] pItems)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            MinWidth = 260,
            Height = PSField.PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PDropdown.PDropdownApply(pCombo);
        pCombo.SelectedItem = pItems.FirstOrDefault(
            pItem => string.Equals(pItem.LLocalizationChoiceToken, pSelected, StringComparison.Ordinal))
            ?? pItems.FirstOrDefault();
        return pCombo;
    }

    internal static string PSComboTextRead(System.Windows.Controls.Primitives.Selector pCombo) =>
        LLocalizationChoice.LLocalizationChoiceRead(pCombo.SelectedItem);
}
