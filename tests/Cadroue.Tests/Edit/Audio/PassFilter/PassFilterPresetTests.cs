using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class PassFilterPresetTests
{
    [Fact]
    public void HighPassPreset_ExactVoiceSettings_Match()
    {
        Assert.Equal("Voice", LPassband.LPassbandMatch(true, 80, 2, 2, 0.707));
    }

    [Fact]
    public void HighPassPreset_SettingsWithinTolerance_Match()
    {
        Assert.Equal("Voice", LPassband.LPassbandMatch(true, 80.3, 2, 2, 0.707));
    }

    [Fact]
    public void HighPassPreset_CutoffBeyondTolerance_DoesNotMatch()
    {
        Assert.Null(LPassband.LPassbandMatch(true, 81, 2, 2, 0.707));
    }

    [Fact]
    public void LowPassPreset_ExactAirTameSettings_Match()
    {
        Assert.Equal("Air tame", LPassband.LPassbandMatch(false, 16000, 2, 2, 0.707));
    }

    [Fact]
    public void HighPassPreset_UnmatchedSettings_DoNotMatch()
    {
        Assert.Null(LPassband.LPassbandMatch(true, 500, 5, 2, 0.5));
    }

    [Fact]
    public void HighPassPreset_KnownToken_ReturnsPreset()
    {
        LPassbandPreset? preset = LPassband.LPassbandRead(true, "Voice");
        Assert.NotNull(preset);
        Assert.Equal(80, preset!.LPassbandCutoff);
    }

    [Fact]
    public void HighPassPreset_UnknownToken_ReturnsNoPreset()
    {
        Assert.Null(LPassband.LPassbandRead(true, "Nope"));
    }

    [Fact]
    public void HighPassDefault_UsesVoiceSettingsAndIsInactive()
    {
        var step = (LWorkPassStep)LPassband.LPassbandStepCreate(true, false);

        Assert.Equal(80, step.LWorkPassFrequency);
        Assert.Equal(2, step.LWorkPassStages);
        Assert.Equal(2, step.LWorkPassPoles);
        Assert.Equal(0.707, step.LWorkPassResonance);
        Assert.False(step.LWorkStepActive);
    }

    [Fact]
    public void LowPassDefault_UsesAirTameCutoff()
    {
        var step = (LWorkPassStep)LPassband.LPassbandStepCreate(false, false);

        Assert.Equal(16000, step.LWorkPassFrequency);
    }
}
