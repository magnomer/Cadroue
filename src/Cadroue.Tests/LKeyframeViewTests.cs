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
}
