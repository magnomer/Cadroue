using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Media;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PFlow;


public sealed partial class PFlow : UserControl
{
    private const int LSectionPaletteCount = 6;
    private const double PFlowHeightMinimum = 200;
    private const double PFlowHeightMaximum = 520;
    private readonly LKeyframeOrchestrator lKeyframeOrchestrator = new();
    private readonly DispatcherTimer lKeyframeRequestTimer;
    private readonly PViewfinder pViewfinder = new();
    private readonly PMap pMap = new();
    private readonly TextBlock pViewfinderLabelLeft = PFlowReelLabelBuild();
    private readonly TextBlock pViewfinderLabelRight = PFlowReelLabelBuild();
    private readonly TextBlock pMapLabelLeft = PFlowReelLabelBuild();
    private readonly TextBlock pMapLabelRight = PFlowReelLabelBuild();
    private readonly List<LSectionEntry> lSectionList = new();
    private readonly StackPanel pFlowSectionButtonGroup = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
    private Border? pFlowResizeHandle;
    private LSpool? lSpool;
    private TimeSpan lCursor;
    private string? lSourcePath;
    private int? lSectionIndexSelect;
    private double pFlowResizeStartY;
    private double pFlowResizeStartHeight;
    private bool pFlowResizeState;
    private bool pFlowSectionUiActive;
    private bool pFlowCommandActive;
    private bool pFlowUnloaded;

    public event Action<TimeSpan>? PFlowCursorChangeRequest;
    public event Action? PFlowPlayRequest;
    public event Action? PFlowPauseRequest;
    public event Action<double>? PFlowVolumeChangeRequest;
    public event Action<double>? PFlowVolumeValueNotice;
    public event Action<IReadOnlyList<LSectionEntry>, int?>? PFlowSectionChangeNotice;

    public PFlow()
    {
        Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
        MinHeight = PFlowHeightMinimum;
        pViewfinder.PViewfinderCursorChangeRequest += PFlowViewfinderCursorChangeHandle;
        pViewfinder.PViewfinderSectionSelectRequest += PFlowViewfinderSectionSelectHandle;
        pMap.PMapCursorChangeRequest += PFlowMapCursorChangeHandle;
        pMap.PMapSpoolChangeRequest += PFlowSpoolPropagateUpdate;
        lKeyframeOrchestrator.LKeyframeNoticeReady += PFlowKeyframeNoticeHandle;
        lKeyframeRequestTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        lKeyframeRequestTimer.Tick += PFlowKeyframeRequestTimerHandle;
        pFlowResizeHandle = PFlowResizeHandleBuild();
        pFlowResizeHandle.MouseLeftButtonDown += PFlowResizeDownHandle;
        pFlowResizeHandle.MouseMove += PFlowResizeMoveHandle;
        pFlowResizeHandle.MouseLeftButtonUp += PFlowResizeUpHandle;
        pFlowResizeHandle.LostMouseCapture += PFlowResizeCaptureLostHandle;

        Grid viewfinderReel = PFlowReelGridBuild(pViewfinder, pViewfinderLabelLeft, pViewfinderLabelRight);
        Grid mapReel = PFlowReelGridBuild(pMap, pMapLabelLeft, pMapLabelRight);
        Grid reelGrid = new() { Margin = new Thickness(8, 0, 8, 8) };
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(37, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(49, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8, GridUnitType.Star) });
        Grid.SetRow(mapReel, 1);
        Grid.SetRow(viewfinderReel, 3);
        reelGrid.Children.Add(mapReel);
        reelGrid.Children.Add(viewfinderReel);

