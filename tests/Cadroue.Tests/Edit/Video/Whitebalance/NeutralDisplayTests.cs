using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class NeutralDisplayTests
{
    [Fact]
    public void Display_ManualWithSample_ShowsClampedSampledRgb()
    {
        LNeutralDisplay display = TNeutral.DisplayResolve(true, 210, 180, 170);

        Assert.True(display.LNeutralDisplayVisible);
        Assert.True(display.LNeutralDisplaySampled);
        Assert.Equal(210, display.LNeutralDisplayRed);
        Assert.Equal(180, display.LNeutralDisplayGreen);
        Assert.Equal(170, display.LNeutralDisplayBlue);
    }

    [Fact]
    public void Display_ManualWithoutSample_VisibleButEmpty()
    {
        LNeutralDisplay display = TNeutral.DisplayResolve(true, 0, 0, 0);

        Assert.True(display.LNeutralDisplayVisible);
        Assert.False(display.LNeutralDisplaySampled);
        Assert.Equal(0, display.LNeutralDisplayRed);
        Assert.Equal(0, display.LNeutralDisplayGreen);
        Assert.Equal(0, display.LNeutralDisplayBlue);
    }

    [Fact]
    public void Display_AutomaticMethod_CollapsesGroupEvenWithRememberedSample()
    {
        LNeutralDisplay display = TNeutral.DisplayResolve(false, 210, 180, 170);

        Assert.False(display.LNeutralDisplayVisible);
        Assert.False(display.LNeutralDisplaySampled);
        Assert.Equal(0, display.LNeutralDisplayRed);
        Assert.Equal(0, display.LNeutralDisplayGreen);
        Assert.Equal(0, display.LNeutralDisplayBlue);
    }

    [Fact]
    public void Display_ManualRoundTripFromAutomatic_RestoresRememberedSample()
    {
        // Switching Manual -> automatic keeps the sample in memory; switching back
        // to Manual must render the remembered display from the same values.
        const int red = 120;
        const int green = 130;
        const int blue = 140;

        LNeutralDisplay automatic = TNeutral.DisplayResolve(false, red, green, blue);
        LNeutralDisplay manual = TNeutral.DisplayResolve(true, red, green, blue);

        Assert.False(automatic.LNeutralDisplayVisible);
        Assert.True(manual.LNeutralDisplayVisible);
        Assert.True(manual.LNeutralDisplaySampled);
        Assert.Equal(red, manual.LNeutralDisplayRed);
        Assert.Equal(green, manual.LNeutralDisplayGreen);
        Assert.Equal(blue, manual.LNeutralDisplayBlue);
    }

    [Theory]
    [InlineData(-5, 256, 300, 0, 255, 255)]
    [InlineData(1, 2, 3, 1, 2, 3)]
    public void Display_ClampsSampleChannelsToByteRange(
        int red, int green, int blue, int expectedRed, int expectedGreen, int expectedBlue)
    {
        LNeutralDisplay display = TNeutral.DisplayResolve(true, red, green, blue);

        Assert.True(display.LNeutralDisplaySampled);
        Assert.Equal(expectedRed, display.LNeutralDisplayRed);
        Assert.Equal(expectedGreen, display.LNeutralDisplayGreen);
        Assert.Equal(expectedBlue, display.LNeutralDisplayBlue);
    }
}
