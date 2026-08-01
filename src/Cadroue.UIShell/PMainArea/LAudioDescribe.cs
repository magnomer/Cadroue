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
        LWorkOutput lAudioOutput = lExportSpecificState.LPresetOutputCreate();
        string lAudioTab = PControlBar.LTabset.LTabsetTitleRead(lAudioRelaySource);
        LWorkItem? lAudioItem = Cadroue.Application.LAudio.LAudioItemCreate(
            lWorkPriority, lAudioSourcePath, lAudioProcessing, lAudioOutput, lAudioTab,
            lAudioMessage => LTraceLog.LTraceInfoRecord(lAudioMessage),
            lAudioMessage => LTraceLog.LTraceErrorRecord(lAudioMessage));
        if (lAudioItem is null)
        {
            return 0;
        }

        int lAudioAdded = LSchedule.LScheduleCurrent.LScheduleAdd(
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
        LWorkOutput lAudioOutput = lExportSpecificState.LPresetOutputCreate();
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

        return LSchedule.LScheduleCurrent.LScheduleAdd(
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
        foreach (LWorkAudioKind lAudioKind in LAudioKindsRead())
        {
            LWorkAudioStep? lAudioPersistentStep = lAudioPersistent?.LWorkAudioSteps
                .FirstOrDefault(lStep => lStep.LWorkAudioStepKind == lAudioKind);
            LWorkAudioStep? lAudioSavedStep = lAudioSaved?.LWorkAudioSteps
                .FirstOrDefault(lStep => lStep.LWorkAudioStepKind == lAudioKind);
            lAudioSteps.Add(lAudioPersistentStep ?? lAudioSavedStep ?? LAudioDefaultCreate(lAudioKind));
        }

        return new LWorkAudio(lAudioSteps) { LWorkAudioSkip = lAudioSkip };
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

    private static LWorkAudioStep LAudioStepCreate(Cadroue.Core.LSidecarAudioStepRecord lAudioRecord) =>
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

    private static Cadroue.Core.LSidecarAudioStepRecord LAudioRecordCreate(LWorkAudioStep lAudioStep) => new()
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
