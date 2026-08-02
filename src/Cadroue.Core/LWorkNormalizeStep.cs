namespace Cadroue.Core;

public sealed record LWorkNormalizeStep(
    bool LWorkAudioStepActive,
    LWorkAudioNormalizeMode LWorkNormalizeMode,
    double LWorkNormalizeTarget,
    double LWorkNormalizePeak,
    double LWorkNormalizeRange,
    bool LWorkNormalizeTwoPass,
    double LWorkNormalizeFrame,
    double LWorkNormalizeGauss,
    double LWorkNormalizeMaxGain,
    double LWorkNormalizeCompress)
    : LWorkAudioStep(LWorkAudioKind.LWorkAudioKindNormalize, LWorkAudioStepActive)
{
    public override bool LWorkStepLoudness =>
        LWorkAudioStepActive
        && LWorkNormalizeTwoPass
        && LWorkNormalizeMode == LWorkAudioNormalizeMode.LWorkAudioNormalizeLoudness;
}
