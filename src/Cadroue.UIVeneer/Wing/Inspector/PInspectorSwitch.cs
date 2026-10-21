using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PInspectorSwitch
{
    internal static CheckBox PInspectorSwitchBuild(string pSwitchLabel, string pSwitchTip)
    {
        var pSwitch = new CheckBox
        {
            Content = pSwitchLabel,
            ToolTip = pSwitchTip,
            FontSize = 12,
            FontFamily = QTool.QToolFontRead(),
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
