using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// The pass filter rules as the engine has them, each call recorded by name.
internal sealed class TFilterPort : LFilterPort
{
    private readonly LFilterPort tFilterOutlet = Interface.TInterface.TFilterOutletCreate();

    public List<string> TFilterCalls { get; } = [];

    public IReadOnlyList<double> LFilterLeastsRead(bool lHigh) => tFilterOutlet.LFilterLeastsRead(lHigh);

    public IReadOnlyList<double> LFilterMostsRead(bool lHigh) => tFilterOutlet.LFilterMostsRead(lHigh);

    public IReadOnlyList<string> LFilterTokensRead(bool lHigh) => tFilterOutlet.LFilterTokensRead(lHigh);

    public (IReadOnlyList<double> LFilterValues, int LFilterPoles, string? LFilterPreset) LFilterOriginRead(
        bool lHigh) => tFilterOutlet.LFilterOriginRead(lHigh);

    public IReadOnlyList<double> LFilterDefaultRead(bool lHigh, string? lToken, double lFrequency) =>
        tFilterOutlet.LFilterDefaultRead(lHigh, lToken, lFrequency);

    public (IReadOnlyList<double> LFilterValues, int LFilterPoles) LFilterNormalize(
        bool lHigh, IReadOnlyList<double> lValues, int lPoles)
    {
        TFilterCalls.Add("normalize");
        return tFilterOutlet.LFilterNormalize(lHigh, lValues, lPoles);
    }

    public (IReadOnlyList<double> LFilterValues, int LFilterPoles)? LFilterPresetRead(bool lHigh, string lToken)
    {
        TFilterCalls.Add("preset");
        return tFilterOutlet.LFilterPresetRead(lHigh, lToken);
    }

    public string? LFilterMatchFind(bool lHigh, IReadOnlyList<double> lValues, int lPoles) =>
        tFilterOutlet.LFilterMatchFind(lHigh, lValues, lPoles);
}
