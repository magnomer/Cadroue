using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class TLevelingPreset
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
        LLevelingLoudnessPreset? preset = TInterface.TLevelingLoudnessRead(token);
        Assert.NotNull(preset);
        Assert.Equal(token, TInterface.TLevelingLoudnessMatch(preset!.LLevelingTarget, preset.LLevelingPeak, preset.LLevelingRange));
    }

    [Theory]
    [InlineData("Gentle")]
    [InlineData("Leveler")]
    [InlineData("Voice")]
    [InlineData("Aggressive")]
    [InlineData("Music")]
    public void DynamicPreset_KnownToken_RoundTrips(string token)
    {
        LLevelingDynamicPreset? preset = TInterface.TLevelingDynamicRead(token);
        Assert.NotNull(preset);
        Assert.Equal(token, TInterface.TLevelingDynamicMatch(preset!.LLevelingFrame, preset.LLevelingGauss, preset.LLevelingMaxGain, preset.LLevelingCompress));
    }

    [Fact]
    public void LoudnessPreset_SettingsOutsideTolerance_AreNotMatched()
    {
        Assert.Null(TInterface.TLevelingLoudnessMatch(-9.5, -1, 6));
    }

    [Fact]
    public void DynamicPreset_SettingsOutsideTolerance_AreNotMatched()
    {
        Assert.Null(TInterface.TLevelingDynamicMatch(500, 31, 7.5, 0));
    }

    [Fact]
    public void LoudnessPreset_UnknownToken_ReturnsNull()
    {
        Assert.Null(TInterface.TLevelingLoudnessRead("Nope"));
    }

    [Fact]
    public void DynamicPreset_UnknownToken_ReturnsNull()
    {
        Assert.Null(TInterface.TLevelingDynamicRead("Nope"));
    }

    [Fact]
    public void LevelingDefaultRead_ReturnsCanonicalStep()
    {
        LLevelingDefault preset = TInterface.TLevelingDefaultRead();

        Assert.Equal(-21, preset.LLevelingTarget);
        Assert.Equal(-2, preset.LLevelingPeak);
        Assert.Equal(6, preset.LLevelingRange);
        Assert.True(preset.LLevelingTwoPass);
        Assert.Equal(300, preset.LLevelingFrame);
        Assert.Equal(21, preset.LLevelingGauss);
        Assert.Equal(10, preset.LLevelingMaxGain);
        Assert.Equal(6, preset.LLevelingCompress);
    }
}
