using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private ComboBox PSensorPresetBuild(StackPanel pStack, LDetectorKind pDetectorKind, int pIndex)
    {
        var pBox = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pBox);
        pBox.Items.Add(new LLocalizationChoice("Conservative", "Inspector.Detector.Conservative"));
        pBox.Items.Add(new LLocalizationChoice("Normal", "Inspector.Detector.Normal"));
        pBox.Items.Add(new LLocalizationChoice("Sensitive", "Inspector.Detector.Sensitive"));
        pBox.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pBox.SelectedIndex = 1;
        pBox.SelectionChanged += (_, _) =>
        {
            if (PInspectorPresetRead(
                    pBox, LSensor.LSensorTokenRead(pDetectorKind), LSensor.LSensorMatchRead(pDetectorKind))
                is { } pToken)
            {
                LSensor.LSensorPresetSelect(pDetectorKind, pToken);
            }
        };

        pStack.Children.Insert(pIndex, PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pBox));
        return pBox;
    }

    private static string PSensorKeyRead(string pToken) => pToken switch
    {
        "Conservative" => "Inspector.Detector.Conservative",
        "Normal" => "Inspector.Detector.Normal",
        "Sensitive" => "Inspector.Detector.Sensitive",
        _ => "Inspector.Common.Custom"
    };
}
