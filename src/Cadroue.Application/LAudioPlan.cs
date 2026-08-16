using Cadroue.Core;

namespace Cadroue.Application;

public static partial class LAudio
{
    public static LSidecarAudioRecord LAudioPersistentCreate(LWorkAudio lAudioPlan) => new()
    {
        LSidecarSkip = lAudioPlan.LWorkAudioSkip,
        LSidecarSteps = lAudioPlan.LWorkAudioSteps.Select(LAudioRecordCreate).ToList()
    };

    public static LWorkAudio LAudioPersistentRead(LSidecarAudioRecord lAudioRecord) =>
        new(lAudioRecord.LSidecarSteps.Select(LAudioStepCreate).ToArray()) { LWorkAudioSkip = lAudioRecord.LSidecarSkip };

    public static LWorkAudio? LAudioPlanRead(string lAudioSourcePath, Func<string, LSidecarAudioRecord?> lSidecarRead) =>
        lSidecarRead(lAudioSourcePath) is { } lAudioRecord
            ? new LWorkAudio(lAudioRecord.LSidecarSteps.Select(LAudioStepCreate).ToArray()) { LWorkAudioSkip = lAudioRecord.LSidecarSkip }
            : null;

    public static void LAudioPlanSave(
        string lAudioSourcePath, LWorkAudio lAudioPlan, Func<string, LSidecarAudioRecord?, bool> lSidecarSave) =>
        lSidecarSave(lAudioSourcePath, LAudioPersistentCreate(lAudioPlan));

    public static LWorkAudio LAudioPlanResolve(
        LWorkAudio? lAudioSaved,
        LWorkAudio? lAudioPersistent,
        bool lAudioSkipPersistent,
        bool lAudioSkipApply)
    {
        bool lAudioSkip = lAudioSkipPersistent ? lAudioSkipApply : (lAudioSaved?.LWorkAudioSkip ?? false);
        var lAudioSteps = new List<LWorkAudioStep>();
        foreach (LAudioKind lAudioKind in LAudioKindsRead())
        {
            LWorkAudioStep? lAudioPersistentStep = lAudioPersistent?.LWorkAudioSteps
                .FirstOrDefault(lStep => lStep.LWorkStepKind == lAudioKind);
            LWorkAudioStep? lAudioSavedStep = lAudioSaved?.LWorkAudioSteps
                .FirstOrDefault(lStep => lStep.LWorkStepKind == lAudioKind);
            lAudioSteps.Add(lAudioPersistentStep ?? lAudioSavedStep ?? LAudioDefaultCreate(lAudioKind));
        }

        return new LWorkAudio(lAudioSteps) { LWorkAudioSkip = lAudioSkip };
    }

    private static IReadOnlyList<LAudioKind> LAudioKindsRead() => new[]
    {
        LAudioKind.LAudioKindHighpass,
        LAudioKind.LAudioKindLowpass,
        LAudioKind.LAudioKindDenoise,
        LAudioKind.LAudioKindEqualizer,
        LAudioKind.LAudioKindVolume,
        LAudioKind.LAudioKindLeveling
    };

    public static LWorkAudioStep LAudioNormalizeCreate()
    {
        var (lTarget, lPeak, lRange, lTwoPass, lFrame, lGauss, lMaxGain, lCompress) =
            LLevelingCatalog.LLevelingDefaultRead();
        return LWorkAudioStep.LWorkNormalizeCreate(
            false, LLeveling.LLevelingLoudness, lTarget, lPeak, lRange, lTwoPass, lFrame, lGauss, lMaxGain, lCompress);
    }

