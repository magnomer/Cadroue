using Cadroue.Core;

namespace Cadroue.Application;

public static class LAudioSidecar
{
    public static LSidecarAudioEntry LAudioPersistentCreate(LWorkAudio lAudioPlan) => new()
    {
        LSidecarSkip = lAudioPlan.LWorkAudioSkip,
        LSidecarSteps = lAudioPlan.LWorkAudioSteps.Select(LAudioRecordCreate).ToList()
    };

    public static LWorkAudio LAudioPersistentRead(LSidecarAudioEntry lAudioRecord) =>
        new(lAudioRecord.LSidecarSteps.Select(LAudioStepCreate).ToArray())
        {
            LWorkAudioSkip = lAudioRecord.LSidecarSkip
        };

    public static LWorkAudio? LAudioSidecarRead(
        string lAudioSourcePath,
        Func<string, LSidecarAudioEntry?> lSidecarRead) =>
        lSidecarRead(lAudioSourcePath) is { } lAudioRecord
            ? LAudioPersistentRead(lAudioRecord)
            : null;

    public static bool LAudioSidecarSave(
        string lAudioSourcePath, LWorkAudio lAudioPlan, Func<string, LSidecarAudioEntry?, bool> lSidecarSave) =>
        lSidecarSave(lAudioSourcePath, LAudioPersistentCreate(lAudioPlan));

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
                lAudioRecord.LSidecarMode = lNormalize.LWorkNormalizeMode == LLeveling.LLevelingDynamic
                    ? "Dynamic"
                    : "Loudness";
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
