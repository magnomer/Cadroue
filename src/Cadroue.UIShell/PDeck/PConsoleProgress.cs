using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PDeck;

public sealed partial class PConsole
{
    private const double PConsoleProgressHeight = 14;

    private static readonly Brush pConsoleProgressBrush = PConsoleProgressCreate();
    private static readonly Brush pConsoleProgressGloss = PConsoleGlossCreate();

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

    private static ProgressBar PConsoleProgressBuild() => new()
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
        pTrack.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));

        pTrack.SetValue(Border.BorderBrushProperty, PRosterTheme.PRosterLineBrush);
        pTrack.SetValue(Border.BorderThicknessProperty, new Thickness(1));

        var pIndicator = new FrameworkElementFactory(typeof(Border));
        pIndicator.Name = "PART_Indicator";
        pIndicator.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        pIndicator.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        pIndicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));
        pIndicator.SetValue(UIElement.SnapsToDevicePixelsProperty, true);
        pIndicator.SetValue(UIElement.ClipToBoundsProperty, true);

        var pFill = new FrameworkElementFactory(typeof(Border));
        pFill.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        pFill.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));
        pFill.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        pFill.SetValue(UIElement.IsHitTestVisibleProperty, false);
        pFill.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding("ActualWidth")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(
                System.Windows.Data.RelativeSourceMode.TemplatedParent)
        });

        var pGloss = new FrameworkElementFactory(typeof(Border));
        pGloss.SetValue(Border.BackgroundProperty, pConsoleProgressGloss);
        pGloss.SetValue(Border.CornerRadiusProperty, new CornerRadius(PConsoleProgressHeight / 2));
        pGloss.SetValue(UIElement.IsHitTestVisibleProperty, false);
        pFill.AppendChild(pGloss);
        pIndicator.AppendChild(pFill);

        var pRoot = new FrameworkElementFactory(typeof(Grid));
        pRoot.AppendChild(pTrack);
        pRoot.AppendChild(pIndicator);
        return new ControlTemplate(typeof(ProgressBar)) { VisualTree = pRoot };
    }
}
