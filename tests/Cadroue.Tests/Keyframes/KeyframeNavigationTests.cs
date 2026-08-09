using Cadroue.Infrastructure;

using Xunit;

using static Cadroue.Tests.KeyframeData;

namespace Cadroue.Tests;

public sealed class KeyframeNavigationTests
{
    [Fact]
    public void PreviousKeyframe_WithUnscannedGap_RemainsPending()
    {
        var result = LKeyframeOrchestrator.LKeyframeMoveResolve(
            new long[] { 90_000 },
            new HashSet<int> { 3, 4 },
            At(600),
            At(180),
            -1);

        Assert.False(result.LKeyframeReady);
        Assert.Null(result.LKeyframeTarget);
    }

    [Fact]
    public void PreviousKeyframe_WithContiguousCoverage_ReturnsCandidate()
    {
        var result = LKeyframeOrchestrator.LKeyframeMoveResolve(
            new long[] { 90_000 },
            new HashSet<int> { 4, 5, 6, 7, 8 },
            At(600),
            At(180),
            -1);

        Assert.True(result.LKeyframeReady);
        Assert.Equal(At(90), result.LKeyframeTarget);
    }

    [Fact]
    public void NextKeyframe_WithUnscannedGap_RemainsPending()
    {
        var result = LKeyframeOrchestrator.LKeyframeMoveResolve(
            new long[] { 270_000 },
            new HashSet<int> { 13 },
            At(600),
            At(180),
            1);

        Assert.False(result.LKeyframeReady);
    }

    [Fact]
    public void PreviousKeyframe_WhenFullyScannedWithoutCandidate_IsReadyWithoutTarget()
    {
        var result = LKeyframeOrchestrator.LKeyframeMoveResolve(
            Array.Empty<long>(),
            new HashSet<int> { 0, 1, 2 },
            At(60),
            At(60),
            -1);

        Assert.True(result.LKeyframeReady);
        Assert.Null(result.LKeyframeTarget);
    }
}
