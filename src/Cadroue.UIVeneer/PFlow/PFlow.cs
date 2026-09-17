using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Media;
using Cadroue.UIVeneer;

using Cadroue.Core;

using Cadroue.Application;

using Cadroue.Infrastructure;


namespace Cadroue.UIVeneer.PFlow;

public sealed partial class PFlow : UserControl
{
    private readonly PViewfinder pViewfinder = new();
    private readonly PMap pMap = new();
    private readonly TextBlock pViewfinderLabelLeft = PReelLabelBuild();
    private readonly TextBlock pViewfinderLabelRight = PReelLabelBuild();
    private readonly TextBlock pMapLabelLeft = PReelLabelBuild();
    private readonly TextBlock pMapLabelRight = PReelLabelBuild();
    private readonly LSegment lSegment = new();
    private readonly StackPanel pFlowSectionButtons = new()
    {
        Orientation = Orientation.Horizontal,
        VerticalAlignment = VerticalAlignment.Center
    };
    private readonly Grid pFlowViewfinderReel;
    private readonly Grid pFlowMapReel;
    private LSpool? lSpool;
    private string? lSourcePath;
    private LMediaInfo? lMediaInfo;

    private bool pFlowSectionActive;
    private bool pFlowCommandActive;
    private bool pFlowUnloaded;

    public event Action<IReadOnlyList<LPiece>, int?>? PFlowSectionChange;

    public event Action? PFlowMediaChange;

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
        lSegment.LSegmentNotice += PFlowSegmentHandle;
        lSegment.LSegmentFaultNotice += PFlowFaultHandle;
        lKeyframeOrchestrator.LKeyframeNoticeReady += PFlowNoticeHandle;
        lWaveformOrchestrator.LWaveformReady += PFlowWaveformHandle;
        lKeyframeRequestTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        lKeyframeRequestTimer.Tick += PFlowTimerHandle;
        lKeyframeResumeTimer = new DispatcherTimer { Interval = PFlowResumeRead() };
        lKeyframeResumeTimer.Tick += PFlowResumeHandle;
        PDividerAttach();

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
        Height = LFrameStore.LFrameStateCurrent.LFrameFlowHeight;
    }

    public void PFlowAttach(LMediaInfo mediaInfo, string? sourcePath, TimeSpan cursorTime)
    {
        if (!pFlowCommandActive) return;
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        string? pFlowNextSource = string.IsNullOrWhiteSpace(sourcePath) ? null : sourcePath;
        bool pFlowSameSource = pFlowNextSource is not null
            && string.Equals(lSourcePath, pFlowNextSource, StringComparison.OrdinalIgnoreCase);
        TimeSpan pFlowResumeAt = pFlowSameSource ? lCursor : cursorTime;
        lSourcePath = pFlowNextSource;
        lMediaInfo = mediaInfo;
        lSpool = new LSpool(mediaInfo.LMediaInfoDuration);
        pFlowWaveformAudio = mediaInfo.LMediaAudioPresent;
        pFlowKeyframeDirection = null;
        lCursor = PFlowCursorClamp(pFlowResumeAt);
        lSegment.LSegmentSourceSet(lSourcePath);
        lSegment.LSegmentReset();
        pViewfinder.PViewfinderAttach(lSpool, lCursor, lSourcePath);
        pMap.PMapAttach(lSpool, lCursor, lSourcePath);
        pViewfinder.PViewfinderSectionsUpdate(lSegment.LSegmentListRead(), lSegment.LSegmentSelectionRead());
        pMap.PMapSectionsUpdate(lSegment.LSegmentListRead(), lSegment.LSegmentSelectionRead());
        pViewfinderLabelLeft.Text = PFlowTimeFormat(lSpool.LSpoolRangeOrigin);
        pViewfinderLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolRangeLimit);
        pMapLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelRight.Text = PFlowTimeFormat(lSpool.LSpoolDuration);
        pViewfinder.PViewfinderKeyframesUpdate(Array.Empty<LKeyframeEntry>(), Array.Empty<LKeyframeScanRange>());
        pMap.PMapKeyframesUpdate(Array.Empty<LKeyframeScanRange>());
        lSegment.LSegmentLoad(lSpool.LSpoolDuration);
        PFlowSectionChange?.Invoke(lSegment.LSegmentListRead(), lSegment.LSegmentSelectionRead());
        PFlowMediaChange?.Invoke();
        PFlowKeyframeRun();
        PFlowWaveformStart();
        if (pFlowSameSource && lCursor > TimeSpan.Zero)
        {
            PFlowCursorChange?.Invoke(lCursor);
        }
    }

    public bool PFlowClear()
    {
        if (lSourcePath is null && lSpool is null
            && lSegment.LSegmentListRead().Count == 0 && lSegment.LSegmentSelectionRead() is null)
        {
            return false;
        }

        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        lKeyframeOrchestrator.LKeyframeSuspend();
        pFlowKeyframeDirection = null;
        lSourcePath = null;
        lMediaInfo = null;
        lSpool = null;
        lCursor = TimeSpan.Zero;
        lSegment.LSegmentSourceSet(null);
        lSegment.LSegmentReset();
        pViewfinder.PViewfinderClear();
        pMap.PMapClear();
        PFlowWaveformClear();
        pViewfinderLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pViewfinderLabelRight.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelLeft.Text = PFlowTimeFormat(TimeSpan.Zero);
        pMapLabelRight.Text = PFlowTimeFormat(TimeSpan.Zero);
        PFlowSectionChange?.Invoke(lSegment.LSegmentListRead(), lSegment.LSegmentSelectionRead());
        PFlowMediaChange?.Invoke();
        return true;
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
            pFlowKeyframeDirection = null;
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
        PDividerDetach();
    }

    public bool PFlowShortcutDispatch(string pFlowShortcutCode)
    {
        if (!pFlowCommandActive || lSpool is null) return false;
        switch (pFlowShortcutCode)
        {
            case "zoomIn": lSpool.LSpoolZoom(lCursor, 1); PFlowSpoolUpdate(); return true;
            case "zoomOut": lSpool.LSpoolZoom(lCursor, -1); PFlowSpoolUpdate(); return true;
            case "addSection" when pFlowSectionActive && pFlowSectionEditable: PFlowSectionAdd(); return true;
            case "setStart" when pFlowSectionActive && pFlowSectionEditable: PFlowStartSet(); return true;
            case "splitSection" when pFlowSectionActive && pFlowSectionEditable: PFlowSectionDivide(); return true;
            case "setEnd" when pFlowSectionActive && pFlowSectionEditable: PFlowEndSet(); return true;
            case "deleteSection" when pFlowSectionActive && pFlowSectionEditable: PFlowSectionDelete(); return true;
            case "nameSection" when pFlowSectionActive && pFlowSectionEditable: return PFlowNameShow();
            case "previousKey": PFlowKeyframeMove(-1); return true;
            case "nearestKey": PFlowKeyframeMove(0); return true;
            case "nextKey": PFlowKeyframeMove(1); return true;
            default: return false;
        }
    }
}
