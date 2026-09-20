using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIDeportment;
using Cadroue.Application;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PHouse;

using static Cadroue.UIVeneer.PSField;
using static Cadroue.UIVeneer.PSFooter;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSMonitor : Window
{
    private const string PSMonitorPlacementKey = "NormalizePreview";
    private const double PSMonitorWidthDefault = 660;
    private const double PSMonitorWidthMinimum = 480;
    private const double PSMonitorHeightDefault = 560;
    private const double PSMonitorHeightMinimum = 360;
    private const double PSMonitorInset = 18;
    private const double PSMonitorRailMinimum = 110;
    private const double PSMonitorGutter = LSMonitorPlan.LSMonitorGutter;

    private static readonly Brush psMonitorBeforeFill = new SolidColorBrush(Color.FromRgb(0xB4, 0xC2, 0xD6));
    private static readonly Brush psMonitorAfterFill = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));
    private static readonly Brush psMonitorGridFill = new SolidColorBrush(Color.FromRgb(0xE4, 0xE9, 0xF0));
    private static readonly Brush psMonitorAxisFill = new SolidColorBrush(Color.FromRgb(0x8A, 0x95, 0xA6));

    private readonly PSGrabber psMonitorGrabber;
    private readonly LSMonitor psMonitorSource;
    private readonly PFlow psMonitorFlow;
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
    private Image psMonitorPlayImage = null!;
    private Button psMonitorPlayButton = null!;
    private ScrollBar psMonitorScrollbar = null!;

    internal static PSMonitor PSMonitorShow(Window? pOwner, LSMonitor pSource, PFlow pFlow, PViewer pViewer)
    {
        System.Windows.Application.Current.Windows.OfType<PSMonitor>().FirstOrDefault()?.Close();
        var psMonitor = new PSMonitor(pOwner, pSource, pFlow, pViewer);
        psMonitor.Show();
        return psMonitor;
    }

    private PSMonitor(Window? pOwner, LSMonitor pSource, PFlow pFlow, PViewer pViewer)
    {
        psMonitorSource = pSource;
        psMonitorFlow = pFlow;
        psMonitorViewer = pViewer;
        Title = LLocalization.LLocalizationTextRead("NormalizePreview.Window.Title");
        Owner = pOwner;
        ShowInTaskbar = true;
        Width = PSMonitorWidthDefault;
        Height = PSMonitorHeightDefault;
        MinWidth = PSMonitorWidthMinimum;
        MinHeight = PSMonitorHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
        PScrollbar.PScrollbarApply(this);

        psMonitorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        psMonitorTimer.Tick += PSMonitorTickHandle;

        Content = PSMonitorBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSMonitorPlacementKey);
        psMonitorGrabber = new PSGrabber(this);
        psMonitorGrabber.PSGrabberAttach();
        psMonitorSource.LSMonitorReady += PSMonitorReadyHandle;
        psMonitorSource.LSMonitorCursorChange += PSMonitorCursorApply;
        psMonitorSource.LSMonitorPlayingChange += PSMonitorPlayingApply;
        psMonitorSource.LSMonitorZoomChange += PSMonitorZoomApply;
        psMonitorSource.LSMonitorBypassChange += PSMonitorBypassApply;
        psMonitorSource.LSMonitorSeekApply += psMonitorFlow.LFlow.LFlowCursorSeek;
        psMonitorSource.LSMonitorPlayApply += psMonitorFlow.LFlow.LFlowPlayRaise;
        psMonitorSource.LSMonitorPauseApply += psMonitorFlow.LFlow.LFlowPauseRaise;
        psMonitorFlow.LFlow.LFlowCursorChange += PSMonitorCursorHandle;
        Closed += PSMonitorCloseHandle;
        psMonitorSource.LSMonitorViewerAttach(psMonitorViewer.LViewer);
        psMonitorSource.LSMonitorCursorSet(pFlow.LFlow.LFlowCursor);
        psMonitorSource.LSMonitorUpdate();
        PSMonitorZoomApply();
    }

    private UIElement PSMonitorBuild() =>
        PSDialog.PSDialogBuild(this, Title, PSMonitorRootBuild());

    private DockPanel PSMonitorRootBuild()
    {
        var psMonitor = new DockPanel { Background = Brushes.White };
        var psFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
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

    private UIElement PSMonitorContentBuild()
    {
        var psMonitor = new Grid();
        psMonitor.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star),
            MinHeight = PSMonitorRailMinimum
        });
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        psMonitor.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star),
            MinHeight = PSMonitorRailMinimum
        });
        psMonitor.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        psMonitorBeforeRadio = PSMonitorRadioBuild("NormalizePreview.Before", "NormalizePreview.BeforeSelect", true);
        psMonitorAfterRadio = PSMonitorRadioBuild("NormalizePreview.After", "NormalizePreview.AfterSelect", false);

        Grid psBefore = PSMonitorRailBuild(
            psMonitorBeforeRadio,
            out psMonitorBeforeCanvas,
            out psMonitorBeforeStatus,
            out psMonitorBeforeHead);
        Grid.SetRow(psBefore, 0);
        psMonitor.Children.Add(psBefore);

        var psDivider = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            Margin = new Thickness(0, 14, 0, 14)
        };
        Grid.SetRow(psDivider, 1);
        psMonitor.Children.Add(psDivider);

        Grid psAfter = PSMonitorRailBuild(
            psMonitorAfterRadio,
            out psMonitorAfterCanvas,
            out psMonitorAfterStatus,
            out psMonitorAfterHead);
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
        psMonitorScrollbar.ValueChanged += (_, pEvent) => psMonitorSource.LSMonitorOffsetSet(pEvent.NewValue);
        Grid.SetRow(psMonitorScrollbar, 3);
        psMonitor.Children.Add(psMonitorScrollbar);
        return psMonitor;
    }

    private Grid PSMonitorRailBuild(RadioButton pRadio, out Canvas pCanvas, out TextBlock pStatus, out Border pHead)
    {
        var psRail = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC)),
            ClipToBounds = true
        };

        var psCanvas = new Canvas { Background = Brushes.Transparent };
        psCanvas.SizeChanged += (_, _) => PSMonitorUpdate();
        psCanvas.MouseLeftButtonDown += PSMonitorSeekStart;
        psCanvas.MouseMove += PSMonitorSeekHandle;
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

    private void PSMonitorUpdate()
    {
        PSMonitorRailDraw(psMonitorBeforeCanvas, psMonitorBeforeStatus, psMonitorBeforeFill, false);
        PSMonitorRailDraw(psMonitorAfterCanvas, psMonitorAfterStatus, psMonitorAfterFill, true);
        PSMonitorHeadPlace();
    }

    private void PSMonitorReadyHandle() => Dispatcher.BeginInvoke(PSMonitorReadyApply);

    private void PSMonitorReadyApply()
    {
        psMonitorTimer.Stop();
        psMonitorTimer.Start();
    }

    private void PSMonitorTickHandle(object? pSender, EventArgs pEvent)
    {
        psMonitorTimer.Stop();
        PSMonitorUpdate();
    }

    private void PSMonitorRailDraw(Canvas pCanvas, TextBlock pStatus, Brush pFill, bool pAfter)
    {
        pStatus.Text = psMonitorSource.LSMonitorStatusRead(pAfter);
        pCanvas.Children.Clear();
        LSMonitorFrame lFrame = psMonitorSource.LSMonitorFrameResolve(
            pAfter, pCanvas.ActualWidth, pCanvas.ActualHeight);
        lFrame.LSMonitorFrameLines.ToList().ForEach(pY => PSMonitorLineDraw(pCanvas, pY));
        lFrame.LSMonitorFrameLabels.ToList().ForEach(lLabel => PSMonitorLabelDraw(pCanvas, lLabel));
        pCanvas.Children.Add(new System.Windows.Shapes.Path
        {
            Data = PFlow.PFlowWaveformBuild(lFrame.LSMonitorFrameOutline),
            Fill = pFill
        });
    }

    private static void PSMonitorLineDraw(Canvas pCanvas, double pY) => pCanvas.Children.Add(
        new System.Windows.Shapes.Line
        {
            X1 = PSMonitorGutter,
            X2 = pCanvas.ActualWidth,
            Y1 = pY,
            Y2 = pY,
            Stroke = psMonitorGridFill,
            StrokeThickness = 1,
            IsHitTestVisible = false
        });

    private static void PSMonitorLabelDraw(Canvas pCanvas, LSMonitorLabel lLabel)
    {
        var pText = new TextBlock
        {
            Text = lLabel.LSMonitorLabelText,
            FontSize = 10,
            Foreground = psMonitorAxisFill,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(pText, 4);
        Canvas.SetTop(pText, lLabel.LSMonitorLabelTop);
        pCanvas.Children.Add(pText);
    }

    private void PSMonitorCloseHandle(object? pSender, EventArgs pEvent)
    {
        psMonitorSource.LSMonitorReady -= PSMonitorReadyHandle;
        psMonitorSource.LSMonitorCursorChange -= PSMonitorCursorApply;
        psMonitorSource.LSMonitorPlayingChange -= PSMonitorPlayingApply;
        psMonitorSource.LSMonitorZoomChange -= PSMonitorZoomApply;
        psMonitorSource.LSMonitorBypassChange -= PSMonitorBypassApply;
        psMonitorSource.LSMonitorSeekApply -= psMonitorFlow.LFlow.LFlowCursorSeek;
        psMonitorSource.LSMonitorPlayApply -= psMonitorFlow.LFlow.LFlowPlayRaise;
        psMonitorSource.LSMonitorPauseApply -= psMonitorFlow.LFlow.LFlowPauseRaise;
        psMonitorSource.LSMonitorViewerDetach();
        psMonitorFlow.LFlow.LFlowCursorChange -= PSMonitorCursorHandle;
        psMonitorTimer.Stop();
        psMonitorTimer.Tick -= PSMonitorTickHandle;
        PSGrabber.PSGrabberPlacementSave(this, PSMonitorPlacementKey);
        psMonitorGrabber.PSGrabberDetach();
    }
}
