using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// The leveling rules as the engine has them, each changing call recorded by name.
internal sealed class TLoudnessPort : LLoudnessPort
{
    private readonly LLoudnessPort tLoudnessOutlet = Interface.TInterface.TLoudnessOutletCreate();

    public List<string> TLoudnessCalls { get; } = [];

    public IReadOnlyList<double> LLoudnessLeastsRead(bool lDynamic) => tLoudnessOutlet.LLoudnessLeastsRead(lDynamic);

    public IReadOnlyList<double> LLoudnessMostsRead(bool lDynamic) => tLoudnessOutlet.LLoudnessMostsRead(lDynamic);

    public IReadOnlyList<string> LLoudnessTokensRead(bool lDynamic) => tLoudnessOutlet.LLoudnessTokensRead(lDynamic);

    public (bool LLoudnessDynamic, IReadOnlyList<double> LLoudnessValues, bool LLoudnessTwoPass,
        string? LLoudnessPreset) LLoudnessOriginRead() => tLoudnessOutlet.LLoudnessOriginRead();

    public IReadOnlyList<double> LLoudnessDefaultRead(bool lDynamic, string? lToken) =>
        tLoudnessOutlet.LLoudnessDefaultRead(lDynamic, lToken);

    public IReadOnlyList<double> LLoudnessNormalize(IReadOnlyList<double> lValues)
    {
        TLoudnessCalls.Add("normalize");
        return tLoudnessOutlet.LLoudnessNormalize(lValues);
    }

    public IReadOnlyList<double>? LLoudnessPresetRead(bool lDynamic, string lToken, IReadOnlyList<double> lValues)
    {
        TLoudnessCalls.Add("preset");
        return tLoudnessOutlet.LLoudnessPresetRead(lDynamic, lToken, lValues);
    }

    public string? LLoudnessMatchFind(bool lDynamic, IReadOnlyList<double> lValues) =>
        tLoudnessOutlet.LLoudnessMatchFind(lDynamic, lValues);
}
