using System.Globalization;
using System.Text.Json.Serialization;

namespace Cadroue.Core;

public enum LAudioKind
{
    LAudioKindVolume,
    LAudioKindLeveling,
    LAudioKindDenoise,
    LAudioKindHighpass,
    LAudioKindLowpass,
    LAudioKindEqualizer
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "LWorkStepType")]
[JsonDerivedType(typeof(LWorkVolumeStep), "Volume")]
[JsonDerivedType(typeof(LWorkNormalizeStep), "Normalize")]
[JsonDerivedType(typeof(LWorkNoiseStep), "Noise")]
[JsonDerivedType(typeof(LWorkPassStep), "Pass")]
[JsonDerivedType(typeof(LWorkEqualizerStep), "Equalizer")]
public abstract record LWorkAudioStep(LAudioKind LWorkStepKind, bool LWorkStepActive)
{
    [JsonIgnore]
    public virtual bool LWorkStepLoudness => false;

    public static LWorkAudioStep LWorkVolumeCreate(bool lStepActive, double lStepGain) =>
        new LWorkVolumeStep(lStepActive,
            Math.Clamp(lStepGain, LWorkAudio.LWorkGainLeast, LWorkAudio.LWorkGainMost));

    public static LWorkAudioStep LWorkNormalizeCreate(
        bool lStepActive,
        LLeveling lStepMode,
        double lStepTarget,
        double lStepPeak,
        double lStepRange,
        bool lStepTwoPass,
        double lStepFrame = 300,
        double lStepGauss = 21,
        double lStepMaxGain = 10,
        double lStepCompress = 6) =>
        new LWorkNormalizeStep(
            lStepActive, lStepMode,
            Math.Clamp(lStepTarget, LLevelingCatalog.LLevelingTargetLeast, LLevelingCatalog.LLevelingTargetMost),
            Math.Clamp(lStepPeak, LLevelingCatalog.LLevelingPeakLeast, LLevelingCatalog.LLevelingPeakMost),
            Math.Clamp(lStepRange, LLevelingCatalog.LLevelingRangeLeast, LLevelingCatalog.LLevelingRangeMost),
            lStepTwoPass,
            Math.Clamp(lStepFrame, LLevelingCatalog.LLevelingFrameLeast, LLevelingCatalog.LLevelingFrameMost),
            Math.Clamp(lStepGauss, LLevelingCatalog.LLevelingGaussLeast, LLevelingCatalog.LLevelingGaussMost),
            Math.Clamp(lStepMaxGain, LLevelingCatalog.LLevelingGainLeast, LLevelingCatalog.LLevelingGainMost),
            Math.Clamp(lStepCompress, LLevelingCatalog.LLevelingCompressLeast, LLevelingCatalog.LLevelingCompressMost));

    public static LWorkAudioStep LWorkNoiseCreate(
        bool lStepActive,
        double lStepReduction,
        double lStepNoiseFloor,
        bool lStepTrackNoise,
        LGrain lStepNoiseType,
        double lStepGainSmooth,
        double lStepAdaptivity,
        double lStepResidualFloor) =>
        new LWorkNoiseStep(
            lStepActive,
            Math.Clamp(lStepReduction, LGrainCatalog.LGrainReductionLeast, LGrainCatalog.LGrainReductionMost),
            Math.Clamp(lStepNoiseFloor, LGrainCatalog.LGrainFloorLeast, LGrainCatalog.LGrainFloorMost),
            lStepTrackNoise, lStepNoiseType,
            Math.Clamp(lStepGainSmooth, LGrainCatalog.LGrainSmoothLeast, LGrainCatalog.LGrainSmoothMost),
            Math.Clamp(lStepAdaptivity, LGrainCatalog.LGrainAdaptivityLeast, LGrainCatalog.LGrainAdaptivityMost),
            Math.Clamp(lStepResidualFloor, LGrainCatalog.LGrainFloorLeast, LGrainCatalog.LGrainFloorMost));

    public static LWorkAudioStep LWorkHighCreate(
        bool lStepActive, double lStepFrequency, int lStepStages, int lStepPoles, double lStepResonance) =>
        new LWorkPassStep(
            LAudioKind.LAudioKindHighpass, lStepActive, true,
            Math.Clamp(lStepFrequency, LPassband.LPassbandHighFloor, LPassband.LPassbandHighCeiling),
            Math.Clamp(lStepStages, LPassband.LPassbandStagesLeast, LPassband.LPassbandStagesMost),
            lStepPoles <= 1 ? 1 : 2,
            Math.Clamp(lStepResonance, LPassband.LPassbandResonanceLeast, LPassband.LPassbandResonanceMost));

    public static LWorkAudioStep LWorkLowCreate(
        bool lStepActive, double lStepFrequency, int lStepStages, int lStepPoles, double lStepResonance) =>
        new LWorkPassStep(
            LAudioKind.LAudioKindLowpass, lStepActive, false,
            Math.Clamp(lStepFrequency, LPassband.LPassbandLowFloor, LPassband.LPassbandLowCeiling),
            Math.Clamp(lStepStages, LPassband.LPassbandStagesLeast, LPassband.LPassbandStagesMost),
            lStepPoles <= 1 ? 1 : 2,
            Math.Clamp(lStepResonance, LPassband.LPassbandResonanceLeast, LPassband.LPassbandResonanceMost));

    public static LWorkAudioStep LWorkEqualizerCreate(
        bool lStepActive, IReadOnlyList<LWorkBand> lStepBands)
    {
        var lStepClamped = lStepBands
            .Select(lBand => new LWorkBand(
                Math.Clamp(lBand.LWorkBandFrequency,
                    LContourCatalog.LContourFrequencyLeast, LContourCatalog.LContourFrequencyMost),
                Math.Clamp(lBand.LWorkBandGain,
                    LContourCatalog.LContourGainLeast, LContourCatalog.LContourGainMost)))
            .ToArray();
        return new LWorkEqualizerStep(lStepActive, lStepClamped);
    }
}

