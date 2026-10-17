using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PExport
{
    private Button PExportInlineBuild(string pIconPath, Brush pIconBrush, string pTooltip, RoutedEventHandler pClick)
    {
        Button pButton = PExportGlyphBuild(pIconPath, pIconBrush, pTooltip);
        PExportClickAttach(pButton, pClick);
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
