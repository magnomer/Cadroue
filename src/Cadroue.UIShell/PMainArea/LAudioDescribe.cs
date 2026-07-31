using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LAudio
{
    public static int LAudioDescribe(
        LWorkPriority lWorkPriority,
        string? lAudioSourcePath,
        LWorkAudio lAudioProcessing,
        LPreset lExportSpecificState,
        Guid lAudioRelayTarget = default)
    {
        return LAudio.LAudioInterpret(
            lWorkPriority,
            lAudioSourcePath,
            lAudioProcessing,
            lExportSpecificState.LPresetOutputCreate(),
            lAudioRelayTarget);
    }

    public static int LAudioAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lAudioSourcePaths,
        LWorkAudio lAudioProcessing,
        LPreset lExportSpecificState,
        LWorkAudio? lAudioPersistent = null,
        Guid lAudioRelayTarget = default)
    {
        LWorkOutput lAudioOutput = lExportSpecificState.LPresetOutputCreate();
        int lAudioAdded = 0;
        foreach (string lAudioSourcePath in lAudioSourcePaths)
        {
            LWorkAudio? lAudioSaved = LAudioPlanRead(lAudioSourcePath);
            if (lAudioPersistent is null && lAudioSaved is not { LWorkAudioActive: true })
            {
                continue;
            }

            LWorkAudio lAudioPlan = LAudioPlanResolve(lAudioSaved, lAudioPersistent);
            if (lAudioPersistent is not null)
            {
                LAudioPlanSave(lAudioSourcePath, lAudioPlan);
            }

            lAudioAdded += LAudio.LAudioInterpret(
                lWorkPriority, lAudioSourcePath, lAudioPlan, lAudioOutput, lAudioRelayTarget);
        }

        return lAudioAdded;
    }

    public static LWorkAudio? LAudioPlanRead(string lAudioSourcePath) =>
        Cadroue.Media.LSidecarStore.LSidecarAudioRead(lAudioSourcePath) is { } lAudioRecord
            ? new LWorkAudio(lAudioRecord.LSidecarSteps.Select(LAudioStepCreate).ToArray())
            : null;

    public static void LAudioPlanSave(string lAudioSourcePath, LWorkAudio lAudioPlan)
    {
        Cadroue.Media.LSidecarStore.LSidecarAudioSave(
            lAudioSourcePath,
            new Cadroue.Media.LSidecarAudioRecord
            {
                LSidecarSteps = lAudioPlan.LWorkAudioSteps.Select(LAudioRecordCreate).ToList()
            });
    }

    public static LWorkAudio LAudioPlanResolve(LWorkAudio? lAudioSaved, LWorkAudio? lAudioPersistent)
    {
        var lAudioSteps = new List<LWorkAudioStep>();
        foreach (LWorkAudioKind lAudioKind in LAudioKindsRead())
        {
            LWorkAudioStep? lAudioPersistentStep = lAudioPersistent?.LWorkAudioSteps
                .FirstOrDefault(lStep => lStep.LWorkAudioStepKind == lAudioKind);
            LWorkAudioStep? lAudioSavedStep = lAudioSaved?.LWorkAudioSteps
                .FirstOrDefault(lStep => lStep.LWorkAudioStepKind == lAudioKind);
            lAudioSteps.Add(lAudioPersistentStep ?? lAudioSavedStep ?? LAudioDefaultCreate(lAudioKind));
        }

        return new LWorkAudio(lAudioSteps);
    }

    private static IReadOnlyList<LWorkAudioKind> LAudioKindsRead() => new[]
    {
        LWorkAudioKind.LWorkAudioKindHighPass,
        LWorkAudioKind.LWorkAudioKindLowPass,
        LWorkAudioKind.LWorkAudioKindNoiseReduction,
        LWorkAudioKind.LWorkAudioKindVolume,
        LWorkAudioKind.LWorkAudioKindNormalize
    };

    private static LWorkAudioStep LAudioDefaultCreate(LWorkAudioKind lAudioKind) => lAudioKind switch
    {
        LWorkAudioKind.LWorkAudioKindNormalize => LWorkAudioStep.LWorkNormalizeCreate(false, LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness, -21, -2, 11, true),
        LWorkAudioKind.LWorkAudioKindNoiseReduction => LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LWorkAudioNoiseType.LWorkAudioNoiseWhite, 6, 0.5, -38),
        LWorkAudioKind.LWorkAudioKindHighPass => LWorkAudioStep.LWorkHighCreate(false, 100, 1, 2, 0.707),
        LWorkAudioKind.LWorkAudioKindLowPass => LWorkAudioStep.LWorkLowCreate(false, 12000, 1, 2, 0.707),
        _ => LWorkAudioStep.LWorkVolumeCreate(false, 0)
    };

    private static LWorkAudioStep LAudioStepCreate(Cadroue.Media.LSidecarAudioStepRecord lAudioRecord) =>
        new(
            LAudioKindCreate(lAudioRecord.LSidecarKind),
            lAudioRecord.LSidecarActive,
            lAudioRecord.LSidecarGain,
            string.Equals(lAudioRecord.LSidecarMode, "Dynamic", StringComparison.Ordinal)
                ? LWorkAudioNormalizeMode.LWorkAudioNormalizeDynamic
                : LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness,
            lAudioRecord.LSidecarTarget,
            lAudioRecord.LSidecarPeak,
            lAudioRecord.LSidecarRange,
            lAudioRecord.LSidecarTwoPass,
            lAudioRecord.LSidecarReduction,
            lAudioRecord.LSidecarNoiseFloor,
            lAudioRecord.LSidecarTrackNoise,
            lAudioRecord.LSidecarFrequency,
            lAudioRecord.LSidecarStages,
            lAudioRecord.LSidecarPoles,
            lAudioRecord.LSidecarResonance,
            lAudioRecord.LSidecarNoiseType switch
            {
                "Vinyl" => LWorkAudioNoiseType.LWorkAudioNoiseVinyl,
                "Shellac" => LWorkAudioNoiseType.LWorkAudioNoiseShellac,
                _ => LWorkAudioNoiseType.LWorkAudioNoiseWhite
            },
            lAudioRecord.LSidecarGainSmooth,
            lAudioRecord.LSidecarAdaptivity,
            lAudioRecord.LSidecarResidualFloor);

    private static Cadroue.Media.LSidecarAudioStepRecord LAudioRecordCreate(LWorkAudioStep lAudioStep) => new()
    {
        LSidecarKind = lAudioStep.LWorkAudioStepKind.ToString().Replace("LWorkAudioKind", string.Empty),
        LSidecarActive = lAudioStep.LWorkAudioStepActive,
        LSidecarGain = lAudioStep.LWorkAudioStepGain,
        LSidecarMode = lAudioStep.LWorkAudioStepMode == LWorkAudioNormalizeMode.LWorkAudioNormalizeDynamic ? "Dynamic" : "Loudness",
        LSidecarTarget = lAudioStep.LWorkAudioStepTarget,
        LSidecarPeak = lAudioStep.LWorkAudioStepPeak,
        LSidecarRange = lAudioStep.LWorkAudioStepRange,
        LSidecarTwoPass = lAudioStep.LWorkAudioStepTwoPass,
        LSidecarReduction = lAudioStep.LWorkAudioStepReduction,
        LSidecarNoiseFloor = lAudioStep.LWorkAudioStepNoiseFloor,
        LSidecarTrackNoise = lAudioStep.LWorkAudioStepTrackNoise,
        LSidecarFrequency = lAudioStep.LWorkAudioStepFrequency,
        LSidecarStages = lAudioStep.LWorkAudioStepStages,
        LSidecarPoles = lAudioStep.LWorkAudioStepPoles,
        LSidecarResonance = lAudioStep.LWorkAudioStepResonance,
        LSidecarNoiseType = lAudioStep.LWorkAudioStepNoiseType.ToString().Replace("LWorkAudioNoise", string.Empty),
        LSidecarGainSmooth = lAudioStep.LWorkAudioStepGainSmooth,
        LSidecarAdaptivity = lAudioStep.LWorkAudioStepAdaptivity,
        LSidecarResidualFloor = lAudioStep.LWorkAudioStepResidualFloor
    };

    private static LWorkAudioKind LAudioKindCreate(string lAudioKind) => lAudioKind switch
    {
        "Normalize" => LWorkAudioKind.LWorkAudioKindNormalize,
        "NoiseReduction" => LWorkAudioKind.LWorkAudioKindNoiseReduction,
        "HighPass" => LWorkAudioKind.LWorkAudioKindHighPass,
        "LowPass" => LWorkAudioKind.LWorkAudioKindLowPass,
        _ => LWorkAudioKind.LWorkAudioKindVolume
    };
}
