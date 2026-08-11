using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class GammaSettingTests
{
    [Theory]
    [InlineData(-150, -100, 0.1)]
    [InlineData(-50, -50, 0.31622776601683794)]
    [InlineData(0, 0, 1)]
    [InlineData(50, 50, 3.1622776601683795)]
    [InlineData(150, 100, 10)]
    public void Gamma_Value_IsClampedAndMappedForFfmpeg(
        double value, double expectedDisplay, double expectedFfmpeg)
    {
        LWorkVideoStep step = TInterface.WorkGammaCreate(true, value);

        Assert.Equal(expectedDisplay, step.LWorkStepValue);
        Assert.Equal(expectedFfmpeg, step.LWorkFfmpegValue, 12);
    }

    [Fact]
    public void Gamma_Capability_GatesEffectiveWorkWithoutChangingStoredStep()
    {
        LWorkVideoStep gamma = TInterface.WorkGammaCreate(true, 50);

        LWorkVideo mpv = TInterface.EditVideoCreate(new[] { gamma }, true);
        LWorkVideo flyleaf = TInterface.EditVideoCreate(new[] { gamma }, false);

        Assert.Same(gamma, Assert.Single(mpv.LWorkVideoSteps));
        Assert.Empty(flyleaf.LWorkVideoSteps);
        Assert.True(gamma.LWorkStepActive);
        Assert.Equal(50, gamma.LWorkStepValue);
    }

    [Fact]
    public void Gamma_Factory_ClampsCompletePayloadAndKeepsGlobalCompatibilityValue()
    {
        LWorkVideoStep step = TInterface.WorkGammaCreate(true, -150, -120, 25.5, 150, 120);

        Assert.Equal(-100, step.LWorkStepValue);
        Assert.NotNull(step.LWorkStepGamma);
        Assert.Equal(-100, step.LWorkStepGamma.LWorkGammaGlobal);
        Assert.Equal(-100, step.LWorkStepGamma.LWorkGammaRed);
        Assert.Equal(25.5, step.LWorkStepGamma.LWorkGammaGreen);
        Assert.Equal(100, step.LWorkStepGamma.LWorkGammaBlue);
        Assert.Equal(100, step.LWorkStepGamma.LWorkGammaHighlightProtection);
    }

    [Fact]
    public void BrightnessAndContrast_HaveNoGammaPayload()
    {
        Assert.Null(TInterface.WorkBrightnessCreate(true, 10).LWorkStepGamma);
        Assert.Null(TInterface.WorkContrastCreate(true, 110).LWorkStepGamma);
    }

    [Fact]
    public void Gamma_Diagnostic_ListsOnlyNonNeutralAdvancedValues()
    {
        LWorkVideoStep neutral = TInterface.WorkGammaCreate(true, 20, 0, 0, 0, 0);
        LWorkVideoStep advanced = TInterface.WorkGammaCreate(true, 20, -10, 10, 0, 25);

        Assert.Equal("LColorKindGamma 20", TInterface.WorkVideoStepDiagnosticRead(neutral));
        Assert.Equal(
            "LColorKindGamma 20 (red -10, green 10, highlight 25)",
            TInterface.WorkVideoStepDiagnosticRead(advanced));
    }
}
