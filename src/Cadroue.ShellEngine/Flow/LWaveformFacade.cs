using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One flow's waveform scan: the calls over one scanner the rig hands in. Whether a scan runs and when its notice
// reaches the readers is the gate's.
public sealed class LWaveformFacade : LWaveformPort
{
    private readonly LRigWaveform _lWaveformFacadeRig;

    internal LWaveformFacade(LRigWaveform lRig)
    {
        ArgumentNullException.ThrowIfNull(lRig);
        _lWaveformFacadeRig = lRig;
        lRig.LRigWaveformAttachment(LWaveformHandle);
    }

    public event Action? LWaveformArrived;

    public bool LWaveformPeaksCheck() => _lWaveformFacadeRig.LRigWaveformPeaks().Length > 0;

    public void LWaveformStart(string? lSource, TimeSpan lDuration, bool lAudio) =>
        _lWaveformFacadeRig.LRigWaveformScanner(lSource, lDuration, lAudio);

    public void LWaveformClear() => _lWaveformFacadeRig.LRigWaveformClearance();

    public double[]? LWaveformRangeRead(TimeSpan lStart, TimeSpan lEnd, int lColumns)
    {
        byte[] lPeaks = _lWaveformFacadeRig.LRigWaveformPeaks();
        return lPeaks.Length == 0 ? null : _lWaveformFacadeRig.LRigWaveformRange(lPeaks, lStart, lEnd, lColumns);
    }

    public void LWaveformClose() => _lWaveformFacadeRig.LRigWaveformDisposal();

    private void LWaveformHandle() => LWaveformArrived?.Invoke();
}
