using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The equalizer rules from the contour catalogue, on bands as plain (frequency, gain) pairs. An edited band is
// held inside the catalogue's ranges; an added band starts at the default frequency with no gain.
public sealed class LEqualizerFacade : LEqualizerPort
{
    internal LEqualizerFacade()
    {
    }

    public double LEqualizerGainLeast => LContourCatalog.LContourGainLeast;

    public double LEqualizerGainMost => LContourCatalog.LContourGainMost;

    public double LEqualizerFrequencyOrigin => LContourCatalog.LContourFrequencyDefault;

    public IReadOnlyList<string> LEqualizerTokensRead() => LContourCatalog.LContourTokensRead();

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerOriginRead() =>
        LEqualizerAnswerCreate(LEqualizerPairsRead(LWorkEqualizerStep.LWorkBandsCreate()));

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandSet(
        IReadOnlyList<(double, double)> lBands, int lIndex, double lFrequency, double lGain)
    {
        ArgumentNullException.ThrowIfNull(lBands);
        LWorkBand lBand = ((LWorkEqualizerStep)LWorkAudioStep.LWorkEqualizerCreate(
                false, new[] { new LWorkBand(lFrequency, lGain) }))
            .LWorkEqualizerBands[0];
        List<(double, double)> lEdited = lBands.ToList();
        lEdited[lIndex] = (lBand.LWorkBandFrequency, lBand.LWorkBandGain);
        return LEqualizerAnswerCreate(lEdited);
    }

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandAdd(
        IReadOnlyList<(double, double)> lBands)
    {
        ArgumentNullException.ThrowIfNull(lBands);
        List<(double, double)> lEdited = lBands.ToList();
        lEdited.Add((LEqualizerFrequencyOrigin, 0));
        return LEqualizerAnswerCreate(lEdited);
    }

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandRemove(
        IReadOnlyList<(double, double)> lBands, int lIndex)
    {
        ArgumentNullException.ThrowIfNull(lBands);
        List<(double, double)> lEdited = lBands.ToList();
        lEdited.RemoveAt(lIndex);
        return LEqualizerAnswerCreate(lEdited);
    }

    // The preset's gains laid on the band grid; null for an unknown preset.
    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset)? LEqualizerPresetRead(
        string lToken)
    {
        if (LContourCatalog.LContourGainsRead(lToken) is not { } lGains)
        {
            return null;
        }

        double[] lGrid = LContourCatalog.LContourBandGrid;
        var lBands = new List<(double, double)>();
        for (int lIndex = 0; lIndex < lGrid.Length; lIndex++)
        {
            lBands.Add((lGrid[lIndex], lGains[lIndex]));
        }

        return LEqualizerAnswerCreate(lBands);
    }

    // The audio step for the gate's switch and bands; the bands are held inside the catalogue's ranges.
    internal static LWorkAudioStep LEqualizerStepCreate(bool lActive, IReadOnlyList<(double, double)> lBands)
    {
        ArgumentNullException.ThrowIfNull(lBands);
        return LWorkAudioStep.LWorkEqualizerCreate(
            lActive,
            lBands.Select(lBand => new LWorkBand(lBand.Item1, lBand.Item2)).ToArray());
    }

    internal static IReadOnlyList<(double, double)> LEqualizerPairsRead(IReadOnlyList<LWorkBand> lBands) =>
        lBands.Select(lBand => (lBand.LWorkBandFrequency, lBand.LWorkBandGain)).ToList();

    internal static string? LEqualizerMatchFind(IReadOnlyList<(double, double)> lBands) =>
        LContourCatalog.LContourPresetFind(
            lBands.Select(lBand => lBand.Item1).ToArray(),
            lBands.Select(lBand => lBand.Item2).ToArray());

    private static (IReadOnlyList<(double, double)>, string?) LEqualizerAnswerCreate(
        IReadOnlyList<(double, double)> lBands) => (lBands, LEqualizerMatchFind(lBands));
}
