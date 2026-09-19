using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

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
        IReadOnlyList<string> pTokens = LDetector.LDetectorTokensRead(pDetectorKind);
        foreach (string pItem in pTokens)
        {
            pBox.Items.Add(new LLocalizationChoice(pItem, PSensorKeyRead(pDetectorKind, pItem)));
        }

        pBox.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pBox.SelectedIndex = Math.Max(
            0, pTokens.ToList().IndexOf(LSensor.LSensorTokenRead(pDetectorKind) ?? string.Empty));
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

    private static string PSensorKeyRead(LDetectorKind pDetectorKind, string pToken) =>
        LDetector.LDetectorTokensRead(pDetectorKind).Contains(pToken)
            ? "Inspector.Detector." + pToken
            : "Inspector.Common.Custom";
}
