using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TViewfinderSelect
{
    private static LFlow TFlowBuild()
    {
        LFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSectionSet(flow, true);
        TInterface.TFlowSourceSet(
            flow, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), "C:\\media\\clip.mp4");
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(10));
        TInterface.TFlowSectionAdd(flow);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));
        TInterface.TFlowSectionDivide(flow);
        return flow;
    }

    [Fact]
    public void Press_SelectsTheSectionUnderTheCursor_AndDragsThePlayhead()
    {
        LFlow flow = TFlowBuild();
        LViewfinder viewfinder = TInterface.TViewfinderCreate(flow);
        List<bool> drags = [];
        TInterface.TFlowPlayAttach(flow, () => { }, () => { }, drags.Add, _ => { });

        Assert.True(TInterface.TViewfinderPressHandle(viewfinder, 450, 600));
        Assert.Equal(1, flow.LFlowSectionIndex);
        Assert.Equal(TimeSpan.FromSeconds(45), flow.LFlowCursor);
        Assert.True(viewfinder.LViewfinderDragActive);

        Assert.True(TInterface.TViewfinderMoveHandle(viewfinder, 150, 600));
        Assert.Equal(TimeSpan.FromSeconds(15), flow.LFlowCursor);
        Assert.Equal(1, flow.LFlowSectionIndex);

        TInterface.TViewfinderDragClear(viewfinder);
        Assert.False(viewfinder.LViewfinderDragActive);
        Assert.False(TInterface.TViewfinderMoveHandle(viewfinder, 300, 600));
        Assert.Equal([true, false], drags);

        TInterface.TViewfinderPressHandle(viewfinder, 150, 600);
        Assert.Equal(0, flow.LFlowSectionIndex);
        LViewfinder bare = TInterface.TViewfinderCreate(TInterface.TFlowCreate());
        Assert.False(TInterface.TViewfinderPressHandle(bare, 10, 600));
    }

    [Fact]
    public void Bands_ClampToTheRange_AndFeedThePopupOffset()
    {
        LFlow flow = TFlowBuild();
        LViewfinder viewfinder = TInterface.TViewfinderCreate(flow);

        IReadOnlyList<LViewfinderBand> bands =
            TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).LViewfinderFrameBands;
        Assert.Equal(2, bands.Count);
        Assert.Equal(100, bands[0].LViewfinderBandLeft);
        Assert.Equal(300, bands[0].LViewfinderBandRight);
        Assert.Equal(23, bands[0].LViewfinderBandTop);
        Assert.Equal(92, bands[0].LViewfinderBandBottom);
        Assert.True(bands[0].LViewfinderBandSelected);

        LViewfinderPoint offset = TInterface.TViewfinderPopupResolve(viewfinder, 0, 600, 100);
        Assert.Equal(-100, offset.LViewfinderPointX);
        Assert.Equal(7.5, offset.LViewfinderPointY);
        LViewfinderPoint missing = TInterface.TViewfinderPopupResolve(viewfinder, 5, 600, 100);
        Assert.Equal(0, missing.LViewfinderPointX);
        Assert.Equal(0, missing.LViewfinderPointY);

        TInterface.TFlowRangeSet(flow, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(50));
        bands = TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).LViewfinderFrameBands;
        Assert.Equal(0, bands[0].LViewfinderBandLeft);
        Assert.Equal(200, bands[0].LViewfinderBandRight);
        Assert.Equal(600, bands[1].LViewfinderBandRight);
    }

    [Fact]
    public void Label_FitsBadgeThenName_AndPlacesBoth()
    {
        LFlow flow = TFlowBuild();
        LViewfinder viewfinder = TInterface.TViewfinderCreate(flow);
        LViewfinderBand band = TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).LViewfinderFrameBands[0];

        LViewfinderLabel label = Assert.Single(TInterface.TViewfinderLabelResolve(band, 8, 14));
        Assert.Equal(string.Empty, label.LViewfinderLabelName);
        Assert.Equal(164, label.LViewfinderLabelRoom);
        Assert.Empty(TInterface.TViewfinderLabelResolve(band, 200, 14));

        LViewfinderBadge badge = TInterface.TViewfinderBadgeResolve(band, label, 8, 14, 0, 0);
        Assert.Equal(190, badge.LViewfinderBadgeLeft);
        Assert.Equal(210, badge.LViewfinderBadgeRight);
        Assert.Equal(49.5, badge.LViewfinderBadgeTop);
        Assert.Equal(196, badge.LViewfinderBadgeText.LViewfinderPointX);
        Assert.Equal(50.5, badge.LViewfinderBadgeText.LViewfinderPointY);

        TInterface.TFlowNameSet(flow, 0, "Intro", null, null);
        band = TInterface.TViewfinderFrameResolve(viewfinder, 600, 100).LViewfinderFrameBands[0];
        label = Assert.Single(TInterface.TViewfinderLabelResolve(band, 8, 14));
        Assert.Equal("Intro", label.LViewfinderLabelName);
        badge = TInterface.TViewfinderBadgeResolve(band, label, 8, 14, 30, 16);
        Assert.Equal(172, badge.LViewfinderBadgeLeft);
        Assert.Equal(198, badge.LViewfinderBadgeName.LViewfinderPointX);
        Assert.Equal(49.5, badge.LViewfinderBadgeName.LViewfinderPointY);
    }
}
