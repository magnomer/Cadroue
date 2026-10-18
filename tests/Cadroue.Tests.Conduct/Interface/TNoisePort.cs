using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// The noise rules as the engine has them, each call recorded by name.
internal sealed class TNoisePort : LNoisePort
{
    private readonly LNoisePort tNoiseOutlet = Interface.TInterface.TNoiseOutletCreate();

    public List<string> TNoiseCalls { get; } = [];

    public IReadOnlyList<double> LNoiseLeasts => tNoiseOutlet.LNoiseLeasts;

    public IReadOnlyList<double> LNoiseMosts => tNoiseOutlet.LNoiseMosts;

    public IReadOnlyList<string> LNoiseTokensRead() => tNoiseOutlet.LNoiseTokensRead();

    public IReadOnlyList<string> LNoiseTypesRead() => tNoiseOutlet.LNoiseTypesRead();

    public (IReadOnlyList<double> LNoiseValues, int LNoiseType, string? LNoisePreset) LNoiseOriginRead() =>
        tNoiseOutlet.LNoiseOriginRead();

    public IReadOnlyList<double> LNoiseDefaultRead(string? lToken) => tNoiseOutlet.LNoiseDefaultRead(lToken);

    public IReadOnlyList<double> LNoiseNormalize(IReadOnlyList<double> lValues)
    {
        TNoiseCalls.Add("normalize");
        return tNoiseOutlet.LNoiseNormalize(lValues);
    }

    public (IReadOnlyList<double> LNoiseValues, int LNoiseType)? LNoisePresetRead(string lToken)
    {
        TNoiseCalls.Add("preset");
        return tNoiseOutlet.LNoisePresetRead(lToken);
    }

    public string? LNoiseMatchFind(IReadOnlyList<double> lValues, int lType) =>
        tNoiseOutlet.LNoiseMatchFind(lValues, lType);
}
