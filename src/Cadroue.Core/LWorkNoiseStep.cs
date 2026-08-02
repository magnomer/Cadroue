namespace Cadroue.Core;

public sealed record LWorkNoiseStep(
    bool LWorkStepActive,
    double LWorkNoiseReduction,
    double LWorkNoiseFloor,
    bool LWorkNoiseTrack,
    LGrain LWorkNoiseType,
    double LWorkNoiseSmooth,
    double LWorkNoiseAdaptivity,
    double LWorkNoiseResidual)
    : LWorkAudioStep(LAudioKind.LAudioKindDenoise, LWorkStepActive);
