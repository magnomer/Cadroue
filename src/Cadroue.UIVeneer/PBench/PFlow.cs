using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PBench;

public sealed class PFlow : UserControl
{
    private static readonly IReadOnlyDictionary<bool, int> pFlowConfirmChoices = new Dictionary<bool, int>
    {
        [true] = 0,
        [false] = -1,
    };

    private static readonly IReadOnlyDictionary<PSDecisionChoice, int> pFlowSelectChoices =
        new Dictionary<PSDecisionChoice, int>
        {
            [PSDecisionChoice.PSDecisionPrimary] = 0,
            [PSDecisionChoice.PSDecisionAlternate] = 1,
            [PSDecisionChoice.PSDecisionDismiss] = -1,
        };

    private readonly IReadOnlyDictionary<LFlowLosslesscutKind, Action<LFlowLosslesscutPrompt, Action<int>>>
        pFlowLosslesscutShows;

    private readonly PViewfinder pViewfinder;
    private readonly PMap pMap;
    private readonly TextBlock pViewfinderLabelLeft = PReelLabelBuild();
    private readonly TextBlock pViewfinderLabelRight = PReelLabelBuild();
    private readonly TextBlock pMapLabelLeft = PReelLabelBuild();
    private readonly TextBlock pMapLabelRight = PReelLabelBuild();
    private readonly Grid pFlowViewfinderReel;
    private readonly Grid pFlowMapReel;
    private readonly PDivider pDivider;
    private readonly PFlowName pFlowName;
    private readonly DispatcherTimer pKeyframeRequestTimer = new() { Interval = LFlowKeyframe.LFlowRequestDelay };
    private readonly DispatcherTimer pKeyframeResumeTimer = new() { Interval = LFlowKeyframe.LFlowResumeDelay };

    public LFlow LFlow { get; } = new();

    public PFlow()
    {
        Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
        MinHeight = LDivider.LDividerMinimum;
        pFlowLosslesscutShows = new Dictionary<LFlowLosslesscutKind, Action<LFlowLosslesscutPrompt, Action<int>>>
        {
            [LFlowLosslesscutKind.LFlowKindDecision] = PFlowConfirmShow,
            [LFlowLosslesscutKind.LFlowKindChoice] = PFlowSelectShow,
            [LFlowLosslesscutKind.LFlowKindNotice] = PFlowNoticeShow,
            [LFlowLosslesscutKind.LFlowKindWarning] = PFlowWarningShow,
        };
        pViewfinder = new PViewfinder(LFlow);
        pMap = new PMap(LFlow);
        pDivider = new PDivider(this);
        pFlowName = new PFlowName(LFlow, pViewfinder);
        LFlow.LFlowPaletteApply(PSectionPalette.PSectionActiveCount);
        pViewfinder.PViewfinderCursorChange += LFlow.LFlowCursorSeek;
        pViewfinder.PViewfinderSectionSelect += LFlow.LFlowSection.LFlowSectionSelect;
        pViewfinder.PViewfinderDragChange += LFlow.LFlowDragHandle;
        pMap.PMapCursorChange += LFlow.LFlowCursorSeek;
        pMap.PMapSpoolChange += LFlow.LFlowSpoolHandle;
        pMap.PMapDragChange += LFlow.LFlowDragHandle;
        LFlow.LFlowAttachApply += PFlowAttachApply;
        LFlow.LFlowClearApply += PFlowClearApply;
        LFlow.LFlowCursorApply += PFlowCursorApply;
        LFlow.LFlowSpoolApply += PFlowSpoolApply;
        LFlow.LFlowSection.LFlowSectionChange += PFlowSectionApply;
        LFlow.LFlowKeyframe.LFlowTimerDefer += PFlowTimerDefer;
        LFlow.LFlowKeyframe.LFlowTimerResume += PFlowTimerResume;
        LFlow.LFlowKeyframe.LFlowTimerReset += PFlowTimerReset;
        LFlow.LFlowKeyframe.LFlowKeyframeReady += PFlowKeyframeHandle;
        LFlow.LFlowKeyframe.LFlowKeyframeChange += PFlowKeyframeApply;
        LFlow.LFlowWaveform.LFlowWaveformReady += PFlowWaveformHandle;
        LFlow.LFlowWaveform.LFlowWaveformUpdate += PFlowWaveformApply;
        LFlow.LFlowLosslesscut.LFlowLosslesscutAsk += PFlowLosslesscutHandle;
        pKeyframeRequestTimer.Tick += PFlowTimerHandle;
        pKeyframeResumeTimer.Tick += PFlowTimerHandle;

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
        DockPanel.SetDock(pDivider, Dock.Top);
        root.Children.Add(pDivider);
        root.Children.Add(reelGrid);
        Content = root;
        Height = LFrameStore.LFrameStateCurrent.LFrameFlowHeight;
    }

