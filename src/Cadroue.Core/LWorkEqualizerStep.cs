namespace Cadroue.Core;

public sealed record LWorkEqualizerStep(
    bool LWorkStepActive,
    IReadOnlyList<LWorkBand> LWorkEqualizerBands)
    : LWorkAudioStep(LAudioKind.LAudioKindEqualizer, LWorkStepActive)
{
    public static IReadOnlyList<LWorkBand> LWorkBandsCreate() =>
        LContourCatalog.LContourBandGrid.Select(lFrequency => new LWorkBand(lFrequency, 0)).ToArray();
}
