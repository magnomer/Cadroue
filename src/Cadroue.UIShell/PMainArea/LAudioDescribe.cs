using Cadroue.Core;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PMainArea;

public static partial class LAudio
{
    public static int LAudioDescribe(
        LWorkPriority lWorkPriority,
        string? lAudioSourcePath,
        LWorkAudio lAudioProcessing,
        LExportSpecificState lExportSpecificState)
    {
        return LAudio.LAudioInterpret(
            lWorkPriority,
            lAudioSourcePath,
            lAudioProcessing,
            lExportSpecificState.LPresetOutputCreate());
    }

    public static int LAudioAllDescribe(
        LWorkPriority lWorkPriority,
        IReadOnlyList<string> lAudioSourcePaths,
        LWorkAudio lAudioProcessing,
        LExportSpecificState lExportSpecificState,
        LWorkAudio? lAudioPersistent = null)
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

            lAudioAdded += LAudio.LAudioInterpret(lWorkPriority, lAudioSourcePath, lAudioPlan, lAudioOutput);
        }

        return lAudioAdded;
    }

    public static LWorkAudio? LAudioPlanRead(string lAudioSourcePath) =>
        Cadroue.Media.LSidecarStore.LSidecarAudioRead(lAudioSourcePath) is { } lAudioRecord
            ? new LWorkAudio(lAudioRecord.Steps.Select(LAudioStepCreate).ToArray())
            : null;

    public static void LAudioPlanSave(string lAudioSourcePath, LWorkAudio lAudioPlan)
    {
        Cadroue.Media.LSidecarStore.LSidecarAudioSave(
            lAudioSourcePath,
            new Cadroue.Media.LSidecarAudioRecord
            {
                Steps = lAudioPlan.LWorkAudioSteps.Select(LAudioRecordCreate).ToList()
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
            lAudioSteps.Add(lAudioPersistentStep ?? lAudioSavedStep ?? LAudioDefaultStepCreate(lAudioKind));
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

    private static LWorkAudioStep LAudioDefaultStepCreate(LWorkAudioKind lAudioKind) => lAudioKind switch
    {
        LWorkAudioKind.LWorkAudioKindNormalize => LWorkAudioStep.LWorkAudioNormalizeCreate(false, LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness, -21, -2, 11, true),
        LWorkAudioKind.LWorkAudioKindNoiseReduction => LWorkAudioStep.LWorkAudioNoiseCreate(false, 12, -50, false, LWorkAudioNoiseType.LWorkAudioNoiseWhite, 6, 0.5, -38),
        LWorkAudioKind.LWorkAudioKindHighPass => LWorkAudioStep.LWorkAudioHighPassCreate(false, 100, 1, 2, 0.707),
        LWorkAudioKind.LWorkAudioKindLowPass => LWorkAudioStep.LWorkAudioLowPassCreate(false, 12000, 1, 2, 0.707),
        _ => LWorkAudioStep.LWorkAudioVolumeCreate(false, 0)
    };

    private static LWorkAudioStep LAudioStepCreate(Cadroue.Media.LSidecarAudioStepRecord lAudioRecord) =>
        new(
            LAudioKindCreate(lAudioRecord.Kind),
            lAudioRecord.Active,
            lAudioRecord.Gain,
            string.Equals(lAudioRecord.Mode, "Dynamic", StringComparison.Ordinal)
                ? LWorkAudioNormalizeMode.LWorkAudioNormalizeDynamic
                : LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness,
            lAudioRecord.Target,
            lAudioRecord.Peak,
            lAudioRecord.Range,
            lAudioRecord.TwoPass,
            lAudioRecord.Reduction,
            lAudioRecord.NoiseFloor,
            lAudioRecord.TrackNoise,
            lAudioRecord.Frequency,
            lAudioRecord.Stages,
            lAudioRecord.Poles,
            lAudioRecord.Resonance,
            lAudioRecord.NoiseType switch
            {
                "Vinyl" => LWorkAudioNoiseType.LWorkAudioNoiseVinyl,
                "Shellac" => LWorkAudioNoiseType.LWorkAudioNoiseShellac,
                _ => LWorkAudioNoiseType.LWorkAudioNoiseWhite
            },
            lAudioRecord.GainSmooth,
            lAudioRecord.Adaptivity,
            lAudioRecord.ResidualFloor);

    private static Cadroue.Media.LSidecarAudioStepRecord LAudioRecordCreate(LWorkAudioStep lAudioStep) => new()
    {
        Kind = lAudioStep.LWorkAudioStepKind.ToString().Replace("LWorkAudioKind", string.Empty),
        Active = lAudioStep.LWorkAudioStepActive,
        Gain = lAudioStep.LWorkAudioStepGain,
        Mode = lAudioStep.LWorkAudioStepMode == LWorkAudioNormalizeMode.LWorkAudioNormalizeDynamic ? "Dynamic" : "Loudness",
        Target = lAudioStep.LWorkAudioStepTarget,
        Peak = lAudioStep.LWorkAudioStepPeak,
        Range = lAudioStep.LWorkAudioStepRange,
        TwoPass = lAudioStep.LWorkAudioStepTwoPass,
        Reduction = lAudioStep.LWorkAudioStepReduction,
        NoiseFloor = lAudioStep.LWorkAudioStepNoiseFloor,
        TrackNoise = lAudioStep.LWorkAudioStepTrackNoise,
        Frequency = lAudioStep.LWorkAudioStepFrequency,
        Stages = lAudioStep.LWorkAudioStepStages,
        Poles = lAudioStep.LWorkAudioStepPoles,
        Resonance = lAudioStep.LWorkAudioStepResonance,
        NoiseType = lAudioStep.LWorkAudioStepNoiseType.ToString().Replace("LWorkAudioNoise", string.Empty),
        GainSmooth = lAudioStep.LWorkAudioStepGainSmooth,
        Adaptivity = lAudioStep.LWorkAudioStepAdaptivity,
        ResidualFloor = lAudioStep.LWorkAudioStepResidualFloor
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
