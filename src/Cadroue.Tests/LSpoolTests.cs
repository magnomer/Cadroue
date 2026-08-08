using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LSpoolTests
{
    [Fact]
    public void StepResolve_FullRange_SpanOverForty()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(10.0, spool.LSpoolStepResolve(1).TotalSeconds, 6);
    }

    [Fact]
    public void StepResolve_SmallRange_FloorsAtFloor()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(1));
        Assert.Equal(0.04, spool.LSpoolStepResolve(1).TotalSeconds, 6);
    }

    [Fact]
    public void StepResolve_NegativeSteps_NegativeDelta()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(-10.0, spool.LSpoolStepResolve(-1).TotalSeconds, 6);
    }

    [Fact]
    public void StepResolve_MagnitudeScalesLinearly()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        Assert.Equal(
            spool.LSpoolStepResolve(1).TotalSeconds * 2,
            spool.LSpoolStepResolve(2).TotalSeconds,
            6);
    }

    [Fact]
    public void Zoom_PositiveStep_HalvesRange()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        spool.LSpoolZoom(TimeSpan.FromSeconds(200), 1);
        Assert.Equal(200.0, (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds, 6);
    }

    [Fact]
    public void Zoom_NegativeStep_DoublesRange()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        spool.LSpoolZoom(TimeSpan.FromSeconds(200), 1);
        spool.LSpoolZoom(TimeSpan.FromSeconds(200), -1);
        Assert.Equal(400.0, (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds, 6);
    }

    [Fact]
    public void Zoom_ThreeSteps_MatchesThreeSingleZooms()
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
    public void Zoom_ZeroSteps_LeavesRangeUnchanged()
    {
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        var origin = spool.LSpoolRangeOrigin;
        var limit = spool.LSpoolRangeLimit;
        spool.LSpoolZoom(TimeSpan.FromSeconds(200), 0);
        Assert.Equal(origin, spool.LSpoolRangeOrigin);
        Assert.Equal(limit, spool.LSpoolRangeLimit);
    }

    [Fact]
    public void Zoom_CursorAnchoredRatio_PreservedAcrossMultiStep()
    {
        var cursor = TimeSpan.FromSeconds(100);
        var spool = new LSpool(TimeSpan.FromSeconds(400));
        spool.LSpoolZoom(cursor, 2);

        var span = (spool.LSpoolRangeLimit - spool.LSpoolRangeOrigin).TotalSeconds;
        double ratio = (cursor - spool.LSpoolRangeOrigin).TotalSeconds / span;
        Assert.Equal(0.25, ratio, 6);
    }
}
