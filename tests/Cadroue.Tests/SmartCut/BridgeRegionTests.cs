using Cadroue.Application;
using Xunit;

namespace Cadroue.Tests;

public sealed class BridgeRegionTests
{
    private static TimeSpan S(double seconds) => TimeSpan.FromSeconds(seconds);

    private static readonly IReadOnlyList<TimeSpan> Grid =
        new[] { S(0), S(2), S(4), S(6), S(8), S(10) };

    [Fact]
    public void OriginOnKeyframe_EndOnKeyframe_CopyOnlyNoBridges()
    {
        LBridgePlan plan = TInterface.BridgeResolve(Grid, S(2), S(8));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeTail);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(S(2), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(S(8), plan.LBridgeMiddle.LBridgeSpanEnd);
    }

    [Fact]
    public void BoundaryBetweenKeyframes_AllThreeRegions()
    {
        LBridgePlan plan = TInterface.BridgeResolve(Grid, S(3), S(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);

        Assert.NotNull(plan.LBridgeHead);
        Assert.Equal(S(3), plan.LBridgeHead!.LBridgeSpanOrigin);
        Assert.Equal(S(4), plan.LBridgeHead.LBridgeSpanEnd);

        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(S(4), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(S(6), plan.LBridgeMiddle.LBridgeSpanEnd);

        Assert.NotNull(plan.LBridgeTail);
        Assert.Equal(S(6), plan.LBridgeTail!.LBridgeSpanOrigin);
        Assert.Equal(S(7), plan.LBridgeTail.LBridgeSpanEnd);
    }

    [Fact]
    public void NoInteriorKeyframe_ReportsWholeEncode()
    {
        IReadOnlyList<TimeSpan> sparse = new[] { S(0), S(10) };

        LBridgePlan plan = TInterface.BridgeResolve(sparse, S(3), S(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeWhole, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeMiddle);
        Assert.Null(plan.LBridgeTail);
        Assert.Equal(S(3), plan.LBridgeInterval.LBridgeSpanOrigin);
        Assert.Equal(S(7), plan.LBridgeInterval.LBridgeSpanEnd);
    }

    [Fact]
    public void SingleInteriorKeyframe_NoCopyableSpan_ReportsWholeEncode()
    {
        LBridgePlan plan = TInterface.BridgeResolve(Grid, S(2), S(3));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeWhole, plan.LBridgeOutcome);
    }

    [Fact]
    public void OriginBetweenKeyframes_EndOnKeyframe_HeadAndCopyNoTail()
    {
        LBridgePlan plan = TInterface.BridgeResolve(Grid, S(3), S(6));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeHead);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Null(plan.LBridgeTail);
        Assert.Equal(S(6), plan.LBridgeMiddle!.LBridgeSpanEnd);
    }

    [Fact]
    public void PacketKeyframes_CarryDecodeCutoffIntoCopiedMiddle()
    {
        var keyframes = new[]
        {
            KeyframeData.Entry(2, 1.9),
            KeyframeData.Entry(4, 3.9),
            KeyframeData.Entry(6, 5.9)
        };

        LBridgePlan plan = TInterface.BridgeResolve(keyframes, S(3), S(7));

        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(S(5.9), plan.LBridgeMiddle!.LBridgeDecodeEnd);
    }

    [Fact]
    public void PacketKeyframes_MissingDecodeCutoff_DoesNotEraseCopyableMiddle()
    {
        var keyframes = new[]
        {
            KeyframeData.Entry(2),
            KeyframeData.Entry(4),
            KeyframeData.Entry(6)
        };

        LBridgePlan plan = TInterface.BridgeResolve(keyframes, S(3), S(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(S(4), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(S(6), plan.LBridgeMiddle.LBridgeSpanEnd);
    }

    [Fact]
    public void PacketKeyframes_UnusableDecodeCutoff_DoesNotEraseCopyableMiddle()
    {
        var keyframes = new[]
        {
            KeyframeData.Entry(2, 1.9),
            KeyframeData.Entry(4, 3.9),
            KeyframeData.Entry(6, 3.0)
        };

        LBridgePlan plan = TInterface.BridgeResolve(keyframes, S(3), S(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(S(4), plan.LBridgeMiddle!.LBridgeSpanOrigin);
        Assert.Equal(S(6), plan.LBridgeMiddle.LBridgeSpanEnd);
    }

    [Fact]
    public void KeyframeJustBeyondEnd_CopyClampedToRequestedEnd()
    {
        IReadOnlyList<TimeSpan> grid =
            new[] { S(0), S(2), S(4), S(6), S(8) + TimeSpan.FromTicks(5_000) };

        LBridgePlan plan = TInterface.BridgeResolve(grid, S(2), S(8));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeSmart, plan.LBridgeOutcome);
        Assert.NotNull(plan.LBridgeMiddle);
        Assert.Equal(S(8), plan.LBridgeMiddle!.LBridgeSpanEnd);
        Assert.Null(plan.LBridgeTail);
    }

    [Fact]
    public void ReversedInterval_ReportsInvalid()
    {
        LBridgePlan plan = TInterface.BridgeResolve(Grid, S(7), S(3));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeInvalid, plan.LBridgeOutcome);
        Assert.Null(plan.LBridgeHead);
        Assert.Null(plan.LBridgeMiddle);
        Assert.Null(plan.LBridgeTail);
    }

    [Fact]
    public void EmptyInterval_ReportsInvalid()
    {
        LBridgePlan plan = TInterface.BridgeResolve(Grid, S(4), S(4));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeInvalid, plan.LBridgeOutcome);
    }

    [Fact]
    public void EmptyKeyframes_ReportsWholeEncode()
    {
        LBridgePlan plan = TInterface.BridgeResolve(Array.Empty<TimeSpan>(), S(3), S(7));

        Assert.Equal(LBridgeOutcome.LBridgeOutcomeWhole, plan.LBridgeOutcome);
    }
}
