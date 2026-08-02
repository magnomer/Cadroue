namespace Cadroue.Core;

public sealed record LWorkVolumeStep(bool LWorkStepActive, double LWorkVolumeGain)
    : LWorkAudioStep(LAudioKind.LAudioKindVolume, LWorkStepActive);
