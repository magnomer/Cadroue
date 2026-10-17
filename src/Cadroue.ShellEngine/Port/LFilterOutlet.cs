namespace Cadroue.ShellEngine;

// The engine's face for the filter gate: every member forwards to the same-named member of the engine's filter
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LFilterOutlet : LFilterPort
{
    private readonly LEngine _lFilterOutletEngine;

    public LFilterOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lFilterOutletEngine = lEngine;
    }

    public IReadOnlyList<double> LFilterLeastsRead(bool lHigh) =>
        _lFilterOutletEngine.LEngineFilter.LFilterLeastsRead(lHigh);

    public IReadOnlyList<double> LFilterMostsRead(bool lHigh) =>
        _lFilterOutletEngine.LEngineFilter.LFilterMostsRead(lHigh);

    public IReadOnlyList<string> LFilterTokensRead(bool lHigh) =>
        _lFilterOutletEngine.LEngineFilter.LFilterTokensRead(lHigh);

    public (IReadOnlyList<double> LFilterValues, int LFilterPoles, string? LFilterPreset) LFilterOriginRead(
        bool lHigh) =>
        _lFilterOutletEngine.LEngineFilter.LFilterOriginRead(lHigh);

    public IReadOnlyList<double> LFilterDefaultRead(bool lHigh, string? lToken, double lFrequency) =>
        _lFilterOutletEngine.LEngineFilter.LFilterDefaultRead(lHigh, lToken, lFrequency);

    public (IReadOnlyList<double> LFilterValues, int LFilterPoles) LFilterNormalize(
        bool lHigh, IReadOnlyList<double> lValues, int lPoles) =>
        _lFilterOutletEngine.LEngineFilter.LFilterNormalize(lHigh, lValues, lPoles);

    public (IReadOnlyList<double> LFilterValues, int LFilterPoles)? LFilterPresetRead(bool lHigh, string lToken) =>
        _lFilterOutletEngine.LEngineFilter.LFilterPresetRead(lHigh, lToken);

    public string? LFilterMatchFind(bool lHigh, IReadOnlyList<double> lValues, int lPoles) =>
        _lFilterOutletEngine.LEngineFilter.LFilterMatchFind(lHigh, lValues, lPoles);
}
