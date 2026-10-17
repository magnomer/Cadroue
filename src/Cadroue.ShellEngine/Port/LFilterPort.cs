namespace Cadroue.ShellEngine;

// The high- and low-pass rules for their gate; each member takes the side, true for the high pass. Values run in slot
// order: frequency, stages, resonance; the pole count stands apart.
public interface LFilterPort
{
    IReadOnlyList<double> LFilterLeastsRead(bool lHigh);

    IReadOnlyList<double> LFilterMostsRead(bool lHigh);

    IReadOnlyList<string> LFilterTokensRead(bool lHigh);

    (IReadOnlyList<double> LFilterValues, int LFilterPoles, string? LFilterPreset) LFilterOriginRead(bool lHigh);

    IReadOnlyList<double> LFilterDefaultRead(bool lHigh, string? lToken, double lFrequency);

    (IReadOnlyList<double> LFilterValues, int LFilterPoles) LFilterNormalize(
        bool lHigh, IReadOnlyList<double> lValues, int lPoles);

    (IReadOnlyList<double> LFilterValues, int LFilterPoles)? LFilterPresetRead(bool lHigh, string lToken);

    string? LFilterMatchFind(bool lHigh, IReadOnlyList<double> lValues, int lPoles);
}
