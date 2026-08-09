using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class NormalizationPresetTests
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
    public void LoudnessPreset_KnownToken_RoundTrips(string token)
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
    public void DynamicPreset_KnownToken_RoundTrips(string token)
    {
        (double Frame, double Gauss, double MaxGain, double Compress)? preset = LLevelingCatalog.LLevelingDynamicRead(token);
        Assert.NotNull(preset);
        Assert.Equal(token, LLevelingCatalog.LLevelingDynamicMatch(preset!.Value.Frame, preset.Value.Gauss, preset.Value.MaxGain, preset.Value.Compress));
    }

    [Fact]
    public void LoudnessPreset_SettingsOutsideTolerance_AreNotMatched()
    {
        Assert.Null(LLevelingCatalog.LLevelingLoudnessMatch(-9.5, -1, 6));
    }

    [Fact]
    public void DynamicPreset_SettingsOutsideTolerance_AreNotMatched()
    {
        Assert.Null(LLevelingCatalog.LLevelingDynamicMatch(500, 31, 7.5, 0));
    }

    [Fact]
    public void LoudnessPreset_UnknownToken_ReturnsNull()
    {
        Assert.Null(LLevelingCatalog.LLevelingLoudnessRead("Nope"));
    }

    [Fact]
    public void DynamicPreset_UnknownToken_ReturnsNull()
    {
        Assert.Null(LLevelingCatalog.LLevelingDynamicRead("Nope"));
    }

    [Fact]
    public void LevelingDefaultRead_ReturnsCanonicalStep()
    {
        var (target, peak, range, twoPass, frame, gauss, maxGain, compress) =
            LLevelingCatalog.LLevelingDefaultRead();

        Assert.Equal(-21, target);
        Assert.Equal(-2, peak);
        Assert.Equal(6, range);
        Assert.True(twoPass);
        Assert.Equal(300, frame);
        Assert.Equal(21, gauss);
        Assert.Equal(10, maxGain);
        Assert.Equal(6, compress);
    }
}
