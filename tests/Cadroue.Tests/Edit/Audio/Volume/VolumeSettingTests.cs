using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class VolumeSettingTests
{
    [Fact]
    public void VolumeGain_AboveMaximum_IsClamped()
    {
        var step = (LWorkVolumeStep)LWorkAudioStep.LWorkVolumeCreate(true, 40);

        Assert.Equal(24, step.LWorkVolumeGain);
    }

    [Fact]
    public void VolumeGain_BelowMinimum_IsClamped()
    {
        var step = (LWorkVolumeStep)LWorkAudioStep.LWorkVolumeCreate(true, -40);

        Assert.Equal(-24, step.LWorkVolumeGain);
    }

    [Fact]
    public void VolumeGain_WithinRange_IsUnchanged()
    {
        var step = (LWorkVolumeStep)LWorkAudioStep.LWorkVolumeCreate(true, 6);

        Assert.Equal(6, step.LWorkVolumeGain);
    }
}
