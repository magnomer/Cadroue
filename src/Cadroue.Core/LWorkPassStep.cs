namespace Cadroue.Core;

public sealed record LWorkPassStep(
    LAudioKind LWorkStepKind,
    bool LWorkStepActive,
    bool LWorkPassHigh,
    double LWorkPassFrequency,
    int LWorkPassStages,
    int LWorkPassPoles,
    double LWorkPassResonance)
    : LWorkAudioStep(LWorkStepKind, LWorkStepActive);
