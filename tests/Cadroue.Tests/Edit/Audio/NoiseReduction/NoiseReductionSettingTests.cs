using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class NoiseReductionSettingTests
{
    [Fact]
    public void NoiseReductionAmount_AboveMaximum_IsClamped()
    {
        var step = (LWorkNoiseStep)LWorkAudioStep.LWorkNoiseCreate(
            true, 99, -50, false, LGrain.LGrainWhite, 6, 0.5, -38);

        Assert.Equal(30, step.LWorkNoiseReduction);
    }

    [Fact]
    public void NoiseFloor_BelowMinimum_ClampsFloorAndResidual()
    {
        var step = (LWorkNoiseStep)LWorkAudioStep.LWorkNoiseCreate(
            true, 12, -90, false, LGrain.LGrainWhite, 6, 0.5, -5);

        Assert.Equal(-80, step.LWorkNoiseFloor);
        Assert.Equal(-20, step.LWorkNoiseResidual);
    }

    [Fact]
    public void SmoothAndAdaptivity_AboveMaximum_AreClamped()
    {
        var step = (LWorkNoiseStep)LWorkAudioStep.LWorkNoiseCreate(
            true, 12, -50, false, LGrain.LGrainWhite, 99, 2, -38);

        Assert.Equal(50, step.LWorkNoiseSmooth);
        Assert.Equal(1, step.LWorkNoiseAdaptivity);
    }

    [Fact]
    public void NoiseReductionSettings_WithinRange_PassThrough()
    {
        var step = (LWorkNoiseStep)LWorkAudioStep.LWorkNoiseCreate(
            true, 20, -50, false, LGrain.LGrainWhite, 6, 0.5, -38);

        Assert.Equal(20, step.LWorkNoiseReduction);
        Assert.Equal(-50, step.LWorkNoiseFloor);
        Assert.Equal(6, step.LWorkNoiseSmooth);
        Assert.Equal(0.5, step.LWorkNoiseAdaptivity);
        Assert.Equal(-38, step.LWorkNoiseResidual);
    }
}
