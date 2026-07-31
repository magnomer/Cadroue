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

    private const int PSCasementDwmPreference = 33;
    private const int PSCasementDwmRound = 2;
    private const int PSCasementDwmCaption = 35;
    private const int PSCasementDwmColor = 0x00F7E8DC;

    internal static void PSCasementDwmApply(Window pWindow)
    {
        IntPtr pCasementHandle = new System.Windows.Interop.WindowInteropHelper(pWindow).Handle;
        if (pCasementHandle == IntPtr.Zero)
        {
            return;
        }

        int pCasementCorner = PSCasementDwmRound;
        _ = DwmSetWindowAttribute(
            pCasementHandle, PSCasementDwmPreference, ref pCasementCorner, System.Runtime.InteropServices.Marshal.SizeOf<int>());

        int pCasementCaption = PSCasementDwmColor;
        _ = DwmSetWindowAttribute(
            pCasementHandle, PSCasementDwmCaption, ref pCasementCaption, System.Runtime.InteropServices.Marshal.SizeOf<int>());
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);

    internal static readonly Brush PSCasementBandFill = PSCasementFillCreate();

    private static Brush PSCasementFillCreate()
    {
        var pCasementBrush = new SolidColorBrush(Color.FromRgb(0xEA, 0xF2, 0xFC));
        pCasementBrush.Freeze();
        return pCasementBrush;
    }

    internal static UIElement PSCasementBandBuild() => new Border
    {
        Height = PSCasementBandHeight,
        VerticalAlignment = VerticalAlignment.Top,
        Background = PSCasementBandFill
    };

    internal static void PSCasementEscapeAttach(Window pWindow)
    {
        pWindow.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Escape)
            {
                return;
            }

            e.Handled = true;
            pWindow.Close();
        };
    }

    internal static UIElement PSCasementOverlayBuild(Window pWindow, double pStripWidth) =>
        PSCasementOverlayBuild(pWindow, pStripWidth, null);

    internal static UIElement PSCasementOverlayBuild(Window pWindow, double pStripWidth, string? pTitle) =>
        PSCasementOverlayBuild(pWindow, pStripWidth, pTitle, pCloseOnly: false);

    internal static UIElement PSCasementOverlayBuild(
        Window pWindow,
        double pStripWidth,
        string? pTitle,
        bool pCloseOnly)
    {
        PSCasementEscapeAttach(pWindow);
        var pGrid = new Grid { Height = PSCasementBandHeight, VerticalAlignment = VerticalAlignment.Top };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSCasementLeadColumn) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var pLeadArea = new Border { Background = Brushes.Transparent };
        pLeadArea.MouseLeftButtonDown += (_, e) => PSCasementDragHandle(pWindow, e, pCloseOnly);
        pGrid.Children.Add(pLeadArea);

        var pDragArea = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(pStripWidth, 0, 0, 0)
        };
        pDragArea.MouseLeftButtonDown += (_, e) => PSCasementDragHandle(pWindow, e, pCloseOnly);
        if (!string.IsNullOrWhiteSpace(pTitle))
        {
            pDragArea.Child = new TextBlock
            {
                Text = pTitle,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x2B, 0x34, 0x43)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                IsHitTestVisible = false
            };
        }

        Grid.SetColumn(pDragArea, 1);
        pGrid.Children.Add(pDragArea);

        var pButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top,
            Background = PSCasementBandFill
        };
        if (!pCloseOnly)
        {
            pButtons.Children.Add(PSCasementButtonBuild(
                PSCasementMinimizeBuild(),
                (_, _) => pWindow.WindowState = WindowState.Minimized));
            pButtons.Children.Add(PSCasementButtonBuild(
                PSCasementMaximizeBuild(),
                (_, _) => PSCasementMaximizeToggle(pWindow)));
        }

        pButtons.Children.Add(PSCasementButtonBuild(
            PSCasementCloseBuild(),
            (_, _) => pWindow.Close(),
            pClose: true));
        Grid.SetColumn(pButtons, 2);
        pGrid.Children.Add(pButtons);
        return pGrid;
    }

    private static void PSCasementDragHandle(Window pWindow, MouseButtonEventArgs e, bool pCloseOnly)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount > 1)
        {
            if (pCloseOnly)
            {
                return;
            }

            PSCasementMaximizeToggle(pWindow);
            return;
        }

        pWindow.DragMove();
    }

    internal static void PSCasementMaximizeToggle(Window pWindow) =>
        pWindow.WindowState = pWindow.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

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
        PSCasementGlyphAttach(pSquare);
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
        PSCasementGlyphAttach(pRule);
        return pRule;
    }

    private static void PSCasementGlyphAttach(System.Windows.Shapes.Shape pGlyph)
    {
        pGlyph.SetBinding(System.Windows.Shapes.Shape.StrokeProperty, new System.Windows.Data.Binding("Foreground")
        {
            RelativeSource = new System.Windows.Data.RelativeSource(
                System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Button), 1)
        });
    }
}
