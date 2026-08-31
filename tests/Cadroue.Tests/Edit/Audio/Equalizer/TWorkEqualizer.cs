using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TWorkEqualizer
{
    [Fact]
    public void EqualizerSetting_FrequencyBelowMinimumAndGainAboveMaximum_AreClamped()
    {
        var step = (LWorkEqualizerStep)TInterface.TWorkEqualizerCreate(
            true, new[] { TInterface.TWorkBandCreate(5, 99) });

        Assert.Equal(20, step.LWorkEqualizerBands[0].LWorkBandFrequency);
        Assert.Equal(12, step.LWorkEqualizerBands[0].LWorkBandGain);
    }

    [Fact]
    public void EqualizerSetting_FrequencyAboveMaximumAndGainBelowMinimum_AreClamped()
    {
        var step = (LWorkEqualizerStep)TInterface.TWorkEqualizerCreate(
            true, new[] { TInterface.TWorkBandCreate(999999, -99) });

        Assert.Equal(20000, step.LWorkEqualizerBands[0].LWorkBandFrequency);
        Assert.Equal(-12, step.LWorkEqualizerBands[0].LWorkBandGain);
    }
}
