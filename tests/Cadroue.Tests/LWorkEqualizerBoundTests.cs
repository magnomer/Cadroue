using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LWorkEqualizerBoundTests
{
    [Fact]
    public void EqualizerCreate_AboveRange_ClampsToBound()
    {
        var step = (LWorkEqualizerStep)LWorkAudioStep.LWorkEqualizerCreate(
            true, new[] { new LWorkBand(5, 99) });

        Assert.Equal(20, step.LWorkEqualizerBands[0].LWorkBandFrequency);
        Assert.Equal(12, step.LWorkEqualizerBands[0].LWorkBandGain);
    }

    [Fact]
    public void EqualizerCreate_BelowRange_ClampsToBound()
    {
        var step = (LWorkEqualizerStep)LWorkAudioStep.LWorkEqualizerCreate(
            true, new[] { new LWorkBand(999999, -99) });

        Assert.Equal(20000, step.LWorkEqualizerBands[0].LWorkBandFrequency);
        Assert.Equal(-12, step.LWorkEqualizerBands[0].LWorkBandGain);
    }
}
