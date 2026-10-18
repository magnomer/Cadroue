using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public static class PViewerControl
{
    internal static Button PViewerButtonBuild(string pIconPath, string pTipKey, double pRightMargin)
    {
        return QButton.QButtonBuild(
            new Button
            {
                Content = new Image
                {
                    Width = 16,
                    Height = 16,
                    Source = QIcon.QIconRead(pIconPath, new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
                    Stretch = Stretch.Uniform
                },
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 16, pRightMargin, 0),
                ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTipKey),
                Visibility = Visibility.Collapsed
            },
            QButtonLook.QButtonLookPanel);
    }

    internal static Button PViewerAudioBuild()
    {
        return QButton.QButtonBuild(
            new Button
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(16, 52, 0, 0),
                MinWidth = 84,
                Height = 24,
                Padding = new Thickness(12, 0, 12, 0),
                FontSize = 11,
                Visibility = Visibility.Collapsed
            },
            QButtonLook.QButtonLookPanel);
    }

    internal static Border PViewerSlotBuild()
    {
        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16, 16, 0, 0)
        };
    }
}
