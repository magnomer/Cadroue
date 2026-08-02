namespace Cadroue.Core;

public sealed record LWorkNoiseStep(
    bool LWorkAudioStepActive,
    double LWorkNoiseReduction,
    double LWorkNoiseFloor,
    bool LWorkNoiseTrack,
    LWorkAudioNoiseType LWorkNoiseType,
    double LWorkNoiseSmooth,
    double LWorkNoiseAdaptivity,
    double LWorkNoiseResidual)
    : LWorkAudioStep(LWorkAudioKind.LWorkAudioKindNoiseReduction, LWorkAudioStepActive);
