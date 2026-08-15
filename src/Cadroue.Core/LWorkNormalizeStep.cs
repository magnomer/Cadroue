namespace Cadroue.Core;

public sealed record LWorkNormalizeStep(
    bool LWorkStepActive,
    LLeveling LWorkNormalizeMode,
    double LWorkNormalizeTarget,
    double LWorkNormalizePeak,
    double LWorkNormalizeRange,
    bool LWorkTwoPass,
    double LWorkNormalizeFrame,
    double LWorkNormalizeGauss,
    double LWorkNormalizeGain,
    double LWorkNormalizeCompress)
    : LWorkAudioStep(LAudioKind.LAudioKindLeveling, LWorkStepActive)
{
    public override bool LWorkStepLoudness =>
        LWorkStepActive
        && LWorkTwoPass
        && LWorkNormalizeMode == LLeveling.LLevelingLoudness;
}
