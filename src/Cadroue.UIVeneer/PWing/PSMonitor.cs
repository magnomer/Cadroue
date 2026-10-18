using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PBench;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PSMonitor : Window
{
    private const string PSMonitorPlacementKey = "NormalizePreview";
    private const double PSMonitorWidthDefault = 660;
    private const double PSMonitorWidthMinimum = 480;
    private const double PSMonitorHeightDefault = 560;
    private const double PSMonitorHeightMinimum = 360;
    private const double PSMonitorInset = 18;
    private const double PSMonitorRailMinimum = 110;
    private const double PSMonitorGutter = QSMonitorPlan.QSMonitorGutter;

    private static readonly Brush psMonitorBeforeFill = new SolidColorBrush(Color.FromRgb(0xB4, 0xC2, 0xD6));
    private static readonly Brush psMonitorAfterFill = new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6));

    private readonly PSGrabber psMonitorGrabber;
    private readonly QSMonitor psMonitorSource;
    private readonly PFlow psMonitorFlow;
    private readonly PViewer psMonitorViewer;
    private readonly DispatcherTimer psMonitorTimer;
    private readonly PSMonitorTransport psMonitorTransport;

    private PSMonitorRail psMonitorBeforeRail = null!;
    private PSMonitorRail psMonitorAfterRail = null!;
    private ScrollBar psMonitorScrollbar = null!;

    internal static PSMonitor PSMonitorShow(Window? pOwner, QSMonitor pSource, PFlow pFlow, PViewer pViewer)
    {
        System.Windows.Application.Current.Windows.OfType<PSMonitor>().FirstOrDefault()?.Close();
        var psMonitor = new PSMonitor(pOwner, pSource, pFlow, pViewer);
        psMonitor.Show();
        return psMonitor;
    }

    private PSMonitor(Window? pOwner, QSMonitor pSource, PFlow pFlow, PViewer pViewer)
    {
        psMonitorSource = pSource;
        psMonitorFlow = pFlow;
        psMonitorViewer = pViewer;
        PSMonitorChromePrepare(pOwner);
        QScrollbar.QScrollbarAttach(this);
        psMonitorTimer = PSMonitorTimerCreate();
        psMonitorTransport = new PSMonitorTransport(pSource);
        Content = PSMonitorBuild();
        PSGrabberPlacement.PSGrabberPlacementRestore(this, PSMonitorPlacementKey);
        psMonitorGrabber = new PSGrabber(this);
        psMonitorGrabber.PSGrabberAttach();
        PSMonitorHandlersAttach();
        psMonitorSource.QSMonitorViewerAttach(psMonitorViewer.QViewer);
        psMonitorSource.QSMonitorCursorSet(pFlow.QFlow.QFlowCursor);
        psMonitorSource.QSMonitorWaveform.QSMonitorUpdate();
        PSMonitorZoomApply();
    }

    private void PSMonitorChromePrepare(Window? pOwner)
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("NormalizePreview.Window.Title");
        Owner = pOwner;
        ShowInTaskbar = true;
        Width = PSMonitorWidthDefault;
        Height = PSMonitorHeightDefault;
        MinWidth = PSMonitorWidthMinimum;
        MinHeight = PSMonitorHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
    }

    private static DispatcherTimer PSMonitorTimerCreate() => new() { Interval = TimeSpan.FromMilliseconds(30) };

    private void PSMonitorHandlersAttach()
    {
        psMonitorTimer.Tick += PSMonitorTickHandle;
        psMonitorSource.QSMonitorWaveform.QSMonitorReady += PSMonitorReadyHandle;
        psMonitorSource.QSMonitorCursorChanged += PSMonitorCursorApply;
        psMonitorSource.QSMonitorPlayingChanged += psMonitorTransport.PSMonitorPlayingApply;
        psMonitorSource.QSMonitorViewport.QSMonitorZoomChanged += PSMonitorZoomApply;
        psMonitorSource.QSMonitorBypassChanged += PSMonitorBypassApply;
        psMonitorSource.QSMonitorSeekApplied += psMonitorFlow.QFlow.QFlowCursorSeek;
        psMonitorSource.QSMonitorPlayApplied += psMonitorFlow.QFlow.QFlowPlayRaise;
        psMonitorSource.QSMonitorPauseApplied += psMonitorFlow.QFlow.QFlowPauseRaise;
        psMonitorFlow.QFlow.QFlowCursorChanged += PSMonitorCursorHandle;
        Closed += PSMonitorCloseHandle;
    }

    private UIElement PSMonitorBuild() =>
        PSDialog.PSDialogBuild(this, Title, PSMonitorRootBuild());

    private DockPanel PSMonitorRootBuild()
    {
        Button psClose = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead(
            "NormalizePreview.Close"));
        PSMonitorCloseAttach(psClose);
        return PSMonitorLayoutBuild(psClose);
    }

    private void PSMonitorCloseAttach(Button psClose) => psClose.Click += (_, _) => Close();

    private DockPanel PSMonitorLayoutBuild(Button psClose)
    {
        var psMonitor = new DockPanel { Background = Brushes.White };
        var psFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
        psFooter.Children.Add(psClose);
        DockPanel.SetDock(psFooter, Dock.Bottom);
        psMonitor.Children.Add(psFooter);

        var psContent = new DockPanel { Margin = new Thickness(PSMonitorInset, 14, PSMonitorInset, 8) };
        UIElement psZoomBar = psMonitorTransport.PSMonitorZoomBuild();
        DockPanel.SetDock(psZoomBar, Dock.Top);
        psContent.Children.Add(psZoomBar);
        UIElement psTransport = psMonitorTransport.PSMonitorTransportBuild();
        DockPanel.SetDock(psTransport, Dock.Bottom);
        psContent.Children.Add(psTransport);
        psContent.Children.Add(PSMonitorContentBuild());
        psMonitor.Children.Add(psContent);
        return psMonitor;
    }

    private UIElement PSMonitorContentBuild()
    {
        Grid psMonitor = PSMonitorGridBuild();
        PSMonitorScrollAttach();
        return psMonitor;
    }

    private void PSMonitorScrollAttach() =>
        psMonitorScrollbar.ValueChanged += (_, pEvent) =>
            psMonitorSource.QSMonitorViewport.QSMonitorOffsetSet(pEvent.NewValue);

    private Grid PSMonitorGridBuild()
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

        psMonitorBeforeRail = new PSMonitorRail(
            psMonitorSource,
            psMonitorViewer.QViewer,
            psMonitorBeforeFill,
            false,
            "NormalizePreview.Before",
            "NormalizePreview.BeforeSelect",
            PSMonitorUpdate);
        Grid psBefore = psMonitorBeforeRail.PSMonitorRailGrid;
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

        psMonitorAfterRail = new PSMonitorRail(
            psMonitorSource,
            psMonitorViewer.QViewer,
            psMonitorAfterFill,
            true,
            "NormalizePreview.After",
            "NormalizePreview.AfterSelect",
            PSMonitorUpdate);
        Grid psAfter = psMonitorAfterRail.PSMonitorRailGrid;
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
        Grid.SetRow(psMonitorScrollbar, 3);
        psMonitor.Children.Add(psMonitorScrollbar);
        return psMonitor;
    }

    private void PSMonitorUpdate()
    {
        psMonitorBeforeRail.PSMonitorRailDraw();
        psMonitorAfterRail.PSMonitorRailDraw();
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

    private void PSMonitorZoomApply()
    {
        QSMonitorScrollbar lScroll = psMonitorSource.QSMonitorViewport.QSMonitorScrollRead();
        PSMonitorScrollSet(lScroll);
        PSMonitorUpdate();
    }

    private void PSMonitorScrollSet(QSMonitorScrollbar lScroll)
    {
        psMonitorScrollbar.ViewportSize = lScroll.QSMonitorScrollViewport;
        psMonitorScrollbar.Maximum = lScroll.QSMonitorScrollMaximum;
        psMonitorScrollbar.Value = lScroll.QSMonitorScrollValue;
        psMonitorScrollbar.IsEnabled = lScroll.QSMonitorScrollEnabled;
        psMonitorScrollbar.Opacity = lScroll.QSMonitorScrollOpacity;
    }

    private void PSMonitorCursorHandle(TimeSpan pCursor) =>
        Dispatcher.BeginInvoke(() => psMonitorSource.QSMonitorCursorSet(pCursor));

    private void PSMonitorCursorApply(TimeSpan pCursor) => PSMonitorHeadPlace();

    private void PSMonitorBypassApply()
    {
        psMonitorBeforeRail.PSMonitorRadioApply();
        psMonitorAfterRail.PSMonitorRadioApply();
    }

    private void PSMonitorHeadPlace()
    {
        psMonitorBeforeRail.PSMonitorHeadApply();
        psMonitorAfterRail.PSMonitorHeadApply();
    }

    private void PSMonitorCloseHandle(object? pSender, EventArgs pEvent)
    {
        PSMonitorHandlersDetach();
        psMonitorSource.QSMonitorViewerDetach();
        psMonitorTimer.Stop();
        PSGrabberPlacement.PSGrabberPlacementSave(this, PSMonitorPlacementKey);
        psMonitorGrabber.PSGrabberDetach();
    }

    private void PSMonitorHandlersDetach()
    {
        psMonitorSource.QSMonitorWaveform.QSMonitorReady -= PSMonitorReadyHandle;
        psMonitorSource.QSMonitorCursorChanged -= PSMonitorCursorApply;
        psMonitorSource.QSMonitorPlayingChanged -= psMonitorTransport.PSMonitorPlayingApply;
        psMonitorSource.QSMonitorViewport.QSMonitorZoomChanged -= PSMonitorZoomApply;
        psMonitorSource.QSMonitorBypassChanged -= PSMonitorBypassApply;
        psMonitorSource.QSMonitorSeekApplied -= psMonitorFlow.QFlow.QFlowCursorSeek;
        psMonitorSource.QSMonitorPlayApplied -= psMonitorFlow.QFlow.QFlowPlayRaise;
        psMonitorSource.QSMonitorPauseApplied -= psMonitorFlow.QFlow.QFlowPauseRaise;
        psMonitorFlow.QFlow.QFlowCursorChanged -= PSMonitorCursorHandle;
        psMonitorTimer.Tick -= PSMonitorTickHandle;
    }
}
