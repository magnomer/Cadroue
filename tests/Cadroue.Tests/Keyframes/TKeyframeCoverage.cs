using Cadroue.Core;

using Xunit;

using static Cadroue.Tests.TKeyframeData;

namespace Cadroue.Tests;

public sealed class TKeyframeCoverage
{
    [Fact]
    public void CoverageRange_FullyInsideBounds_IsKeptUnchanged()
    {
        var spool = TKeyframeSpoolCreate(100, 200);
        var ranges = new[] { TKeyframeScanCreate(120, 180) };

        var result = TInterface.TKeyframeCoverageResolve(ranges, spool, false);

        Assert.Single(result);
        Assert.Equal(TKeyframeAtCreate(120), result[0].LKeyframeRangeOrigin);
        Assert.Equal(TKeyframeAtCreate(180), result[0].LKeyframeRangeLimit);
    }

    [Fact]
    public void CoverageRanges_StraddlingBounds_AreClipped()
    {
        var spool = TKeyframeSpoolCreate(100, 200);
        var ranges = new[] { TKeyframeScanCreate(50, 150), TKeyframeScanCreate(180, 250) };

        var result = TInterface.TKeyframeCoverageResolve(ranges, spool, false);

        Assert.Equal(2, result.Count);
        Assert.Equal(TKeyframeAtCreate(100), result[0].LKeyframeRangeOrigin);
        Assert.Equal(TKeyframeAtCreate(150), result[0].LKeyframeRangeLimit);
        Assert.Equal(TKeyframeAtCreate(180), result[1].LKeyframeRangeOrigin);
        Assert.Equal(TKeyframeAtCreate(200), result[1].LKeyframeRangeLimit);
    }

    [Fact]
    public void CoverageRanges_FullyOutsideBounds_AreDropped()
    {
        var spool = TKeyframeSpoolCreate(100, 200);
        var ranges = new[] { TKeyframeScanCreate(10, 50), TKeyframeScanCreate(300, 400) };

        var result = TInterface.TKeyframeCoverageResolve(ranges, spool, false);

        Assert.Empty(result);
    }

    [Fact]
    public void CoverageRanges_WithInvertedBounds_AreEmpty()
    {
        LSpool spool = TInterface.TSpoolCreate(TimeSpan.Zero);
        var ranges = new[] { TKeyframeScanCreate(120, 180) };

        var result = TInterface.TKeyframeCoverageResolve(ranges, spool, true);

        Assert.Empty(result);
    }

    [Fact]
    public void CoverageRanges_PreserveInputOrder()
    {
        var spool = TKeyframeSpoolCreate(0, 500);
        var ranges = new[] { TKeyframeScanCreate(300, 350), TKeyframeScanCreate(100, 150), TKeyframeScanCreate(200, 250) };

        var result = TInterface.TKeyframeCoverageResolve(ranges, spool, true);

        Assert.Equal(
            new[] { TKeyframeAtCreate(300), TKeyframeAtCreate(100), TKeyframeAtCreate(200) },
            result.Select(r => r.LKeyframeRangeOrigin).ToArray());
    }

    [Fact]
    public void CoverageRanges_ForWholeMedia_UseFullDuration()
    {
        var spool = TKeyframeSpoolCreate(100, 200);
        TInterface.TSpoolStartSet(spool, TKeyframeAtCreate(120));
        TInterface.TSpoolEndSet(spool, TKeyframeAtCreate(180));
        var ranges = new[] { TKeyframeScanCreate(0, 300) };

        var whole = TInterface.TKeyframeCoverageResolve(ranges, spool, true);
        var working = TInterface.TKeyframeCoverageResolve(ranges, spool, false);

        Assert.Equal(TKeyframeAtCreate(0), whole[0].LKeyframeRangeOrigin);
        Assert.Equal(spool.LSpoolDuration, whole[0].LKeyframeRangeLimit);
        Assert.Equal(spool.LSpoolRangeOrigin, working[0].LKeyframeRangeOrigin);
        Assert.Equal(spool.LSpoolRangeLimit, working[0].LKeyframeRangeLimit);
    }
}
