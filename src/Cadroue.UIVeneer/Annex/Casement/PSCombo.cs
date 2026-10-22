using System.Collections;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSCombo
{
    internal static ComboBox PSComboBuild(int pIndex, Action<int> pChoice, params string[] pItems) =>
        PSComboShapeBuild(pItems, pItems.ElementAtOrDefault(pIndex), pChoice);

    private static ComboBox PSComboShapeBuild(IEnumerable pItems, object? pSelected, Action<int> pChoice)
    {
        ComboBox pCombo = PSComboControlBuild(pItems, pSelected);
        PSComboChoiceAttach(pCombo, pChoice);
        return pCombo;
    }

    private static ComboBox PSComboControlBuild(IEnumerable pItems, object? pSelected)
    {
        var pCombo = new ComboBox
        {
            ItemsSource = pItems,
            SelectedItem = pSelected,
            MinWidth = 260,
            Height = PSField.PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        QDropdown.QDropdownAttach(pCombo);
        return pCombo;
    }

    private static void PSComboChoiceAttach(ComboBox pCombo, Action<int> pChoice) =>
        pCombo.SelectionChanged += (_, _) => pChoice(pCombo.SelectedIndex);
}
