using Xunit;

namespace Cadroue.Tests;

[Collection("MediaProbe")]
public sealed class MediaProbeTests
{
    [Fact]
    public void VideoAndAudioOutput_ProducesDurationAndBothStreams()
    {
        var info = TScout.ProbeParse(ProbeOutput(
            "12.5",
            """{"codec_type":"video","codec_name":"h264","width":1920,"height":1080,"r_frame_rate":"30000/1001","duration":"12.4"}""",
            """{"codec_type":"audio","codec_name":"aac","sample_rate":"48000","channels":2,"bit_rate":"192000"}"""));

        Assert.Equal(TimeSpan.FromSeconds(12.5), info.LMediaInfoDuration);
        Assert.Equal(TimeSpan.FromSeconds(12.4), info.LMediaVideoDuration);
        Assert.True(info.LMediaVideoPresent);
        Assert.True(info.LMediaAudioPresent);
        Assert.Equal(1920, info.LMediaVideoWidth);
        Assert.Equal(1080, info.LMediaVideoHeight);
        Assert.Equal(30000d / 1001d, info.LMediaVideoRate, 6);
        Assert.Equal("h264", info.LMediaVideoCodec);
        Assert.Equal("aac", info.LMediaAudioCodec);
    }

    [Fact]
    public void AudioOnlyOutput_IsIdentifiedCorrectly()
    {
        var info = TScout.ProbeParse(ProbeOutput(
            "3.25",
            """{"codec_type":"audio","codec_name":"flac","sample_rate":"44100","channels":2}"""));

        Assert.True(info.LMediaAudioOnly);
        Assert.False(info.LMediaVideoPresent);
        Assert.True(info.LMediaAudioPresent);
        Assert.Equal(0, info.LMediaVideoWidth);
        Assert.Equal("flac", info.LMediaAudioCodec);
    }

    [Fact]
    public void VideoOnlyOutput_IsIdentifiedCorrectly()
    {
        var info = TScout.ProbeParse(ProbeOutput(
            "8",
            """{"codec_type":"video","codec_name":"vp9","width":1280,"height":720,"avg_frame_rate":"24/1"}"""));

        Assert.True(info.LMediaVideoPresent);
        Assert.False(info.LMediaAudioPresent);
        Assert.False(info.LMediaAudioOnly);
        Assert.Equal(string.Empty, info.LMediaAudioCodec);
        Assert.Equal(0, info.LMediaSampleRate);
        Assert.Equal(0, info.LMediaAudioChannels);
    }

    [Fact]
    public void MissingOptionalMetadata_UsesOnlyUnknownOrEmptyStates()
    {
        var info = TScout.ProbeParse(ProbeOutput(
            "1",
            """{"codec_type":"video","width":640,"height":360}"""));

        Assert.Equal("unknown", info.LMediaVideoCodec);
        Assert.Equal(0, info.LMediaVideoRate);
        Assert.Equal(0, info.LMediaAudioBitrate);
        Assert.False(info.LMediaAudioPresent);
    }

    [Fact]
    public void PacketOutput_ResolvesLastPresentationTimestamp()
    {
        TimeSpan? end = TScout.ProbeEndParse(
            "9600.000000\n9599.933267\n9600.033367\nN/A\n",
            TimeSpan.FromMilliseconds(33.367));

        Assert.Equal(TimeSpan.FromSeconds(9600), end);
    }

    [Fact]
    public void PacketOutput_WithoutTimestampsHasNoEnd()
    {
        Assert.Null(TScout.ProbeEndParse("N/A\n\n", TimeSpan.Zero));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"format\":{\"duration\":\"1\"},\"streams\":{}}")]
    public void InvalidOrMalformedOutput_FailsSafely(string output)
    {
        Assert.ThrowsAny<Exception>(() => TScout.ProbeParse(output));
    }

    internal static string ProbeOutput(string duration, params string[] streams) =>
        $$"""
        {"format":{"duration":"{{duration}}"},"streams":[{{string.Join(',', streams)}}]}
        """;
}