    private static LWorkAudioStep LAudioDefaultCreate(LAudioKind lAudioKind) => lAudioKind switch
    {
        LAudioKind.LAudioKindLeveling => LAudioNormalizeCreate(),
        LAudioKind.LAudioKindDenoise => LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38),
        LAudioKind.LAudioKindHighpass => LPassband.LPassbandStepCreate(true, false),
        LAudioKind.LAudioKindLowpass => LPassband.LPassbandStepCreate(false, false),
        LAudioKind.LAudioKindEqualizer => LWorkAudioStep.LWorkEqualizerCreate(false, LWorkEqualizerStep.LWorkBandsCreate()),
        _ => LWorkAudioStep.LWorkVolumeCreate(false, 0)
    };

    private static LWorkAudioStep LAudioStepCreate(LSidecarAudioStep lAudioRecord) =>
        LAudioKindCreate(lAudioRecord.LSidecarKind) switch
        {
            LAudioKind.LAudioKindLeveling => LWorkAudioStep.LWorkNormalizeCreate(
                lAudioRecord.LSidecarActive,
                string.Equals(lAudioRecord.LSidecarMode, "Dynamic", StringComparison.Ordinal)
                    ? LLeveling.LLevelingDynamic
                    : LLeveling.LLevelingLoudness,
                lAudioRecord.LSidecarTarget,
                lAudioRecord.LSidecarPeak,
                lAudioRecord.LSidecarRange,
                lAudioRecord.LSidecarTwoPass,
                lAudioRecord.LSidecarFrame,
                lAudioRecord.LSidecarGauss,
                lAudioRecord.LSidecarMaxGain,
                lAudioRecord.LSidecarCompress),
            LAudioKind.LAudioKindDenoise => LWorkAudioStep.LWorkNoiseCreate(
                lAudioRecord.LSidecarActive,
                lAudioRecord.LSidecarReduction,
                lAudioRecord.LSidecarNoiseFloor,
                lAudioRecord.LSidecarTrackNoise,
                LGrainCatalog.LGrainParse(lAudioRecord.LSidecarNoiseType),
                lAudioRecord.LSidecarGainSmooth,
                lAudioRecord.LSidecarAdaptivity,
                lAudioRecord.LSidecarResidualFloor),
            LAudioKind.LAudioKindHighpass => LWorkAudioStep.LWorkHighCreate(
                lAudioRecord.LSidecarActive, lAudioRecord.LSidecarFrequency,
                lAudioRecord.LSidecarStages, lAudioRecord.LSidecarPoles, lAudioRecord.LSidecarResonance),
            LAudioKind.LAudioKindLowpass => LWorkAudioStep.LWorkLowCreate(
                lAudioRecord.LSidecarActive, lAudioRecord.LSidecarFrequency,
                lAudioRecord.LSidecarStages, lAudioRecord.LSidecarPoles, lAudioRecord.LSidecarResonance),
            LAudioKind.LAudioKindEqualizer => LWorkAudioStep.LWorkEqualizerCreate(
                lAudioRecord.LSidecarActive,
                lAudioRecord.LSidecarEqualizerBands
                    .Select(lBand => new LWorkBand(lBand.LSidecarBandFrequency, lBand.LSidecarBandGain))
                    .ToArray()),
            _ => LWorkAudioStep.LWorkVolumeCreate(lAudioRecord.LSidecarActive, lAudioRecord.LSidecarGain)
        };

    private static LSidecarAudioStep LAudioRecordCreate(LWorkAudioStep lAudioStep)
    {
        var lAudioRecord = new LSidecarAudioStep
        {
            LSidecarKind = LAudioKindFormat(lAudioStep.LWorkStepKind),
            LSidecarActive = lAudioStep.LWorkStepActive
        };

        switch (lAudioStep)
        {
            case LWorkVolumeStep lVolume:
                lAudioRecord.LSidecarGain = lVolume.LWorkVolumeGain;
                break;
            case LWorkNormalizeStep lNormalize:
                lAudioRecord.LSidecarMode = lNormalize.LWorkNormalizeMode == LLeveling.LLevelingDynamic ? "Dynamic" : "Loudness";
                lAudioRecord.LSidecarTarget = lNormalize.LWorkNormalizeTarget;
                lAudioRecord.LSidecarPeak = lNormalize.LWorkNormalizePeak;
                lAudioRecord.LSidecarRange = lNormalize.LWorkNormalizeRange;
                lAudioRecord.LSidecarTwoPass = lNormalize.LWorkTwoPass;
                lAudioRecord.LSidecarFrame = lNormalize.LWorkNormalizeFrame;
                lAudioRecord.LSidecarGauss = lNormalize.LWorkNormalizeGauss;
                lAudioRecord.LSidecarMaxGain = lNormalize.LWorkNormalizeGain;
                lAudioRecord.LSidecarCompress = lNormalize.LWorkNormalizeCompress;
                break;
            case LWorkNoiseStep lNoise:
                lAudioRecord.LSidecarReduction = lNoise.LWorkNoiseReduction;
                lAudioRecord.LSidecarNoiseFloor = lNoise.LWorkNoiseFloor;
                lAudioRecord.LSidecarTrackNoise = lNoise.LWorkNoiseTrack;
                lAudioRecord.LSidecarNoiseType = LGrainCatalog.LGrainFormat(lNoise.LWorkNoiseType);
                lAudioRecord.LSidecarGainSmooth = lNoise.LWorkNoiseSmooth;
                lAudioRecord.LSidecarAdaptivity = lNoise.LWorkNoiseAdaptivity;
                lAudioRecord.LSidecarResidualFloor = lNoise.LWorkNoiseResidual;
                break;
            case LWorkPassStep lPass:
                lAudioRecord.LSidecarFrequency = lPass.LWorkPassFrequency;
                lAudioRecord.LSidecarStages = lPass.LWorkPassStages;
                lAudioRecord.LSidecarPoles = lPass.LWorkPassPoles;
                lAudioRecord.LSidecarResonance = lPass.LWorkPassResonance;
                break;
            case LWorkEqualizerStep lEqualizer:
                lAudioRecord.LSidecarEqualizerBands = lEqualizer.LWorkEqualizerBands
                    .Select(lBand => new LSidecarEqualizerBand
                    {
                        LSidecarBandFrequency = lBand.LWorkBandFrequency,
                        LSidecarBandGain = lBand.LWorkBandGain
                    })
                    .ToList();
                break;
        }

        return lAudioRecord;
    }

    private static string LAudioKindFormat(LAudioKind lAudioKind) => lAudioKind switch
    {
        LAudioKind.LAudioKindLeveling => "Normalize",
        LAudioKind.LAudioKindDenoise => "NoiseReduction",
        LAudioKind.LAudioKindHighpass => "HighPass",
        LAudioKind.LAudioKindLowpass => "LowPass",
        LAudioKind.LAudioKindEqualizer => "Equalizer",
        _ => "Volume"
    };

    private static LAudioKind LAudioKindCreate(string lAudioKind) => lAudioKind switch
    {
        "Normalize" => LAudioKind.LAudioKindLeveling,
        "NoiseReduction" => LAudioKind.LAudioKindDenoise,
        "HighPass" => LAudioKind.LAudioKindHighpass,
        "LowPass" => LAudioKind.LAudioKindLowpass,
        "Equalizer" => LAudioKind.LAudioKindEqualizer,
        _ => LAudioKind.LAudioKindVolume
    };
}
