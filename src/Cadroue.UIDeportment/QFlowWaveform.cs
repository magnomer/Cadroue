using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QFlowWaveformPoint(double QFlowPointX, double QFlowPointY);

public sealed record QFlowWaveformOutline(
    double QFlowOutlineX,
    double QFlowOutlineY,
    IReadOnlyList<QFlowWaveformPoint> QFlowOutlinePoints);

// The waveform's wiring: relays the gate's notices to the parts and paints the outline the gate answers.
public sealed class QFlowWaveform
{
    public QFlowWaveform(CWaveform lGate)
    {
        QFlowWaveformGate = lGate;
        lGate.CWaveformArrived += QFlowWaveformHandle;
        lGate.CWaveformChanged += QFlowWaveformRaise;
    }

    // Told on the scanner's thread; the surface hands QFlowWaveformApply to the UI thread.
    public event Action? QFlowWaveformReady;

    public event Action? QFlowWaveformUpdated;

    public CWaveform QFlowWaveformGate { get; }

    public void QFlowWaveformApply() => QFlowWaveformGate.CWaveformApply();

    public QFlowWaveformOutline QFlowOutlineResolve(
        double lWidth,
        double lRailTop,
        double lRailHeight,
        TimeSpan lRangeStart,
        TimeSpan lRangeEnd)
    {
        (double lOriginX, double lOriginY, IReadOnlyList<(double, double)> lPoints) =
            QFlowWaveformGate.CWaveformOutlineResolve(lWidth, lRailTop, lRailHeight, lRangeStart, lRangeEnd);
        return new QFlowWaveformOutline(
            lOriginX,
            lOriginY,
            lPoints.Select(lPoint => new QFlowWaveformPoint(lPoint.Item1, lPoint.Item2)).ToArray());
    }

    private void QFlowWaveformHandle() => QFlowWaveformReady?.Invoke();

    private void QFlowWaveformRaise() => QFlowWaveformUpdated?.Invoke();
}
