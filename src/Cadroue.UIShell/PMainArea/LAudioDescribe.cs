using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LAudio
{
    public static int LAudioDescribe(
        LWorkPriority lWorkPriority,
        string? lAudioSourcePath,
        LWorkAudio lAudioProcessing,
        LPreset lExportSpecificState,
        Guid lAudioRelayTarget = default,
        Guid lAudioRelaySource = default)
    {
        LEncoding lAudioOutput = lExportSpecificState.LPresetOutputCreate();
        string lAudioTab = PControlBar.LTabset.LTabsetTitleRead(lAudioRelaySource);
        LWorkItem? lAudioItem = Cadroue.Application.LAudio.LAudioItemCreate(
            lWorkPriority, lAudioSourcePath, lAudioProcessing, lAudioOutput, lAudioTab,
            lAudioMessage => LTraceLog.LTraceInfoRecord(lAudioMessage),
            lAudioMessage => LTraceLog.LTraceErrorRecord(lAudioMessage));
        if (lAudioItem is null)
        {
            return 0;
        }

        int lAudioAdded = LCourier.LCourierScheduleAdd(
            new[] { lAudioItem }, lAudioRelayTarget, lAudioRelaySource);
        LTraceLog.LTraceInfoRecord(
            $"Audio queued {lAudioAdded} job at {lWorkPriority} from " +
            $"'{System.IO.Path.GetFileName(lAudioSourcePath)}'");
        return lAudioAdded;
    }

    public static int LAudioAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<LWorkSource> lAudioSources,
        LPreset lExportSpecificState,
        Guid lAudioRelayTarget = default,
        Guid lAudioRelaySource = default)
    {
        LEncoding lAudioOutput = lExportSpecificState.LPresetOutputCreate();
        string lAudioTab = PControlBar.LTabset.LTabsetTitleRead(lAudioRelaySource);
        Guid lAudioLooseBatch = Guid.NewGuid();
        var lAudioItems = new List<LWorkItem>();
        foreach (LWorkSource lAudioSource in lAudioSources)
        {
            string lAudioSourcePath = lAudioSource.LWorkSourcePath;
            if (LAudioPlanRead(lAudioSourcePath) is not { LWorkAudioActive: true } lAudioPlan)
            {
                continue;
            }

            Guid lAudioBatch = lAudioSource.LWorkSourceBatch != Guid.Empty
                ? lAudioSource.LWorkSourceBatch
                : lAudioLooseBatch;
            if (Cadroue.Application.LAudio.LAudioItemCreate(
                    lWorkPriority, lAudioSourcePath, lAudioPlan, lAudioOutput, lAudioTab,
                    lAudioMessage => LTraceLog.LTraceInfoRecord(lAudioMessage),
                    lAudioMessage => LTraceLog.LTraceErrorRecord(lAudioMessage),
                    lAudioBatch)
                is { } lAudioItem)
            {
                lAudioItems.Add(lAudioItem);
            }
        }

        return LCourier.LCourierScheduleAdd(
            lAudioItems, lAudioRelayTarget, lAudioRelaySource);
    }

    public static Cadroue.Core.LSidecarAudioRecord LAudioPersistentCreate(LWorkAudio lAudioPlan) => new()
    {
        LSidecarSkip = lAudioPlan.LWorkAudioSkip,
        LSidecarSteps = lAudioPlan.LWorkAudioSteps.Select(LAudioRecordCreate).ToList()
    };

    public static LWorkAudio LAudioPersistentRead(Cadroue.Core.LSidecarAudioRecord lAudioRecord) =>
        new(lAudioRecord.LSidecarSteps.Select(LAudioStepCreate).ToArray()) { LWorkAudioSkip = lAudioRecord.LSidecarSkip };

    public static LWorkAudio? LAudioPlanRead(string lAudioSourcePath) =>
        Cadroue.Media.LSidecarStore.LSidecarAudioRead(lAudioSourcePath) is { } lAudioRecord
            ? new LWorkAudio(lAudioRecord.LSidecarSteps.Select(LAudioStepCreate).ToArray()) { LWorkAudioSkip = lAudioRecord.LSidecarSkip }
            : null;

    public static void LAudioPlanSave(string lAudioSourcePath, LWorkAudio lAudioPlan)
    {
        Cadroue.Media.LSidecarStore.LSidecarAudioSave(
            lAudioSourcePath,
            new Cadroue.Core.LSidecarAudioRecord
            {
                LSidecarSkip = lAudioPlan.LWorkAudioSkip,
                LSidecarSteps = lAudioPlan.LWorkAudioSteps.Select(LAudioRecordCreate).ToList()
            });
    }

    public static LWorkAudio LAudioPlanResolve(LWorkAudio? lAudioSaved, LWorkAudio? lAudioPersistent)
    {
        bool lAudioSkip = (lAudioPersistent?.LWorkAudioSkip ?? false) || (lAudioSaved?.LWorkAudioSkip ?? false);
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
        LAudioKind.LAudioKindNormalize
    };

    private static LWorkAudioStep LAudioDefaultCreate(LAudioKind lAudioKind) => lAudioKind switch
    {
        LAudioKind.LAudioKindNormalize => LWorkAudioStep.LWorkNormalizeCreate(false, LLeveling.LLevelingLoudness, -21, -2, 6, true),
        LAudioKind.LAudioKindDenoise => LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38),
        LAudioKind.LAudioKindHighpass => LWorkAudioStep.LWorkHighCreate(false, 80, 2, 2, 0.707),
        LAudioKind.LAudioKindLowpass => LWorkAudioStep.LWorkLowCreate(false, 16000, 2, 2, 0.707),
        LAudioKind.LAudioKindEqualizer => LWorkAudioStep.LWorkEqualizerCreate(false, LWorkEqualizerStep.LWorkBandsCreate()),
        _ => LWorkAudioStep.LWorkVolumeCreate(false, 0)
    };

    private static LWorkAudioStep LAudioStepCreate(Cadroue.Core.LSidecarAudioStep lAudioRecord) =>
        LAudioKindCreate(lAudioRecord.LSidecarKind) switch
        {
            LAudioKind.LAudioKindNormalize => LWorkAudioStep.LWorkNormalizeCreate(
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
                lAudioRecord.LSidecarNoiseType switch
                {
                    "Vinyl" => LGrain.LGrainVinyl,
                    "Shellac" => LGrain.LGrainShellac,
                    _ => LGrain.LGrainWhite
                },
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

    private static Cadroue.Core.LSidecarAudioStep LAudioRecordCreate(LWorkAudioStep lAudioStep)
    {
        var lAudioRecord = new Cadroue.Core.LSidecarAudioStep
        {
            LSidecarKind = lAudioStep.LWorkStepKind.ToString().Replace("LAudioKind", string.Empty),
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
                lAudioRecord.LSidecarNoiseType = lNoise.LWorkNoiseType.ToString().Replace("LWorkAudioNoise", string.Empty);
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
                    .Select(lBand => new Cadroue.Core.LSidecarEqualizerBand
                    {
                        LSidecarBandFrequency = lBand.LWorkBandFrequency,
                        LSidecarBandGain = lBand.LWorkBandGain
                    })
                    .ToList();
                break;
        }

        return lAudioRecord;
    }

    private static LAudioKind LAudioKindCreate(string lAudioKind) => lAudioKind switch
    {
        "Normalize" => LAudioKind.LAudioKindNormalize,
        "NoiseReduction" => LAudioKind.LAudioKindDenoise,
        "HighPass" => LAudioKind.LAudioKindHighpass,
        "LowPass" => LAudioKind.LAudioKindLowpass,
        "Equalizer" => LAudioKind.LAudioKindEqualizer,
        _ => LAudioKind.LAudioKindVolume
    };
}
