using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

public class PPanelFrame : UserControl
{
    protected static readonly Brush PPanelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    protected static readonly Brush PPanelTextBrush = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73));
    protected static readonly CornerRadius PPanelCornerRadius = new(10);
    protected static readonly Thickness PPanelOuterMargin = new(8);

    public PPanelFrame(string pPanelTitle)
    {
        FocusVisualStyle = null;
        Content = PPanelFrameBorderBuild(new TextBlock
        {
            Text = pPanelTitle,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = PPanelTextBrush,
            FontSize = 18
        });
    }

    public static Border PPanelFrameBorderBuild(UIElement pPanelContent)
    {
        var pContentBorder = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(9),
            Child = pPanelContent,
            SnapsToDevicePixels = true
        };
        PPanelFrameClipApply(pContentBorder, 9);

        return new Border
        {
            Margin = PPanelOuterMargin,
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = PPanelCornerRadius,
            Child = pContentBorder,
            SnapsToDevicePixels = true
        };
    }

    protected static void PPanelFrameClipApply(Border pBorder, double pRadius)
    {
        pBorder.SizeChanged += (_, _) =>
        {
            pBorder.Clip = new RectangleGeometry(
                new Rect(0, 0, pBorder.ActualWidth, pBorder.ActualHeight),
                pRadius,
                pRadius);
        };
    }
}
