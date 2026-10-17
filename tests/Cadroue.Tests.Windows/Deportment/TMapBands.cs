using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TMapBands
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
    public void Bands_FollowSections_WithBadge_Selection_AndHidden()
    {
        QFlow flow = TFlowBuild();
        QMap map = TInterface.TMapRead(flow);

        IReadOnlyList<QMapBand> bands = TInterface.TMapFrameResolve(map, 600, 40).QMapFrameBands;
        Assert.Equal(2, bands.Count);
        Assert.Equal(100, bands[0].QMapBandLeft);
        Assert.Equal(300, bands[0].QMapBandRight);
        Assert.Equal(300, bands[1].QMapBandLeft);
        Assert.Equal(600, bands[1].QMapBandRight);
        Assert.Equal("1", bands[0].QMapBandBadge);
        Assert.Equal("2", bands[1].QMapBandBadge);
        Assert.Equal(4, bands[0].QMapBandTop);
        Assert.True(bands[0].QMapBandSelected);
        Assert.False(bands[1].QMapBandSelected);
        Assert.True(bands[0].QMapBandShown);
        Assert.Equal(2, TInterface.TMapGlyphRead(map));

        TInterface.TFlowSectionToggle(flow, 1);
        bands = TInterface.TMapFrameResolve(map, 600, 40).QMapFrameBands;
        Assert.False(bands[1].QMapBandShown);
        Assert.Equal("sections", TInterface.TMapTriggerRead(map));
    }

    [Fact]
    public void Badge_CentresInTheBand_AndSkipsWhenItDoesNotFit()
    {
        QFlow flow = TFlowBuild();
        QMap map = TInterface.TMapRead(flow);
        QMapBand band = TInterface.TMapFrameResolve(map, 600, 40).QMapFrameBands[0];

        QMapBadge badge = Assert.Single(TInterface.TMapBadgeResolve(map, band, 8, 14));
        Assert.Equal(190, badge.QMapBadgeLeft);
        Assert.Equal(210, badge.QMapBadgeRight);
        Assert.Equal(8, badge.QMapBadgeRadius);
        Assert.Equal(196, badge.QMapBadgeX);
        Assert.Equal(badge.QMapBadgeTop + 1, badge.QMapBadgeY);

        Assert.Empty(TInterface.TMapBadgeResolve(map, band, 500, 14));
        Assert.Empty(TInterface.TMapBadgeResolve(map, band, 8, 40));
    }
}
