using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal static class PSCasement
{
    internal const double PSCasementBandHeight = 56;
    internal const double PSCasementContentOverlap = 2;
    internal const double PSCasementLeadColumn = 28;
    internal const double PSCasementButtonWidth = 48;
    internal const double PSCasementButtonStrip = PSCasementButtonWidth * 3;

    internal static readonly Brush PSCasementBandFill = PSCasementFillCreate();

    private static Brush PSCasementFillCreate()
    {
        var pCasementBrush = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFC));
        pCasementBrush.Freeze();
        return pCasementBrush;
    }

    internal static UIElement PSCasementOverlayBuild(Window pWindow, double pStripWidth) =>
        PSCasementOverlayBuild(
            pWindow,
            pStripWidth,
            null,
            pCloseOnly: false,
            PSCasementBandHeight,
            14 * (PSCasementBandHeight / PSCasementBandHeight),
            PSCasementBandFill);

    internal static UIElement PSCasementOverlayBuild(
        Window pWindow,
        double pStripWidth,
        string? pTitle,
        bool pCloseOnly,
        double pBandHeight,
        double pTitleFont,
        Brush pBandFill)
    {
        PSCasementWindow.PSCasementEscapeAttach(pWindow);
        (double pScale, double pButtonWidth, double pButtonHeight) = PSCasementSizeResolve(pBandHeight);
        Border pLeadArea = PSCasementLeadBuild();
        PSCasementWindow.PSCasementDragAttach(pLeadArea, pWindow, pCloseOnly, pBandHeight);
        Border pDragArea = PSCasementDragBuild(pStripWidth);
        PSCasementWindow.PSCasementDragAttach(pDragArea, pWindow, pCloseOnly, pBandHeight);
        if (!string.IsNullOrWhiteSpace(pTitle))
        {
            PSCasementTitleBuild(pDragArea, pTitle, pTitleFont);
        }

        StackPanel pButtons = PSCasementButton.PSCasementStripBuild(
            pWindow, pCloseOnly, pButtonWidth, pButtonHeight, pScale, pBandFill);
        return PSCasementGridBuild(pBandHeight, pLeadArea, pDragArea, pButtons);
    }

    private static (double, double, double) PSCasementSizeResolve(double pBandHeight)
    {
        double pScale = pBandHeight / PSCasementBandHeight;
        double pButtonHeight = pBandHeight - PSCasementContentOverlap;
        double pButtonWidth = PSCasementButtonWidth * pScale;
        return (pScale, pButtonWidth, pButtonHeight);
    }

    private static Border PSCasementLeadBuild() => new() { Background = Brushes.Transparent };

    private static Border PSCasementDragBuild(double pStripWidth) => new()
    {
        Background = Brushes.Transparent,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Margin = new Thickness(pStripWidth, 0, 0, 0)
    };

    private static void PSCasementTitleBuild(Border pDragArea, string pTitle, double pTitleFont) =>
        pDragArea.Child = new TextBlock
        {
            Text = pTitle,
            FontSize = pTitleFont,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x2B, 0x34, 0x43)),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            IsHitTestVisible = false
        };

    private static Grid PSCasementGridBuild(double pBandHeight, Border pLeadArea, Border pDragArea, StackPanel pButtons)
    {
        var pGrid = new Grid { Height = pBandHeight, VerticalAlignment = VerticalAlignment.Top };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSCasementLeadColumn) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pGrid.Children.Add(pLeadArea);
        Grid.SetColumn(pDragArea, 1);
        pGrid.Children.Add(pDragArea);
        Grid.SetColumn(pButtons, 2);
        pGrid.Children.Add(pButtons);
        return pGrid;
    }
}
