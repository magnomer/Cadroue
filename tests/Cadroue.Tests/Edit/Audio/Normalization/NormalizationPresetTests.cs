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
        (double Target, double Peak, double Range)? preset = TInterface.LevelingLoudnessRead(token);
        Assert.NotNull(preset);
        Assert.Equal(token, TInterface.LevelingLoudnessMatch(preset!.Value.Target, preset.Value.Peak, preset.Value.Range));
    }

    [Theory]
    [InlineData("Gentle")]
    [InlineData("Leveler")]
    [InlineData("Voice")]
    [InlineData("Aggressive")]
    [InlineData("Music")]
    public void DynamicPreset_KnownToken_RoundTrips(string token)
    {
        (double Frame, double Gauss, double MaxGain, double Compress)? preset = TInterface.LevelingDynamicRead(token);
        Assert.NotNull(preset);
        Assert.Equal(token, TInterface.LevelingDynamicMatch(preset!.Value.Frame, preset.Value.Gauss, preset.Value.MaxGain, preset.Value.Compress));
    }

    [Fact]
    public void LoudnessPreset_SettingsOutsideTolerance_AreNotMatched()
    {
        Assert.Null(TInterface.LevelingLoudnessMatch(-9.5, -1, 6));
    }

    [Fact]
    public void DynamicPreset_SettingsOutsideTolerance_AreNotMatched()
    {
        Assert.Null(TInterface.LevelingDynamicMatch(500, 31, 7.5, 0));
    }

    [Fact]
    public void LoudnessPreset_UnknownToken_ReturnsNull()
    {
        Assert.Null(TInterface.LevelingLoudnessRead("Nope"));
    }

    [Fact]
    public void DynamicPreset_UnknownToken_ReturnsNull()
    {
        Assert.Null(TInterface.LevelingDynamicRead("Nope"));
    }

    [Fact]
    public void LevelingDefaultRead_ReturnsCanonicalStep()
    {
        var (target, peak, range, twoPass, frame, gauss, maxGain, compress) =
            TInterface.LevelingDefaultRead();

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
