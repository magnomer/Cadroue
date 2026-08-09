using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class PassFilterSettingTests
{
    [Fact]
    public void HighPassSettings_AboveMaximum_AreClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkHighCreate(true, 80, 12, 3, 5);

        Assert.Equal(8, step.LWorkPassStages);
        Assert.Equal(2, step.LWorkPassPoles);
        Assert.Equal(2, step.LWorkPassResonance);
    }

    [Fact]
    public void LowPassSettings_BelowMinimum_AreClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkLowCreate(true, 9000, 0, 0, 0);

        Assert.Equal(1, step.LWorkPassStages);
        Assert.Equal(1, step.LWorkPassPoles);
        Assert.Equal(0.1, step.LWorkPassResonance);
    }

    [Fact]
    public void HighPassStages_BelowMinimum_IsClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkHighCreate(true, 80, 0, 2, 0.707);

        Assert.Equal(1, step.LWorkPassStages);
    }

    [Fact]
    public void HighPassResonance_BelowMinimum_IsClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkHighCreate(true, 80, 2, 2, 0);

        Assert.Equal(0.1, step.LWorkPassResonance);
    }

    [Fact]
    public void LowPassStages_AboveMaximum_IsClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkLowCreate(true, 9000, 20, 2, 0.707);

        Assert.Equal(8, step.LWorkPassStages);
    }

    [Fact]
    public void LowPassResonance_AboveMaximum_IsClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkLowCreate(true, 9000, 2, 2, 5);

        Assert.Equal(2, step.LWorkPassResonance);
    }

    [Fact]
    public void HighPassCutoff_BelowMinimum_IsClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkHighCreate(true, 5, 2, 2, 0.707);

        Assert.Equal(20, step.LWorkPassFrequency);
    }

    [Fact]
    public void HighPassCutoff_AboveMaximum_IsClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkHighCreate(true, 9999, 2, 2, 0.707);

        Assert.Equal(300, step.LWorkPassFrequency);
    }

    [Fact]
    public void LowPassCutoff_BelowMinimum_IsClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkLowCreate(true, 100, 2, 2, 0.707);

        Assert.Equal(3000, step.LWorkPassFrequency);
    }

    [Fact]
    public void LowPassCutoff_AboveMaximum_IsClamped()
    {
        var step = (LWorkPassStep)LWorkAudioStep.LWorkLowCreate(true, 99999, 2, 2, 0.707);

        Assert.Equal(20000, step.LWorkPassFrequency);
    }
}
