using Cadroue.Application;
using Xunit;

namespace Cadroue.Tests;

public sealed class TBridgeRegion
{
    private static TimeSpan TBridgeSecondCreate(double seconds) => TimeSpan.FromSeconds(seconds);

    private static readonly IReadOnlyList<TimeSpan> TBridgeGrid =
        new[] { TBridgeSecondCreate(0), TBridgeSecondCreate(2), TBridgeSecondCreate(4), TBridgeSecondCreate(6), TBridgeSecondCreate(8), TBridgeSecondCreate(10) };

    [Fact]
    public void OriginOnKeyframe_EndOnKeyframe_CopyOnlyNoBridges()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(2), TBridgeSecondCreate(8));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeTail);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(2), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(8), plan.LBridgeMiddle.LBridgeSpanEnd);
    }

    [Fact]
    public void RoundedOriginOnPacketKeyframe_UsesExactPacketBoundary()
    {
        var keyframes = new[]
        {
            TKeyframeData.TKeyframeEntryCreate(2.043708, 1.960292),
            TKeyframeData.TKeyframeEntryCreate(18.393375, 18.309958)
        };

        LBridgePlan plan = TInterface.TBridgeResolve(keyframes, TBridgeSecondCreate(2.044), TBridgeSecondCreate(18.393));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeTail);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(2.043708), plan.LBridgeMiddle!.LBridgeSpanOrigin);
    }

    [Fact]
    public void BoundaryBetweenKeyframes_AllThreeRegions()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(3), TBridgeSecondCreate(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);

        Assert.NotNull(plan.LBridgeHead);
        Assert.Equal(TBridgeSecondCreate(3), plan.LBridgeHead!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(4), plan.LBridgeHead.LBridgeSpanEnd);

        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(4), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(6), plan.LBridgeMiddle.LBridgeSpanEnd);

        Assert.NotNull(plan.LBridgeTail);
        Assert.Equal(TBridgeSecondCreate(6), plan.LBridgeTail!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(7), plan.LBridgeTail.LBridgeSpanEnd);
    }

    [Fact]
    public void NoInteriorKeyframe_ReportsWholeEncode()
    {
        IReadOnlyList<TimeSpan> sparse = new[] { TBridgeSecondCreate(0), TBridgeSecondCreate(10) };

        LBridgePlan plan = TInterface.TBridgeResolve(sparse, TBridgeSecondCreate(3), TBridgeSecondCreate(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeWhole, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeMiddle);
        Assert.Null(plan.LBridgeTail);
        Assert.Equal(TBridgeSecondCreate(3), plan.LBridgeInterval.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(7), plan.LBridgeInterval.LBridgeSpanEnd);
    }

    [Fact]
    public void SingleInteriorKeyframe_NoCopyableSpan_ReportsWholeEncode()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(2), TBridgeSecondCreate(3));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeWhole, plan.LBridgeOutcome);
    }

    [Fact]
    public void OriginBetweenKeyframes_EndOnKeyframe_HeadAndCopyNoTail()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(3), TBridgeSecondCreate(6));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeHead);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Null(plan.LBridgeTail);
        Assert.Equal(TBridgeSecondCreate(6), plan.LBridgeMiddle!.LBridgeSpanEnd);
    }

    [Fact]
    public void PacketKeyframes_CarryDecodeCutoffIntoCopiedMiddle()
    {
        var keyframes = new[]
        {
            TKeyframeData.TKeyframeEntryCreate(2, 1.9),
            TKeyframeData.TKeyframeEntryCreate(4, 3.9),
            TKeyframeData.TKeyframeEntryCreate(6, 5.9)
        };

        LBridgePlan plan = TInterface.TBridgeResolve(keyframes, TBridgeSecondCreate(3), TBridgeSecondCreate(7));

        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(5.9), plan.LBridgeMiddle!.LBridgeDecodeEnd);
    }

    [Fact]
    public void PacketKeyframes_MissingDecodeCutoff_DoesNotEraseCopyableMiddle()
    {
        var keyframes = new[]
        {
            TKeyframeData.TKeyframeEntryCreate(2),
            TKeyframeData.TKeyframeEntryCreate(4),
            TKeyframeData.TKeyframeEntryCreate(6)
        };

        LBridgePlan plan = TInterface.TBridgeResolve(keyframes, TBridgeSecondCreate(3), TBridgeSecondCreate(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(4), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(6), plan.LBridgeMiddle.LBridgeSpanEnd);
    }

    [Fact]
    public void PacketKeyframes_UnusableDecodeCutoff_DoesNotEraseCopyableMiddle()
    {
        var keyframes = new[]
        {
            TKeyframeData.TKeyframeEntryCreate(2, 1.9),
            TKeyframeData.TKeyframeEntryCreate(4, 3.9),
            TKeyframeData.TKeyframeEntryCreate(6, 3.0)
        };

        LBridgePlan plan = TInterface.TBridgeResolve(keyframes, TBridgeSecondCreate(3), TBridgeSecondCreate(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(4), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(6), plan.LBridgeMiddle.LBridgeSpanEnd);
    }

    [Fact]
    public void KeyframeJustBeyondEnd_CopyClampedToRequestedEnd()
    {
        IReadOnlyList<TimeSpan> grid =
            new[] { TBridgeSecondCreate(0), TBridgeSecondCreate(2), TBridgeSecondCreate(4), TBridgeSecondCreate(6), TBridgeSecondCreate(8) + TimeSpan.FromTicks(5_000) };

        LBridgePlan plan = TInterface.TBridgeResolve(grid, TBridgeSecondCreate(2), TBridgeSecondCreate(8));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(8), plan.LBridgeMiddle!.LBridgeSpanEnd);
        Assert.Null(plan.LBridgeTail);
    }

    [Fact]
    public void ReversedInterval_ReportsInvalid()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(7), TBridgeSecondCreate(3));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeInvalid, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeMiddle);
        Assert.Null(plan.LBridgeTail);
    }

    [Fact]
    public void EmptyInterval_ReportsInvalid()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(4), TBridgeSecondCreate(4));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeInvalid, plan.LBridgeOutcome);
    }

    [Fact]
    public void EmptyKeyframes_ReportsWholeEncode()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(Array.Empty<TimeSpan>(), TBridgeSecondCreate(3), TBridgeSecondCreate(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeWhole, plan.LBridgeOutcome);
    }

    [Fact]
    public void WholeSource_EndReachesEof_CopiesThroughWithNoBridges()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(0), TBridgeSecondCreate(11), openEnd: true);

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeTail);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(0), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(11), plan.LBridgeMiddle.LBridgeSpanEnd);
        Assert.Null(plan.LBridgeMiddle.LBridgeDecodeEnd);
    }

    [Fact]
    public void OriginBetweenKeyframes_EndReachesEof_HeadAndCopyThroughNoTail()
    {
        LBridgePlan plan = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(3), TBridgeSecondCreate(11), openEnd: true);

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeHead);
        Assert.Equal(TBridgeSecondCreate(3), plan.LBridgeHead!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(4), plan.LBridgeHead.LBridgeSpanEnd);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(4), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(11), plan.LBridgeMiddle.LBridgeSpanEnd);
        Assert.Null(plan.LBridgeTail);
    }

    [Fact]
    public void SameIntervalPastLastKeyframe_TailOnlyWhenEndIsMidStream()
    {
        LBridgePlan closed = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(2), TBridgeSecondCreate(11), openEnd: false);
        Assert.NotNull(closed.LBridgeTail);
        Assert.Equal(TBridgeSecondCreate(10), closed.LBridgeMiddle!.LBridgeSpanEnd);

        LBridgePlan open = TInterface.TBridgeResolve(TBridgeGrid, TBridgeSecondCreate(2), TBridgeSecondCreate(11), openEnd: true);
        Assert.Null(open.LBridgeTail);
        Assert.Equal(TBridgeSecondCreate(11), open.LBridgeMiddle!.LBridgeSpanEnd);
    }

    [Fact]
    public void LoneKeyframe_EndReachesEof_CopiesInsteadOfWholeEncode()
    {
        IReadOnlyList<TimeSpan> sparse = new[] { TBridgeSecondCreate(0) };

        LBridgePlan plan = TInterface.TBridgeResolve(sparse, TBridgeSecondCreate(0), TBridgeSecondCreate(11), openEnd: true);

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeTail);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(TBridgeSecondCreate(0), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(TBridgeSecondCreate(11), plan.LBridgeMiddle.LBridgeSpanEnd);
    }

    [Theory]
    [InlineData(12.0, 12.0, 30.0, true)]
    [InlineData(11.99, 12.0, 30.0, true)]
    [InlineData(11.0, 12.0, 30.0, false)]
    [InlineData(12.0, 0.0, 30.0, false)]
    public void EndCheck_ReachesSourceEndWithinOneFrame(
        double end, double duration, double framerate, bool expected)
    {
        Assert.Equal(expected, TInterface.TBridgeEndCheck(TBridgeSecondCreate(end), TBridgeSecondCreate(duration), framerate));
    }
}
