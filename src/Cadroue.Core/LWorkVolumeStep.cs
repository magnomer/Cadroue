namespace Cadroue.Core;

public sealed record LWorkVolumeStep(bool LWorkAudioStepActive, double LWorkVolumeGain)
    : LWorkAudioStep(LWorkAudioKind.LWorkAudioKindVolume, LWorkAudioStepActive);
