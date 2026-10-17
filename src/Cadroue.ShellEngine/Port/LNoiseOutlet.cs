namespace Cadroue.ShellEngine;

// The engine's face for the noise gate: every member forwards to the same-named member of the engine's noise
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LNoiseOutlet : LNoisePort
{
    private readonly LEngine _lNoiseOutletEngine;

    public LNoiseOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lNoiseOutletEngine = lEngine;
    }

    public IReadOnlyList<double> LNoiseLeasts => _lNoiseOutletEngine.LEngineNoise.LNoiseLeasts;

    public IReadOnlyList<double> LNoiseMosts => _lNoiseOutletEngine.LEngineNoise.LNoiseMosts;

    public IReadOnlyList<string> LNoiseTokensRead() => _lNoiseOutletEngine.LEngineNoise.LNoiseTokensRead();

    public IReadOnlyList<string> LNoiseTypesRead() => _lNoiseOutletEngine.LEngineNoise.LNoiseTypesRead();

    public (IReadOnlyList<double> LNoiseValues, int LNoiseType, string? LNoisePreset) LNoiseOriginRead() =>
        _lNoiseOutletEngine.LEngineNoise.LNoiseOriginRead();

    public IReadOnlyList<double> LNoiseDefaultRead(string? lToken) =>
        _lNoiseOutletEngine.LEngineNoise.LNoiseDefaultRead(lToken);

    public IReadOnlyList<double> LNoiseNormalize(IReadOnlyList<double> lValues) =>
        _lNoiseOutletEngine.LEngineNoise.LNoiseNormalize(lValues);

    public (IReadOnlyList<double> LNoiseValues, int LNoiseType)? LNoisePresetRead(string lToken) =>
        _lNoiseOutletEngine.LEngineNoise.LNoisePresetRead(lToken);

    public string? LNoiseMatchFind(IReadOnlyList<double> lValues, int lType) =>
        _lNoiseOutletEngine.LEngineNoise.LNoiseMatchFind(lValues, lType);
}
