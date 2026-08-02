namespace Cadroue.Core;

public enum LWorkAudioKind
{
    LWorkAudioKindVolume,
    LWorkAudioKindNormalize,
    LWorkAudioKindNoiseReduction,
    LWorkAudioKindHighPass,
    LWorkAudioKindLowPass,
    LWorkAudioKindEqualizer
}

public enum LWorkAudioNormalizeMode
{
    LWorkAudioNormalizeLoudness,
    LWorkAudioNormalizeDynamic
}

public enum LWorkAudioNoiseType
{
    LWorkAudioNoiseWhite,
    LWorkAudioNoiseVinyl,
    LWorkAudioNoiseShellac
}

public abstract record LWorkAudioStep(LWorkAudioKind LWorkAudioStepKind, bool LWorkAudioStepActive)
{
    public virtual bool LWorkStepLoudness => false;

    public static LWorkAudioStep LWorkVolumeCreate(bool lStepActive, double lStepGain) =>
        new LWorkVolumeStep(lStepActive, lStepGain);

    public static LWorkAudioStep LWorkNormalizeCreate(
        bool lStepActive,
        LWorkAudioNormalizeMode lStepMode,
        double lStepTarget,
        double lStepPeak,
        double lStepRange,
        bool lStepTwoPass,
        double lStepFrame = 300,
        double lStepGauss = 21,
        double lStepMaxGain = 10,
        double lStepCompress = 6) =>
        new LWorkNormalizeStep(
            lStepActive, lStepMode, lStepTarget, lStepPeak, lStepRange, lStepTwoPass,
            lStepFrame, lStepGauss, lStepMaxGain, lStepCompress);

    public static LWorkAudioStep LWorkNoiseCreate(
        bool lStepActive,
        double lStepReduction,
        double lStepNoiseFloor,
        bool lStepTrackNoise,
        LWorkAudioNoiseType lStepNoiseType,
        double lStepGainSmooth,
        double lStepAdaptivity,
        double lStepResidualFloor) =>
        new LWorkNoiseStep(
            lStepActive, lStepReduction, lStepNoiseFloor, lStepTrackNoise,
            lStepNoiseType, lStepGainSmooth, lStepAdaptivity, lStepResidualFloor);

    public static LWorkAudioStep LWorkHighCreate(
        bool lStepActive, double lStepFrequency, int lStepStages, int lStepPoles, double lStepResonance) =>
        new LWorkPassStep(
            LWorkAudioKind.LWorkAudioKindHighPass, lStepActive, true, lStepFrequency, lStepStages, lStepPoles, lStepResonance);

    public static LWorkAudioStep LWorkLowCreate(
        bool lStepActive, double lStepFrequency, int lStepStages, int lStepPoles, double lStepResonance) =>
        new LWorkPassStep(
            LWorkAudioKind.LWorkAudioKindLowPass, lStepActive, false, lStepFrequency, lStepStages, lStepPoles, lStepResonance);

    public static LWorkAudioStep LWorkEqualizerCreate(
        bool lStepActive, IReadOnlyList<LWorkEqualizerBand> lStepBands) =>
        new LWorkEqualizerStep(lStepActive, lStepBands);
}

public sealed record LWorkAudio(IReadOnlyList<LWorkAudioStep> LWorkAudioSteps)
{
    public bool LWorkAudioSkip { get; init; }

    public static LWorkAudio LWorkAudioCreate() => new(Array.Empty<LWorkAudioStep>());

    public bool LWorkAudioActive => LWorkAudioSkip || LWorkAudioSteps.Any(lStep => lStep.LWorkAudioStepActive);
}
