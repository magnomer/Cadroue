using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSashResize
{
    [Fact]
    public void Direction_ReadsEdgesAndCorners()
    {
        Assert.Equal(LSash.LSashNone, TInterface.TSashDirectionResolve(100, 100, 400, 300, 8));
        Assert.Equal(LSash.LSashLeft, TInterface.TSashDirectionResolve(3, 100, 400, 300, 8));
        Assert.Equal(LSash.LSashRight, TInterface.TSashDirectionResolve(397, 100, 400, 300, 8));
        Assert.Equal(LSash.LSashTop, TInterface.TSashDirectionResolve(100, 2, 400, 300, 8));
        Assert.Equal(LSash.LSashBottom, TInterface.TSashDirectionResolve(100, 298, 400, 300, 8));
        Assert.Equal(LSash.LSashLeft | LSash.LSashTop, TInterface.TSashDirectionResolve(1, 1, 400, 300, 8));
        Assert.Equal(LSash.LSashRight | LSash.LSashBottom, TInterface.TSashDirectionResolve(399, 299, 400, 300, 8));
    }

    [Fact]
    public void Press_StartsOnlyOnEdgeOfNormalWindow()
    {
        LSash sash = TInterface.TSashCreate(8);
        LSashBounds bounds = TInterface.TSashBoundsCreate(10, 20, 400, 300);

        Assert.False(TInterface.TSashPressHandle(sash, false, false, 2, 100, 12, 120, bounds));
        Assert.False(TInterface.TSashPressHandle(sash, true, true, 2, 100, 12, 120, bounds));
        Assert.False(TInterface.TSashPressHandle(sash, true, false, 200, 100, 210, 120, bounds));
        Assert.False(sash.LSashActive);

        Assert.True(TInterface.TSashPressHandle(sash, true, false, 2, 100, 12, 120, bounds));
        Assert.True(sash.LSashActive);
        Assert.Equal(LSash.LSashLeft, sash.LSashDirection);
    }

    [Fact]
    public void Move_ResizesFromTheLeftEdgeAndClampsToMinimum()
    {
        LSash sash = TInterface.TSashCreate(8);
        List<LSashBounds> changes = [];
        TInterface.TSashBoundsAttach(sash, changes.Add);
        TInterface.TSashPressHandle(
            sash, true, false, 2, 100, 12, 120, TInterface.TSashBoundsCreate(10, 20, 400, 300));

        Assert.True(TInterface.TSashMoveHandle(sash, true, false, 0, 0, 400, 300, 32, 120, 100, 100));
        Assert.Equal(TInterface.TSashBoundsCreate(30, 20, 380, 300), changes[^1]);

        Assert.True(TInterface.TSashMoveHandle(sash, true, false, 0, 0, 400, 300, 400, 120, 100, 100));
        Assert.Equal(TInterface.TSashBoundsCreate(310, 20, 100, 300), changes[^1]);
    }

    [Fact]
    public void Hover_TracksDirection_AndReleaseStopsCapture()
    {
        LSash sash = TInterface.TSashCreate(8);
        int starts = 0;
        int stops = 0;
        List<bool> actives = [];
        TInterface.TSashCaptureAttach(sash, () => starts++, () => stops++, actives.Add);

        Assert.False(TInterface.TSashMoveHandle(sash, true, false, 398, 100, 400, 300, 0, 0, 100, 100));
        Assert.Equal(LSash.LSashRight, sash.LSashDirection);
        Assert.Equal(LSash.LSashNone, TInterface.TSashLeaveResolve(sash));
        Assert.False(TInterface.TSashReleaseHandle(sash));

        TInterface.TSashPressHandle(sash, true, false, 398, 100, 0, 0, TInterface.TSashBoundsCreate(0, 0, 400, 300));
        Assert.Equal(LSash.LSashRight, TInterface.TSashLeaveResolve(sash));
        Assert.True(TInterface.TSashReleaseHandle(sash));
        Assert.Equal(1, starts);
        Assert.Equal(1, stops);
        Assert.Equal([true, false], actives);

        TInterface.TSashPressHandle(sash, true, false, 398, 100, 0, 0, TInterface.TSashBoundsCreate(0, 0, 400, 300));
        TInterface.TSashCaptureHandle(sash);
        Assert.False(sash.LSashActive);
        Assert.Equal(1, stops);
        Assert.Equal([true, false, true, false], actives);
    }

    [Fact]
    public void Clamp_KeepsBoundsInsideWorkArea()
    {
        LSashBounds work = TInterface.TSashBoundsCreate(0, 0, 1000, 800);
        LSashBounds clamped = TInterface.TSashBoundsClamp(
            TInterface.TSashBoundsCreate(900, 700, 300, 200),
            work,
            100,
            100,
            double.PositiveInfinity,
            double.PositiveInfinity);
        Assert.Equal(TInterface.TSashBoundsCreate(700, 600, 300, 200), clamped);

        LSashBounds limited = TInterface.TSashBoundsClamp(
            TInterface.TSashBoundsCreate(-50, -50, 2000, 50), work, 100, 100, 500, double.PositiveInfinity);
        Assert.Equal(TInterface.TSashBoundsCreate(0, 0, 500, 100), limited);
    }

    [Fact]
    public void Placement_FallsBackToCurrentThenWorkArea()
    {
        LSashBounds current = TInterface.TSashBoundsCreate(double.NaN, 40, 640, 480);
        LSashBounds resolved = TInterface.TSashPlacementResolve(null, 15, 0, double.NaN, current, 300, 200, 5, 7);
        Assert.Equal(TInterface.TSashBoundsCreate(5, 15, 640, 480), resolved);

        LSashBounds saved = TInterface.TSashPlacementResolve(100, 200, 800, 600, current, 300, 200, 5, 7);
        Assert.Equal(TInterface.TSashBoundsCreate(100, 200, 800, 600), saved);
    }

    [Fact]
    public void Relay_AndCenter_PlaceTheWindow()
    {
        Assert.Equal((0d, 0d), TInterface.TSashRelayResolve(50, 10, 0, 0, 1920, 1080));
        Assert.Equal((1720d, 960d), TInterface.TSashRelayResolve(5000, 5000, 0, 0, 1920, 1080));

        LSashBounds window = TInterface.TSashBoundsCreate(0, 0, 200, 100);
        LSashBounds owner = TInterface.TSashBoundsCreate(100, 100, 600, 400);
        Assert.Equal((300d, 250d), TInterface.TSashCenterResolve(false, window, owner));
        Assert.Equal((0d, 0d), TInterface.TSashCenterResolve(true, window, owner));
    }
}
