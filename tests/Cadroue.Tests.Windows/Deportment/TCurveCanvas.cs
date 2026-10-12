using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TCurveCanvas
{
    private const double TCurveSize = 150;

    [Fact]
    public void Press_NearPoint_SelectsIt()
    {
        QCurve curve = TInterface.TCurveCreate();

        TInterface.TCurvePressHandle(curve, 4, TCurveSize - 3, TCurveSize);

        Assert.Equal(0, TInterface.TCurveSelectedRead(curve));
        Assert.Equal(2, TInterface.TCurvePointsRead(curve).Count);
        Assert.True(TInterface.TCurveDragRead(curve));
    }

    [Fact]
    public void Press_OnEmpty_AddsAndSelectsPoint()
    {
        QCurve curve = TInterface.TCurveCreate();

        TInterface.TCurvePressHandle(curve, TCurveSize / 2, TCurveSize / 2, TCurveSize);

        Assert.Equal(3, TInterface.TCurvePointsRead(curve).Count);
        Assert.Equal(1, TInterface.TCurveSelectedRead(curve));
        Assert.Equal(0.5, TInterface.TCurvePointsRead(curve)[1].LWorkCurveInput, 6);
        Assert.Equal(0.5, TInterface.TCurvePointsRead(curve)[1].LWorkCurveOutput, 6);
    }

    [Fact]
    public void Move_WhileDragging_KeepsGapBelowNeighbour()
    {
        QCurve curve = TInterface.TCurveCreate();
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
        QCurve curve = TInterface.TCurveCreate();

        IReadOnlyList<QCurvePoint> track = TInterface.TCurveTrackRead(curve, TCurveSize);

        Assert.Equal(97, track.Count);
        Assert.Equal(0, track[0].QCurvePointX, 6);
        Assert.Equal(TCurveSize, track[0].QCurvePointY, 6);
        Assert.Equal(TCurveSize, track[96].QCurvePointX, 6);
        Assert.Equal(0, track[96].QCurvePointY, 6);
    }

    [Fact]
    public void GridRead_IdentityLine_OnlyWhileIdentity()
    {
        QCurve curve = TInterface.TCurveCreate();
        Assert.Contains(TInterface.TCurveGridRead(curve, TCurveSize), line => line.QCurveLineKey == "Identity");
        Assert.Equal(6, TInterface.TCurveGridRead(curve, TCurveSize).Count(line => line.QCurveLineKey != "Identity"));

        TInterface.TCurvePointAdd(curve, 0.5, 0.7);

        Assert.DoesNotContain(TInterface.TCurveGridRead(curve, TCurveSize), line => line.QCurveLineKey == "Identity");
    }

    [Fact]
    public void DotsRead_SelectedIsLarger_EndpointsFilledAsGuide()
    {
        QCurve curve = TInterface.TCurveCreate();
        TInterface.TCurvePointAdd(curve, 0.5, 0.5);

        IReadOnlyList<QCurveDot> dots = TInterface.TCurveDotsRead(curve, TCurveSize);

        Assert.Equal(3, dots.Count);
        Assert.Equal("Endpoint", dots[0].QCurveDotFill);
        Assert.Equal("Selected", dots[1].QCurveDotFill);
        Assert.Equal("Endpoint", dots[2].QCurveDotFill);
        Assert.True(dots[1].QCurveDotSize > dots[0].QCurveDotSize);
        Assert.Equal((TCurveSize / 2) - (dots[1].QCurveDotSize / 2), dots[1].QCurveDotLeft, 6);
    }

    [Fact]
    public void HistogramRead_ScalesPeakToHeadroom_EmptyWithoutCounts()
    {
        QCurve curve = TInterface.TCurveCreate();
        Assert.Empty(TInterface.TCurveHistogramRead(curve, TCurveSize));

        byte[] pixels = new byte[4 * 4];
        Array.Fill(pixels, (byte)255);
        LHistogramCounts histogram = THistogram.THistogramCreate(pixels, 2, 2);
        TInterface.TCurveHistogramSet(curve, histogram);
        IReadOnlyList<QCurvePoint> area = TInterface.TCurveHistogramRead(curve, TCurveSize);

        Assert.Equal(258, area.Count);
        Assert.Equal(TCurveSize, area[0].QCurvePointY, 6);
        Assert.Equal(TCurveSize - (TCurveSize * 0.94), area[256].QCurvePointY, 6);
        Assert.Equal(TCurveSize, area[257].QCurvePointX, 6);
    }

    [Fact]
    public void PointCommit_ReadsPercentText_ClampsToUnit()
    {
        QCurve curve = TInterface.TCurveCreate();
        TInterface.TCurvePointAdd(curve, 0.5, 0.5);

        TInterface.TCurvePointCommit(curve, "60", "250");

        Assert.Equal(60, TInterface.TCurveInputRead(curve), 6);
        Assert.Equal(1, TInterface.TCurvePointsRead(curve)[1].LWorkCurveOutput, 6);
    }
}
