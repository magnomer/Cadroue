using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal static class PSCasement
{
    internal const double PSCasementBandHeight = 56;
    internal const double PSCasementContentOverlap = 2;
    internal const double PSCasementLeadColumn = 28;
    internal const double PSCasementButtonWidth = 48;
    internal const double PSCasementButtonStrip = PSCasementButtonWidth * 3;

    private const double PSCasementButtonHeight = PSCasementBandHeight - PSCasementContentOverlap;

    internal static UIElement PSCasementOverlayBuild(Window pWindow, double pStripWidth)
    {
        var pGrid = new Grid { Height = PSCasementBandHeight, VerticalAlignment = VerticalAlignment.Top };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSCasementLeadColumn) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var pLeadArea = new Border { Background = Brushes.Transparent };
        pLeadArea.MouseLeftButtonDown += (_, e) => PSCasementDragHandle(pWindow, e);
        pGrid.Children.Add(pLeadArea);

        var pDragArea = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(pStripWidth, 0, 0, 0)
        };
        pDragArea.MouseLeftButtonDown += (_, e) => PSCasementDragHandle(pWindow, e);
        Grid.SetColumn(pDragArea, 1);
        pGrid.Children.Add(pDragArea);

        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFC))
        };
        pButtons.Children.Add(PSCasementButtonBuild(
            PSCasementMinimizeBuild(),
            (_, _) => pWindow.WindowState = WindowState.Minimized));
        pButtons.Children.Add(PSCasementButtonBuild(
            PSCasementMaximizeBuild(),
            (_, _) => pWindow.WindowState = pWindow.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized));
        pButtons.Children.Add(PSCasementButtonBuild(
            PSCasementCloseBuild(),
            (_, _) => pWindow.Close(),
            pClose: true));
        Grid.SetColumn(pButtons, 2);
        pGrid.Children.Add(pButtons);
        return pGrid;
    }

    private static void PSCasementDragHandle(Window pWindow, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.ClickCount > 1)
        {
            return;
        }

        pWindow.DragMove();
    }

    private static Button PSCasementButtonBuild(UIElement pIcon, RoutedEventHandler pClick, bool pClose = false)
    {
        var pButton = new Button
        {
            Width = PSCasementButtonWidth,
            Height = PSCasementButtonHeight,
            Content = pIcon,
            Style = PButton.PButtonChromeCreate(pClose)
        };
        pButton.Click += pClick;
        return pButton;
    }

    private static Canvas PSCasementMinimizeBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        pCanvas.Children.Add(PSRuleBuild(2, 12, 14, 12));
        return pCanvas;
    }

    private static Canvas PSCasementMaximizeBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        var pSquare = new System.Windows.Shapes.Rectangle
        {
            Width = 12,
            Height = 12,
            StrokeThickness = 1.2,
            Fill = Brushes.Transparent,
            Margin = new Thickness(2, 2, 0, 0)
        };
        PSCasementGlyphBind(pSquare);
        pCanvas.Children.Add(pSquare);
        return pCanvas;
    }

    private static Canvas PSCasementCloseBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        pCanvas.Children.Add(PSRuleBuild(2.5, 2.5, 13.5, 13.5));
        pCanvas.Children.Add(PSRuleBuild(13.5, 2.5, 2.5, 13.5));
        return pCanvas;
    }

    private static System.Windows.Shapes.Line PSRuleBuild(double pX1, double pY1, double pX2, double pY2)
    {
        var pRule = new System.Windows.Shapes.Line
        {
            X1 = pX1,
            Y1 = pY1,
            X2 = pX2,
            Y2 = pY2,
            StrokeThickness = 1.25,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        PSCasementGlyphBind(pRule);
        return pRule;
    }

    private static void PSCasementGlyphBind(System.Windows.Shapes.Shape pGlyph)
    {
        pGlyph.SetBinding(System.Windows.Shapes.Shape.StrokeProperty, new System.Windows.Data.Binding("Foreground")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(
                System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Button), 1)
        });
    }
}
