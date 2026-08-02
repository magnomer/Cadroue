namespace Cadroue.Core;

public sealed record LWorkPassStep(
    LWorkAudioKind LWorkAudioStepKind,
    bool LWorkAudioStepActive,
    bool LWorkPassHigh,
    double LWorkPassFrequency,
    int LWorkPassStages,
    int LWorkPassPoles,
    double LWorkPassResonance)
    : LWorkAudioStep(LWorkAudioStepKind, LWorkAudioStepActive);
