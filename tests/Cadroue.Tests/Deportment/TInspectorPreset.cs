using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorPreset
{
    [Fact]
    public void NoisePreset_RoundTripsThroughValues()
    {
        LNoise noise = TInterface.TNoiseCreate();

        TInterface.TNoisePresetSelect(noise, "Strong");

        Assert.Equal("Strong", TInterface.TNoiseTokenRead(noise));
        Assert.Equal("Strong", TInterface.TNoiseMatchRead(noise));
        Assert.Equal(24, TInterface.TNoiseStepRead(noise).LWorkNoiseReduction);
    }

    [Fact]
    public void NoiseValueDrift_KeepsBaseToken_LosesMatch()
    {
        LNoise noise = TInterface.TNoiseCreate();
        TInterface.TNoisePresetSelect(noise, "Strong");

        TInterface.TNoiseValueSet(noise, 0, 20);

        Assert.Equal("Strong", TInterface.TNoiseTokenRead(noise));
        Assert.Null(TInterface.TNoiseMatchRead(noise));

        TInterface.TNoiseValueSet(noise, 0, 24);

        Assert.Equal("Strong", TInterface.TNoiseMatchRead(noise));
    }

    [Fact]
    public void NoiseStepApply_TakesMatchAsToken()
    {
        LNoise noise = TInterface.TNoiseCreate();

        TInterface.TNoiseStepSet(
            noise, TInterface.TWorkNoiseCreate(true, 10, -50, false, LGrain.LGrainWhite, 5, 0.8, -40));

        Assert.Equal("Dialogue", TInterface.TNoiseTokenRead(noise));
        Assert.True(TInterface.TNoiseStepRead(noise).LWorkStepActive);
    }

    [Fact]
    public void LoudnessMode_SwitchesPresetFamily()
    {
        LLoudness loudness = TInterface.TLoudnessCreate();
        Assert.Equal("Audiobook", TInterface.TLoudnessTokenRead(loudness));

        TInterface.TLoudnessModeSet(loudness, LLeveling.LLevelingDynamic);

        Assert.Equal("Leveler", TInterface.TLoudnessTokenRead(loudness));
        TInterface.TLoudnessPresetSelect(loudness, "Voice");
        Assert.Equal(200, TInterface.TLoudnessStepRead(loudness).LWorkNormalizeFrame);
        Assert.Equal("Voice", TInterface.TLoudnessMatchRead(loudness));

        TInterface.TLoudnessDynamicSet(loudness, 0, 250);
        Assert.Null(TInterface.TLoudnessMatchRead(loudness));
        Assert.Equal("Voice", TInterface.TLoudnessTokenRead(loudness));
    }

    [Fact]
    public void LoudnessPreset_SetsTargetPeakRange()
    {
        LLoudness loudness = TInterface.TLoudnessCreate();
        int notices = 0;
        TInterface.TLoudnessAttach(loudness, () => notices++);

        TInterface.TLoudnessPresetSelect(loudness, "Streaming");
        TInterface.TLoudnessPresetSelect(loudness, "Streaming");
        LWorkNormalizeStep step = TInterface.TLoudnessStepRead(loudness);

        Assert.Equal(1, notices);
        Assert.Equal(-14, step.LWorkNormalizeTarget);
        Assert.Equal(9, step.LWorkNormalizeRange);
        TInterface.TLoudnessValueSet(loudness, 0, -16);
        Assert.Null(TInterface.TLoudnessMatchRead(loudness));
    }

    [Fact]
    public void FilterPreset_HighAndLow_MatchTheirOwnTables()
    {
        LFilter high = TInterface.TFilterCreate(true);
        LFilter low = TInterface.TFilterCreate(false);

        Assert.Equal("Voice", TInterface.TFilterTokenRead(high));
        Assert.Equal("Air tame", TInterface.TFilterTokenRead(low));

        TInterface.TFilterPresetSelect(high, "Rumble");
        TInterface.TFilterPresetSelect(low, "Rumble");

        Assert.Equal("Rumble", TInterface.TFilterMatchRead(high));
        Assert.Equal("Air tame", TInterface.TFilterTokenRead(low));

        TInterface.TFilterFrequencySet(high, TInterface.TFilterStepRead(high).LWorkPassFrequency + 10);
        Assert.Null(TInterface.TFilterMatchRead(high));
        Assert.Equal("Rumble", TInterface.TFilterTokenRead(high));
    }

    [Fact]
    public void EqualizerPreset_BandsFollowGrid_EditBreaksMatch()
    {
        LEqualizer equalizer = TInterface.TEqualizerCreate();
        Assert.Equal("Flat", TInterface.TEqualizerTokenRead(equalizer));

        TInterface.TEqualizerPresetSelect(equalizer, "Bright");

        Assert.Equal(10, TInterface.TEqualizerBandsRead(equalizer).Count);
        Assert.Equal("Bright", TInterface.TEqualizerMatchRead(equalizer));

        TInterface.TEqualizerBandAdd(equalizer);
        Assert.Equal(11, TInterface.TEqualizerBandsRead(equalizer).Count);
        Assert.Null(TInterface.TEqualizerMatchRead(equalizer));
        Assert.Equal("Bright", TInterface.TEqualizerTokenRead(equalizer));

        TInterface.TEqualizerBandRemove(equalizer, 10);
        Assert.Equal("Bright", TInterface.TEqualizerMatchRead(equalizer));
    }

    [Fact]
    public void EqualizerBandSet_SameBand_IsSilent()
    {
        LEqualizer equalizer = TInterface.TEqualizerCreate();
        int notices = 0;
        TInterface.TEqualizerAttach(equalizer, () => notices++);

        TInterface.TEqualizerBandSet(equalizer, 3, 250, 0);
        TInterface.TEqualizerBandSet(equalizer, 3, 250, 2);
        TInterface.TEqualizerBandSet(equalizer, 3, 250, 2);

        Assert.Equal(1, notices);
        Assert.Equal(2, TInterface.TEqualizerBandsRead(equalizer)[3].LWorkBandGain);
    }
}
