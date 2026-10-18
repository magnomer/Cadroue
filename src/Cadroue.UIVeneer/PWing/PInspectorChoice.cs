using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal static class PInspectorChoice
{
    internal static ComboBox PInspectorComboBuild()
    {
        var pCombo = new ComboBox
        {
            Height = PInspectorLook.PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily
        };
        QDropdown.QDropdownAttach(pCombo);
        return pCombo;
    }

    internal static ComboBox PInspectorChoiceBuild(IReadOnlyList<string> pNames, Action<int> pSelect)
    {
        ComboBox pCombo = PInspectorComboBuild();
        PInspectorChoiceAppend(pCombo, pNames);
        PInspectorChoiceAttach(pCombo, pSelect);
        return pCombo;
    }

    private static void PInspectorChoiceAppend(ComboBox pCombo, IReadOnlyList<string> pNames) =>
        pNames.ToList().ForEach(pName => pCombo.Items.Add(pName));

    private static void PInspectorChoiceAttach(ComboBox pCombo, Action<int> pSelect) =>
        pCombo.SelectionChanged += (_, _) => pSelect(pCombo.SelectedIndex);

    internal static void PInspectorChoiceApply(ComboBox pCombo, QInspectorChoice lChoice)
    {
        pCombo.Items[lChoice.QInspectorChoiceCustom] = lChoice.QInspectorChoiceText;
        pCombo.SelectedIndex = lChoice.QInspectorChoiceIndex;
    }
}
