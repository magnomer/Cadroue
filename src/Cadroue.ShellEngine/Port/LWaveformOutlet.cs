using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One flow's waveform scan: the calls over one scanner the rig hands in. Whether a scan runs and when its notice
// reaches the readers is the gate's.
public sealed class LWaveformOutlet : LWaveformPort
{
    private readonly LRigWaveform _lWaveformOutletRig;

    public LWaveformOutlet(LRigWaveform lRig)
    {
        ArgumentNullException.ThrowIfNull(lRig);
        _lWaveformOutletRig = lRig;
        lRig.LRigWaveformAttachment(LWaveformHandle);
    }

    public event Action? LWaveformArrived;

    public bool LWaveformPeaksCheck() => _lWaveformOutletRig.LRigWaveformPeaks().Length > 0;

    public void LWaveformStart(string? lSource, TimeSpan lDuration, bool lAudio) =>
        _lWaveformOutletRig.LRigWaveformScanner(lSource, lDuration, lAudio);

    public void LWaveformClear() => _lWaveformOutletRig.LRigWaveformClearance();

    public double[]? LWaveformRangeRead(TimeSpan lStart, TimeSpan lEnd, int lColumns)
    {
        byte[] lPeaks = _lWaveformOutletRig.LRigWaveformPeaks();
        return lPeaks.Length == 0 ? null : _lWaveformOutletRig.LRigWaveformRange(lPeaks, lStart, lEnd, lColumns);
    }

    public void LWaveformClose() => _lWaveformOutletRig.LRigWaveformDisposal();

    private void LWaveformHandle() => LWaveformArrived?.Invoke();
}
