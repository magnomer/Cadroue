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

    private readonly IReadOnlyDictionary<QFlowLosslesscutKind, Action<QFlowLosslesscutPrompt, Action<int>>>
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
    private readonly DispatcherTimer pKeyframeRequestTimer = new() { Interval = QFlowKeyframe.QFlowRequestDelay };
    private readonly DispatcherTimer pKeyframeResumeTimer = new() { Interval = QFlowKeyframe.QFlowResumeDelay };

    public QFlow QFlow { get; }

    public PFlow(QFlow lFlow)
    {
        QFlow = lFlow;
        Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
        MinHeight = QDivider.QDividerMinimum;
        pFlowLosslesscutShows = new Dictionary<QFlowLosslesscutKind, Action<QFlowLosslesscutPrompt, Action<int>>>
        {
            [QFlowLosslesscutKind.QFlowKindDecision] = PFlowConfirmShow,
            [QFlowLosslesscutKind.QFlowKindChoice] = PFlowSelectShow,
            [QFlowLosslesscutKind.QFlowKindNotice] = PFlowNoticeShow,
            [QFlowLosslesscutKind.QFlowKindWarning] = PFlowWarningShow,
        };
        pViewfinder = new PViewfinder(QFlow.QFlowViewfinder);
        pMap = new PMap(QFlow.QFlowMap);
        pDivider = new PDivider(this, QFlow.QFlowDivider);
        pFlowName = new PFlowName(QFlow, pViewfinder);
        QFlow.QFlowPaletteApply(QSection.QSectionCountRead());
        QFlow.QFlowAttachApplied += PFlowLabelsApply;
        QFlow.QFlowClearApplied += PFlowLabelsApply;
        QFlow.QFlowSpool.QFlowSpoolApplied += PFlowLabelsApply;
        QFlow.QFlowKeyframe.QFlowTimerDeferred += PFlowTimerDefer;
        QFlow.QFlowKeyframe.QFlowTimerResumed += PFlowTimerResume;
        QFlow.QFlowKeyframe.QFlowTimerStopped += PFlowTimerReset;
        QFlow.QFlowKeyframe.QFlowKeyframeReady += PFlowKeyframeHandle;
        QFlow.QFlowWaveform.QFlowWaveformReady += PFlowWaveformHandle;
        QFlow.QFlowLosslesscut.QFlowLosslesscutAsk += PFlowLosslesscutHandle;
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
        QFlow.QFlowAttach(lMediaInfo, lSourcePath, lCursorTime);

    public bool PFlowClear() => QFlow.QFlowClear();

    public void PFlowHeightSet(double pFlowHeight) => Height = pFlowHeight;

    public void PFlowClose()
    {
        QFlow.QFlowClose();
        PFlowTimerReset();
        pKeyframeRequestTimer.Tick -= PFlowTimerHandle;
        pKeyframeResumeTimer.Tick -= PFlowTimerHandle;
        pDivider.PDividerDetach();
        pFlowName.PFlowNameDetach();
    }

    public void PFlowOrderApply()
    {
        Grid.SetRow(pFlowMapReel, QFlow.QFlowMapRow);
        Grid.SetRow(pFlowViewfinderReel, QFlow.QFlowViewfinderRow);
    }

    public void PFlowPaletteApply()
    {
        QFlow.QFlowPaletteApply(QSection.QSectionCountRead());
        QFlow.QFlowSection.QFlowSectionRaise();
    }

    public static Geometry PFlowWaveformBuild(QFlowWaveformOutline lOutline)
    {
        var pFlowWaveformGeometry = new StreamGeometry();
        using (StreamGeometryContext pFlowWaveformContext = pFlowWaveformGeometry.Open())
        {
            pFlowWaveformContext.BeginFigure(new Point(lOutline.QFlowOutlineX, lOutline.QFlowOutlineY), true, true);
            lOutline.QFlowOutlinePoints.ToList().ForEach(lPoint =>
                pFlowWaveformContext.LineTo(new Point(lPoint.QFlowPointX, lPoint.QFlowPointY), false, false));
        }

        pFlowWaveformGeometry.Freeze();
        return pFlowWaveformGeometry;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        e.Handled = QFlow.QFlowWheel.QFlowWheelHandle(e.Delta);
    }

    private void PFlowLabelsApply()
    {
        pViewfinderLabelLeft.Text = QFlow.QFlowSpool.QFlowLabelOrigin;
        pViewfinderLabelRight.Text = QFlow.QFlowSpool.QFlowLabelLimit;
        pMapLabelLeft.Text = QFlow.QFlowSpool.QFlowLabelZero;
        pMapLabelRight.Text = QFlow.QFlowSpool.QFlowLabelDuration;
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

    private void PFlowTimerHandle(object? sender, EventArgs e) => QFlow.QFlowKeyframe.QFlowKeyframeTick();

    private void PFlowKeyframeHandle(LKeyframeNotice lNotice) =>
        Dispatcher.InvokeAsync(() => QFlow.QFlowKeyframe.QFlowKeyframeApply(lNotice), DispatcherPriority.Background);

    private void PFlowWaveformHandle() =>
        Dispatcher.InvokeAsync(QFlow.QFlowWaveform.QFlowWaveformApply, DispatcherPriority.Background);

    private void PFlowLosslesscutHandle(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        pFlowLosslesscutShows[lPrompt.QFlowPromptKind](lPrompt, lAnswer);

    private void PFlowConfirmShow(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        lAnswer(pFlowConfirmChoices[PSDecision.PSDecisionConfirm(
            Window.GetWindow(this),
            lPrompt.QFlowPromptTitle,
            lPrompt.QFlowPromptMessage,
            lPrompt.QFlowPromptPrimary,
            lPrompt.QFlowPromptDismiss)]);

    private void PFlowSelectShow(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer) =>
        lAnswer(pFlowSelectChoices[PSDecision.PSDecisionSelect(
            Window.GetWindow(this),
            lPrompt.QFlowPromptTitle,
            lPrompt.QFlowPromptMessage,
            lPrompt.QFlowPromptPrimary,
            lPrompt.QFlowPromptAlternate,
            lPrompt.QFlowPromptDismiss)]);

    private void PFlowNoticeShow(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer)
    {
        PSAnnouncement.PSAnnouncementShow(Window.GetWindow(this), lPrompt.QFlowPromptTitle, lPrompt.QFlowPromptMessage);
        lAnswer(0);
    }

    private void PFlowWarningShow(QFlowLosslesscutPrompt lPrompt, Action<int> lAnswer)
    {
        PSWarning.PSWarningShow(Window.GetWindow(this), lPrompt.QFlowPromptTitle, lPrompt.QFlowPromptMessage);
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
