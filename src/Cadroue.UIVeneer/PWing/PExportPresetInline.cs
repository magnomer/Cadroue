using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PExport
{
    private Button PExportInlineBuild(string pIconPath, Brush pIconBrush, string pTooltip, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 13,
                Height = 13,
                Source = PIcon.PIconRead(pIconPath, pIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 22,
            Height = 20,
            Margin = new Thickness(2, 0, 0, 0),
            Style = PExportStyleRead()
        };
        pButton.Click += pClick;
        return pButton;
    }

}
