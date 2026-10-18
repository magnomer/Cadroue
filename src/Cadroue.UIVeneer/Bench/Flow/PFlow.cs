using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PFlow : UserControl
{
    private readonly PViewfinder pViewfinder;
    private readonly PMap pMap;
    private readonly PReel pFlowViewfinderReel;
    private readonly PReel pFlowMapReel;
    private readonly PDivider pDivider;
    private readonly PFlowName pFlowName;
    private readonly PFlowKeyframe pFlowKeyframe;

    public QFlow QFlow { get; }

    public PFlow(QFlow lFlow)
    {
        QFlow = lFlow;
        PFlowLookApply();
        pViewfinder = new PViewfinder(QFlow.QFlowViewfinder);
        pMap = new PMap(QFlow.QFlowMap);
        pDivider = new PDivider(this, QFlow.QFlowDivider);
        pFlowName = new PFlowName(QFlow, pViewfinder);
        pFlowViewfinderReel = new PReel(pViewfinder);
        pFlowMapReel = new PReel(pMap);
        QFlow.QFlowPaletteApply(QSection.QSectionCountRead());
        PFlowAttach();
        pFlowKeyframe = new PFlowKeyframe(QFlow.QFlowKeyframe, Dispatcher);
        QFlow.QFlowWaveform.QFlowWaveformReady += PFlowWaveformHandle;
        _ = new PFlowLosslesscut(QFlow.QFlowLosslesscut, this);
        Grid reelGrid = PFlowReelBuild();
        PFlowOrderApply();
        PFlowRootBuild(reelGrid);
        PFlowHeightSet(QFlow.QFlowHeight);
    }

    private void PFlowLookApply()
    {
        Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
        MinHeight = QDivider.QDividerMinimum;
    }

    private void PFlowAttach()
    {
        QFlow.QFlowAttachApplied += PFlowLabelsApply;
        QFlow.QFlowClearApplied += PFlowLabelsApply;
        QFlow.QFlowSpool.QFlowSpoolApplied += PFlowLabelsApply;
    }

    private Grid PFlowReelBuild()
    {
        Grid reelGrid = new() { Margin = new Thickness(8, 0, 8, 8) };
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(5, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(37, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(49, GridUnitType.Star) });
        reelGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8, GridUnitType.Star) });
        reelGrid.Children.Add(pFlowMapReel.PReelGrid);
        reelGrid.Children.Add(pFlowViewfinderReel.PReelGrid);
        return reelGrid;
    }

    private void PFlowRootBuild(Grid reelGrid)
    {
        DockPanel root = new() { LastChildFill = true };
        DockPanel.SetDock(pDivider, Dock.Top);
        root.Children.Add(pDivider);
        root.Children.Add(reelGrid);
        Content = root;
    }

    public void PFlowHeightSet(double pFlowHeight) => Height = pFlowHeight;

    public void PFlowLayoutApply(double pFlowHeight)
    {
        PFlowHeightSet(pFlowHeight);
        PFlowOrderApply();
    }

    public void PFlowSelectedApply(double pFlowHeight)
    {
        PFlowLayoutApply(pFlowHeight);
        PFlowPaletteApply();
    }

    public void PFlowClose()
    {
        QFlow.QFlowClose();
        pFlowKeyframe.PFlowKeyframeClose();
        pDivider.PDividerDetach();
        pFlowName.PFlowNameDetach();
    }

    public void PFlowOrderApply()
    {
        Grid.SetRow(pFlowMapReel.PReelGrid, QFlow.QFlowMapRow);
        Grid.SetRow(pFlowViewfinderReel.PReelGrid, QFlow.QFlowViewfinderRow);
    }

    public void PFlowPaletteApply()
    {
        QFlow.QFlowPaletteApply(QSection.QSectionCountRead());
        QFlow.QFlowSection.QFlowSectionRaise();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        e.Handled = QFlow.QFlowWheel.QFlowWheelHandle(e.Delta);
    }

    private void PFlowLabelsApply()
    {
        pFlowViewfinderReel.PReelLabelsSet(QFlow.QFlowSpool.QFlowLabelOrigin, QFlow.QFlowSpool.QFlowLabelLimit);
        pFlowMapReel.PReelLabelsSet(QFlow.QFlowSpool.QFlowLabelZero, QFlow.QFlowSpool.QFlowLabelDuration);
    }

    private void PFlowWaveformHandle() =>
        Dispatcher.InvokeAsync(QFlow.QFlowWaveform.QFlowWaveformApply, DispatcherPriority.Background);
}
