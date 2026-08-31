using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TWorkVolume
{
    [Fact]
    public void VolumeGain_AboveMaximum_IsClamped()
    {
        var step = (LWorkVolumeStep)TInterface.TWorkVolumeCreate(true, 40);

        Assert.Equal(24, step.LWorkVolumeGain);
    }

    [Fact]
    public void VolumeGain_BelowMinimum_IsClamped()
    {
        var step = (LWorkVolumeStep)TInterface.TWorkVolumeCreate(true, -40);

        Assert.Equal(-24, step.LWorkVolumeGain);
    }

    [Fact]
    public void VolumeGain_WithinRange_IsUnchanged()
    {
        var step = (LWorkVolumeStep)TInterface.TWorkVolumeCreate(true, 6);

        Assert.Equal(6, step.LWorkVolumeGain);
    }
}
