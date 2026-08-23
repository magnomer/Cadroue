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
        Assert.Equal(50, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorThresholdRead_LuminanceUsesPercentDomain()
    {
        LDetectorBound lBound = LDetector.LDetectorThresholdRead(LDetectorKind.LDetectorKindLuminance);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(50, lBound.LDetectorBoundMost);
        Assert.Equal(10, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorWindowRead_LuminanceUsesSecondsWindow()
    {
        LDetectorBound lBound = LDetector.LDetectorWindowRead(LDetectorKind.LDetectorKindLuminance);

        Assert.Equal(0.1, lBound.LDetectorBoundLeast);
        Assert.Equal(5, lBound.LDetectorBoundMost);
        Assert.Equal(0.5, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorMinimumRead_LuminanceUsesSecondsDuration()
    {
        LDetectorBound lBound = LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindLuminance);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(10, lBound.LDetectorBoundMost);
        Assert.Equal(0.5, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorMinimumRead_SilenceUsesSecondsDuration()
    {
        LDetectorBound lBound = LDetector.LDetectorMinimumRead(LDetectorKind.LDetectorKindSilence);

        Assert.Equal(0, lBound.LDetectorBoundLeast);
        Assert.Equal(60, lBound.LDetectorBoundMost);
        Assert.Equal(0.5, lBound.LDetectorBoundDefault);
    }

    [Fact]
    public void LDetectorThresholdRead_SilenceUsesDecibelDomain()
    {
        LDetectorBound lBound = LDetector.LDetectorThresholdRead(LDetectorKind.LDetectorKindSilence);

        Assert.Equal(-80, lBound.LDetectorBoundLeast);
        Assert.Equal(0, lBound.LDetectorBoundMost);
        Assert.Equal(-30, lBound.LDetectorBoundDefault);
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
    [InlineData(0, 12)]
    [InlineData(50, 7.5)]
    [InlineData(100, 3)]
    public void LDetectorThresholdResolve_MapsSensitivityToScdet(double lSensitivity, double lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorThresholdResolve(lSensitivity), 9);
    }

    [Theory]
    [InlineData(200, 0)]
    [InlineData(-1000, 100)]
    public void LDetectorThresholdResolve_ClampsToScdetLimits(double lSensitivity, double lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorThresholdResolve(lSensitivity), 9);
    }

    [Theory]
    [InlineData(50, 50)]
    [InlineData(200, 400.0 / 3.0)]
    [InlineData(-1000, -8800.0 / 9.0)]
    public void LDetectorSensitivityClamp_HoldsPlausibleBand(double lSensitivity, double lExpected)
    {
        Assert.Equal(lExpected, LDetector.LDetectorSensitivityClamp(lSensitivity), 9);
    }
}