        DockPanel root = new() { LastChildFill = true };
        DockPanel.SetDock(pFlowResizeHandle, Dock.Top);
        root.Children.Add(pFlowResizeHandle);
        root.Children.Add(reelGrid);
        Content = root;
        Height = App.LPreferenceStateCurrent.LPreferenceFlowHeight;
    }

    public void PFlowAttach(LMediaInfo mediaInfo, string? sourcePath, TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        lKeyframeRequestTimer.Stop();
        lSourcePath = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
        lSpool = new LSpool(mediaInfo.LMediaInfoDuration);
        lCursor = PFlowCursorClamp(cursorTime);
        lSectionList.Clear();
        lSectionIndexSelect = null;
        pViewfinder.PViewfinderAttach(lSpool, lCursor);
        pMap.PMapAttach(lSpool, lCursor);
        pViewfinder.PViewfinderSectionsUpdate(lSectionList, lSectionIndexSelect);
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolWorkingRangeStart);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolWorkingRangeEnd);
        pMapLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolDuration);
        pViewfinder.PViewfinderKeyframesUpdate(Array.Empty<LKeyframeEntry>(), Array.Empty<LKeyframeScanRange>());
        pMap.PMapKeyframesUpdate(Array.Empty<LKeyframeScanRange>());
        PFlowSectionChangeNotice?.Invoke(lSectionList.AsReadOnly(), lSectionIndexSelect);
        PFlowKeyframeRequestNow();
    }

    public void PFlowCursorUpdate(TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        PFlowCursorPropagate(cursorTime, false, false);
        PFlowKeyframeRequestSchedule();
    }

    public void PFlowClear()
    {
        if (!pFlowCommandActive) return;
        lKeyframeRequestTimer.Stop();
        lSourcePath = null;
        lSpool = null;
        lCursor = TimeSpan.Zero;
        lSectionList.Clear();
        lSectionIndexSelect = null;
        pViewfinder.PViewfinderClear();
        pMap.PMapClear();
        pViewfinderLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pViewfinderLabelRight.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelRight.Text = PFlowTimeFormat(TimeSpan.Zero);
        PFlowSectionChangeNotice?.Invoke(lSectionList.AsReadOnly(), lSectionIndexSelect);
    }

    public void PFlowVolumeValueSet(double volume)
    {
        if (!pFlowCommandActive) return;
        PFlowVolumeValueNotice?.Invoke(LPreferenceState.LPreferenceVolumeClamp(volume));
    }

    public void PFlowCommandActiveSet(bool pCommandActive)
    {
        pFlowCommandActive = pCommandActive;
        if (pFlowCommandActive)
        {
            PFlowKeyframeRequestSchedule();
        }
        else
        {
            lKeyframeRequestTimer.Stop();
        }
    }

    public void PFlowSectionUiActiveSet(bool sectionUiActive)
    {
        pFlowSectionUiActive = sectionUiActive;
        pFlowSectionButtonGroup.Visibility = sectionUiActive ? Visibility.Visible : Visibility.Collapsed;
    }

    public void PFlowCloseRequest()
    {
        if (pFlowUnloaded) return;
        pFlowUnloaded = true;
        lKeyframeRequestTimer.Stop();
        lKeyframeRequestTimer.Tick -= PFlowKeyframeRequestTimerHandle;
        lKeyframeOrchestrator.LKeyframeNoticeReady -= PFlowKeyframeNoticeHandle;
        lKeyframeOrchestrator.Dispose();
        if (pFlowResizeHandle is null) return;
        pFlowResizeHandle.MouseLeftButtonDown -= PFlowResizeDownHandle;
        pFlowResizeHandle.MouseMove -= PFlowResizeMoveHandle;
        pFlowResizeHandle.MouseLeftButtonUp -= PFlowResizeUpHandle;
        pFlowResizeHandle.LostMouseCapture -= PFlowResizeCaptureLostHandle;
    }

    public bool PFlowShortcutRequest(string pFlowShortcutCode)
    {
        if (!pFlowCommandActive || lSpool is null) return false;
        switch (pFlowShortcutCode)
        {
            case "zoomIn": lSpool.LSpoolZoomIn(lCursor); PFlowSpoolPropagateUpdate(); return true;
            case "zoomOut": lSpool.LSpoolZoomOut(lCursor); PFlowSpoolPropagateUpdate(); return true;
            case "addSection" when pFlowSectionUiActive: PFlowSectionAdd(); return true;
            case "setStart" when pFlowSectionUiActive: PFlowSectionStartSet(); return true;
            case "splitSection" when pFlowSectionUiActive: PFlowSectionSplit(); return true;
            case "setEnd" when pFlowSectionUiActive: PFlowSectionEndSet(); return true;
            case "deleteSection" when pFlowSectionUiActive: PFlowSectionDelete(); return true;
            case "previousKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframeMovePrevious(lCursor)); return true;
            case "nearestKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframeMoveNearest(lCursor)); return true;
            case "nextKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframeMoveNext(lCursor)); return true;
            default: return false;
        }
    }

    public void PFlowPlayRequestCall()
    {
        if (pFlowCommandActive) PFlowPlayRequest?.Invoke();
    }

    public void PFlowPauseRequestCall()
    {
        if (pFlowCommandActive) PFlowPauseRequest?.Invoke();
    }

    public void PFlowVolumeChangeRequestCall(double pFlowVolume)
    {
        if (!pFlowCommandActive) return;
        double pFlowVolumeClamp = LPreferenceState.LPreferenceVolumeClamp(pFlowVolume);
        PFlowVolumeValueSet(pFlowVolumeClamp);
        PFlowVolumeChangeRequest?.Invoke(pFlowVolumeClamp);
    }

    private void PFlowViewfinderCursorChangeHandle(TimeSpan cursorTime) => PFlowUserCursorRequest(cursorTime);
    private void PFlowMapCursorChangeHandle(TimeSpan cursorTime) => PFlowUserCursorRequest(cursorTime);

    private void PFlowUserCursorRequest(TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        PFlowCursorPropagate(cursorTime, true, true);
    }

    private void PFlowCursorPropagate(TimeSpan cursorTime, bool pFlowViewerSeekRequest, bool lKeyframeRestartRequest)
    {
        lCursor = PFlowCursorClamp(cursorTime);
        pViewfinder.PViewfinderCursorUpdate(lCursor);
        pMap.PMapCursorUpdate(lCursor);

        if (lKeyframeRestartRequest)
        {
            PFlowKeyframeRequestNow();
        }

        if (pFlowViewerSeekRequest)
        {
            PFlowCursorChangeRequest?.Invoke(lCursor);
        }
    }

    private void PFlowSpoolPropagateUpdate()
    {
        pViewfinder.PViewfinderSpoolUpdate();
        pMap.PMapSpoolUpdate();
        if (lSpool is null) return;
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolWorkingRangeStart);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolWorkingRangeEnd);
        PFlowKeyframeRequestSchedule();
    }

    private void PFlowKeyframeRequestSchedule()
    {
        if (!pFlowCommandActive || pFlowUnloaded || lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) { lKeyframeRequestTimer.Stop(); return; }
        lKeyframeRequestTimer.Stop();
        lKeyframeRequestTimer.Start();
    }

    private void PFlowKeyframeRequestNow()
    {
        lKeyframeRequestTimer.Stop();
        if (pFlowCommandActive && !pFlowUnloaded && lSpool is not null && !string.IsNullOrWhiteSpace(lSourcePath))
            lKeyframeOrchestrator.LKeyframeRequest(lSourcePath, lSpool.LSpoolDuration, lCursor);
    }

    private void PFlowKeyframeRequestTimerHandle(object? sender, EventArgs e) => PFlowKeyframeRequestNow();

    private void PFlowKeyframeNoticeHandle(LKeyframeNotice notice)
    {
        if (!pFlowCommandActive || pFlowUnloaded || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        Dispatcher.InvokeAsync(() =>
        {
            if (!pFlowUnloaded && notice.LRequestSerial == lKeyframeOrchestrator.LKeyframeCurrentSerial)
            {
                pViewfinder.PViewfinderKeyframesUpdate(notice.LKeyframes, notice.LScannedRanges);
                pMap.PMapKeyframesUpdate(notice.LScannedRanges);
            }
        }, DispatcherPriority.Background);
    }

    private void PFlowKeyframeMove(TimeSpan? keyframeTargetTime)
    {
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath) || keyframeTargetTime is null)
        {
            PFlowKeyframeRequestSchedule();
            return;
        }
        PFlowCursorPropagate(keyframeTargetTime.Value, true, true);
    }

    private void PFlowResizeDownHandle(object sender, MouseButtonEventArgs e)
    {
        Window? ownerWindow = Window.GetWindow(this);
        if (pFlowResizeHandle is null || ownerWindow is null) return;
        pFlowResizeState = true;
        pFlowResizeStartY = e.GetPosition(ownerWindow).Y;
        pFlowResizeStartHeight = ActualHeight;
        pFlowResizeHandle.CaptureMouse();
        e.Handled = true;
    }

    private void PFlowResizeMoveHandle(object sender, MouseEventArgs e)
    {
        if (!pFlowResizeState) return;
        Window? ownerWindow = Window.GetWindow(this);
        if (ownerWindow is null) { PFlowResizeStateClear(); return; }
        Height = Math.Clamp(pFlowResizeStartHeight + pFlowResizeStartY - e.GetPosition(ownerWindow).Y, PFlowHeightMinimum, PFlowHeightMaximum);
        e.Handled = true;
    }

    private void PFlowResizeUpHandle(object sender, MouseButtonEventArgs e) { PFlowResizeStateClear(); e.Handled = true; }
    private void PFlowResizeCaptureLostHandle(object sender, MouseEventArgs e) => PFlowResizeStateClear();

    private void PFlowResizeStateClear()
    {
        pFlowResizeState = false;
        if (pFlowResizeHandle?.IsMouseCaptured == true) pFlowResizeHandle.ReleaseMouseCapture();
    }

    private TimeSpan PFlowCursorClamp(TimeSpan cursorTime)
    {
        if (lSpool is null || cursorTime < TimeSpan.Zero) return TimeSpan.Zero;
        return cursorTime > lSpool.LSpoolDuration ? lSpool.LSpoolDuration : cursorTime;
    }

    private static string PFlowTimeFormat(TimeSpan displayTime) => displayTime.TotalHours >= 1
        ? $"{(int)displayTime.TotalHours}:{displayTime.Minutes:D2}:{displayTime.Seconds:D2}"
        : $"{displayTime.Minutes}:{displayTime.Seconds:D2}";
}
