using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TExposure
{
    [Theory]
    [InlineData(-10, -3)]
    [InlineData(1.5, 1.5)]
    [InlineData(10, 3)]
    public void Exposure_OutOfRange_IsClampedToStops(double lStepValue, double lExpected)
    {
        var step = TInterface.TWorkExposureCreate(true, lStepValue);

        Assert.Equal(lExpected, step.LWorkStepValue);
    }

    [Fact]
    public void Exposure_ActiveAndValue_RoundTrip()
    {
        var step = TInterface.TWorkExposureCreate(true, 1.5);

        Assert.True(step.LWorkStepActive);
        Assert.Equal(LColorKind.LColorKindExposure, step.LWorkStepKind);
        Assert.Equal(1.5, step.LWorkStepValue);
    }

    [Fact]
    public void Exposure_FfmpegValue_IsRawStops()
    {
        var step = TInterface.TWorkExposureCreate(true, 1.5);

        Assert.Equal(1.5, step.LWorkFfmpegValue);
    }
}
