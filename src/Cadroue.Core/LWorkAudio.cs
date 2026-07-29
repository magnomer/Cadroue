namespace Cadroue.Core;

public enum LWorkAudioKind
{
    LWorkAudioKindVolume,
    LWorkAudioKindNormalize
}

public enum LWorkAudioNormalizeMode
{
    LWorkAudioNormalizeLoudness,
    LWorkAudioNormalizeDynamic
}

public sealed record LWorkAudioStep(
    LWorkAudioKind LWorkAudioStepKind,
    bool LWorkAudioStepActive,
    double LWorkAudioStepGain,
    LWorkAudioNormalizeMode LWorkAudioStepMode,
    double LWorkAudioStepTarget,
    double LWorkAudioStepPeak,
    double LWorkAudioStepRange,
    bool LWorkAudioStepTwoPass)
{
    public static LWorkAudioStep LWorkAudioVolumeCreate(bool lStepActive, double lStepGain) =>
        new(LWorkAudioKind.LWorkAudioKindVolume, lStepActive, lStepGain,
            LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness, -16, -1.5, 11, false);

    public static LWorkAudioStep LWorkAudioNormalizeCreate(
        bool lStepActive,
        LWorkAudioNormalizeMode lStepMode,
        double lStepTarget,
        double lStepPeak,
        double lStepRange,
        bool lStepTwoPass) =>
        new(LWorkAudioKind.LWorkAudioKindNormalize, lStepActive, 0, lStepMode, lStepTarget, lStepPeak, lStepRange, lStepTwoPass);

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
