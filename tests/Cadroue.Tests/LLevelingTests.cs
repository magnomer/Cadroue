using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LLevelingTests
{
    [Theory]
    [InlineData("Loud")]
    [InlineData("Streaming")]
    [InlineData("Podcast")]
    [InlineData("Dialogue")]
    [InlineData("Audiobook")]
    [InlineData("Broadcast")]
    [InlineData("TV")]
    [InlineData("Film")]
    public void Loudness_PresetRoundTrips(string token)
    {
        (double Target, double Peak, double Range)? preset = LLevelingCatalog.LLevelingLoudnessRead(token);
        Assert.NotNull(preset);
        Assert.Equal(token, LLevelingCatalog.LLevelingLoudnessMatch(preset!.Value.Target, preset.Value.Peak, preset.Value.Range));
    }

    [Theory]
    [InlineData("Gentle")]
    [InlineData("Leveler")]
    [InlineData("Voice")]
    [InlineData("Aggressive")]
    [InlineData("Music")]
    public void Dynamic_PresetRoundTrips(string token)
    {
        (double Frame, double Gauss, double MaxGain, double Compress)? preset = LLevelingCatalog.LLevelingDynamicRead(token);
        Assert.NotNull(preset);
        Assert.Equal(token, LLevelingCatalog.LLevelingDynamicMatch(preset!.Value.Frame, preset.Value.Gauss, preset.Value.MaxGain, preset.Value.Compress));
    }

    [Fact]
    public void Loudness_OffTolerance_ReturnsNull()
    {
        Assert.Null(LLevelingCatalog.LLevelingLoudnessMatch(-9.5, -1, 6));
    }

    [Fact]
    public void Dynamic_OffTolerance_ReturnsNull()
    {
        Assert.Null(LLevelingCatalog.LLevelingDynamicMatch(500, 31, 7.5, 0));
    }

    [Fact]
    public void Loudness_UnknownToken_ReturnsNull()
    {
        Assert.Null(LLevelingCatalog.LLevelingLoudnessRead("Nope"));
    }

    [Fact]
    public void Dynamic_UnknownToken_ReturnsNull()
    {
        Assert.Null(LLevelingCatalog.LLevelingDynamicRead("Nope"));
    }
}
