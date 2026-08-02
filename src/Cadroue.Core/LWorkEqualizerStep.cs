namespace Cadroue.Core;

public sealed record LWorkEqualizerStep(
    bool LWorkAudioStepActive,
    IReadOnlyList<LWorkEqualizerBand> LWorkEqualizerBands)
    : LWorkAudioStep(LWorkAudioKind.LWorkAudioKindEqualizer, LWorkAudioStepActive)
{
    public static IReadOnlyList<LWorkEqualizerBand> LWorkEqualizerDefaultCreate() => new LWorkEqualizerBand[]
    {
        new(31, 0), new(62, 0), new(125, 0), new(250, 0), new(500, 0),
        new(1000, 0), new(2000, 0), new(4000, 0), new(8000, 0), new(16000, 0)
    };
}
