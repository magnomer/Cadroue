using Cadroue.Core;

namespace Cadroue.Application;

public static class LAudioPlan
{
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
        LLevelingDefault lDefault = LLevelingCatalog.LLevelingDefaultRead();
        return LWorkAudioStep.LWorkNormalizeCreate(
            false,
            LLeveling.LLevelingLoudness,
            lDefault.LLevelingTarget,
            lDefault.LLevelingPeak,
            lDefault.LLevelingRange,
            lDefault.LLevelingTwoPass,
            lDefault.LLevelingFrame,
            lDefault.LLevelingGauss,
            lDefault.LLevelingMaxGain,
            lDefault.LLevelingCompress);
    }

    private static LWorkAudioStep LAudioDefaultCreate(LAudioKind lAudioKind) => lAudioKind switch
    {
        LAudioKind.LAudioKindLeveling => LAudioNormalizeCreate(),
        LAudioKind.LAudioKindDenoise => LWorkAudioStep.LWorkNoiseCreate(
            false,
            12,
            -50,
            false,
            LGrain.LGrainWhite,
            6,
            0.5,
            -38),
        LAudioKind.LAudioKindHighpass => LPassband.LPassbandStepCreate(true, false),
        LAudioKind.LAudioKindLowpass => LPassband.LPassbandStepCreate(false, false),
        LAudioKind.LAudioKindEqualizer => LWorkAudioStep.LWorkEqualizerCreate(
            false,
            LWorkEqualizerStep.LWorkBandsCreate()),
        _ => LWorkAudioStep.LWorkVolumeCreate(false, 0)
    };
}
