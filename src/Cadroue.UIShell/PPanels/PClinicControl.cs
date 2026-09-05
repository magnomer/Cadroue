using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PClinic
{
    internal static CheckBox PClinicSwitchBuild(string pSwitchLabel, string pSwitchTip)
    {
        var pSwitch = new CheckBox
        {
            Content = pSwitchLabel,
            ToolTip = pSwitchTip,
            FontSize = 12,
            FontFamily = pClinicFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        PCheckbox.PCheckboxApply(pSwitch);
        return pSwitch;
    }

    private static UIElement PClinicSeparatorBuild() => new Border
    {
        Height = 1,
        Background = PPanelLineBrush,
        Margin = new Thickness(0, 10, 0, 10)
    };

    private static Button PClinicButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pClinicIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }
}
