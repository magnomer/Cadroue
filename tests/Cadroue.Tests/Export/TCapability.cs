using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TCapability
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no_such_encoder")]
    public void VideoRead_UnknownEncoder_OffersAtLeastOneMode(string? encoder)
    {
        LCapabilityCodec codec = TInterface.TCapabilityRead(encoder);

        Assert.NotEmpty(codec.LCapabilityModeLabels);
        Assert.Equal(
            codec.LCapabilityModeLabels[0], TInterface.TCapabilityModeFind(codec, "missing").LCapabilityModeLabel);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no_such_encoder")]
    public void AudioRead_UnknownEncoder_OffersAtLeastOneMode(string? encoder)
    {
        LCapabilityCodec codec = TInterface.TCapabilityAudioRead(encoder);

        Assert.NotEmpty(codec.LCapabilityModeLabels);
        Assert.Equal(
            codec.LCapabilityModeLabels[0], TInterface.TCapabilityModeFind(codec, "missing").LCapabilityModeLabel);
    }

    [Fact]
    public void EveryTableEntry_OffersAtLeastOneMode()
    {
        foreach (LCapabilityCodec codec in TInterface.TCapabilityTableRead())
        {
            Assert.NotEmpty(codec.LCapabilityModeLabels);
        }
    }
}
