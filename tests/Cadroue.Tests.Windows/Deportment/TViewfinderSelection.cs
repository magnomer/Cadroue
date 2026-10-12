using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TViewfinderSelection
{
    private static QFlow TFlowBuild()
    {
        QFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSectionSet(flow, true);
        TInterface.TFlowSourceSet(
            flow, TInterfaceViewer.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), "C:\\media\\clip.mp4");
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(10));
        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));
        TInterface.TFlowSectionDivide(flow);
        return flow;
    }

    [Fact]
    public void Press_SelectsTheSectionUnderTheCursor_AndDragsThePlayhead()
    {
        QFlow flow = TFlowBuild();
        QViewfinder viewfinder = TInterface.TViewfinderRead(flow);
        List<bool> drags = [];
        TInterface.TFlowPlayAttach(flow, () => { }, () => { }, drags.Add, _ => { });

        Assert.True(TInterface.TViewfinderPressHandle(viewfinder, 450, 600));
        Assert.Equal(1, flow.QFlowEdit.QFlowSectionIndex);
        Assert.Equal(TimeSpan.FromSeconds(45), flow.QFlowCursor);
        Assert.True(viewfinder.QViewfinderDragActive);

        Assert.True(TInterface.TViewfinderMoveHandle(viewfinder, 150, 600));
        Assert.Equal(TimeSpan.FromSeconds(15), flow.QFlowCursor);
        Assert.Equal(1, flow.QFlowEdit.QFlowSectionIndex);

        TInterface.TViewfinderDragClear(viewfinder);
        Assert.False(viewfinder.QViewfinderDragActive);
        Assert.False(TInterface.TViewfinderMoveHandle(viewfinder, 300, 600));
        Assert.Equal([true, false], drags);

        TInterface.TViewfinderPressHandle(viewfinder, 150, 600);
        Assert.Equal(0, flow.QFlowEdit.QFlowSectionIndex);
        QViewfinder bare = TInterface.TViewfinderRead(TInterface.TFlowCreate());
        Assert.False(TInterface.TViewfinderPressHandle(bare, 10, 600));
    }

    [Fact]
    public void Bands_ClampToTheRange_AndFeedThePopupOffset()
    {
        QFlow flow = TFlowBuild();
        QViewfinder viewfinder = TInterface.TViewfinderRead(flow);

        IReadOnlyList<QViewfinderBand> bands =
            TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).QViewfinderFrameBands;
        Assert.Equal(2, bands.Count);
        Assert.Equal(100, bands[0].QViewfinderBandLeft);
        Assert.Equal(300, bands[0].QViewfinderBandRight);
        Assert.Equal(23, bands[0].QViewfinderBandTop);
        Assert.Equal(92, bands[0].QViewfinderBandBottom);
        Assert.True(bands[0].QViewfinderBandSelected);

        QViewfinderPoint offset = TInterface.TViewfinderPopupResolve(viewfinder, 0, 600, 100);
        Assert.Equal(-100, offset.QViewfinderPointX);
        Assert.Equal(7.5, offset.QViewfinderPointY);
        QViewfinderPoint missing = TInterface.TViewfinderPopupResolve(viewfinder, 5, 600, 100);
        Assert.Equal(0, missing.QViewfinderPointX);
        Assert.Equal(0, missing.QViewfinderPointY);

        TInterface.TFlowRangeSet(flow, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(50));
        bands = TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).QViewfinderFrameBands;
        Assert.Equal(0, bands[0].QViewfinderBandLeft);
        Assert.Equal(200, bands[0].QViewfinderBandRight);
        Assert.Equal(600, bands[1].QViewfinderBandRight);
    }

    [Fact]
    public void Label_FitsBadgeThenName_AndPlacesBoth()
    {
        QFlow flow = TFlowBuild();
        QViewfinder viewfinder = TInterface.TViewfinderRead(flow);
        QViewfinderBand band = TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).QViewfinderFrameBands[0];

        QViewfinderLabel label = Assert.Single(TInterface.TViewfinderLabelResolve(band, 8, 14));
        Assert.Equal(string.Empty, label.QViewfinderLabelName);
        Assert.Equal(164, label.QViewfinderLabelRoom);
        Assert.Empty(TInterface.TViewfinderLabelResolve(band, 200, 14));

        QViewfinderBadge badge = TInterface.TViewfinderBadgeResolve(band, label, 8, 14, 0, 0);
        Assert.Equal(190, badge.QViewfinderBadgeLeft);
        Assert.Equal(210, badge.QViewfinderBadgeRight);
        Assert.Equal(49.5, badge.QViewfinderBadgeTop);
        Assert.Equal(196, badge.QViewfinderBadgeText.QViewfinderPointX);
        Assert.Equal(50.5, badge.QViewfinderBadgeText.QViewfinderPointY);

        TInterface.TFlowNameSet(flow, 0, "Intro", null, null);
        band = TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).QViewfinderFrameBands[0];
        label = Assert.Single(TInterface.TViewfinderLabelResolve(band, 8, 14));
        Assert.Equal("Intro", label.QViewfinderLabelName);
        badge = TInterface.TViewfinderBadgeResolve(band, label, 8, 14, 30, 16);
        Assert.Equal(172, badge.QViewfinderBadgeLeft);
        Assert.Equal(198, badge.QViewfinderBadgeName.QViewfinderPointX);
        Assert.Equal(49.5, badge.QViewfinderBadgeName.QViewfinderPointY);
    }
}
