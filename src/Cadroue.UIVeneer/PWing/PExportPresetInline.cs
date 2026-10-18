using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal static class PExportPresetInline
{
    internal static Button PExportInlineBuild(
        string pIconPath, Brush pIconBrush, string pTooltip, RoutedEventHandler pClick)
    {
        Button pButton = PExportGlyphBuild(pIconPath, pIconBrush, pTooltip);
        pButton.Click += pClick;
        return pButton;
    }

    private static Button PExportGlyphBuild(string pIconPath, Brush pIconBrush, string pTooltip)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 16,
                Height = 16,
                Source = QIcon.QIconRead(pIconPath, pIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 22,
            Height = 20,
            Margin = new Thickness(2, 0, 0, 0)
        };
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }
}
