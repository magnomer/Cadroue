namespace Cadroue.ShellEngine;

public interface LWaveformPort
{
    // Told on the scanner's thread when the peaks change or a scan starts.
    event Action? LWaveformArrived;

    // Peaks are held for the source last scanned.
    bool LWaveformPeaksCheck();

    void LWaveformStart(string? lSource, TimeSpan lDuration, bool lAudio);

    void LWaveformClear();

    // The level of each column over the range, from the peaks held; null when none are held.
    double[]? LWaveformRangeRead(TimeSpan lStart, TimeSpan lEnd, int lColumns);

    void LWaveformClose();
}
