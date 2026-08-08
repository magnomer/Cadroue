using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LWorkVolumeBoundTests
{
    [Fact]
    public void VolumeCreate_AboveRange_ClampsToBound()
    {
        var step = (LWorkVolumeStep)LWorkAudioStep.LWorkVolumeCreate(true, 40);

        Assert.Equal(24, step.LWorkVolumeGain);
    }

    [Fact]
    public void VolumeCreate_BelowRange_ClampsToBound()
    {
        var step = (LWorkVolumeStep)LWorkAudioStep.LWorkVolumeCreate(true, -40);

        Assert.Equal(-24, step.LWorkVolumeGain);
    }

    [Fact]
    public void VolumeCreate_InRange_Unchanged()
    {
        var step = (LWorkVolumeStep)LWorkAudioStep.LWorkVolumeCreate(true, 6);

        Assert.Equal(6, step.LWorkVolumeGain);
    }
}
