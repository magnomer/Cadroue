using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class SaturationSettingTests
{
    [Theory]
    [InlineData(-10, 0)]
    [InlineData(50, 50)]
    [InlineData(500, 200)]
    public void Saturation_OutOfRange_IsClampedToBounds(double lStepValue, double lExpected)
    {
        var step = TInterface.WorkSaturationCreate(true, lStepValue);

        Assert.Equal(lExpected, step.LWorkStepValue);
    }

    [Fact]
    public void Saturation_ActiveAndValue_RoundTrip()
    {
        var step = TInterface.WorkSaturationCreate(true, 120);

        Assert.True(step.LWorkStepActive);
        Assert.Equal(LColorKind.LColorKindSaturation, step.LWorkStepKind);
        Assert.Equal(120, step.LWorkStepValue);
    }

    [Fact]
    public void Saturation_FfmpegValue_IsValueOverHundred()
    {
        var step = TInterface.WorkSaturationCreate(true, 150);

        Assert.Equal(1.5, step.LWorkFfmpegValue);
    }
}
