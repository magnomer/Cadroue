using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Cadroue.UIVeneer.PAsset;

namespace Cadroue.UIVeneer.PCabin;

public static class PConsoleIndicator
{
    private const double PConsoleIndicatorSize = PRosterTheme.PRosterIconSize;
    private const double PConsoleProgressHeight = 14;
    private const double PConsoleProgressRadius = 7;
    private const double PConsoleSpinnerStroke = 3;
    private const double PConsoleSpinnerRadius = 10.5;
    private const double PConsoleSpinnerCenter = 12;
    private const double PConsoleSpinnerTop = 1.5;

    private static readonly Brush pConsoleSpinnerBrush = PConsoleSpinnerCreate();
    private static readonly Brush pConsoleProgressBrush = PConsoleProgressCreate();
    private static readonly Brush pConsoleProgressGloss = PConsoleGlossCreate();

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

    internal static Path PConsoleSpinnerBuild(RotateTransform pRotate)
    {
        var pStart = new System.Windows.Point(PConsoleSpinnerCenter, PConsoleSpinnerTop);
        var pEnd = new System.Windows.Point(PConsoleSpinnerTop, PConsoleSpinnerCenter);

        var pFigure = new PathFigure { StartPoint = pStart };
        pFigure.Segments.Add(new ArcSegment(
            pEnd,
            new System.Windows.Size(PConsoleSpinnerRadius, PConsoleSpinnerRadius),
            0,
            true,
            SweepDirection.Clockwise,
            true));
        var pGeometry = new PathGeometry();
        pGeometry.Figures.Add(pFigure);
        pGeometry.Freeze();

        return new Path
        {
            Data = pGeometry,
            Stroke = pConsoleSpinnerBrush,
            StrokeThickness = PConsoleSpinnerStroke,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Width = PConsoleIndicatorSize,
            Height = PConsoleIndicatorSize,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
            RenderTransformOrigin = new System.Windows.Point(0.5, 0.5),
            RenderTransform = pRotate,
            Visibility = Visibility.Collapsed
        };
    }

    private static Brush PConsoleProgressCreate()
    {
        var pBrush = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 1),
            EndPoint = new System.Windows.Point(1, 0)
        };
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1E, 0x59, 0xBE), 0));
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x3E, 0x92, 0xE4), 0.55));
        pBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x74, 0xCB, 0xF7), 1));
        pBrush.Freeze();
        return pBrush;
    }

    private static Brush PConsoleGlossCreate()
    {
        var pGloss = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(0, 1)
        };
        pGloss.GradientStops.Add(new GradientStop(Color.FromArgb(0x59, 0xFF, 0xFF, 0xFF), 0));
        pGloss.GradientStops.Add(new GradientStop(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF), 0.45));
        pGloss.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF), 0.46));
        pGloss.Freeze();
        return pGloss;
    }

    internal static ProgressBar PConsoleProgressBuild() => new()
    {
        Height = PConsoleProgressHeight,
        Minimum = 0,
        Maximum = 1,
        Value = 0,
        Background = PRosterTheme.PRosterTrackBrush,
        Foreground = pConsoleProgressBrush,
        BorderThickness = new Thickness(0),
        Template = PConsoleTrackBuild()
    };

    private static ControlTemplate PConsoleTrackBuild()
    {
        var pTrack = new FrameworkElementFactory(typeof(Border));
        pTrack.Name = "PART_Track";
        pTrack.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pTrack.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressRadius));

        pTrack.SetValue(Border.BorderBrushProperty, PRosterTheme.PRosterLineBrush);
        pTrack.SetValue(Border.BorderThicknessProperty, new Thickness(1));

        var pIndicator = new FrameworkElementFactory(typeof(Border));
        pIndicator.Name = "PART_Indicator";
        pIndicator.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        pIndicator.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        pIndicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressRadius));
        pIndicator.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
        pIndicator.SetValue(UIElement.ClipToBoundsProperty, true);

        var pFill = new FrameworkElementFactory(typeof(Border));
        pFill.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        pFill.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressRadius));
        pFill.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        pFill.SetValue(UIElement.IsHitTestVisibleProperty, false);
        pFill.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding("ActualWidth")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(
                System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });

        var pGloss = new FrameworkElementFactory(typeof(Border));
        pGloss.SetValue(Border.BackgroundProperty, pConsoleProgressGloss);
        pGloss.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressRadius));
        pGloss.SetValue(UIElement.IsHitTestVisibleProperty, false);
        pFill.AppendChild(pGloss);
        pIndicator.AppendChild(pFill);

        var pRoot = new FrameworkElementFactory(typeof(Grid));
        pRoot.AppendChild(pTrack);
        pRoot.AppendChild(pIndicator);
        return new ControlTemplate(typeof(ProgressBar)) { VisualTree = pRoot };
    }
}