public sealed record LWorkAudio(IReadOnlyList<LWorkAudioStep> LWorkAudioSteps)
{
    public const double LWorkGainLeast = -24;
    public const double LWorkGainMost = 24;

    public bool LWorkAudioSkip { get; init; }

    public static LWorkAudio LWorkAudioCreate() => new(Array.Empty<LWorkAudioStep>());

    public bool LWorkAudioActive => LWorkAudioSkip || LWorkAudioSteps.Any(lStep => lStep.LWorkStepActive);

    public string LWorkAudioFormat()
    {
        var lFilters = new List<string>();
        foreach (LWorkAudioStep lStep in LWorkAudioSteps)
        {
            if (!lStep.LWorkStepActive)
            {
                continue;
            }

            switch (lStep)
            {
                case LWorkVolumeStep lVolume:
                    lFilters.Add(string.Create(
                        CultureInfo.InvariantCulture,
                        $"volume={lVolume.LWorkVolumeGain.ToString("0.###", CultureInfo.InvariantCulture)}dB"));
                    break;
                case LWorkNoiseStep lNoise:
                    lFilters.Add(LWorkNoiseFormat(lNoise));
                    break;
                case LWorkPassStep lPass:
                    LWorkPassAppend(lFilters, lPass, lPass.LWorkPassHigh ? "highpass" : "lowpass");
                    break;
                case LWorkEqualizerStep lEqualizer:
                    foreach (LWorkBand lBand in lEqualizer.LWorkEqualizerBands)
                    {
                        LWorkBandAppend(lFilters, lBand.LWorkBandFrequency, lBand.LWorkBandGain);
                    }

                    break;
                case LWorkNormalizeStep lNormalize:
                    lFilters.Add(LWorkNormalizeFormat(lNormalize));
                    break;
            }
        }

        return lFilters.Count > 0 ? string.Join(',', lFilters) : string.Empty;
    }

    private static string LWorkNoiseFormat(LWorkNoiseStep lNoise)
    {
        string lNoiseType = lNoise.LWorkNoiseType switch
        {
            LGrain.LGrainVinyl => "vinyl",
            LGrain.LGrainShellac => "shellac",
            _ => "white"
        };
        string lDenoise = string.Create(
            CultureInfo.InvariantCulture,
            $"afftdn=nr={lNoise.LWorkNoiseReduction.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"nf={lNoise.LWorkNoiseFloor.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"rf={lNoise.LWorkNoiseResidual.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"ad={lNoise.LWorkNoiseAdaptivity.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"gs={lNoise.LWorkNoiseSmooth.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"nt={lNoiseType}");
        if (lNoise.LWorkNoiseTrack)
        {
            lDenoise += ":tn=1";
        }

        return lDenoise;
    }

    private static void LWorkPassAppend(List<string> lFilters, LWorkPassStep lStep, string lFilterName)
    {
        int lStages = Math.Max(1, lStep.LWorkPassStages);
        int lPoles = lStep.LWorkPassPoles == 1 ? 1 : 2;
        string lFragment = string.Create(
            CultureInfo.InvariantCulture,
            $"{lFilterName}=f={lStep.LWorkPassFrequency.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"poles={lPoles}:width_type=q:width={lStep.LWorkPassResonance.ToString("0.###", CultureInfo.InvariantCulture)}");

        for (int lStage = 0; lStage < lStages; lStage++)
        {
            lFilters.Add(lFragment);
        }
    }

    private static void LWorkBandAppend(List<string> lFilters, double lFrequency, double lGain)
    {
        if (lGain == 0)
        {
            return;
        }

        lFilters.Add(string.Create(
            CultureInfo.InvariantCulture,
            $"equalizer=f={lFrequency.ToString("0.###", CultureInfo.InvariantCulture)}:t=q:w=1:" +
            $"g={lGain.ToString("0.###", CultureInfo.InvariantCulture)}"));
    }

    private static string LWorkNormalizeFormat(LWorkNormalizeStep lNormalize)
    {
        if (lNormalize.LWorkNormalizeMode == LLeveling.LLevelingDynamic)
        {
            int lFrame = (int)Math.Clamp(Math.Round(lNormalize.LWorkNormalizeFrame), 10, 8000);
            int lGauss = (int)Math.Clamp(Math.Round(lNormalize.LWorkNormalizeGauss), 3, 301);
            if (lGauss % 2 == 0)
            {
                lGauss++;
            }

            double lMaxGain = Math.Clamp(lNormalize.LWorkNormalizeGain, 1, 100);
            string lDynamic = string.Create(
                CultureInfo.InvariantCulture,
                $"dynaudnorm=f={lFrame}:g={lGauss}:m={lMaxGain.ToString("0.###", CultureInfo.InvariantCulture)}:p=0.95");
            if (lNormalize.LWorkNormalizeCompress >= 3)
            {
                lDynamic += string.Create(
                    CultureInfo.InvariantCulture,
                    $":s={Math.Clamp(lNormalize.LWorkNormalizeCompress, 3, 30).ToString("0.###", CultureInfo.InvariantCulture)}");
            }

            return lDynamic;
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"loudnorm=I={lNormalize.LWorkNormalizeTarget.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"TP={lNormalize.LWorkNormalizePeak.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"LRA={lNormalize.LWorkNormalizeRange.ToString("0.###", CultureInfo.InvariantCulture)}");
    }
}
