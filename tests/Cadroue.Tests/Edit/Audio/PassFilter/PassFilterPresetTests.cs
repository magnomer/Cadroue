using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class PassFilterPresetTests
{
    [Fact]
    public void HighPassPreset_ExactVoiceSettings_Match()
    {
        Assert.Equal("Voice", TInterface.PassbandMatch(true, 80, 2, 2, 0.707));
    }

    [Fact]
    public void HighPassPreset_SettingsWithinTolerance_Match()
    {
        Assert.Equal("Voice", TInterface.PassbandMatch(true, 80.3, 2, 2, 0.707));
    }

    [Fact]
    public void HighPassPreset_CutoffBeyondTolerance_DoesNotMatch()
    {
        Assert.Null(TInterface.PassbandMatch(true, 81, 2, 2, 0.707));
    }

    [Fact]
    public void LowPassPreset_ExactAirTameSettings_Match()
    {
        Assert.Equal("Air tame", TInterface.PassbandMatch(false, 16000, 2, 2, 0.707));
    }

    [Fact]
    public void HighPassPreset_UnmatchedSettings_DoNotMatch()
    {
        Assert.Null(TInterface.PassbandMatch(true, 500, 5, 2, 0.5));
    }

    [Fact]
    public void HighPassPreset_KnownToken_ReturnsPreset()
    {
        LPassbandPreset? preset = TInterface.PassbandRead(true, "Voice");
        Assert.NotNull(preset);
        Assert.Equal(80, preset!.LPassbandCutoff);
    }

    [Fact]
    public void HighPassPreset_UnknownToken_ReturnsNoPreset()
    {
        Assert.Null(TInterface.PassbandRead(true, "Nope"));
    }

    [Fact]
    public void HighPassDefault_UsesVoiceSettingsAndIsInactive()
    {
        var step = (LWorkPassStep)TInterface.PassbandStepCreate(true, false);

        Assert.Equal(80, step.LWorkPassFrequency);
        Assert.Equal(2, step.LWorkPassStages);
        Assert.Equal(2, step.LWorkPassPoles);
        Assert.Equal(0.707, step.LWorkPassResonance);
        Assert.False(step.LWorkStepActive);
    }

    [Fact]
    public void LowPassDefault_UsesAirTameCutoff()
    {
        var step = (LWorkPassStep)TInterface.PassbandStepCreate(false, false);

        Assert.Equal(16000, step.LWorkPassFrequency);
    }
}
