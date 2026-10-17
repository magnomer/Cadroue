namespace Cadroue.ShellEngine;

// The engine's face for the leveling gate: every member forwards to the same-named member of the engine's loudness
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LLoudnessOutlet : LLoudnessPort
{
    private readonly LEngine _lLoudnessOutletEngine;

    public LLoudnessOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lLoudnessOutletEngine = lEngine;
    }

    public IReadOnlyList<double> LLoudnessLeastsRead(bool lDynamic) =>
        _lLoudnessOutletEngine.LEngineLoudness.LLoudnessLeastsRead(lDynamic);

    public IReadOnlyList<double> LLoudnessMostsRead(bool lDynamic) =>
        _lLoudnessOutletEngine.LEngineLoudness.LLoudnessMostsRead(lDynamic);

    public IReadOnlyList<string> LLoudnessTokensRead(bool lDynamic) =>
        _lLoudnessOutletEngine.LEngineLoudness.LLoudnessTokensRead(lDynamic);

    public (bool LLoudnessDynamic, IReadOnlyList<double> LLoudnessValues, bool LLoudnessTwoPass,
        string? LLoudnessPreset) LLoudnessOriginRead() =>
        _lLoudnessOutletEngine.LEngineLoudness.LLoudnessOriginRead();

    public IReadOnlyList<double> LLoudnessDefaultRead(bool lDynamic, string? lToken) =>
        _lLoudnessOutletEngine.LEngineLoudness.LLoudnessDefaultRead(lDynamic, lToken);

    public IReadOnlyList<double> LLoudnessNormalize(IReadOnlyList<double> lValues) =>
        _lLoudnessOutletEngine.LEngineLoudness.LLoudnessNormalize(lValues);

    public IReadOnlyList<double>? LLoudnessPresetRead(bool lDynamic, string lToken, IReadOnlyList<double> lValues) =>
        _lLoudnessOutletEngine.LEngineLoudness.LLoudnessPresetRead(lDynamic, lToken, lValues);

    public string? LLoudnessMatchFind(bool lDynamic, IReadOnlyList<double> lValues) =>
        _lLoudnessOutletEngine.LEngineLoudness.LLoudnessMatchFind(lDynamic, lValues);
}