    public void PFlowAttach(LMediaInfo lMediaInfo, string? lSourcePath, TimeSpan lCursorTime) =>
        LFlow.LFlowAttach(lMediaInfo, lSourcePath, lCursorTime);

    public bool PFlowClear() => LFlow.LFlowClear();

    public void PFlowHeightSet(double pFlowHeight) => Height = pFlowHeight;

    public void PFlowClose()
    {
        LFlow.LFlowClose();
        PFlowTimerReset();
        pKeyframeRequestTimer.Tick -= PFlowTimerHandle;
        pKeyframeResumeTimer.Tick -= PFlowTimerHandle;
        pDivider.PDividerDetach();
        pFlowName.PFlowNameDetach();
    }

    public void PFlowOrderApply()
    {
        Grid.SetRow(pFlowMapReel, LFlow.LFlowMapRow);
        Grid.SetRow(pFlowViewfinderReel, LFlow.LFlowViewfinderRow);
    }

    public void PFlowPaletteApply()
    {
        LFlow.LFlowPaletteApply(PSectionPalette.PSectionActiveCount);
        LFlow.LFlowSection.LFlowSectionRaise();
    }

    public static Geometry PFlowWaveformBuild(
        byte[] pFlowWaveformPeaks,
        double pFlowWaveformWidth,
        double pFlowWaveformRailTop,
        double pFlowWaveformRailHeight,
        TimeSpan pFlowWaveformRangeStart,
        TimeSpan pFlowWaveformRangeEnd)
    {
        LFlowWaveformOutline lOutline = LFlowWaveform.LFlowOutlineResolve(
            pFlowWaveformPeaks,
            pFlowWaveformWidth,
            pFlowWaveformRailTop,
            pFlowWaveformRailHeight,
            pFlowWaveformRangeStart,
            pFlowWaveformRangeEnd);
        var pFlowWaveformGeometry = new StreamGeometry();
        using (StreamGeometryContext pFlowWaveformContext = pFlowWaveformGeometry.Open())
        {
            pFlowWaveformContext.BeginFigure(new Point(lOutline.LFlowOutlineX, lOutline.LFlowOutlineY), true, true);
            lOutline.LFlowOutlinePoints.ToList().ForEach(lPoint =>
                pFlowWaveformContext.LineTo(new Point(lPoint.LFlowPointX, lPoint.LFlowPointY), false, false));
        }

        pFlowWaveformGeometry.Freeze();
        return pFlowWaveformGeometry;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        e.Handled = LFlow.LFlowWheelHandle(e.Delta);
    }

    private void PFlowAttachApply()
    {
        pViewfinder.PViewfinderAttach();
        pMap.PMapAttach();
        PFlowSectionApply(LFlow.LFlowSection.LFlowSectionsRead(), LFlow.LFlowSection.LFlowSelectionRead());
        PFlowLabelsApply();
        pViewfinder.PViewfinderKeyframesUpdate(Array.Empty<LKeyframeEntry>(), Array.Empty<LKeyframeScanRange>());
        pMap.PMapKeyframesUpdate(Array.Empty<LKeyframeScanRange>());
    }

    private void PFlowClearApply()
    {
        pViewfinder.PViewfinderClear();
        pMap.PMapClear();
        PFlowLabelsApply();
    }

    private void PFlowCursorApply()
    {
        pViewfinder.PViewfinderCursorUpdate();
        pMap.PMapCursorUpdate();
    }

    private void PFlowSpoolApply()
    {
        pViewfinder.PViewfinderSpoolUpdate();
        pMap.PMapSpoolUpdate();
        PFlowLabelsApply();
    }

    private void PFlowLabelsApply()
    {
        pViewfinderLabelLeft.Text = LFlow.LFlowLabelOrigin;
        pViewfinderLabelRight.Text = LFlow.LFlowLabelLimit;
        pMapLabelLeft.Text = LFlow.LFlowLabelZero;
        pMapLabelRight.Text = LFlow.LFlowLabelDuration;
    }

