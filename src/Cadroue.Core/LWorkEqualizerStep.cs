namespace Cadroue.Core;

public sealed record LWorkEqualizerStep(
    bool LWorkStepActive,
    IReadOnlyList<LWorkBand> LWorkEqualizerBands)
    : LWorkAudioStep(LAudioKind.LAudioKindEqualizer, LWorkStepActive)
{
    public static IReadOnlyList<LWorkBand> LWorkBandsCreate() => new LWorkBand[]
    {
        new(31, 0), new(62, 0), new(125, 0), new(250, 0), new(500, 0),
        new(1000, 0), new(2000, 0), new(4000, 0), new(8000, 0), new(16000, 0)
    };
}
