using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

public sealed class LKeyframeOrchestratorTests
{
    private static TimeSpan At(double seconds) => TimeSpan.FromSeconds(seconds);

    [Fact]
    public void PreviousMove_UnscannedGapBeforeCursor_RemainsPending()
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
    public void PreviousMove_ContiguousCoverageToCandidate_ReturnsCandidate()
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
    public void NextMove_UnscannedGapAfterCursor_RemainsPending()
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
    public void PreviousMove_FullyScannedWithoutCandidate_IsReadyWithoutTarget()
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
