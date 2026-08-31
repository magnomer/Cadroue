using Xunit;

namespace Cadroue.Tests;

public sealed class TInventoryVersion
{
    [Theory]
    [InlineData("ffmpeg version 7.1.1 Copyright (c) FFmpeg developers", "7.1.1")]
    [InlineData("FFmpeg version N-118225-g03a8e121e2 Copyright (c) FFmpeg developers", "N-118225-g03a8e121e2")]
    [InlineData("\r\n  ffmpeg version 6.0-static  \r\nconfiguration: --enable-gpl", "6.0-static")]
    public void RecognizableFfmpegBanner_ReturnsVersion(string output, string expected)
    {
        Assert.Equal(expected, TInventory.TInventoryVersionParse(output));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \r\n")]
    [InlineData("not ffmpeg")]
    [InlineData("wrapper output: ffmpeg version 7.1.1")]
    [InlineData("ffmpeg version ")]
    public void UnrecognizedOutput_IsRejected(string output)
    {
        Assert.Empty(TInventory.TInventoryVersionParse(output));
    }
}
