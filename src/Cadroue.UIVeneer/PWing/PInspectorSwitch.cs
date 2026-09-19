using System.Windows;
using System.Windows.Controls;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private static CheckBox PInspectorSwitchBuild(string pSwitchLabel, string pSwitchTip)
    {
        var pSwitch = new CheckBox
        {
            Content = pSwitchLabel,
            ToolTip = pSwitchTip,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pSwitch);
        return pSwitch;
    }

    private static UIElement PInspectorSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PPanelLineBrush,
        Margin = new Thickness(0, 12, 0, 12)
    };
}
