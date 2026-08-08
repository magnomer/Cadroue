using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed class PSMonitor : Window
{
    private const string PSMonitorPlacementKey = "NormalizePreview";
    private const double PSMonitorWidthDefault = 660;
    private const double PSMonitorWidthMinimum = 480;
    private const double PSMonitorHeightDefault = 560;
    private const double PSMonitorHeightMinimum = 360;
    private const double PSMonitorInset = 18;
    private const double PSMonitorRailMinimum = 110;
    private const double PSMonitorGutter = 46;
    private const double PSMonitorZoomStep = 2;
    private const double PSMonitorZoomMost = 32;
    private static PSMonitor? psMonitorCurrent;

    private static readonly Brush psMonitorBeforeFill = new SolidColorBrush(Color.FromRgb(0xB4, 0xC2, 0xD6));
    private static readonly Brush psMonitorAfterFill = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
    private static readonly Brush psMonitorGridFill = new SolidColorBrush(Color.FromRgb(0xE4, 0xE9, 0xF0));
    private static readonly Brush psMonitorAxisFill = new SolidColorBrush(Color.FromRgb(0x8A, 0x95, 0xA6));

    private readonly string psMonitorTitle;
    private readonly PSGrabber psMonitorGrabber;
    private readonly LSMonitor psMonitorSource;
    private readonly DispatcherTimer psMonitorTimer;

    private Canvas psMonitorBeforeCanvas = null!;
    private Canvas psMonitorAfterCanvas = null!;
    private TextBlock psMonitorBeforeStatus = null!;
    private TextBlock psMonitorAfterStatus = null!;
    private LSMonitorEstimate psMonitorEstimate;
    private ScrollBar psMonitorScrollbar = null!;
    private double psMonitorZoom = 1;
    private double psMonitorOffset;

    internal static void PSMonitorShow(Window? pOwner, LSMonitor pSource)
    {
        psMonitorCurrent?.Close();
        var psMonitor = new PSMonitor(pOwner, pSource);
        psMonitorCurrent = psMonitor;
        psMonitor.Show();
    }

    private PSMonitor(Window? pOwner, LSMonitor pSource)
    {
        psMonitorSource = pSource;
        psMonitorTitle = LLocalization.LLocalizationTextRead("NormalizePreview.Window.Title");
        Title = psMonitorTitle;
        Owner = pOwner?.Owner ?? pOwner;
        ShowInTaskbar = true;
        Width = PSMonitorWidthDefault;
        Height = PSMonitorHeightDefault;
        MinWidth = PSMonitorWidthMinimum;
        MinHeight = PSMonitorHeightMinimum;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSFieldFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        PScrollbar.PScrollbarApply(this);

        psMonitorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        psMonitorTimer.Tick += PSMonitorTickHandle;

        Content = PSMonitorBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSMonitorPlacementKey);
        psMonitorGrabber = new PSGrabber(this);
        psMonitorGrabber.PSGrabberAttach();
        psMonitorSource.LSMonitorReady += PSMonitorReadyHandle;
        Closed += PSMonitorCloseHandle;
        psMonitorSource.LSMonitorUpdate();
    }

    private UIElement PSMonitorBuild()
    {
        var psMonitor = new Grid { Background = PSCasement.PSCasementBandFill };
        psMonitor.Children.Add(PSMonitorRootBuild());
        psMonitor.Children.Add(PSCasement.PSCasementOverlayBuild(this, 0, psMonitorTitle, pCloseOnly: true));
        return psMonitor;
    }

    private UIElement PSMonitorRootBuild()
    {
        var psMonitor = new DockPanel { Background = Brushes.White, Margin = new Thickness(0, PSCasement.PSCasementBandHeight, 0, 0) };
        var psFooter = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(12) };
        Button psClose = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("NormalizePreview.Close"));
        psClose.Click += (_, _) => Close();
        psFooter.Children.Add(psClose);
        DockPanel.SetDock(psFooter, Dock.Bottom);
        psMonitor.Children.Add(psFooter);

        var psContent = new DockPanel { Margin = new Thickness(PSMonitorInset, 14, PSMonitorInset, 8) };
        UIElement psZoomBar = PSMonitorZoomBuild();
        DockPanel.SetDock(psZoomBar, Dock.Top);
        psContent.Children.Add(psZoomBar);
        psContent.Children.Add(PSMonitorContentBuild());
        psMonitor.Children.Add(psContent);
        return psMonitor;
    }

    private UIElement PSMonitorZoomBuild()
    {
        var psZoom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        psZoom.Children.Add(PSMonitorButtonBuild(
            "/PAssets/PCompass/PCompassZoomIncrease.svg", "NormalizePreview.ZoomIn", () => PSMonitorZoomApply(PSMonitorZoomStep)));
        psZoom.Children.Add(PSMonitorButtonBuild(
            "/PAssets/PCompass/PCompassZoomDecrease.svg", "NormalizePreview.ZoomOut", () => PSMonitorZoomApply(1 / PSMonitorZoomStep)));
        return psZoom;
    }

    private Button PSMonitorButtonBuild(string pIconPath, string pTooltipKey, Action pAction)
    {
        var psButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = psMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey),
            Content = new Image
            {
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                Source = PAssets.PIcon.PIconRead(pIconPath, psMonitorAxisFill)
            }
        };
        psButton.Click += (_, _) => pAction();
        return psButton;
    }

    private void PSMonitorZoomApply(double pFactor)
    {
        double pCenter = psMonitorOffset + 1.0 / psMonitorZoom / 2;
        psMonitorZoom = Math.Clamp(psMonitorZoom * pFactor, 1, PSMonitorZoomMost);
        double pViewport = 1.0 / psMonitorZoom;
        psMonitorOffset = Math.Clamp(pCenter - pViewport / 2, 0, 1 - pViewport);
        PSMonitorScrollbarApply();
        PSMonitorUpdate();
    }

    private void PSMonitorScrollbarApply()
    {
        double pViewport = 1.0 / psMonitorZoom;
        psMonitorScrollbar.ViewportSize = pViewport;
        psMonitorScrollbar.Maximum = 1 - pViewport;
        psMonitorScrollbar.Value = psMonitorOffset;
        psMonitorScrollbar.IsEnabled = psMonitorZoom > 1;
        psMonitorScrollbar.Opacity = psMonitorZoom > 1 ? 1 : 0.35;
    }

    private void PSMonitorScrollbarHandle(object pSender, System.Windows.RoutedPropertyChangedEventArgs<double> pEvent)
    {
        psMonitorOffset = pEvent.NewValue;
        PSMonitorUpdate();
    }

    private void PSMonitorUpdate()
    {
        PSMonitorEnvelopeDraw(psMonitorBeforeCanvas);
        PSMonitorEnvelopeDraw(psMonitorAfterCanvas);
    }

    private UIElement PSMonitorContentBuild()
    {
        var psMonitor = new Grid();
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = PSMonitorRailMinimum });
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = PSMonitorRailMinimum });
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid psBefore = PSMonitorRailBuild("NormalizePreview.Before", psMonitorBeforeFill, out psMonitorBeforeCanvas, out psMonitorBeforeStatus);
        Grid.SetRow(psBefore, 0);
        psMonitor.Children.Add(psBefore);

        var psDivider = new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)), Margin = new Thickness(0, 14, 0, 14) };
        Grid.SetRow(psDivider, 1);
        psMonitor.Children.Add(psDivider);

        Grid psAfter = PSMonitorRailBuild("NormalizePreview.After", psMonitorAfterFill, out psMonitorAfterCanvas, out psMonitorAfterStatus);
        Grid.SetRow(psAfter, 2);
        psMonitor.Children.Add(psAfter);

        psMonitorScrollbar = new ScrollBar
        {
            Orientation = Orientation.Horizontal,
            Minimum = 0,
            Maximum = 0,
            Value = 0,
            ViewportSize = 1,
            SmallChange = 0.02,
            LargeChange = 0.2,
            Height = 14,
            Margin = new Thickness(PSMonitorGutter, 10, 0, 0),
            IsEnabled = false,
            Opacity = 0.35
        };
        psMonitorScrollbar.ValueChanged += PSMonitorScrollbarHandle;
        Grid.SetRow(psMonitorScrollbar, 3);
        psMonitor.Children.Add(psMonitorScrollbar);
        return psMonitor;
    }

    private Grid PSMonitorRailBuild(string pLabelKey, Brush pFill, out Canvas pCanvas, out TextBlock pStatus)
    {
        var psRail = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)), ClipToBounds = true };

        var psCanvas = new Canvas { Tag = pFill };
        psCanvas.SizeChanged += (_, _) => PSMonitorEnvelopeDraw(psCanvas);
        psRail.Children.Add(psCanvas);
        pCanvas = psCanvas;

        var psStatus = new TextBlock
        {
            FontSize = 12,
            Foreground = PSFieldMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        psRail.Children.Add(psStatus);
        pStatus = psStatus;

        psRail.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(pLabelKey),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PSFieldMuted,
            Margin = new Thickness(PSMonitorGutter + 8, 8, 12, 0),
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false
        });
        return psRail;
    }

    private void PSMonitorReadyHandle(LSMonitorEstimate pEstimate)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PSMonitorReadyHandle(pEstimate));
            return;
        }

        psMonitorEstimate = pEstimate;
        psMonitorTimer.Stop();
        psMonitorTimer.Start();
    }

    private void PSMonitorTickHandle(object? pSender, EventArgs pEvent)
    {
        psMonitorTimer.Stop();
        PSMonitorRailApply(psMonitorBeforeCanvas, psMonitorBeforeStatus, psMonitorEstimate.LSMonitorBefore);
        PSMonitorRailApply(psMonitorAfterCanvas, psMonitorAfterStatus, psMonitorEstimate.LSMonitorAfter);
    }

    private static double PSMonitorLevelRead(double pPeak) => Math.Clamp(pPeak, 0, 1);

    private void PSMonitorRailApply(Canvas pCanvas, TextBlock pStatus, double[] pEnvelope)
    {
        if (pEnvelope.Length == 0)
        {
            pStatus.Text = LLocalization.LLocalizationTextRead(
                psMonitorSource.LSMonitorScanning ? "NormalizePreview.Loading" : "NormalizePreview.Empty");
            pCanvas.DataContext = null;
            PSMonitorEnvelopeDraw(pCanvas);
            return;
        }

        pStatus.Text = string.Empty;
        pCanvas.DataContext = pEnvelope;
        PSMonitorEnvelopeDraw(pCanvas);
    }

    private void PSMonitorEnvelopeDraw(Canvas pCanvas)
    {
        pCanvas.Children.Clear();
        double pWidth = pCanvas.ActualWidth;
        double pHeight = pCanvas.ActualHeight;
        if (pWidth <= 0 || pHeight <= 0)
        {
            return;
        }

        double pMid = pHeight / 2;
        double pPlotWidth = pWidth - PSMonitorGutter;
        PSMonitorAxisDraw(pCanvas, pWidth, pMid);
        if (pCanvas.DataContext is not double[] pEnvelope || pEnvelope.Length == 0 || pPlotWidth <= 1)
        {
            return;
        }

        var pFill = pCanvas.Tag as Brush ?? Brushes.Gray;
        int pColumns = Math.Max(1, (int)pPlotWidth);
        var pGeometry = new StreamGeometry();
        using (StreamGeometryContext pContext = pGeometry.Open())
        {
            pContext.BeginFigure(new Point(PSMonitorGutter, pMid), true, true);
            for (int pColumn = 0; pColumn < pColumns; pColumn++)
            {
                double pLevel = PSMonitorLevelRead(PSMonitorColumnRead(pEnvelope, pColumn, pColumns));
                pContext.LineTo(new Point(PSMonitorGutter + pColumn, pMid - pLevel * pMid), true, false);
            }

            for (int pColumn = pColumns - 1; pColumn >= 0; pColumn--)
            {
                double pLevel = PSMonitorLevelRead(PSMonitorColumnRead(pEnvelope, pColumn, pColumns));
                pContext.LineTo(new Point(PSMonitorGutter + pColumn, pMid + pLevel * pMid), true, false);
            }
        }

        pGeometry.Freeze();
        pCanvas.Children.Add(new System.Windows.Shapes.Path { Data = pGeometry, Fill = pFill });
    }

    private static void PSMonitorAxisDraw(Canvas pCanvas, double pWidth, double pMid)
    {
        foreach (double pFraction in new[] { 1.0, 0.5 })
        {
            double pDb = 20.0 * Math.Log10(pFraction);
            PSMonitorGridDraw(pCanvas, pWidth, pMid - pFraction * pMid, $"{pDb:0} dB");
            PSMonitorGridDraw(pCanvas, pWidth, pMid + pFraction * pMid, null);
        }

        PSMonitorGridDraw(pCanvas, pWidth, pMid, "-∞");
    }

    private static void PSMonitorGridDraw(Canvas pCanvas, double pWidth, double pY, string? pLabel)
    {
        pCanvas.Children.Add(new System.Windows.Shapes.Line
        {
            X1 = PSMonitorGutter,
            X2 = pWidth,
            Y1 = pY,
            Y2 = pY,
            Stroke = psMonitorGridFill,
            StrokeThickness = 1,
            IsHitTestVisible = false
        });

        if (pLabel is null)
        {
            return;
        }

        var pText = new TextBlock
        {
            Text = pLabel,
            FontSize = 10,
            Foreground = psMonitorAxisFill,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(pText, 4);
        Canvas.SetTop(pText, Math.Clamp(pY - 7, 0, Math.Max(0, pCanvas.ActualHeight - 14)));
        pCanvas.Children.Add(pText);
    }

    private double PSMonitorColumnRead(double[] pEnvelope, int pColumn, int pColumns)
    {
        double pViewport = 1.0 / psMonitorZoom;
        int pLength = pEnvelope.Length;
        double pFromF = (psMonitorOffset + (double)pColumn / pColumns * pViewport) * pLength;
        double pToF = (psMonitorOffset + (double)(pColumn + 1) / pColumns * pViewport) * pLength;
        int pFrom = Math.Clamp((int)Math.Floor(pFromF), 0, pLength - 1);
        int pTo = Math.Clamp((int)Math.Ceiling(pToF), pFrom + 1, pLength);

        double pPeak = 0;
        for (int pIndex = pFrom; pIndex < pTo; pIndex++)
        {
            if (pEnvelope[pIndex] > pPeak)
            {
                pPeak = pEnvelope[pIndex];
            }
        }

        return pPeak;
    }

    private void PSMonitorCloseHandle(object? pSender, EventArgs pEvent)
    {
        psMonitorSource.LSMonitorReady -= PSMonitorReadyHandle;
        psMonitorTimer.Stop();
        psMonitorTimer.Tick -= PSMonitorTickHandle;
        PSGrabber.PSGrabberPlacementSave(this, PSMonitorPlacementKey);
        psMonitorGrabber.PSGrabberDetach();
        psMonitorCurrent = null;
    }

    protected override void OnSourceInitialized(EventArgs pEvent)
    {
        base.OnSourceInitialized(pEvent);
        PSCasement.PSCasementDwmApply(this);
    }
}
