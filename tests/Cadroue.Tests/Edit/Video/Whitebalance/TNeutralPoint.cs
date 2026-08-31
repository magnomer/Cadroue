using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class TNeutralPoint
{
    private const int TNeutralSourceWidth = 40;
    private const int TNeutralSourceHeight = 30;

    public static TheoryData<LRotateKind, bool, bool> TNeutralTransformCreate()
    {
        var data = new TheoryData<LRotateKind, bool, bool>();
        foreach (LRotateKind rotate in new[]
                 {
                     LRotateKind.LRotateNone, LRotateKind.LRotate90,
                     LRotateKind.LRotate180, LRotateKind.LRotate270
                 })
        {
            foreach (bool flipH in new[] { false, true })
            {
                foreach (bool flipV in new[] { false, true })
                {
                    data.Add(rotate, flipH, flipV);
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(TNeutralTransformCreate))]
    public void Resolve_FullFrameRoundTrips(LRotateKind rotate, bool flipH, bool flipV)
    {
        // A letterboxed display rect that is not flush with the overlay origin.
        var display = new TNeutralRect(12, 7, 300, 180);
        (int rotatedWidth, int rotatedHeight) = TNeutralRotateResolve(rotate);
        var shown = new TNeutralRect(0, 0, rotatedWidth, rotatedHeight);

        foreach ((int sx, int sy) in TNeutralPixelRead())
        {
            (double clickX, double clickY) = TNeutralClickResolve(
                sx, sy, rotate, flipH, flipV, display, shown);

            LNeutralPoint point = TNeutral.TNeutralPointResolve(
                clickX, clickY,
                display.TNeutralRectX, display.TNeutralRectY, display.TNeutralRectWidth, display.TNeutralRectHeight,
                shown.TNeutralRectX, shown.TNeutralRectY, shown.TNeutralRectWidth, shown.TNeutralRectHeight,
                rotate, flipH, flipV, TNeutralSourceWidth, TNeutralSourceHeight);

            Assert.True(point.LNeutralPointInside);
            Assert.Equal(sx, point.LNeutralPointX);
            Assert.Equal(sy, point.LNeutralPointY);
        }
    }

    [Theory]
    [MemberData(nameof(TNeutralTransformCreate))]
    public void Resolve_CroppedRegionRoundTrips(LRotateKind rotate, bool flipH, bool flipV)
    {
        var display = new TNeutralRect(0, 0, 260, 160);
        var shown = new TNeutralRect(6, 4, 20, 15);

        // Only pixels that fall inside the shown crop region are recoverable.
        foreach ((int sx, int sy) in TNeutralPixelRead())
        {
            (double finalX, double finalY) = TNeutralFinalResolve(sx, sy, rotate, flipH, flipV);
            if (finalX < shown.TNeutralRectX || finalY < shown.TNeutralRectY
                || finalX >= shown.TNeutralRectX + shown.TNeutralRectWidth || finalY >= shown.TNeutralRectY + shown.TNeutralRectHeight)
            {
                continue;
            }

            (double clickX, double clickY) = TNeutralClickResolve(
                sx, sy, rotate, flipH, flipV, display, shown);

            LNeutralPoint point = TNeutral.TNeutralPointResolve(
                clickX, clickY,
                display.TNeutralRectX, display.TNeutralRectY, display.TNeutralRectWidth, display.TNeutralRectHeight,
                shown.TNeutralRectX, shown.TNeutralRectY, shown.TNeutralRectWidth, shown.TNeutralRectHeight,
                rotate, flipH, flipV, TNeutralSourceWidth, TNeutralSourceHeight);

            Assert.True(point.LNeutralPointInside);
            Assert.Equal(sx, point.LNeutralPointX);
            Assert.Equal(sy, point.LNeutralPointY);
        }
    }

    [Fact]
    public void Resolve_ClickInLetterbox_ReportsOutside()
    {
        var display = new TNeutralRect(20, 10, 200, 120);

        foreach ((double x, double y) in new[]
                 {
                     (19.0, 50.0), (50.0, 9.0),
                     (220.0, 50.0), (50.0, 130.0)
                 })
        {
            LNeutralPoint point = TNeutral.TNeutralPointResolve(
                x, y,
                display.TNeutralRectX, display.TNeutralRectY, display.TNeutralRectWidth, display.TNeutralRectHeight,
                0, 0, TNeutralSourceWidth, TNeutralSourceHeight,
                LRotateKind.LRotateNone, false, false, TNeutralSourceWidth, TNeutralSourceHeight);

            Assert.False(point.LNeutralPointInside);
        }
    }

    [Fact]
    public void Resolve_NoMedia_ReportsOutside()
    {
        LNeutralPoint point = TNeutral.TNeutralPointResolve(
            10, 10, 0, 0, 100, 100, 0, 0, 100, 100,
            LRotateKind.LRotateNone, false, false, 0, 0);

        Assert.False(point.LNeutralPointInside);
    }

    [Fact]
    public void Resolve_DisplayCorners_StayInBounds()
    {
        var display = new TNeutralRect(0, 0, 400, 300);

        LNeutralPoint topLeft = TNeutral.TNeutralPointResolve(
            0, 0, 0, 0, 400, 300, 0, 0, TNeutralSourceWidth, TNeutralSourceHeight,
            LRotateKind.LRotateNone, false, false, TNeutralSourceWidth, TNeutralSourceHeight);
        LNeutralPoint bottomRight = TNeutral.TNeutralPointResolve(
            399.999, 299.999, 0, 0, 400, 300, 0, 0, TNeutralSourceWidth, TNeutralSourceHeight,
            LRotateKind.LRotateNone, false, false, TNeutralSourceWidth, TNeutralSourceHeight);

        Assert.True(topLeft.LNeutralPointInside);
        Assert.Equal(0, topLeft.LNeutralPointX);
        Assert.Equal(0, topLeft.LNeutralPointY);
        Assert.True(bottomRight.LNeutralPointInside);
        Assert.Equal(TNeutralSourceWidth - 1, bottomRight.LNeutralPointX);
        Assert.Equal(TNeutralSourceHeight - 1, bottomRight.LNeutralPointY);
    }

    private static (int, int)[] TNeutralPixelRead() =>
        new[]
        {
            (0, 0),
            (TNeutralSourceWidth - 1, 0),
            (0, TNeutralSourceHeight - 1),
            (TNeutralSourceWidth - 1, TNeutralSourceHeight - 1),
            (13, 9),
            (27, 21),
            (TNeutralSourceWidth / 2, TNeutralSourceHeight / 2)
        };

    private (int, int) TNeutralRotateResolve(LRotateKind rotate) =>
        rotate is LRotateKind.LRotate90 or LRotateKind.LRotate270
            ? (TNeutralSourceHeight, TNeutralSourceWidth)
            : (TNeutralSourceWidth, TNeutralSourceHeight);

    // Independent forward model of the mpv display pipeline: hflip, vflip,
    // transpose(rotate), producing the pixel in final (post-transpose) space.
    private static (double, double) TNeutralFinalResolve(
        int sx, int sy, LRotateKind rotate, bool flipH, bool flipV)
    {
        int x = flipH ? TNeutralSourceWidth - 1 - sx : sx;
        int y = flipV ? TNeutralSourceHeight - 1 - sy : sy;
        (int fx, int fy) = rotate switch
        {
            LRotateKind.LRotate90 => (TNeutralSourceHeight - 1 - y, x),
            LRotateKind.LRotate270 => (y, TNeutralSourceWidth - 1 - x),
            LRotateKind.LRotate180 => (TNeutralSourceWidth - 1 - x, TNeutralSourceHeight - 1 - y),
            _ => (x, y)
        };

        // Pixel centre so the resolver's floor recovers the exact pixel.
        return (fx + 0.5, fy + 0.5);
    }

    private static (double, double) TNeutralClickResolve(
        int sx, int sy, LRotateKind rotate, bool flipH, bool flipV, TNeutralRect display, TNeutralRect shown)
    {
        (double finalX, double finalY) = TNeutralFinalResolve(sx, sy, rotate, flipH, flipV);
        double u = (finalX - shown.TNeutralRectX) / shown.TNeutralRectWidth;
        double v = (finalY - shown.TNeutralRectY) / shown.TNeutralRectHeight;
        return (display.TNeutralRectX + (u * display.TNeutralRectWidth), display.TNeutralRectY + (v * display.TNeutralRectHeight));
    }

    private readonly record struct TNeutralRect(
        double TNeutralRectX, double TNeutralRectY, double TNeutralRectWidth, double TNeutralRectHeight);
}