    private void PFlowSectionApply(IReadOnlyList<LPiece> lSections, int? lActive)
    {
        pViewfinder.PViewfinderSectionsUpdate(lSections);
        pMap.PMapSectionsUpdate(lSections);
    }

    private void PFlowTimerDefer()
    {
        pKeyframeRequestTimer.Stop();
        pKeyframeRequestTimer.Start();
    }

    private void PFlowTimerResume(TimeSpan lDelay)
    {
        pKeyframeResumeTimer.Interval = lDelay;
        pKeyframeResumeTimer.Start();
    }

    private void PFlowTimerReset()
    {
        pKeyframeRequestTimer.Stop();
        pKeyframeResumeTimer.Stop();
    }

    private void PFlowTimerHandle(object? sender, EventArgs e) => LFlow.LFlowKeyframe.LFlowKeyframeTick();

    private void PFlowKeyframeHandle(LKeyframeNotice lNotice) =>
        Dispatcher.InvokeAsync(() => LFlow.LFlowKeyframe.LFlowKeyframeApply(lNotice), DispatcherPriority.Background);

    private void PFlowKeyframeApply(IReadOnlyList<LKeyframeEntry> lEntries, IReadOnlyList<LKeyframeScanRange> lRanges)
    {
        pViewfinder.PViewfinderKeyframesUpdate(lEntries, lRanges);
        pMap.PMapKeyframesUpdate(lRanges);
    }

    private void PFlowWaveformHandle() =>
        Dispatcher.InvokeAsync(LFlow.LFlowWaveform.LFlowWaveformApply, DispatcherPriority.Background);

    private void PFlowWaveformApply(byte[] lPeaks)
    {
        pViewfinder.PViewfinderWaveformUpdate(lPeaks);
        pMap.PMapWaveformUpdate(lPeaks);
    }

    private void PFlowLosslesscutHandle(LFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        pFlowLosslesscutShows[lPrompt.LFlowPromptKind](lPrompt, lAnswer);

    private void PFlowConfirmShow(LFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        lAnswer(pFlowConfirmChoices[PSDecision.PSDecisionConfirm(
            Window.GetWindow(this),
            lPrompt.LFlowPromptTitle,
            lPrompt.LFlowPromptMessage,
            lPrompt.LFlowPromptPrimary,
            lPrompt.LFlowPromptDismiss)]);

    private void PFlowSelectShow(LFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        lAnswer(pFlowSelectChoices[PSDecision.PSDecisionSelect(
            Window.GetWindow(this),
            lPrompt.LFlowPromptTitle,
            lPrompt.LFlowPromptMessage,
            lPrompt.LFlowPromptPrimary,
            lPrompt.LFlowPromptAlternate,
            lPrompt.LFlowPromptDismiss)]);

    private void PFlowNoticeShow(LFlowLosslesscutPrompt lPrompt, Action<int> lAnswer)
    {
        PSAnnouncement.PSAnnouncementShow(Window.GetWindow(this), lPrompt.LFlowPromptTitle, lPrompt.LFlowPromptMessage);
        lAnswer(0);
    }

    private void PFlowWarningShow(LFlowLosslesscutPrompt lPrompt, Action<int> lAnswer)
    {
        PSWarning.PSWarningShow(Window.GetWindow(this), lPrompt.LFlowPromptTitle, lPrompt.LFlowPromptMessage);
        lAnswer(0);
    }

    private static Grid PReelGridBuild(FrameworkElement reelBody, TextBlock labelLeft, TextBlock labelRight)
    {
        const double labelWidth = 64;
        labelLeft.HorizontalAlignment = HorizontalAlignment.Right;
        labelLeft.VerticalAlignment = VerticalAlignment.Center;
        labelLeft.Margin = new Thickness(0, 0, 6, 0);
        labelRight.HorizontalAlignment = HorizontalAlignment.Left;
        labelRight.VerticalAlignment = VerticalAlignment.Center;
        labelRight.Margin = new Thickness(6, 0, 0, 0);
        Grid reelGrid = new();
        reelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
        reelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        reelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
        Grid.SetColumn(labelLeft, 0);
        Grid.SetColumn(reelBody, 1);
        Grid.SetColumn(labelRight, 2);
        reelGrid.Children.Add(labelLeft);
        reelGrid.Children.Add(reelBody);
        reelGrid.Children.Add(labelRight);
        return reelGrid;
    }

    private static TextBlock PReelLabelBuild() => new()
    {
        FontSize = 12,
        Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A))
    };
}
