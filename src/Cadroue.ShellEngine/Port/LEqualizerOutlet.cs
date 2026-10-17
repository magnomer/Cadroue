namespace Cadroue.ShellEngine;

// The engine's face for the equalizer gate: every member forwards to the same-named member of the engine's
// equalizer facade. It rejects a null engine and keeps it for every forward.
public sealed class LEqualizerOutlet : LEqualizerPort
{
    private readonly LEngine _lEqualizerOutletEngine;

    public LEqualizerOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lEqualizerOutletEngine = lEngine;
    }

    public double LEqualizerGainLeast => _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerGainLeast;

    public double LEqualizerGainMost => _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerGainMost;

    public double LEqualizerFrequencyOrigin => _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerFrequencyOrigin;

    public IReadOnlyList<string> LEqualizerTokensRead() =>
        _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerTokensRead();

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerOriginRead() =>
        _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerOriginRead();

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandSet(
        IReadOnlyList<(double, double)> lBands, int lIndex, double lFrequency, double lGain) =>
        _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerBandSet(lBands, lIndex, lFrequency, lGain);

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandAdd(
        IReadOnlyList<(double, double)> lBands) =>
        _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerBandAdd(lBands);

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandRemove(
        IReadOnlyList<(double, double)> lBands, int lIndex) =>
        _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerBandRemove(lBands, lIndex);

    public (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset)? LEqualizerPresetRead(
        string lToken) =>
        _lEqualizerOutletEngine.LEngineEqualizer.LEqualizerPresetRead(lToken);
}
