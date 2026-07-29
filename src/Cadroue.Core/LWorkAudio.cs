namespace Cadroue.Core;

public enum LWorkAudioKind
{
    LWorkAudioKindVolume,
    LWorkAudioKindNormalize,
    LWorkAudioKindNoiseReduction,
    LWorkAudioKindHighPass,
    LWorkAudioKindLowPass
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

public sealed record LWorkAudioStep(
    LWorkAudioKind LWorkAudioStepKind,
    bool LWorkAudioStepActive,
    double LWorkAudioStepGain,
    LWorkAudioNormalizeMode LWorkAudioStepMode,
    double LWorkAudioStepTarget,
    double LWorkAudioStepPeak,
    double LWorkAudioStepRange,
    bool LWorkAudioStepTwoPass,
    double LWorkAudioStepReduction,
    double LWorkAudioStepNoiseFloor,
    bool LWorkAudioStepTrackNoise,
    double LWorkAudioStepFrequency,
    int LWorkAudioStepStages,
    int LWorkAudioStepPoles,
    double LWorkAudioStepResonance,
    LWorkAudioNoiseType LWorkAudioStepNoiseType,
    double LWorkAudioStepGainSmooth,
    double LWorkAudioStepAdaptivity,
    double LWorkAudioStepResidualFloor)
{
    public static LWorkAudioStep LWorkAudioVolumeCreate(bool lStepActive, double lStepGain) =>
        new(LWorkAudioKind.LWorkAudioKindVolume, lStepActive, lStepGain,
            LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness, -16, -1.5, 11, false, 12, -50, false, 0, 1, 2, 0.707,
            LWorkAudioNoiseType.LWorkAudioNoiseWhite, 0, 0.5, -38);

    public static LWorkAudioStep LWorkAudioNormalizeCreate(
        bool lStepActive,
        LWorkAudioNormalizeMode lStepMode,
        double lStepTarget,
        double lStepPeak,
        double lStepRange,
        bool lStepTwoPass) =>
        new(LWorkAudioKind.LWorkAudioKindNormalize, lStepActive, 0, lStepMode, lStepTarget, lStepPeak, lStepRange, lStepTwoPass, 12, -50, false, 0, 1, 2, 0.707,
            LWorkAudioNoiseType.LWorkAudioNoiseWhite, 0, 0.5, -38);

    public static LWorkAudioStep LWorkAudioNoiseCreate(
        bool lStepActive,
        double lStepReduction,
        double lStepNoiseFloor,
        bool lStepTrackNoise,
        LWorkAudioNoiseType lStepNoiseType,
        double lStepGainSmooth,
        double lStepAdaptivity,
        double lStepResidualFloor) =>
        new(LWorkAudioKind.LWorkAudioKindNoiseReduction, lStepActive, 0,
            LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness, -16, -1.5, 11, false,
            lStepReduction, lStepNoiseFloor, lStepTrackNoise, 0, 1, 2, 0.707,
            lStepNoiseType, lStepGainSmooth, lStepAdaptivity, lStepResidualFloor);

    public static LWorkAudioStep LWorkAudioHighPassCreate(
        bool lStepActive, double lStepFrequency, int lStepStages, int lStepPoles, double lStepResonance) =>
        new(LWorkAudioKind.LWorkAudioKindHighPass, lStepActive, 0,
            LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness, -16, -1.5, 11, false, 12, -50, false,
            lStepFrequency, lStepStages, lStepPoles, lStepResonance,
            LWorkAudioNoiseType.LWorkAudioNoiseWhite, 0, 0.5, -38);

    public static LWorkAudioStep LWorkAudioLowPassCreate(
        bool lStepActive, double lStepFrequency, int lStepStages, int lStepPoles, double lStepResonance) =>
        new(LWorkAudioKind.LWorkAudioKindLowPass, lStepActive, 0,
            LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness, -16, -1.5, 11, false, 12, -50, false,
            lStepFrequency, lStepStages, lStepPoles, lStepResonance,
            LWorkAudioNoiseType.LWorkAudioNoiseWhite, 0, 0.5, -38);

    public bool LWorkAudioStepTwoPassLoudness =>
        LWorkAudioStepKind == LWorkAudioKind.LWorkAudioKindNormalize
        && LWorkAudioStepActive
        && LWorkAudioStepTwoPass
        && LWorkAudioStepMode == LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness;
}

public sealed record LWorkAudio(IReadOnlyList<LWorkAudioStep> LWorkAudioSteps)
{
    public static LWorkAudio LWorkAudioNoneCreate() => new(Array.Empty<LWorkAudioStep>());

    public bool LWorkAudioActive => LWorkAudioSteps.Any(lStep => lStep.LWorkAudioStepActive);
}
