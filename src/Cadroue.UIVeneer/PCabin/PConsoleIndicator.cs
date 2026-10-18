using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PCabin;

public static class PConsoleIndicator
{
    private const double PConsoleIndicatorSize = PRosterTheme.PRosterIconSize;

    internal static Grid PConsoleRestBuild()
    {
        var pRest = new Grid
        {
            Width = PConsoleIndicatorSize,
            Height = PConsoleIndicatorSize,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            Visibility = Visibility.Collapsed
        };
        pRest.Children.Add(new Ellipse
        {
            Stroke = PRosterTheme.PRosterDoneBrush,
            StrokeThickness = 1
        });
        pRest.Children.Add(new Image
        {
            Source = QIcon.QIconRead("/PAsset/PPanel/PConsoleRest.svg", PRosterTheme.PRosterDoneBrush),
            Width = 18.2,
            Height = 18.2,
            Stretch = Stretch.Uniform
        });
        return pRest;
    }

    internal static Image PConsoleSpinnerBuild()
    {
        return new Image
        {
            Source = QIcon.QIconRead("/PAsset/P32/PPanel/ConsoleSpinner.png"),
            Stretch = Stretch.Uniform,
            Width = PConsoleIndicatorSize,
            Height = PConsoleIndicatorSize,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            Visibility = Visibility.Collapsed
        };
    }
}
