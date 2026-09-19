using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PCabin;

public sealed partial class PConsole
{
    private const double PConsoleIndicatorSize = PRosterTheme.PRosterIconSize;

    private static readonly Brush pConsoleSpinnerBrush = PConsoleSpinnerCreate();

    private static Grid PConsoleRestBuild()
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
            Source = PIcon.PIconRead("/PAsset/PPanel/PConsoleRest.svg", PRosterTheme.PRosterDoneBrush),
            Width = 18.2,
            Height = 18.2,
            Stretch = Stretch.Uniform
        });
        return pRest;
    }

    private static Brush PConsoleSpinnerCreate()
    {
        var pBrush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1)
        };
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1E, 0x59, 0xBE), 0));
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x3E, 0x92, 0xE4), 0.5));
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x74, 0xCB, 0xF7), 1));
        pBrush.Freeze();
        return pBrush;
    }

    private Path PConsoleSpinnerBuild()
    {
        const double pStroke = 3;
        double pRadius = (PConsoleIndicatorSize - pStroke) / 2;
        double pCenter = PConsoleIndicatorSize / 2;
        var pStart = new System.Windows.Point(pCenter, pCenter - pRadius);
        var pEnd = new System.Windows.Point(pCenter - pRadius, pCenter);

        var pFigure = new PathFigure { StartPoint = pStart };
        pFigure.Segments.Add(new ArcSegment(
            pEnd, new System.Windows.Size(pRadius, pRadius), 0, true, SweepDirection.Clockwise, true));
        var pGeometry = new PathGeometry();
        pGeometry.Figures.Add(pFigure);
        pGeometry.Freeze();

        return new Path
        {
            Data = pGeometry,
            Stroke = pConsoleSpinnerBrush,
            StrokeThickness = pStroke,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Width = PConsoleIndicatorSize,
            Height = PConsoleIndicatorSize,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
            RenderTransform = pConsoleSpinnerRotate,
            Visibility = Visibility.Collapsed
        };
    }

    private void PConsoleSpinnerSet(bool pActive)
    {
        if (!LConsole.LConsoleSpinSet(pActive))
        {
            return;
        }

        if (pActive)
        {
            var pSpin = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(TimeSpan.FromSeconds(1.1)),
                RepeatBehavior = RepeatBehavior.Forever
            };
            pConsoleSpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, pSpin);
        }
        else
        {
            pConsoleSpinnerRotate.BeginAnimation(RotateTransform.AngleProperty, null);
        }
    }
}
