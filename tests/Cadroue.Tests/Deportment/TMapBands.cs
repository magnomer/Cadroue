using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TMapBands
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
    public void Bands_FollowSections_WithBadge_Selection_AndHidden()
    {
        LFlow flow = TFlowBuild();
        LMap map = TInterface.TMapCreate(flow);

        IReadOnlyList<LMapBand> bands = TInterface.TMapFrameResolve(map, 600, 40).LMapFrameBands;
        Assert.Equal(2, bands.Count);
        Assert.Equal(100, bands[0].LMapBandLeft);
        Assert.Equal(300, bands[0].LMapBandRight);
        Assert.Equal(300, bands[1].LMapBandLeft);
        Assert.Equal(600, bands[1].LMapBandRight);
        Assert.Equal("1", bands[0].LMapBandBadge);
        Assert.Equal("2", bands[1].LMapBandBadge);
        Assert.Equal(4, bands[0].LMapBandTop);
        Assert.True(bands[0].LMapBandSelected);
        Assert.False(bands[1].LMapBandSelected);
        Assert.True(bands[0].LMapBandShown);
        Assert.Equal(2, map.LMapGlyphCount);

        TInterface.TFlowSectionToggle(flow, 1);
        bands = TInterface.TMapFrameResolve(map, 600, 40).LMapFrameBands;
        Assert.False(bands[1].LMapBandShown);
        Assert.Equal("sections", map.LMapTrigger);
    }

    [Fact]
    public void Badge_CentresInTheBand_AndSkipsWhenItDoesNotFit()
    {
        LFlow flow = TFlowBuild();
        LMap map = TInterface.TMapCreate(flow);
        LMapBand band = TInterface.TMapFrameResolve(map, 600, 40).LMapFrameBands[0];

        LMapBadge badge = Assert.Single(TInterface.TMapBadgeResolve(map, band, 8, 14));
        Assert.Equal(190, badge.LMapBadgeLeft);
        Assert.Equal(210, badge.LMapBadgeRight);
        Assert.Equal(8, badge.LMapBadgeRadius);
        Assert.Equal(196, badge.LMapBadgeX);
        Assert.Equal(badge.LMapBadgeTop + 1, badge.LMapBadgeY);

        Assert.Empty(TInterface.TMapBadgeResolve(map, band, 500, 14));
        Assert.Empty(TInterface.TMapBadgeResolve(map, band, 8, 40));
    }
}
