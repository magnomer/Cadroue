using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class NeutralPointTests
{
    private const int SourceWidth = 40;
    private const int SourceHeight = 30;

    public static TheoryData<LRotateKind, bool, bool> Transforms()
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
    [MemberData(nameof(Transforms))]
    public void Resolve_FullFrameRoundTrips(LRotateKind rotate, bool flipH, bool flipV)
    {
        // A letterboxed display rect that is not flush with the overlay origin.
        var display = new Rect(12, 7, 300, 180);
        (int rotatedWidth, int rotatedHeight) = RotatedDimensions(rotate);
        var shown = new Rect(0, 0, rotatedWidth, rotatedHeight);

        foreach ((int sx, int sy) in SamplePixels())
        {
            (double clickX, double clickY) = ForwardClick(
                sx, sy, rotate, flipH, flipV, display, shown);

            LNeutralPoint point = TNeutral.PointResolve(
                clickX, clickY,
                display.X, display.Y, display.Width, display.Height,
                shown.X, shown.Y, shown.Width, shown.Height,
                rotate, flipH, flipV, SourceWidth, SourceHeight);

            Assert.True(point.LNeutralPointInside);
            Assert.Equal(sx, point.LNeutralPointX);
            Assert.Equal(sy, point.LNeutralPointY);
        }
    }

    [Theory]
    [MemberData(nameof(Transforms))]
    public void Resolve_CroppedRegionRoundTrips(LRotateKind rotate, bool flipH, bool flipV)
    {
        var display = new Rect(0, 0, 260, 160);
        var shown = new Rect(6, 4, 20, 15);

        // Only pixels that fall inside the shown crop region are recoverable.
        foreach ((int sx, int sy) in SamplePixels())
        {
            (double finalX, double finalY) = ForwardFinal(sx, sy, rotate, flipH, flipV);
            if (finalX < shown.X || finalY < shown.Y
                || finalX >= shown.X + shown.Width || finalY >= shown.Y + shown.Height)
            {
                continue;
            }

            (double clickX, double clickY) = ForwardClick(
                sx, sy, rotate, flipH, flipV, display, shown);

            LNeutralPoint point = TNeutral.PointResolve(
                clickX, clickY,
                display.X, display.Y, display.Width, display.Height,
                shown.X, shown.Y, shown.Width, shown.Height,
                rotate, flipH, flipV, SourceWidth, SourceHeight);

            Assert.True(point.LNeutralPointInside);
            Assert.Equal(sx, point.LNeutralPointX);
            Assert.Equal(sy, point.LNeutralPointY);
        }
    }

    [Fact]
    public void Resolve_ClickInLetterbox_ReportsOutside()
    {
        var display = new Rect(20, 10, 200, 120);

        foreach ((double x, double y) in new[]
                 {
                     (19.0, 50.0), (50.0, 9.0),
                     (220.0, 50.0), (50.0, 130.0)
                 })
        {
            LNeutralPoint point = TNeutral.PointResolve(
                x, y,
                display.X, display.Y, display.Width, display.Height,
                0, 0, SourceWidth, SourceHeight,
                LRotateKind.LRotateNone, false, false, SourceWidth, SourceHeight);

            Assert.False(point.LNeutralPointInside);
        }
    }

    [Fact]
    public void Resolve_NoMedia_ReportsOutside()
    {
        LNeutralPoint point = TNeutral.PointResolve(
            10, 10, 0, 0, 100, 100, 0, 0, 100, 100,
            LRotateKind.LRotateNone, false, false, 0, 0);

        Assert.False(point.LNeutralPointInside);
    }

    [Fact]
    public void Resolve_DisplayCorners_StayInBounds()
    {
        var display = new Rect(0, 0, 400, 300);

        LNeutralPoint topLeft = TNeutral.PointResolve(
            0, 0, 0, 0, 400, 300, 0, 0, SourceWidth, SourceHeight,
            LRotateKind.LRotateNone, false, false, SourceWidth, SourceHeight);
        LNeutralPoint bottomRight = TNeutral.PointResolve(
            399.999, 299.999, 0, 0, 400, 300, 0, 0, SourceWidth, SourceHeight,
            LRotateKind.LRotateNone, false, false, SourceWidth, SourceHeight);

        Assert.True(topLeft.LNeutralPointInside);
        Assert.Equal(0, topLeft.LNeutralPointX);
        Assert.Equal(0, topLeft.LNeutralPointY);
        Assert.True(bottomRight.LNeutralPointInside);
        Assert.Equal(SourceWidth - 1, bottomRight.LNeutralPointX);
        Assert.Equal(SourceHeight - 1, bottomRight.LNeutralPointY);
    }

    private static (int, int)[] SamplePixels() =>
        new[]
        {
            (0, 0),
            (SourceWidth - 1, 0),
            (0, SourceHeight - 1),
            (SourceWidth - 1, SourceHeight - 1),
            (13, 9),
            (27, 21),
            (SourceWidth / 2, SourceHeight / 2)
        };

    private (int, int) RotatedDimensions(LRotateKind rotate) =>
        rotate is LRotateKind.LRotate90 or LRotateKind.LRotate270
            ? (SourceHeight, SourceWidth)
            : (SourceWidth, SourceHeight);

    // Independent forward model of the mpv display pipeline: hflip, vflip,
    // transpose(rotate), producing the pixel in final (post-transpose) space.
    private static (double, double) ForwardFinal(
        int sx, int sy, LRotateKind rotate, bool flipH, bool flipV)
    {
        int x = flipH ? SourceWidth - 1 - sx : sx;
        int y = flipV ? SourceHeight - 1 - sy : sy;
        (int fx, int fy) = rotate switch
        {
            LRotateKind.LRotate90 => (SourceHeight - 1 - y, x),
            LRotateKind.LRotate270 => (y, SourceWidth - 1 - x),
            LRotateKind.LRotate180 => (SourceWidth - 1 - x, SourceHeight - 1 - y),
            _ => (x, y)
        };

        // Pixel centre so the resolver's floor recovers the exact pixel.
        return (fx + 0.5, fy + 0.5);
    }

    private static (double, double) ForwardClick(
        int sx, int sy, LRotateKind rotate, bool flipH, bool flipV, Rect display, Rect shown)
    {
        (double finalX, double finalY) = ForwardFinal(sx, sy, rotate, flipH, flipV);
        double u = (finalX - shown.X) / shown.Width;
        double v = (finalY - shown.Y) / shown.Height;
        return (display.X + (u * display.Width), display.Y + (v * display.Height));
    }

    private readonly record struct Rect(double X, double Y, double Width, double Height);
}
