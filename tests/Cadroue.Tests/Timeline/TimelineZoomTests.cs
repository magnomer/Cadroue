using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TimelineZoomTests
{
    [Fact]
    public void NavigationStep_UsesOneFortiethOfTimelineRange()
    {
        LSpool spool = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        Assert.Equal(10.0, TInterface.SpoolStepResolve(spool, 1).TotalSeconds, 6);
    }

    [Fact]
    public void NavigationStep_ShortTimelineUsesMinimumDistance()
    {
        LSpool spool = TInterface.SpoolCreate(TimeSpan.FromSeconds(1));
        Assert.Equal(0.04, TInterface.SpoolStepResolve(spool, 1).TotalSeconds, 6);
    }

    [Fact]
    public void NavigationStep_NegativeCountMovesBackward()
    {
        LSpool spool = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        Assert.Equal(-10.0, TInterface.SpoolStepResolve(spool, -1).TotalSeconds, 6);
    }

    [Fact]
    public void NavigationStep_MagnitudeScalesWithCount()
    {
        LSpool spool = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        Assert.Equal(
            TInterface.SpoolStepResolve(spool, 1).TotalSeconds * 2,
            TInterface.SpoolStepResolve(spool, 2).TotalSeconds,
            6);
    }

    [Fact]
    public void ZoomIn_HalvesTimelineRange()
    {
        LSpool spool = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        TInterface.SpoolZoom(spool, TimeSpan.FromSeconds(200), 1);
        Assert.Equal(200.0, (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds, 6);
    }

    [Fact]
    public void ZoomOut_DoublesTimelineRange()
    {
        LSpool spool = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        TInterface.SpoolZoom(spool, TimeSpan.FromSeconds(200), 1);
        TInterface.SpoolZoom(spool, TimeSpan.FromSeconds(200), -1);
        Assert.Equal(400.0, (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds, 6);
    }

    [Fact]
    public void Zoom_MultipleStepsMatchRepeatedZooms()
    {
        var cursor = TimeSpan.FromSeconds(200);
        LSpool multi = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        TInterface.SpoolZoom(multi, cursor, 3);

        LSpool single = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        TInterface.SpoolZoom(single, cursor, 1);
        TInterface.SpoolZoom(single, cursor, 1);
        TInterface.SpoolZoom(single, cursor, 1);

        Assert.Equal(single.LSpoolRangeOrigin, multi.LSpoolRangeOrigin);
        Assert.Equal(single.LSpoolRangeLimit, multi.LSpoolRangeLimit);
    }

    [Fact]
    public void Zoom_ZeroStepsKeepsTimelineRange()
    {
        LSpool spool = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        var origin = spool.LSpoolRangeOrigin;
        var limit = spool.LSpoolRangeLimit;
        TInterface.SpoolZoom(spool, TimeSpan.FromSeconds(200), 0);
        Assert.Equal(origin, spool.LSpoolRangeOrigin);
        Assert.Equal(limit, spool.LSpoolRangeLimit);
    }

    [Fact]
    public void Zoom_KeepsCursorRelativePosition()
    {
        var cursor = TimeSpan.FromSeconds(100);
        LSpool spool = TInterface.SpoolCreate(TimeSpan.FromSeconds(400));
        TInterface.SpoolZoom(spool, cursor, 2);

        var span = (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds;
        double ratio = (cursor - spool.LSpoolRangeOrigin).TotalSeconds / span;
        Assert.Equal(0.25, ratio, 6);
    }
}
