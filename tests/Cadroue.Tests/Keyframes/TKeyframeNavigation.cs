using Cadroue.Infrastructure;

using Xunit;

using static Cadroue.Tests.TKeyframeData;

namespace Cadroue.Tests;

public sealed class TKeyframeNavigation
{
    [Fact]
    public void PreviousKeyframe_WithUnscannedGap_RemainsPending()
    {
        var result = TInterface.TKeyframeMoveResolve(
            new long[] { 90_000 },
            new HashSet<int> { 3, 4 },
            TKeyframeAtCreate(600),
            TKeyframeAtCreate(180),
            -1);

        Assert.False(result.LKeyframeReady);
        Assert.Null(result.LKeyframeTarget);
    }

    [Fact]
    public void PreviousKeyframe_WithContiguousCoverage_ReturnsCandidate()
    {
        var result = TInterface.TKeyframeMoveResolve(
            new long[] { 90_000 },
            new HashSet<int> { 4, 5, 6, 7, 8 },
            TKeyframeAtCreate(600),
            TKeyframeAtCreate(180),
            -1);

        Assert.True(result.LKeyframeReady);
        Assert.Equal(TKeyframeAtCreate(90), result.LKeyframeTarget);
    }

    [Fact]
    public void NextKeyframe_WithUnscannedGap_RemainsPending()
    {
        var result = TInterface.TKeyframeMoveResolve(
            new long[] { 270_000 },
            new HashSet<int> { 13 },
            TKeyframeAtCreate(600),
            TKeyframeAtCreate(180),
            1);

        Assert.False(result.LKeyframeReady);
    }

    [Fact]
    public void PreviousKeyframe_WhenFullyScannedWithoutCandidate_IsReadyWithoutTarget()
    {
        var result = TInterface.TKeyframeMoveResolve(
            Array.Empty<long>(),
            new HashSet<int> { 0, 1, 2 },
            TKeyframeAtCreate(60),
            TKeyframeAtCreate(60),
            -1);

        Assert.True(result.LKeyframeReady);
        Assert.Null(result.LKeyframeTarget);
    }
}
