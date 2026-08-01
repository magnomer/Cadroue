using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Media;
using Cadroue.UIShell;

using Cadroue.Core;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow : UserControl
{
    private const double PFlowVolumeStep = 5;
    private const double PFlowSeekFloor = 0.04;
    private const double PFlowSeekDivisor = 40;
    private const double PFlowHeightMinimum = 200;
    private const double PFlowHeightMaximum = 520;
    private static readonly TimeSpan PFlowResumeDelay = TimeSpan.FromSeconds(2);
    private readonly LKeyframeOrchestrator lKeyframeOrchestrator = new();
    private readonly DispatcherTimer lKeyframeRequestTimer;
    private readonly DispatcherTimer lKeyframeResumeTimer;
    private readonly PViewfinder pViewfinder = new();
    private readonly PMap pMap = new();
    private System.Windows.Controls.Primitives.Popup? pFlowNamePopup;

    private string? pFlowKeyframeStamp;

    private const double PFlowNameHeight = 32;
    private const double PFlowNameWidth = 220;
    private const double PFlowAffixWidth = 96;
    private readonly TextBlock pViewfinderLabelLeft = PReelLabelBuild();
    private readonly TextBlock pViewfinderLabelRight = PReelLabelBuild();
    private readonly TextBlock pMapLabelLeft = PReelLabelBuild();
    private readonly TextBlock pMapLabelRight = PReelLabelBuild();
    private readonly List<LSegment> lSectionList = new();
    private readonly StackPanel pFlowSectionButtons = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
    private Border? pDividerThumb;
    private readonly Grid pFlowViewfinderReel;
    private readonly Grid pFlowMapReel;
    private bool pFlowDragPaused;
    private double pFlowVolumeCurrent = 100;
    private LSpool? lSpool;
    private TimeSpan lCursor;
    private string? lSourcePath;

    private bool pFlowSidecarRestoring;
    private int? lSectionIndexActive;
    private double pDividerStartY;
    private double pDividerStartHeight;
    private bool pDividerState;
    private bool pFlowSectionActive;
    private bool pFlowCommandActive;
    private bool pFlowUnloaded;

    public event Action<TimeSpan>? PFlowCursorChange;
    public event Action? PFlowPlay;
    public event Action? PFlowPause;
    public event Action<double>? PFlowVolumeChange;
    public event Action<double>? PFlowVolumeValue;
    public event Action<IReadOnlyList<LSegment>, int?>? PFlowSectionChange;

    public PFlow()
    {
        Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
        MinHeight = PFlowHeightMinimum;
        pViewfinder.PViewfinderCursorChange += PFlowViewfinderSeek;
        pViewfinder.PViewfinderSectionSelect += PFlowViewfinderSelect;
        pViewfinder.PViewfinderDragChange += PFlowDragSet;
        pMap.PMapCursorChange += PFlowMapSeek;
        pMap.PMapSpoolChange += PFlowSpoolHandle;
        pMap.PMapDragChange += PFlowDragSet;
        lKeyframeOrchestrator.LKeyframeNoticeReady += PFlowNoticeHandle;
        lKeyframeOrchestrator.LKeyframeSectionsSource = PFlowSidecarRead;
        lWaveformOrchestrator.LWaveformReady += PFlowWaveformHandle;
        lKeyframeRequestTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        lKeyframeRequestTimer.Tick += PFlowTimerHandle;
        lKeyframeResumeTimer = new DispatcherTimer { Interval = PFlowResumeDelay };
        lKeyframeResumeTimer.Tick += PFlowResumeHandle;
        pDividerThumb = PDividerBuild();
        pDividerThumb.MouseLeftButtonDown += PDividerPressHandle;
        pDividerThumb.MouseMove += PDividerMoveHandle;
        pDividerThumb.MouseLeftButtonUp += PDividerReleaseHandle;
        pDividerThumb.LostMouseCapture += PDividerCaptureHandle;

        pFlowViewfinderReel = PReelGridBuild(pViewfinder, pViewfinderLabelLeft, pViewfinderLabelRight);
        pFlowMapReel = PReelGridBuild(pMap, pMapLabelLeft, pMapLabelRight);
        Grid reelGrid = new() { Margin = new Thickness(8, 0, 8, 8) };
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(37, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(49, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8, GridUnitType.Star) });
        reelGrid.Children.Add(pFlowMapReel);
        reelGrid.Children.Add(pFlowViewfinderReel);
        PFlowOrderApply();

        DockPanel root = new() { LastChildFill = true };
        DockPanel.SetDock(pDividerThumb, Dock.Top);
        root.Children.Add(pDividerThumb);
        root.Children.Add(reelGrid);
        Content = root;
        Height = PProgram.LPreferenceStateCurrent.LPreferenceFlowHeight;
    }

    public void PFlowAttach(LMediaInfo mediaInfo, string? sourcePath, TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        lSourcePath = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
        lSpool = new LSpool(mediaInfo.LMediaInfoDuration);
        lCursor = PFlowCursorClamp(cursorTime);
        lSectionList.Clear();
        lSectionIndexActive = null;
        pViewfinder.PViewfinderAttach(lSpool, lCursor);
        pMap.PMapAttach(lSpool, lCursor);
        pViewfinder.PViewfinderSectionsUpdate(lSectionList, lSectionIndexActive);
        pMap.PMapSectionsUpdate(lSectionList, lSectionIndexActive);
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolRangeOrigin);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolRangeLimit);
        pMapLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolDuration);
        pViewfinder.PViewfinderKeyframesUpdate(Array.Empty<LKeyframeEntry>(), Array.Empty<LKeyframeScanRange>());
        pMap.PMapKeyframesUpdate(Array.Empty<LKeyframeScanRange>());
        PFlowSidecarRestore();
        PFlowSectionChange?.Invoke(lSectionList.AsReadOnly(), lSectionIndexActive);
        PFlowKeyframeRun();
        PFlowWaveformStart();
    }

    private void PFlowSidecarRestore()
    {
        if (lSourcePath is not { } pFlowSourcePath)
        {
            return;
        }

        try
        {
            string pFlowSidecarPath = Cadroue.Media.LSidecarStore.LSidecarPathRead(pFlowSourcePath);
            if (Cadroue.Media.LSidecarStore.LSidecarRead(pFlowSidecarPath) is { } pFlowSidecar)
            {
                PFlowSidecarApply(pFlowSidecar.LSidecarSections);
            }
        }
        catch (Exception pFlowException)
        {
            LTraceLog.LTraceErrorRecord("Sidecar sections could not be restored", pFlowException);
        }
    }

    public void PFlowCursorUpdate(TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        PFlowCursorPropagate(cursorTime, false, false);
        PFlowKeyframeDefer();
    }

    public void PFlowClear()
    {
        if (!pFlowCommandActive) return;
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        lKeyframeOrchestrator.LKeyframeSuspend();
        lSourcePath = null;
        lSpool = null;
        lCursor = TimeSpan.Zero;
        lSectionList.Clear();
        lSectionIndexActive = null;
        pViewfinder.PViewfinderClear();
        pMap.PMapClear();
        PFlowWaveformClear();
        pViewfinderLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pViewfinderLabelRight.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelRight.Text = PFlowTimeFormat(TimeSpan.Zero);
        PFlowSectionChange?.Invoke(lSectionList.AsReadOnly(), lSectionIndexActive);
    }

    public void PFlowVolumeSet(double volume)
    {
        if (!pFlowCommandActive) return;
        pFlowVolumeCurrent = LPreferenceState.LPreferenceVolumeClamp(volume);
        PFlowVolumeValue?.Invoke(pFlowVolumeCurrent);
    }

    public void PFlowCommandSet(bool pCommandActive)
    {
        pFlowCommandActive = pCommandActive;
        if (pFlowCommandActive)
        {
            PFlowKeyframeDefer();
        }
        else
        {
            lKeyframeRequestTimer.Stop();
            lKeyframeResumeTimer.Stop();
            lKeyframeOrchestrator.LKeyframeSuspend();
        }
    }

    public void PFlowSectionShow(bool sectionUiActive)
    {
        pFlowSectionActive = sectionUiActive;
        pFlowSectionButtons.Visibility = sectionUiActive ? Visibility.Visible : Visibility.Collapsed;
    }

    public void PFlowClose()
    {
        if (pFlowUnloaded) return;
        pFlowUnloaded = true;
        PFlowNameClose();
        lKeyframeRequestTimer.Stop();
        lKeyframeRequestTimer.Tick -= PFlowTimerHandle;
        lKeyframeResumeTimer.Stop();
        lKeyframeResumeTimer.Tick -= PFlowResumeHandle;
        lKeyframeOrchestrator.LKeyframeNoticeReady -= PFlowNoticeHandle;
        lKeyframeOrchestrator.Dispose();
        PFlowWaveformClose();
        if (pDividerThumb is null) return;
        pDividerThumb.MouseLeftButtonDown -= PDividerPressHandle;
        pDividerThumb.MouseMove -= PDividerMoveHandle;
        pDividerThumb.MouseLeftButtonUp -= PDividerReleaseHandle;
        pDividerThumb.LostMouseCapture -= PDividerCaptureHandle;
    }

    public bool PFlowShortcutDispatch(string pFlowShortcutCode)
    {
        if (!pFlowCommandActive || lSpool is null) return false;
        switch (pFlowShortcutCode)
        {
            case "zoomIn": lSpool.LSpoolInZoom(lCursor); PFlowSpoolUpdate(); return true;
            case "zoomOut": lSpool.LSpoolOutZoom(lCursor); PFlowSpoolUpdate(); return true;
            case "addSection" when pFlowSectionActive: PFlowSectionAdd(); return true;
            case "setStart" when pFlowSectionActive: PFlowStartSet(); return true;
            case "splitSection" when pFlowSectionActive: PFlowSectionDivide(); return true;
            case "setEnd" when pFlowSectionActive: PFlowEndSet(); return true;
            case "deleteSection" when pFlowSectionActive: PFlowSectionDelete(); return true;
            case "nameSection" when pFlowSectionActive: return PFlowNameShow();
            case "previousKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframePreviousMove(lCursor)); return true;
            case "nearestKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframeNearestMove(lCursor)); return true;
            case "nextKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframeNextMove(lCursor)); return true;
            default: return false;
        }
    }

    public Func<bool>? PFlowPlayingSource { get; set; }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!pFlowCommandActive || e.Delta == 0) return;
        int pWheelSteps = e.Delta / 120;
        if (pWheelSteps == 0) pWheelSteps = e.Delta > 0 ? 1 : -1;

        switch (PProgram.LPreferenceStateCurrent.LPreferenceWheelAction)
        {
            case "Zoom":
                PFlowWheelZoom(pWheelSteps);
                break;
            case "Volume":
                PFlowVolumeRaise(pFlowVolumeCurrent + pWheelSteps * PFlowVolumeStep);
                break;
            default:
                PFlowWheelSeek(pWheelSteps);
                break;
        }

        e.Handled = true;
    }

    private void PFlowWheelSeek(int pWheelSteps)
    {
        if (lSpool is null) return;
        TimeSpan pWheelRange = lSpool.LSpoolRangeLimit - lSpool.LSpoolRangeOrigin;
        double pWheelSeconds = Math.Max(PFlowSeekFloor, pWheelRange.TotalSeconds / PFlowSeekDivisor);
        PFlowCursorSeek(PFlowCursorClamp(lCursor + TimeSpan.FromSeconds(pWheelSeconds * pWheelSteps)));
    }

    private void PFlowWheelZoom(int pWheelSteps)
    {
        if (lSpool is null) return;
        for (int pWheelIndex = 0; pWheelIndex < Math.Abs(pWheelSteps); pWheelIndex++)
        {
            if (pWheelSteps > 0)
            {
                lSpool.LSpoolInZoom(lCursor);
            }
            else
            {
                lSpool.LSpoolOutZoom(lCursor);
            }
        }

        PFlowSpoolHandle();
    }

    internal void PFlowDragSet(bool pFlowDragging)
    {
        if (!pFlowCommandActive || !PProgram.LPreferenceStateCurrent.LPreferenceDragPaused) return;

        if (pFlowDragging)
        {
            if (pFlowDragPaused || PFlowPlayingSource?.Invoke() != true) return;
            pFlowDragPaused = true;
            PFlowPauseRaise();
            return;
        }

        if (!pFlowDragPaused) return;
        pFlowDragPaused = false;
        PFlowPlayRaise();
    }

    public void PFlowPlayRaise()
    {
        if (pFlowCommandActive) PFlowPlay?.Invoke();
    }

    public void PFlowPauseRaise()
    {
        if (pFlowCommandActive) PFlowPause?.Invoke();
    }

    public void PFlowVolumeRaise(double pFlowVolume)
    {
        if (!pFlowCommandActive) return;
        double pFlowVolumeClamp = LPreferenceState.LPreferenceVolumeClamp(pFlowVolume);
        PFlowVolumeSet(pFlowVolumeClamp);
        PFlowVolumeChange?.Invoke(pFlowVolumeClamp);
    }

    private void PFlowViewfinderSeek(TimeSpan cursorTime) => PFlowCursorSeek(cursorTime);
    private void PFlowMapSeek(TimeSpan cursorTime) => PFlowCursorSeek(cursorTime);

    private void PFlowCursorSeek(TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        PFlowKeyframeSuspend();
        PFlowCursorPropagate(cursorTime, true, false);
    }

    private void PFlowSpoolHandle()
    {
        PFlowKeyframeSuspend();
        PFlowSpoolUpdate();
    }

    private void PFlowCursorPropagate(TimeSpan cursorTime, bool pFlowViewerSeekRequest, bool lKeyframeRestartRequest)
    {
        lCursor = PFlowCursorClamp(cursorTime);
        pViewfinder.PViewfinderCursorUpdate(lCursor);
        pMap.PMapCursorUpdate(lCursor);

        if (lKeyframeRestartRequest)
        {
            PFlowKeyframeRun();
        }

        if (pFlowViewerSeekRequest)
        {
            PFlowCursorChange?.Invoke(lCursor);
        }
    }

    private void PFlowSpoolUpdate()
    {
        pViewfinder.PViewfinderSpoolUpdate();
        pMap.PMapSpoolUpdate();
        if (lSpool is null) return;
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolRangeOrigin);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolRangeLimit);
        PFlowKeyframeDefer();
    }

    private void PFlowKeyframeDefer()
    {
        if (!pFlowCommandActive || pFlowUnloaded || lSpool is null || string.IsNullOrWhiteSpace(lSourcePath)) { lKeyframeRequestTimer.Stop(); return; }
        if (lKeyframeResumeTimer.IsEnabled) return;
        lKeyframeRequestTimer.Stop();
        lKeyframeRequestTimer.Start();
    }

    private void PFlowKeyframeSuspend()
    {
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        lKeyframeOrchestrator.LKeyframeSuspend();
        if (pFlowCommandActive && !pFlowUnloaded) lKeyframeResumeTimer.Start();
    }

    private void PFlowKeyframeRun()
    {
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        if (pFlowCommandActive && !pFlowUnloaded && lSpool is not null && !string.IsNullOrWhiteSpace(lSourcePath))
        {
            LTrace.LTraceRecord(
                LTraceKind.LTraceWork,
                $"Keyframe scan requested around {lCursor:hh\\:mm\\:ss\\.fff}",
                $"source {System.IO.Path.GetFileName(lSourcePath)}, duration {lSpool.LSpoolDuration:hh\\:mm\\:ss}\n"
                + $"window {LKeyframeOrchestrator.LKeyframeRangeBefore:hh\\:mm\\:ss} before to {LKeyframeOrchestrator.LKeyframeRangeAfter:hh\\:mm\\:ss} after the cursor");
            lKeyframeOrchestrator.LKeyframeStart(lSourcePath, lSpool.LSpoolDuration, lCursor);
        }
    }

    private void PFlowTimerHandle(object? sender, EventArgs e) => PFlowKeyframeRun();

    private void PFlowResumeHandle(object? sender, EventArgs e) => PFlowKeyframeRun();

    private void PFlowNoticeHandle(LKeyframeNotice notice)
    {
        if (!pFlowCommandActive || pFlowUnloaded || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        Dispatcher.InvokeAsync(() =>
        {
            if (!pFlowUnloaded && notice.LKeyframeSerial == lKeyframeOrchestrator.LKeyframeCurrentSerial)
            {
                PFlowKeyframeRecord(notice);
                pViewfinder.PViewfinderKeyframesUpdate(notice.LKeyframeList, notice.LKeyframeRanges);
                pMap.PMapKeyframesUpdate(notice.LKeyframeRanges);
            }
        }, DispatcherPriority.Background);
    }

    private void PFlowKeyframeRecord(LKeyframeNotice notice)
    {
        double pFlowScanned = notice.LKeyframeRanges.Sum(
            pRange => (pRange.LKeyframeRangeLimit - pRange.LKeyframeRangeOrigin).TotalSeconds);
        string pFlowStamp = $"{notice.LKeyframeList.Count}/{notice.LKeyframeRanges.Count}/{pFlowScanned:0.###}";
        if (string.Equals(pFlowStamp, pFlowKeyframeStamp, StringComparison.Ordinal))
        {
            return;
        }

        pFlowKeyframeStamp = pFlowStamp;
        string pFlowSource = string.IsNullOrWhiteSpace(lSourcePath)
            ? "(no media)"
            : System.IO.Path.GetFileName(lSourcePath);
        LTraceLog.LTraceInfoRecord(
            $"Keyframe scan '{pFlowSource}': {notice.LKeyframeList.Count} keyframe(s) known, " +
            $"{TimeSpan.FromSeconds(pFlowScanned):hh\\:mm\\:ss} scanned across {notice.LKeyframeRanges.Count} range(s)");
    }

    private void PFlowKeyframeMove(TimeSpan? keyframeTargetTime)
    {
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath) || keyframeTargetTime is null)
        {
            PFlowKeyframeDefer();
            return;
        }
        PFlowCursorPropagate(keyframeTargetTime.Value, true, true);
    }

    private void PDividerPressHandle(object sender, MouseButtonEventArgs e)
    {
        Window? ownerWindow = Window.GetWindow(this);
        if (pDividerThumb is null || ownerWindow is null) return;
        pDividerState = true;
        pDividerStartY = e.GetPosition(ownerWindow).Y;
        pDividerStartHeight = ActualHeight;
        pDividerThumb.CaptureMouse();
        e.Handled = true;
    }

    private void PDividerMoveHandle(object sender, MouseEventArgs e)
    {
        if (!pDividerState) return;
        Window? ownerWindow = Window.GetWindow(this);
        if (ownerWindow is null) { PDividerClear(); return; }
        Height = Math.Clamp(pDividerStartHeight + pDividerStartY - e.GetPosition(ownerWindow).Y, PFlowHeightMinimum, PFlowHeightMaximum);
        e.Handled = true;
    }

    private void PDividerReleaseHandle(object sender, MouseButtonEventArgs e) { PDividerClear(); e.Handled = true; }
    private void PDividerCaptureHandle(object sender, MouseEventArgs e) => PDividerClear();

    private void PDividerClear()
    {
        pDividerState = false;
        if (pDividerThumb?.IsMouseCaptured == true) pDividerThumb.ReleaseMouseCapture();
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
