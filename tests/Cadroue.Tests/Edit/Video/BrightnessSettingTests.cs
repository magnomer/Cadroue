using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class BrightnessSettingTests
{
    [Theory]
    [InlineData(-100, -0.5)]
    [InlineData(-50, -0.25)]
    [InlineData(0, 0)]
    [InlineData(50, 0.25)]
    [InlineData(100, 0.5)]
    public void Brightness_Amount_MapsToFfmpegValue(double amount, double expectedFfmpeg)
    {
        LWorkVideoStep step = TInterface.WorkBrightnessCreate(true, amount);

        Assert.Equal(amount, step.LWorkStepValue);
        Assert.Equal(expectedFfmpeg, step.LWorkFfmpegValue);
    }

    [Theory]
    [InlineData(-250, -1)]
    [InlineData(250, 1)]
    public void Brightness_ManualAmount_IsStoredAndFfmpegValueIsClamped(
        double amount, double expectedFfmpeg)
    {
        LWorkVideoStep step = TInterface.WorkBrightnessCreate(true, amount);

        Assert.Equal(amount, step.LWorkStepValue);
        Assert.Equal(expectedFfmpeg, step.LWorkFfmpegValue);
    }
}
