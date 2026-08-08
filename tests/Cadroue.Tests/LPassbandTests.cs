using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LPassbandTests
{
    [Fact]
    public void Match_HighVoiceExact_ReturnsVoice()
    {
        Assert.Equal("Voice", LPassband.LPassbandMatch(true, 80, 2, 2, 0.707));
    }

    [Fact]
    public void Match_HighVoiceWithinTolerance_ReturnsVoice()
    {
        Assert.Equal("Voice", LPassband.LPassbandMatch(true, 80.3, 2, 2, 0.707));
    }

    [Fact]
    public void Match_HighVoiceOneHzOff_ReturnsNull()
    {
        Assert.Null(LPassband.LPassbandMatch(true, 81, 2, 2, 0.707));
    }

    [Fact]
    public void Match_LowAirTameExact_ReturnsAirTame()
    {
        Assert.Equal("Air tame", LPassband.LPassbandMatch(false, 16000, 2, 2, 0.707));
    }

    [Fact]
    public void Match_UnmatchedValues_ReturnsNull()
    {
        Assert.Null(LPassband.LPassbandMatch(true, 500, 5, 2, 0.5));
    }

    [Fact]
    public void Read_HighKnownToken_ReturnsPreset()
    {
        LPassbandPreset? preset = LPassband.LPassbandRead(true, "Voice");
        Assert.NotNull(preset);
        Assert.Equal(80, preset!.LPassbandCutoff);
    }

    [Fact]
    public void Read_UnknownToken_ReturnsNull()
    {
        Assert.Null(LPassband.LPassbandRead(true, "Nope"));
    }
}
