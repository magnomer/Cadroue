using Cadroue.Application;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCurveCanvas
{
    private const double TCurveSize = 150;

    [Fact]
    public void Press_NearPoint_SelectsIt()
    {
        LCurve curve = TInterface.TCurveCreate();

        TInterface.TCurvePressHandle(curve, 4, TCurveSize - 3, TCurveSize);

        Assert.Equal(0, TInterface.TCurveSelectedRead(curve));
        Assert.Equal(2, TInterface.TCurvePointsRead(curve).Count);
        Assert.True(TInterface.TCurveDragRead(curve));
    }

    [Fact]
    public void Press_OnEmpty_AddsAndSelectsPoint()
    {
        LCurve curve = TInterface.TCurveCreate();

        TInterface.TCurvePressHandle(curve, TCurveSize / 2, TCurveSize / 2, TCurveSize);

        Assert.Equal(3, TInterface.TCurvePointsRead(curve).Count);
        Assert.Equal(1, TInterface.TCurveSelectedRead(curve));
        Assert.Equal(0.5, TInterface.TCurvePointsRead(curve)[1].LWorkCurveInput, 6);
        Assert.Equal(0.5, TInterface.TCurvePointsRead(curve)[1].LWorkCurveOutput, 6);
    }

    [Fact]
    public void Move_WhileDragging_KeepsGapBelowNeighbour()
    {
        LCurve curve = TInterface.TCurveCreate();
        TInterface.TCurvePressHandle(curve, TCurveSize / 2, TCurveSize / 2, TCurveSize);

        Assert.True(TInterface.TCurveMoveHandle(curve, TCurveSize + 40, 0, TCurveSize));

        double input = TInterface.TCurvePointsRead(curve)[1].LWorkCurveInput;
        Assert.True(input < 1);
        Assert.Equal(1, TInterface.TCurvePointsRead(curve)[1].LWorkCurveOutput, 6);
        Assert.True(TInterface.TCurveReleaseHandle(curve));
        Assert.False(TInterface.TCurveMoveHandle(curve, 0, 0, TCurveSize));
        Assert.False(TInterface.TCurveReleaseHandle(curve));
    }

    [Fact]
    public void TrackRead_SamplesWholeWidth()
    {
        LCurve curve = TInterface.TCurveCreate();

        IReadOnlyList<LCurvePoint> track = TInterface.TCurveTrackRead(curve, TCurveSize);

        Assert.Equal(97, track.Count);
        Assert.Equal(0, track[0].LCurvePointX, 6);
        Assert.Equal(TCurveSize, track[0].LCurvePointY, 6);
        Assert.Equal(TCurveSize, track[96].LCurvePointX, 6);
        Assert.Equal(0, track[96].LCurvePointY, 6);
    }

    [Fact]
    public void GridRead_IdentityLine_OnlyWhileIdentity()
    {
        LCurve curve = TInterface.TCurveCreate();
        Assert.Contains(TInterface.TCurveGridRead(curve, TCurveSize), line => line.LCurveLineKey == "Identity");
        Assert.Equal(6, TInterface.TCurveGridRead(curve, TCurveSize).Count(line => line.LCurveLineKey != "Identity"));

        TInterface.TCurvePointAdd(curve, 0.5, 0.7);

        Assert.DoesNotContain(TInterface.TCurveGridRead(curve, TCurveSize), line => line.LCurveLineKey == "Identity");
    }

    [Fact]
    public void DotsRead_SelectedIsLarger_EndpointsFilledAsGuide()
    {
        LCurve curve = TInterface.TCurveCreate();
        TInterface.TCurvePointAdd(curve, 0.5, 0.5);

        IReadOnlyList<LCurveDot> dots = TInterface.TCurveDotsRead(curve, TCurveSize);

        Assert.Equal(3, dots.Count);
        Assert.Equal("Endpoint", dots[0].LCurveDotFill);
        Assert.Equal("Selected", dots[1].LCurveDotFill);
        Assert.Equal("Endpoint", dots[2].LCurveDotFill);
        Assert.True(dots[1].LCurveDotSize > dots[0].LCurveDotSize);
        Assert.Equal((TCurveSize / 2) - (dots[1].LCurveDotSize / 2), dots[1].LCurveDotLeft, 6);
    }

    [Fact]
    public void HistogramRead_ScalesPeakToHeadroom_EmptyWithoutCounts()
    {
        LCurve curve = TInterface.TCurveCreate();
        Assert.Empty(TInterface.TCurveHistogramRead(curve, TCurveSize));

        byte[] pixels = new byte[4 * 4];
        Array.Fill(pixels, (byte)255);
        LHistogramCounts histogram = THistogram.THistogramCreate(pixels, 2, 2);
        TInterface.TCurveHistogramSet(curve, histogram);
        IReadOnlyList<LCurvePoint> area = TInterface.TCurveHistogramRead(curve, TCurveSize);

        Assert.Equal(258, area.Count);
        Assert.Equal(TCurveSize, area[0].LCurvePointY, 6);
        Assert.Equal(TCurveSize - (TCurveSize * 0.94), area[256].LCurvePointY, 6);
        Assert.Equal(TCurveSize, area[257].LCurvePointX, 6);
    }

    [Fact]
    public void PointCommit_ReadsPercentText_ClampsToUnit()
    {
        LCurve curve = TInterface.TCurveCreate();
        TInterface.TCurvePointAdd(curve, 0.5, 0.5);

        TInterface.TCurvePointCommit(curve, "60", "250");

        Assert.Equal(60, TInterface.TCurveInputRead(curve), 6);
        Assert.Equal(1, TInterface.TCurvePointsRead(curve)[1].LWorkCurveOutput, 6);
    }
}
