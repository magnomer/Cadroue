using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TDetector
{
    [Fact]
    public void LDetectorThresholdRead_SceneUsesScdetDomain()
    {
        LDetectorBound lBound = LDetector.LDetectorThresholdRead(LDetectorKind.LDetectorKindScene);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(100, lBound.LDetectorBoundMost);
        Assert.Equal(10, lBound.LDetectorBoundDefault);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(50, 50)]
    [InlineData(150, 100)]
    public void LDetectorThresholdClamp_SceneHoldsRange(double lValue, double lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorThresholdClamp(LDetectorKind.LDetectorKindScene, lValue));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(1.0)]
    public void LDetectorThresholdResolve_RoundTripsFromPosition(double lPosition)
    {
        double lThreshold = LDetector.LDetectorPositionResolve(lPosition);
        double lRoundTrip = LDetector.LDetectorThresholdResolve(lThreshold);

        Assert.Equal(lPosition, lRoundTrip, 9);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(14)]
    public void LDetectorPositionResolve_RoundTripsThroughUsefulBand(double lThreshold)
    {
        double lPosition = LDetector.LDetectorThresholdResolve(lThreshold);
        double lRoundTrip = LDetector.LDetectorPositionResolve(lPosition);

        Assert.Equal(lThreshold, lRoundTrip, 9);
    }

    [Fact]
    public void LDetectorPositionResolve_MapsEndpointsToDomain()
    {
        Assert.Equal(0, LDetector.LDetectorPositionResolve(0.0), 9);
        Assert.Equal(100, LDetector.LDetectorPositionResolve(1.0), 9);
        Assert.Equal(0, LDetector.LDetectorThresholdResolve(0.0), 9);
        Assert.Equal(1, LDetector.LDetectorThresholdResolve(100.0), 9);
    }
}
