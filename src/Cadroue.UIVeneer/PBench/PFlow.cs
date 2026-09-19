using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Media;
using Cadroue.UIVeneer;

using Cadroue.Core;

using Cadroue.Application;

using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PBench;

public sealed partial class PFlow : UserControl
{
    private readonly PViewfinder pViewfinder;
    private readonly PMap pMap;
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

    public LFlow LFlow { get; } = new();

    public event Action<IReadOnlyList<LPiece>, int?>? PFlowSectionChange;

    public event Action? PFlowMediaChange;

    public PFlow()
    {
        Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
        MinHeight = PFlowHeightMinimum;
        pViewfinder = new PViewfinder(LFlow);
        pMap = new PMap(LFlow);
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
        if (!LFlow.LFlowCommandActive) return;
        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        bool pFlowSameSource = LFlow.LFlowSourceMatch(sourcePath);
        TimeSpan pFlowResumeAt = pFlowSameSource ? LFlow.LFlowCursor : cursorTime;
        LFlow.LFlowSourceSet(mediaInfo, sourcePath);
        LSpool lSpool = LFlow.LFlowSpool!;
        LFlow.LFlowCursorSet(pFlowResumeAt);
        lSegment.LSegmentSourceSet(LFlow.LFlowSourcePath);
        lSegment.LSegmentReset();
        pViewfinder.PViewfinderAttach();
        pMap.PMapAttach();
        LFlow.LFlowSectionSelect(lSegment.LSegmentSelectionRead());
        pViewfinder.PViewfinderSectionsUpdate(lSegment.LSegmentListRead());
        pMap.PMapSectionsUpdate(lSegment.LSegmentListRead());
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
        if (pFlowSameSource && LFlow.LFlowCursor > TimeSpan.Zero)
        {
            PFlowCursorChange?.Invoke(LFlow.LFlowCursor);
        }
    }

    public bool PFlowClear()
    {
        if (LFlow.LFlowSourcePath is null && LFlow.LFlowSpool is null
            && lSegment.LSegmentListRead().Count == 0 && lSegment.LSegmentSelectionRead() is null)
        {
            return false;
        }

        lKeyframeRequestTimer.Stop();
        lKeyframeResumeTimer.Stop();
        lKeyframeOrchestrator.LKeyframeSuspend();
        LFlow.LFlowSourceClear();
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
        LFlow.LFlowCommandSet(pCommandActive);
        if (pCommandActive)
        {
            PFlowKeyframeDefer();
        }
        else
        {
            lKeyframeRequestTimer.Stop();
            lKeyframeResumeTimer.Stop();
            lKeyframeOrchestrator.LKeyframeSuspend();
            LFlow.LFlowDirectionSet(null);
        }
    }

    public void PFlowSectionShow(bool sectionUiActive)
    {
        LFlow.LFlowSectionSet(sectionUiActive);
        pFlowSectionButtons.Visibility = sectionUiActive ? Visibility.Visible : Visibility.Collapsed;
    }

    public void PFlowClose()
    {
        if (LFlow.LFlowUnloaded) return;
        LFlow.LFlowUnloadSet();
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
        if (!LFlow.LFlowCommandActive || LFlow.LFlowSpool is not { } lSpool) return false;
        bool pFlowEditable = LFlow.LFlowEditCheck();
        switch (pFlowShortcutCode)
        {
            case "zoomIn": lSpool.LSpoolZoom(LFlow.LFlowCursor, 1); PFlowSpoolUpdate(); return true;
            case "zoomOut": lSpool.LSpoolZoom(LFlow.LFlowCursor, -1); PFlowSpoolUpdate(); return true;
            case "addSection" when pFlowEditable: PFlowSectionAdd(); return true;
            case "setStart" when pFlowEditable: PFlowStartSet(); return true;
            case "splitSection" when pFlowEditable: PFlowSectionDivide(); return true;
            case "setEnd" when pFlowEditable: PFlowEndSet(); return true;
            case "deleteSection" when pFlowEditable: PFlowSectionDelete(); return true;
            case "nameSection" when pFlowEditable: return PFlowNameShow();
            case "previousKey": PFlowKeyframeMove(-1); return true;
            case "nearestKey": PFlowKeyframeMove(0); return true;
            case "nextKey": PFlowKeyframeMove(1); return true;
            default: return false;
        }
    }
}
