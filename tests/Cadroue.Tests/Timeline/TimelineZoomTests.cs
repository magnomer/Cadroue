using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TimelineZoomTests
{
    [Fact]
    public void NavigationStep_UsesOneFortiethOfTimelineRange()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(10.0, spool.LSpoolStepResolve(1).TotalSeconds, 6);
    }

    [Fact]
    public void NavigationStep_ShortTimelineUsesMinimumDistance()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(1));
        Assert.Equal(0.04, spool.LSpoolStepResolve(1).TotalSeconds, 6);
    }

    [Fact]
    public void NavigationStep_NegativeCountMovesBackward()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(-10.0, spool.LSpoolStepResolve(-1).TotalSeconds, 6);
    }

    [Fact]
    public void NavigationStep_MagnitudeScalesWithCount()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(
            spool.LSpoolStepResolve(1).TotalSeconds * 2,
            spool.LSpoolStepResolve(2).TotalSeconds,
            6);
    }

    [Fact]
    public void ZoomIn_HalvesTimelineRange()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        spool.LSpoolZoom(TimeSpan.FromSeconds(200), 1);
        Assert.Equal(200.0, (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds, 6);
    }

    [Fact]
    public void ZoomOut_DoublesTimelineRange()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        spool.LSpoolZoom(TimeSpan.FromSeconds(200), 1);
        spool.LSpoolZoom(TimeSpan.FromSeconds(200), -1);
        Assert.Equal(400.0, (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds, 6);
    }

    [Fact]
    public void Zoom_MultipleStepsMatchRepeatedZooms()
    {
        var cursor = TimeSpan.FromSeconds(200);
        var multi = new LSpool(TimeSpan.FromSeconds(400));
        multi.LSpoolZoom(cursor, 3);

        var single = new LSpool(TimeSpan.FromSeconds(400));
        single.LSpoolZoom(cursor, 1);
        single.LSpoolZoom(cursor, 1);
        single.LSpoolZoom(cursor, 1);

        Assert.Equal(single.LSpoolRangeOrigin, multi.LSpoolRangeOrigin);
        Assert.Equal(single.LSpoolRangeLimit, multi.LSpoolRangeLimit);
    }

    [Fact]
    public void Zoom_ZeroStepsKeepsTimelineRange()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        var origin = spool.LSpoolRangeOrigin;
        var limit = spool.LSpoolRangeLimit;
        spool.LSpoolZoom(TimeSpan.FromSeconds(200), 0);
        Assert.Equal(origin, spool.LSpoolRangeOrigin);
        Assert.Equal(limit, spool.LSpoolRangeLimit);
    }

    [Fact]
    public void Zoom_KeepsCursorRelativePosition()
    {
        var cursor = TimeSpan.FromSeconds(100);
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        spool.LSpoolZoom(cursor, 2);

        var span = (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds;
        double ratio = (cursor - spool.LSpoolRangeOrigin).TotalSeconds / span;
        Assert.Equal(0.25, ratio, 6);
    }
}
