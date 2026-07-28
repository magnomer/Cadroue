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
    private static readonly TimeSpan PFlowKeyframeResumeDelay = TimeSpan.FromSeconds(2);
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
    private readonly StackPanel pFlowSectionButtonGroup = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
    private Border? pDividerHandle;
    private LSpool? lSpool;
    private TimeSpan lCursor;
    private string? lSourcePath;

    private bool pFlowSidecarRestoring;
    private int? lSectionIndexSelect;
    private double pDividerStartY;
    private double pDividerStartHeight;
    private bool pDividerState;
    private bool pFlowSectionUiActive;
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
        pMap.PMapCursorChange += PFlowMapSeek;
        pMap.PMapSpoolChange += PFlowSpoolHandle;
        lKeyframeOrchestrator.LKeyframeNoticeReady += PFlowNoticeHandle;
        lKeyframeOrchestrator.LKeyframeSectionsSource = PFlowSidecarSectionsRead;
        lKeyframeRequestTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        lKeyframeRequestTimer.Tick += PFlowTimerHandle;
        lKeyframeResumeTimer = new DispatcherTimer { Interval = PFlowKeyframeResumeDelay };
        lKeyframeResumeTimer.Tick += PFlowResumeHandle;
        pDividerHandle = PDividerBuild();
        pDividerHandle.MouseLeftButtonDown += PDividerPressHandle;
        pDividerHandle.MouseMove += PDividerMoveHandle;
        pDividerHandle.MouseLeftButtonUp += PDividerReleaseHandle;
        pDividerHandle.LostMouseCapture += PDividerCaptureHandle;

        Grid viewfinderReel = PReelGridBuild(pViewfinder, pViewfinderLabelLeft, pViewfinderLabelRight);
        Grid mapReel = PReelGridBuild(pMap, pMapLabelLeft, pMapLabelRight);
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
        DockPanel.SetDock(pDividerHandle, Dock.Top);
        root.Children.Add(pDividerHandle);
        root.Children.Add(reelGrid);
        Content = root;
        Height = App.LPreferenceStateCurrent.LPreferenceFlowHeight;
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
        lSectionIndexSelect = null;
        pViewfinder.PViewfinderAttach(lSpool, lCursor);
        pMap.PMapAttach(lSpool, lCursor);
        pViewfinder.PViewfinderSectionsUpdate(lSectionList, lSectionIndexSelect);
        pMap.PMapSectionsUpdate(lSectionList, lSectionIndexSelect);
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolWorkingRangeStart);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolWorkingRangeEnd);
        pMapLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolDuration);
        pViewfinder.PViewfinderKeyframesUpdate(Array.Empty<LKeyframeEntry>(), Array.Empty<LKeyframeScanRange>());
        pMap.PMapKeyframesUpdate(Array.Empty<LKeyframeScanRange>());
        PFlowSidecarRestore();
        PFlowSectionChange?.Invoke(lSectionList.AsReadOnly(), lSectionIndexSelect);
        PFlowKeyframeRun();
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
                PFlowSidecarSectionsApply(pFlowSidecar.Sections);
            }
        }
        catch (Exception pFlowException)
        {
            LAppLog.LError("Sidecar sections could not be restored", pFlowException);
        }
    }

    public void PFlowCursorUpdate(TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        PFlowCursorPropagate(cursorTime, false, false);
        PFlowKeyframeSchedule();
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
        lSectionIndexSelect = null;
        pViewfinder.PViewfinderClear();
        pMap.PMapClear();
        pViewfinderLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pViewfinderLabelRight.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelRight.Text = PFlowTimeFormat(TimeSpan.Zero);
        PFlowSectionChange?.Invoke(lSectionList.AsReadOnly(), lSectionIndexSelect);
    }

    public void PFlowVolumeSet(double volume)
    {
        if (!pFlowCommandActive) return;
        PFlowVolumeValue?.Invoke(LPreferenceState.LPreferenceVolumeClamp(volume));
    }

    public void PFlowCommandSet(bool pCommandActive)
    {
        pFlowCommandActive = pCommandActive;
        if (pFlowCommandActive)
        {
            PFlowKeyframeSchedule();
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
        pFlowSectionUiActive = sectionUiActive;
        pFlowSectionButtonGroup.Visibility = sectionUiActive ? Visibility.Visible : Visibility.Collapsed;
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
        if (pDividerHandle is null) return;
        pDividerHandle.MouseLeftButtonDown -= PDividerPressHandle;
        pDividerHandle.MouseMove -= PDividerMoveHandle;
        pDividerHandle.MouseLeftButtonUp -= PDividerReleaseHandle;
        pDividerHandle.LostMouseCapture -= PDividerCaptureHandle;
    }

    public bool PFlowShortcutDispatch(string pFlowShortcutCode)
    {
        if (!pFlowCommandActive || lSpool is null) return false;
        switch (pFlowShortcutCode)
        {
            case "zoomIn": lSpool.LSpoolInZoom(lCursor); PFlowSpoolUpdate(); return true;
            case "zoomOut": lSpool.LSpoolOutZoom(lCursor); PFlowSpoolUpdate(); return true;
            case "addSection" when pFlowSectionUiActive: PFlowSectionAdd(); return true;
            case "setStart" when pFlowSectionUiActive: PFlowStartSet(); return true;
            case "splitSection" when pFlowSectionUiActive: PFlowSectionSplit(); return true;
            case "setEnd" when pFlowSectionUiActive: PFlowEndSet(); return true;
            case "deleteSection" when pFlowSectionUiActive: PFlowSectionDelete(); return true;
            case "nameSection" when pFlowSectionUiActive: return PFlowNameShow();
            case "previousKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframePreviousMove(lCursor)); return true;
            case "nearestKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframeNearestMove(lCursor)); return true;
            case "nextKey": PFlowKeyframeMove(lKeyframeOrchestrator.LKeyframeNextMove(lCursor)); return true;
            default: return false;
        }
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
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolWorkingRangeStart);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolWorkingRangeEnd);
        PFlowKeyframeSchedule();
    }

    private void PFlowKeyframeSchedule()
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
            lKeyframeOrchestrator.LKeyframeStart(lSourcePath, lSpool.LSpoolDuration, lCursor);
    }

    private void PFlowTimerHandle(object? sender, EventArgs e) => PFlowKeyframeRun();

    private void PFlowResumeHandle(object? sender, EventArgs e) => PFlowKeyframeRun();

    private void PFlowNoticeHandle(LKeyframeNotice notice)
    {
        if (!pFlowCommandActive || pFlowUnloaded || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
        Dispatcher.InvokeAsync(() =>
        {
            if (!pFlowUnloaded && notice.LRequestSerial == lKeyframeOrchestrator.LKeyframeCurrentSerial)
            {
                PFlowKeyframeRecord(notice);
                pViewfinder.PViewfinderKeyframesUpdate(notice.LKeyframes, notice.LScannedRanges);
                pMap.PMapKeyframesUpdate(notice.LScannedRanges);
            }
        }, DispatcherPriority.Background);
    }

    private void PFlowKeyframeRecord(LKeyframeNotice notice)
    {
        double pFlowScanned = notice.LScannedRanges.Sum(
            pRange => (pRange.LKeyframeScanRangeEndTime - pRange.LKeyframeScanRangeStartTime).TotalSeconds);
        string pFlowStamp = $"{notice.LKeyframes.Count}/{notice.LScannedRanges.Count}/{pFlowScanned:0.###}";
        if (string.Equals(pFlowStamp, pFlowKeyframeStamp, StringComparison.Ordinal))
        {
            return;
        }

        pFlowKeyframeStamp = pFlowStamp;
        string pFlowSource = string.IsNullOrWhiteSpace(lSourcePath)
            ? "(no media)"
            : System.IO.Path.GetFileName(lSourcePath);
        LAppLog.LInfo(
            $"Keyframe scan '{pFlowSource}': {notice.LKeyframes.Count} keyframe(s) known, " +
            $"{TimeSpan.FromSeconds(pFlowScanned):hh\\:mm\\:ss} scanned across {notice.LScannedRanges.Count} range(s)");
    }

    private void PFlowKeyframeMove(TimeSpan? keyframeTargetTime)
    {
        if (lSpool is null || string.IsNullOrWhiteSpace(lSourcePath) || keyframeTargetTime is null)
        {
            PFlowKeyframeSchedule();
            return;
        }
        PFlowCursorPropagate(keyframeTargetTime.Value, true, true);
    }

    private void PDividerPressHandle(object sender, MouseButtonEventArgs e)
    {
        Window? ownerWindow = Window.GetWindow(this);
        if (pDividerHandle is null || ownerWindow is null) return;
        pDividerState = true;
        pDividerStartY = e.GetPosition(ownerWindow).Y;
        pDividerStartHeight = ActualHeight;
        pDividerHandle.CaptureMouse();
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
        if (pDividerHandle?.IsMouseCaptured == true) pDividerHandle.ReleaseMouseCapture();
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
