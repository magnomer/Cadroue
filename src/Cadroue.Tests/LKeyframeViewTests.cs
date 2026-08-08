using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LKeyframeViewTests
{
    private static TimeSpan At(double seconds) => TimeSpan.FromSeconds(seconds);

    private static LKeyframeEntry Entry(double seconds) => new(At(seconds));

    private static LSpool Spool(double origin, double limit)
    {
        var spool = new LSpool(At(limit));
        spool.LSpoolStartSet(At(origin));
        spool.LSpoolEndSet(At(limit));
        return spool;
    }

    [Fact]
    public void VisibleResolve_WindowClampsToSpoolWorkingRange()
    {
        var spool = Spool(100, 200);
        var keyframes = new[] { Entry(90), Entry(150), Entry(210) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(150), spool);

        Assert.Single(result);
        Assert.Equal(At(150), result[0].LKeyframePresentationTime);
    }

    [Fact]
    public void VisibleResolve_CursorWindowOutsideRange_ReturnsEmpty()
    {
        var spool = Spool(100, 200);
        var keyframes = new[] { Entry(150) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(2000), spool);

        Assert.Empty(result);
    }

    [Fact]
    public void VisibleResolve_EntriesOutsideWindowExcluded()
    {
        var spool = Spool(0, 3600);
        var keyframes = new[] { Entry(100), Entry(1000), Entry(2000) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(1000), spool);

        Assert.Single(result);
        Assert.Equal(At(1000), result[0].LKeyframePresentationTime);
    }

    [Fact]
    public void VisibleResolve_BoundaryEntriesIncluded()
    {
        var spool = Spool(0, 3600);
        var keyframes = new[] { Entry(600), Entry(1600) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(1000), spool);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void VisibleResolve_OrderPreserved()
    {
        var spool = Spool(0, 3600);
        var keyframes = new[] { Entry(1050), Entry(1000), Entry(1100) };

        var result = LKeyframeView.LKeyframeVisibleResolve(keyframes, At(1050), spool);

        Assert.Equal(
            new[] { At(1050), At(1000), At(1100) },
            result.Select(e => e.LKeyframePresentationTime).ToArray());
    }

    private static LKeyframeScanRange Scan(double start, double end) => new(At(start), At(end));

    [Fact]
    public void CoverageResolve_RangeFullyInsideKeptUnchanged()
    {
        var spool = Spool(100, 200);
        var ranges = new[] { Scan(120, 180) };

        var result = LKeyframeView.LKeyframeCoverageResolve(ranges, spool, false);

        Assert.Single(result);
        Assert.Equal(At(120), result[0].LKeyframeRangeOrigin);
        Assert.Equal(At(180), result[0].LKeyframeRangeLimit);
    }

    [Fact]
    public void CoverageResolve_StraddlingBoundClipped()
    {
        var spool = Spool(100, 200);
        var ranges = new[] { Scan(50, 150), Scan(180, 250) };

        var result = LKeyframeView.LKeyframeCoverageResolve(ranges, spool, false);

        Assert.Equal(2, result.Count);
        Assert.Equal(At(100), result[0].LKeyframeRangeOrigin);
        Assert.Equal(At(150), result[0].LKeyframeRangeLimit);
        Assert.Equal(At(180), result[1].LKeyframeRangeOrigin);
        Assert.Equal(At(200), result[1].LKeyframeRangeLimit);
    }

    [Fact]
    public void CoverageResolve_RangeFullyOutsideDropped()
    {
        var spool = Spool(100, 200);
        var ranges = new[] { Scan(10, 50), Scan(300, 400) };

        var result = LKeyframeView.LKeyframeCoverageResolve(ranges, spool, false);

        Assert.Empty(result);
    }

    [Fact]
    public void CoverageResolve_InvertedBoundsReturnsEmpty()
    {
        var spool = new LSpool(TimeSpan.Zero);
        var ranges = new[] { Scan(120, 180) };

        var result = LKeyframeView.LKeyframeCoverageResolve(ranges, spool, true);

        Assert.Empty(result);
    }

    [Fact]
    public void CoverageResolve_OrderPreserved()
    {
        var spool = Spool(0, 500);
        var ranges = new[] { Scan(300, 350), Scan(100, 150), Scan(200, 250) };

        var result = LKeyframeView.LKeyframeCoverageResolve(ranges, spool, true);

        Assert.Equal(
            new[] { At(300), At(100), At(200) },
            result.Select(r => r.LKeyframeRangeOrigin).ToArray());
    }

    [Fact]
    public void CoverageResolve_WholeMediaUsesFullDuration()
    {
        var spool = Spool(100, 200);
        spool.LSpoolStartSet(At(120));
        spool.LSpoolEndSet(At(180));
        var ranges = new[] { Scan(0, 300) };

        var whole = LKeyframeView.LKeyframeCoverageResolve(ranges, spool, true);
        var working = LKeyframeView.LKeyframeCoverageResolve(ranges, spool, false);

        Assert.Equal(At(0), whole[0].LKeyframeRangeOrigin);
        Assert.Equal(spool.LSpoolDuration, whole[0].LKeyframeRangeLimit);
        Assert.Equal(spool.LSpoolRangeOrigin, working[0].LKeyframeRangeOrigin);
        Assert.Equal(spool.LSpoolRangeLimit, working[0].LKeyframeRangeLimit);
    }
}
