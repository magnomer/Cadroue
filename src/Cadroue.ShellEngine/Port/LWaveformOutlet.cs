namespace Cadroue.ShellEngine;

// The engine's face for one flow's waveform scan: every member forwards to the same-named member of that flow's
// waveform facade, which the flow outlet has the engine make. It rejects a null facade and keeps it for every forward.
public sealed class LWaveformOutlet : LWaveformPort
{
    private readonly LWaveformFacade _lWaveformOutletEngine;

    internal LWaveformOutlet(LWaveformFacade lFacade)
    {
        ArgumentNullException.ThrowIfNull(lFacade);
        _lWaveformOutletEngine = lFacade;
    }

    public event Action? LWaveformArrived
    {
        add => _lWaveformOutletEngine.LWaveformArrived += value;
        remove => _lWaveformOutletEngine.LWaveformArrived -= value;
    }

    public bool LWaveformPeaksCheck() => _lWaveformOutletEngine.LWaveformPeaksCheck();

    public void LWaveformStart(string? lSource, TimeSpan lDuration, bool lAudio) =>
        _lWaveformOutletEngine.LWaveformStart(lSource, lDuration, lAudio);

    public void LWaveformClear() => _lWaveformOutletEngine.LWaveformClear();

    public double[]? LWaveformRangeRead(TimeSpan lStart, TimeSpan lEnd, int lColumns) =>
        _lWaveformOutletEngine.LWaveformRangeRead(lStart, lEnd, lColumns);

    public void LWaveformClose() => _lWaveformOutletEngine.LWaveformClose();
}
