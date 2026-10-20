using System.Collections.Generic;

using Cadroue.Conduct;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TEncoderSpeed
{
    [Fact]
    public void AudioSpeed_EmptyChoices_DoesNotThrow()
    {
        LCapabilitySpeed capability = new("Compression level", "-compression_level", string.Empty, []);
        var speed = new CSEncoderSpeed(
            capability,
            () => (string.Empty, string.Empty, string.Empty, new Dictionary<string, string>()),
            string.Empty);

        Assert.Equal(string.Empty, speed.CSEncoderSpeedShown);
        Assert.Equal(string.Empty, speed.CSEncoderSpeedFormat(0));
        Assert.Equal(string.Empty, speed.CSEncoderSpeedRead(0));
    }
}
