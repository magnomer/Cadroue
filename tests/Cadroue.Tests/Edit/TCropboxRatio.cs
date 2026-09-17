using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCropboxRatio
{
    private static readonly LCropbox TCropboxBounds = TInterface.TCropboxCreate(0, 0, 1920, 1080);

    [Fact]
    public void RatioNotFixed_LeavesDrawnBoxAlone()
    {
        LCropbox desired = TInterface.TCropboxCreate(100, 100, 1000, 500);

        LCropbox? fit = TInterface.TCropboxFitResolve(
            desired, TCropboxBounds, TInterface.TCropboxRatioCreate(false, false, 16, 9), 0, -1, -1);

        Assert.Null(fit);
    }

    [Fact]
    public void StrictRatio_SnapsToWholeRatioUnits()
    {
        LCropbox desired = TInterface.TCropboxCreate(100, 100, 1000, 500);

        LCropbox? fit = TInterface.TCropboxFitResolve(
            desired, TCropboxBounds, TInterface.TCropboxRatioCreate(true, false, 16, 9), 0, -1, -1);

        Assert.NotNull(fit);
        Assert.Equal(992, fit.LCropboxWidth);
        Assert.Equal(558, fit.LCropboxHeight);
        Assert.Equal(100, fit.LCropboxX);
        Assert.Equal(100, fit.LCropboxY);
    }

    [Fact]
    public void LenientRatio_KeepsDrawnWidthWithinTolerance()
    {
        LCropbox desired = TInterface.TCropboxCreate(100, 100, 1000, 500);

        LCropbox? fit = TInterface.TCropboxFitResolve(
            desired, TCropboxBounds, TInterface.TCropboxRatioCreate(true, true, 16, 9), 0, -1, -1);

        Assert.NotNull(fit);
        Assert.Equal(1000, fit.LCropboxWidth);
        Assert.Equal(562, fit.LCropboxHeight);
    }

    [Fact]
    public void Tolerance_AcceptsNearRatio_RejectsFarRatio()
    {
        Assert.True(TInterface.TCropboxToleranceCheck(1000, 562, 16, 9));
        Assert.False(TInterface.TCropboxToleranceCheck(1000, 500, 16, 9));
    }

    [Fact]
    public void EdgeLock_UnlockedBottom_MovesBottomToKeepRatio()
    {
        LCropboxEdgeLock edgeLock = TInterface.TCropboxLockCreate();

        LCropboxEdges? edges = TInterface.TCropboxEdgeResolve(
            edgeLock, 1920, 1080, TInterface.TCropboxEdgesCreate(0, 0, 320, 100), 16, 9, true);

        Assert.NotNull(edges);
        Assert.Equal(0, edges.Value.LCropboxTop);
        Assert.Equal(180, edges.Value.LCropboxBottom);
    }

    [Fact]
    public void EdgeLock_LockedBottom_HoldsBottomAndMovesTop()
    {
        LCropboxEdgeLock edgeLock = TInterface.TCropboxLockCreate();
        TInterface.TCropboxEdgeSet(edgeLock, 3, true);

        LCropboxEdges? edges = TInterface.TCropboxEdgeResolve(
            edgeLock, 1920, 1080, TInterface.TCropboxEdgesCreate(0, 0, 320, 100), 16, 9, true);

        Assert.NotNull(edges);
        Assert.Equal(80, edges.Value.LCropboxTop);
        Assert.Equal(100, edges.Value.LCropboxBottom);
    }
}
