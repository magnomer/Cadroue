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
using static Cadroue.UIShell.PSShared.PSFooter;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSMonitor : Window
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

    internal static PSMonitor PSMonitorShow(Window? pOwner, LSMonitor pSource, PFlowControl pFlow, PViewer pViewer)
    {
        psMonitorCurrent?.Close();
        var psMonitor = new PSMonitor(pOwner, pSource, pFlow, pViewer);
        psMonitorCurrent = psMonitor;
        psMonitor.Show();
        return psMonitor;
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
        psMonitorViewer.PViewerClockTick += PSMonitorCursorHandle;
        psMonitorViewer.PViewerBypassChange += PSMonitorBypassHandle;
        psMonitorViewer.PViewerPlayingChange += PSMonitorPlayingHandle;
        psMonitorFlow.PFlowCursorChange += PSMonitorCursorHandle;
        Closed += PSMonitorCloseHandle;
        PSMonitorBypassHandle(psMonitorViewer.PViewerBypassRead());
        PSMonitorPlayingApply(psMonitorViewer.PViewerPlayingRead());
        psMonitorSource.LSMonitorUpdate();
    }

    private UIElement PSMonitorBuild() =>
        PSDialog.PSDialogBuild(this, psMonitorTitle, PSMonitorRootBuild());

    private DockPanel PSMonitorRootBuild()
    {
        var psMonitor = new DockPanel { Background = Brushes.White };
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
}
