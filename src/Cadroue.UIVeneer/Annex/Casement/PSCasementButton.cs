using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSCasementButton
{
    internal static StackPanel PSCasementStripBuild(
        Window pWindow,
        bool pCloseOnly,
        double pButtonWidth,
        double pButtonHeight,
        double pScale,
        Brush pBandFill)
    {
        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top,
            Background = pBandFill
        };
        if (!pCloseOnly)
        {
            pButtons.Children.Add(PSCasementButtonBuild(
                PSCasementGlyph.PSCasementMinimizeBuild(),
                (_, _) => PSCasementWindow.PSCasementMinimizeApply(pWindow),
                pButtonWidth, pButtonHeight, pScale));
            pButtons.Children.Add(PSCasementButtonBuild(
                PSCasementGlyph.PSCasementMaximizeBuild(),
                (_, _) => PSCasementWindow.PSCasementMaximizeToggle(pWindow),
                pButtonWidth, pButtonHeight, pScale));
        }

        pButtons.Children.Add(PSCasementButtonBuild(
            PSCasementGlyph.PSCasementCloseBuild(),
            (_, _) => pWindow.Close(),
            pButtonWidth, pButtonHeight, pScale,
            pLook: QButtonLook.QButtonLookExit));
        return pButtons;
    }

    private static Button PSCasementButtonBuild(
        UIElement pIcon,
        RoutedEventHandler pClick,
        double pWidth,
        double pHeight,
        double pScale,
        QButtonLook pLook = QButtonLook.QButtonLookChrome)
    {
        PSCasementScaleApply(pIcon, pScale);
        Button pButton = PSCasementCaptionBuild(pIcon, pWidth, pHeight);
        PSCasementClickAttach(pButton, pClick);
        QButton.QButtonAttach(pButton, pLook);
        return pButton;
    }

    private static void PSCasementScaleApply(UIElement pIcon, double pScale)
    {
        if (pScale != 1 && pIcon is FrameworkElement pGlyph)
        {
            pGlyph.LayoutTransform = new ScaleTransform(pScale, pScale);
        }
    }

    private static Button PSCasementCaptionBuild(UIElement pIcon, double pWidth, double pHeight) => new()
    {
        Width = pWidth,
        Height = pHeight,
        Content = pIcon
    };

    private static void PSCasementClickAttach(Button pButton, RoutedEventHandler pClick) => pButton.Click += pClick;
}
