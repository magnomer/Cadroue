using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.KeyframeData;

namespace Cadroue.Tests;

public sealed class KeyframeCoverageTests
{
    [Fact]
    public void CoverageRange_FullyInsideBounds_IsKeptUnchanged()
    {
        var spool = Spool(100, 200);
        var ranges = new[] { Scan(120, 180) };

        var result = TInterface.KeyframeCoverageResolve(ranges, spool, false);

        Assert.Single(result);
        Assert.Equal(At(120), result[0].LKeyframeRangeOrigin);
        Assert.Equal(At(180), result[0].LKeyframeRangeLimit);
    }

    [Fact]
    public void CoverageRanges_StraddlingBounds_AreClipped()
    {
        var spool = Spool(100, 200);
        var ranges = new[] { Scan(50, 150), Scan(180, 250) };

        var result = TInterface.KeyframeCoverageResolve(ranges, spool, false);

        Assert.Equal(2, result.Count);
        Assert.Equal(At(100), result[0].LKeyframeRangeOrigin);
        Assert.Equal(At(150), result[0].LKeyframeRangeLimit);
        Assert.Equal(At(180), result[1].LKeyframeRangeOrigin);
        Assert.Equal(At(200), result[1].LKeyframeRangeLimit);
    }

    [Fact]
    public void CoverageRanges_FullyOutsideBounds_AreDropped()
    {
        var spool = Spool(100, 200);
        var ranges = new[] { Scan(10, 50), Scan(300, 400) };

        var result = TInterface.KeyframeCoverageResolve(ranges, spool, false);

        Assert.Empty(result);
    }

    [Fact]
    public void CoverageRanges_WithInvertedBounds_AreEmpty()
    {
        LSpool spool = TInterface.SpoolCreate(TimeSpan.Zero);
        var ranges = new[] { Scan(120, 180) };

        var result = TInterface.KeyframeCoverageResolve(ranges, spool, true);

        Assert.Empty(result);
    }

    [Fact]
    public void CoverageRanges_PreserveInputOrder()
    {
        var spool = Spool(0, 500);
        var ranges = new[] { Scan(300, 350), Scan(100, 150), Scan(200, 250) };

        var result = TInterface.KeyframeCoverageResolve(ranges, spool, true);

        Assert.Equal(
            new[] { At(300), At(100), At(200) },
            result.Select(r => r.LKeyframeRangeOrigin).ToArray());
    }

    [Fact]
    public void CoverageRanges_ForWholeMedia_UseFullDuration()
    {
        var spool = Spool(100, 200);
        TInterface.SpoolStartSet(spool, At(120));
        TInterface.SpoolEndSet(spool, At(180));
        var ranges = new[] { Scan(0, 300) };

        var whole = TInterface.KeyframeCoverageResolve(ranges, spool, true);
        var working = TInterface.KeyframeCoverageResolve(ranges, spool, false);

        Assert.Equal(At(0), whole[0].LKeyframeRangeOrigin);
        Assert.Equal(spool.LSpoolDuration, whole[0].LKeyframeRangeLimit);
        Assert.Equal(spool.LSpoolRangeOrigin, working[0].LKeyframeRangeOrigin);
        Assert.Equal(spool.LSpoolRangeLimit, working[0].LKeyframeRangeLimit);
    }
}
