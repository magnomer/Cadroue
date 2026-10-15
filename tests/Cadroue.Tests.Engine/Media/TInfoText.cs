using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Engine;

[Collection("Localization")]
public sealed class TInfoText
{
    [Fact]
    public void Shorten_KeepsEightyCharacters_AndMarksTheCut()
    {
        string exact = new('x', 80);

        Assert.Equal(exact, TInterface.TInfoTextShorten(exact));
        Assert.Equal(exact + "…", TInterface.TInfoTextShorten(exact + "yz"));
    }

    [Theory]
    [InlineData(75, "01:15")]
    [InlineData(3661, "01:01:01")]
    [InlineData(0, "00:00")]
    public void Duration_ShowsHoursOnlyFromOneHour(int seconds, string expected) =>
        Assert.Equal(expected, TInterface.TInfoDurationFormat(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData("avc", "H.264")]
    [InlineData(" HEVC ", "H.265")]
    [InlineData("pcm_s16le", "PCM")]
    [InlineData("opus", "OPUS")]
    [InlineData(" ", "")]
    public void Codec_MapsKnownNames_AndRaisesTheRest(string codec, string expected) =>
        Assert.Equal(expected, TInterface.TInfoCodecFormat(codec));

    [Fact]
    public void Channel_NamesMonoAndStereo_AndCountsTheRest()
    {
        Assert.Equal(TInterface.TLocalizationTextRead("Encoder.Value.Mono"), TInterface.TInfoChannelFormat(1));
        Assert.Equal(TInterface.TLocalizationTextRead("Encoder.Value.Stereo"), TInterface.TInfoChannelFormat(2));
        Assert.Equal("6ch", TInterface.TInfoChannelFormat(6));
    }
}
