using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal static class PGroupButton
{
    private static readonly Brush pGroupIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    public static Button PGroupButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler pClick)
    {
        Button pButton = PGroupButtonCreate(pIconPath, pTooltip);
        PGroupButtonAttach(pButton, pClick);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static Button PGroupButtonCreate(string pIconPath, string pTooltip) => new()
    {
        Content = new Image
        {
            Width = 20,
            Height = 20,
            Source = QIcon.QIconRead(pIconPath, pGroupIconBrush),
            Stretch = Stretch.Uniform
        },
        ToolTip = pTooltip,
        Width = 28,
        Height = 26,
        Margin = new Thickness(0, 0, 2, 0)
    };

    private static void PGroupButtonAttach(Button pButton, RoutedEventHandler pClick) => pButton.Click += pClick;
}
