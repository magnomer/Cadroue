using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

internal static class PInspectorSwitch
{
    internal static CheckBox PInspectorSwitchBuild(string pSwitchLabel, string pSwitchTip)
    {
        var pSwitch = new CheckBox
        {
            Content = pSwitchLabel,
            ToolTip = pSwitchTip,
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = QPanel.QPanelTextRead(),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        QInteractionClick.QInteractionClickAttach(pSwitch);
        QCheckbox.QCheckboxAttach(pSwitch);
        return pSwitch;
    }

    internal static void PInspectorSwitchAttach(CheckBox pBox, Action<bool> pSet)
    {
        pBox.Checked += (_, _) => pSet(true);
        pBox.Unchecked += (_, _) => pSet(false);
    }

    internal static void PInspectorSwitchUpdate(CheckBox pBox, bool pChecked) =>
        pBox.IsChecked = PLook.PLookChecked[pChecked];
}
