using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

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
    private readonly PFlowControl psMonitorFlow;
    private readonly PViewer psMonitorViewer;
    private readonly DispatcherTimer psMonitorTimer;

    private Canvas psMonitorBeforeCanvas = null!;
    private Canvas psMonitorAfterCanvas = null!;
    private TextBlock psMonitorBeforeStatus = null!;
    private TextBlock psMonitorAfterStatus = null!;
    private RadioButton psMonitorBeforeRadio = null!;
    private RadioButton psMonitorAfterRadio = null!;
    private Border psMonitorBeforeHead = null!;
    private Border psMonitorAfterHead = null!;
    private LSMonitorEstimate psMonitorEstimate;
    private Image psMonitorPlayImage = null!;
    private Button psMonitorPlayButton = null!;
    private bool psMonitorPlaying;
    private ScrollBar psMonitorScrollbar = null!;
    private TimeSpan psMonitorCursor;
    private bool psMonitorRadioProgram;
    private double psMonitorScale = 1;
    private double psMonitorOffset;

    internal static void PSMonitorShow(Window? pOwner, LSMonitor pSource, PFlowControl pFlow, PViewer pViewer)
    {
        psMonitorCurrent?.Close();
        var psMonitor = new PSMonitor(pOwner, pSource, pFlow, pViewer);
        psMonitorCurrent = psMonitor;
        psMonitor.Show();
    }

    private PSMonitor(Window? pOwner, LSMonitor pSource, PFlowControl pFlow, PViewer pViewer)
    {
        psMonitorSource = pSource;
        psMonitorFlow = pFlow;
        psMonitorViewer = pViewer;
        psMonitorCursor = pFlow.PFlowCursorRead();
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
        psMonitorViewer.PViewerClockTick += PSMonitorCursorHandle;
        psMonitorViewer.PViewerBypassChange += PSMonitorBypassHandle;
        psMonitorViewer.PViewerPlayingChange += PSMonitorPlayingHandle;
        psMonitorFlow.PFlowCursorChange += PSMonitorCursorHandle;
        Closed += PSMonitorCloseHandle;
        PSMonitorBypassHandle(psMonitorViewer.PViewerBypassRead());
        PSMonitorPlayingApply(psMonitorViewer.PViewerPlayingRead());
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
        UIElement psTransport = PSMonitorTransportBuild();
        DockPanel.SetDock(psTransport, Dock.Bottom);
        psContent.Children.Add(psTransport);
        psContent.Children.Add(PSMonitorContentBuild());
        psMonitor.Children.Add(psContent);
        return psMonitor;
    }

    private UIElement PSMonitorTransportBuild()
    {
        var psTransport = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        };
        psTransport.Children.Add(PSMonitorPlayBuild());
        return psTransport;
    }

    private Button PSMonitorPlayBuild()
    {
        psMonitorPlayImage = new Image { Width = 18, Height = 18, Stretch = Stretch.Uniform };
        psMonitorPlayButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = psMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Content = psMonitorPlayImage
        };
        psMonitorPlayButton.Click += (_, _) => PSMonitorPlayToggle();
        PSMonitorPlayingApply(false);
        return psMonitorPlayButton;
    }

    private void PSMonitorPlayingApply(bool pPlaying)
    {
        psMonitorPlaying = pPlaying;
        string pIcon = pPlaying ? "PCompassPause.svg" : "PCompassPlay.svg";
        string pTooltip = pPlaying ? "NormalizePreview.PauseTooltip" : "NormalizePreview.PlayTooltip";
        psMonitorPlayImage.Source = PAssets.PIcon.PIconRead($"/PAssets/PCompass/{pIcon}", psMonitorAxisFill);
        psMonitorPlayButton.ToolTip = LLocalization.LLocalizationTextRead(pTooltip);
    }

    private void PSMonitorPlayToggle()
    {
        if (psMonitorPlaying)
        {
            psMonitorFlow.PFlowPauseRaise();
        }
        else
        {
            psMonitorFlow.PFlowPlayRaise();
        }
    }

    private void PSMonitorPlayingHandle(bool pPlaying)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PSMonitorPlayingHandle(pPlaying));
            return;
        }

        PSMonitorPlayingApply(pPlaying);
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
        double pCenter = psMonitorOffset + 1.0 / psMonitorScale / 2;
        psMonitorScale = Math.Clamp(psMonitorScale * pFactor, 1, PSMonitorZoomMost);
        double pViewport = 1.0 / psMonitorScale;
        psMonitorOffset = Math.Clamp(pCenter - pViewport / 2, 0, 1 - pViewport);
        PSMonitorScrollbarApply();
        PSMonitorUpdate();
    }

    private void PSMonitorScrollbarApply()
    {
        double pViewport = 1.0 / psMonitorScale;
        psMonitorScrollbar.ViewportSize = pViewport;
        psMonitorScrollbar.Maximum = 1 - pViewport;
        psMonitorScrollbar.Value = psMonitorOffset;
        psMonitorScrollbar.IsEnabled = psMonitorScale > 1;
        psMonitorScrollbar.Opacity = psMonitorScale > 1 ? 1 : 0.35;
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
        PSMonitorHeadPlace();
    }

    private UIElement PSMonitorContentBuild()
    {
        var psMonitor = new Grid();
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = PSMonitorRailMinimum });
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = PSMonitorRailMinimum });
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        psMonitorBeforeRadio = PSMonitorRadioBuild("NormalizePreview.Before", "NormalizePreview.BeforeSelect", true);
        psMonitorAfterRadio = PSMonitorRadioBuild("NormalizePreview.After", "NormalizePreview.AfterSelect", false);

        Grid psBefore = PSMonitorRailBuild(psMonitorBeforeFill, psMonitorBeforeRadio, out psMonitorBeforeCanvas, out psMonitorBeforeStatus, out psMonitorBeforeHead);
        Grid.SetRow(psBefore, 0);
        psMonitor.Children.Add(psBefore);

        var psDivider = new Border { Height = 1, Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)), Margin = new Thickness(0, 14, 0, 14) };
        Grid.SetRow(psDivider, 1);
        psMonitor.Children.Add(psDivider);

        Grid psAfter = PSMonitorRailBuild(psMonitorAfterFill, psMonitorAfterRadio, out psMonitorAfterCanvas, out psMonitorAfterStatus, out psMonitorAfterHead);
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

    private RadioButton PSMonitorRadioBuild(string pLabelKey, string pTooltipKey, bool pBypass)
    {
        var psRadio = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead(pLabelKey),
            GroupName = "PSMonitorAudio",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PSFieldMuted,
            Cursor = Cursors.Hand,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey),
            Margin = new Thickness(PSMonitorGutter + 8, 8, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        psRadio.Checked += (_, _) =>
        {
            if (!psMonitorRadioProgram)
            {
                psMonitorViewer.PViewerBypassSet(pBypass);
            }
        };
        return psRadio;
    }

    private Grid PSMonitorRailBuild(Brush pFill, RadioButton pRadio, out Canvas pCanvas, out TextBlock pStatus, out Border pHead)
    {
        var psRail = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)), ClipToBounds = true };

        var psCanvas = new Canvas { Tag = pFill, Background = Brushes.Transparent };
        psCanvas.SizeChanged += (_, _) =>
        {
            PSMonitorEnvelopeDraw(psCanvas);
            PSMonitorHeadPlace();
        };
        psCanvas.MouseLeftButtonDown += (_, pEvent) => PSMonitorSeekStart(psCanvas, pEvent);
        psCanvas.MouseMove += (_, pEvent) => PSMonitorSeekMove(psCanvas, pEvent);
        psCanvas.MouseLeftButtonUp += (_, _) => psCanvas.ReleaseMouseCapture();
        psRail.Children.Add(psCanvas);
        pCanvas = psCanvas;

        var psHead = new Border
        {
            Width = 1.5,
            Background = new SolidColorBrush(Color.FromRgb(0x1F, 0x27, 0x33)),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        psRail.Children.Add(psHead);
        pHead = psHead;

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

        psRail.Children.Add(pRadio);
        return psRail;
    }

    private void PSMonitorSeekStart(Canvas pCanvas, MouseButtonEventArgs pEvent)
    {
        pCanvas.CaptureMouse();
        PSMonitorSeekApply(pCanvas, pEvent.GetPosition(pCanvas).X);
    }

    private void PSMonitorSeekMove(Canvas pCanvas, MouseEventArgs pEvent)
    {
        if (pCanvas.IsMouseCaptured)
        {
            PSMonitorSeekApply(pCanvas, pEvent.GetPosition(pCanvas).X);
        }
    }

    private void PSMonitorSeekApply(Canvas pCanvas, double pX)
    {
        double pPlot = pCanvas.ActualWidth - PSMonitorGutter;
        double pDuration = psMonitorViewer.PViewerDurationRead().TotalSeconds;
        if (pPlot <= 1 || pDuration <= 0)
        {
            return;
        }

        double pLocal = Math.Clamp((pX - PSMonitorGutter) / pPlot, 0, 1);
        double pFraction = Math.Clamp(psMonitorOffset + pLocal / psMonitorScale, 0, 1);
        psMonitorFlow.PFlowCursorSeek(TimeSpan.FromSeconds(pFraction * pDuration));
    }

    private void PSMonitorCursorHandle(TimeSpan pCursor)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PSMonitorCursorHandle(pCursor));
            return;
        }

        psMonitorCursor = pCursor;
        PSMonitorHeadPlace();
    }

    private void PSMonitorBypassHandle(bool pBypass)
    {
        psMonitorRadioProgram = true;
        psMonitorBeforeRadio.IsChecked = pBypass;
        psMonitorAfterRadio.IsChecked = !pBypass;
        psMonitorRadioProgram = false;
    }

    private void PSMonitorHeadPlace()
    {
        PSMonitorHeadApply(psMonitorBeforeCanvas, psMonitorBeforeHead);
        PSMonitorHeadApply(psMonitorAfterCanvas, psMonitorAfterHead);
    }

    private void PSMonitorHeadApply(Canvas pCanvas, Border pHead)
    {
        double pPlot = pCanvas.ActualWidth - PSMonitorGutter;
        double pDuration = psMonitorViewer.PViewerDurationRead().TotalSeconds;
        if (pPlot <= 1 || pDuration <= 0)
        {
            pHead.Visibility = Visibility.Collapsed;
            return;
        }

        double pFraction = Math.Clamp(psMonitorCursor.TotalSeconds / pDuration, 0, 1);
        double pLocal = (pFraction - psMonitorOffset) * psMonitorScale;
        if (pLocal < 0 || pLocal > 1)
        {
            pHead.Visibility = Visibility.Collapsed;
            return;
        }

        pHead.Visibility = Visibility.Visible;
        pHead.Margin = new Thickness(PSMonitorGutter + pLocal * pPlot, 0, 0, 0);
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
        PSMonitorRailApply(psMonitorBeforeCanvas, psMonitorBeforeStatus, psMonitorEstimate.LSMonitorBefore, false);
        PSMonitorRailApply(psMonitorAfterCanvas, psMonitorAfterStatus, psMonitorEstimate.LSMonitorAfter, psMonitorSource.LSMonitorScanning);
    }

    private static double PSMonitorLevelRead(double pPeak) => Math.Clamp(pPeak, 0, 1);

    private void PSMonitorRailApply(Canvas pCanvas, TextBlock pStatus, double[] pEnvelope, bool pScanning)
    {
        if (pEnvelope.Length == 0)
        {
            pStatus.Text = LLocalization.LLocalizationTextRead(
                psMonitorSource.LSMonitorScanning ? "NormalizePreview.Loading" : "NormalizePreview.Empty");
            pCanvas.DataContext = null;
            PSMonitorEnvelopeDraw(pCanvas);
            return;
        }

        pStatus.Text = pScanning ? LLocalization.LLocalizationTextRead("NormalizePreview.Updating") : string.Empty;
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
        double pViewport = 1.0 / psMonitorScale;
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
        psMonitorViewer.PViewerClockTick -= PSMonitorCursorHandle;
        psMonitorViewer.PViewerBypassChange -= PSMonitorBypassHandle;
        psMonitorViewer.PViewerPlayingChange -= PSMonitorPlayingHandle;
        psMonitorFlow.PFlowCursorChange -= PSMonitorCursorHandle;
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
