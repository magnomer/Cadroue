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

    [Fact]
    public void EqualizerBandSet_OutOfRangeBand_StoresClampedValue()
    {
        LEqualizer equalizer = TInterface.TEqualizerCreate();

        TInterface.TEqualizerBandSet(equalizer, 0, 5, 99);

        LWorkBand shown = TInterface.TEqualizerBandsRead(equalizer)[0];
        Assert.Equal(LContourCatalog.LContourFrequencyLeast, shown.LWorkBandFrequency);
        Assert.Equal(LContourCatalog.LContourGainMost, shown.LWorkBandGain);
        var step = (LWorkEqualizerStep)TInterface.TEqualizerStepRead(equalizer);
        Assert.Equal(shown, step.LWorkEqualizerBands[0]);
    }

    [Fact]
    public void SensorDefaults_MatchNormalForEveryPresetKind()
    {
        LSensor sensor = TInterface.TSensorCreate();

        foreach (LDetectorKind kind in LDetector.LDetectorKinds)
        {
            if (TInterface.TDetectorTokensRead(kind).Count == 0)
            {
                Assert.Null(TInterface.TSensorTokenRead(sensor, kind));
                continue;
            }

            Assert.Equal(LDetector.LDetectorTokenDefault, TInterface.TSensorTokenRead(sensor, kind));
            Assert.Equal(LDetector.LDetectorTokenDefault, TInterface.TSensorMatchRead(sensor, kind));
        }
    }

    [Fact]
    public void SensorValueDrift_KeepsBaseToken_SurvivesRestore()
    {
        LSensor sensor = TInterface.TSensorCreate();
        TInterface.TSensorPresetSelect(sensor, LDetectorKind.LDetectorKindStill, "Sensitive");
        TInterface.TSensorThresholdSet(sensor, LDetectorKind.LDetectorKindStill, 0.3);

        Assert.Equal("Sensitive", TInterface.TSensorTokenRead(sensor, LDetectorKind.LDetectorKindStill));
        Assert.Null(TInterface.TSensorMatchRead(sensor, LDetectorKind.LDetectorKindStill));

        LSensor restored = TInterface.TSensorCreate();
        TInterface.TSensorStepSet(restored, TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindStill));
        TInterface.TSensorTokenSet(
            restored,
            LDetectorKind.LDetectorKindStill,
            TInterface.TSensorTokenRead(sensor, LDetectorKind.LDetectorKindStill) ?? string.Empty);

        Assert.Equal("Sensitive", TInterface.TSensorTokenRead(restored, LDetectorKind.LDetectorKindStill));
        Assert.Null(TInterface.TSensorMatchRead(restored, LDetectorKind.LDetectorKindStill));
        Assert.Equal(
            0.3, TInterface.TSensorStepRead(restored, LDetectorKind.LDetectorKindStill).LDetectorStepThreshold);
    }

    [Fact]
    public void SensorTokenSet_RejectsTokenOutsideKindCatalog()
    {
        LSensor sensor = TInterface.TSensorCreate();

        TInterface.TSensorTokenSet(sensor, LDetectorKind.LDetectorKindScene, "Strong");
        Assert.Null(TInterface.TSensorTokenRead(sensor, LDetectorKind.LDetectorKindScene));

        TInterface.TSensorTokenSet(sensor, LDetectorKind.LDetectorKindSilence, "Normal");
        Assert.Null(TInterface.TSensorTokenRead(sensor, LDetectorKind.LDetectorKindSilence));
    }

    [Fact]
    public void SensorMetricSet_FollowsPresetOnlyWhileValuesMatch()
    {
        LSensor sensor = TInterface.TSensorCreate();

        TInterface.TSensorMetricSet(sensor, LDetectorMetricMode.LDetectorMetricRms);
        Assert.Equal(19, TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindVolume).LDetectorStepThreshold);
        Assert.Equal("Normal", TInterface.TSensorMatchRead(sensor, LDetectorKind.LDetectorKindVolume));

        TInterface.TSensorThresholdSet(sensor, LDetectorKind.LDetectorKindVolume, 15);
        TInterface.TSensorMetricSet(sensor, LDetectorMetricMode.LDetectorMetricLufs);

        Assert.Equal(15, TInterface.TSensorStepRead(sensor, LDetectorKind.LDetectorKindVolume).LDetectorStepThreshold);
        Assert.Equal("Normal", TInterface.TSensorTokenRead(sensor, LDetectorKind.LDetectorKindVolume));
    }
}
