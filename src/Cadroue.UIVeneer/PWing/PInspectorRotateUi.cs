using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private ComboBox PInspectorRotateBuild()
    {
        var pRotateCombo = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pRotateCombo);
        pRotateCombo.Items.Add(new LLocalizationChoice("None", "Inspector.Crop.None"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Clockwise90", "Inspector.Crop.Clockwise90"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Degrees180", "Inspector.Crop.Degrees180"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Clockwise270", "Inspector.Crop.Clockwise270"));
        pRotateCombo.SelectedIndex = 0;
        pRotateCombo.SelectionChanged += (_, _) =>
        {
            if (pRotateCombo.SelectedIndex >= 0)
            {
                PInspectorRotateChange(pRotateCombo.SelectedIndex);
            }
        };
        return pRotateCombo;
    }
}
