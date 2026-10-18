namespace Cadroue.UIDeportment;

public sealed record QSMonitorScrollbar(
    double QSMonitorScrollViewport,
    double QSMonitorScrollMaximum,
    double QSMonitorScrollValue,
    bool QSMonitorScrollEnabled,
    double QSMonitorScrollOpacity);

// The monitor's visible window over the waveform: its zoom scale and its left offset as a fraction of the whole.
public sealed class QSMonitorViewport
{
    private const double QSMonitorZoomStep = 2;
    private const double QSMonitorZoomMost = 32;
    private const double QSMonitorScrollDim = 0.35;

    private double qMonitorScale = 1;
    private double qMonitorOffset;

    public event Action? QSMonitorZoomChanged;

    public double QSMonitorScale => qMonitorScale;

    public double QSMonitorOffset => qMonitorOffset;

    public QSMonitorScrollbar QSMonitorScrollRead()
    {
        double lViewport = 1.0 / qMonitorScale;
        bool lEnabled = qMonitorScale > 1;
        return new QSMonitorScrollbar(
            lViewport, 1 - lViewport, qMonitorOffset, lEnabled, lEnabled ? 1 : QSMonitorScrollDim);
    }

    public void QSMonitorIncreaseZoom() => QSMonitorZoom(QSMonitorZoomStep, QSMonitorZoomMost);

    public void QSMonitorDecreaseZoom() => QSMonitorZoom(1 / QSMonitorZoomStep, QSMonitorZoomMost);

    public void QSMonitorZoom(double lFactor, double lMost)
    {
        (qMonitorScale, qMonitorOffset) = QSMonitorZoomResolve(lFactor, lMost);
        QSMonitorZoomChanged?.Invoke();
    }

    // A zoom keeps the window's centre where it was, as far as the window still fits inside the whole.
    private (double, double) QSMonitorZoomResolve(double lFactor, double lMost)
    {
        double lCenter = qMonitorOffset + 1.0 / qMonitorScale / 2;
        double lScale = Math.Clamp(qMonitorScale * lFactor, 1, lMost);
        double lViewport = 1.0 / lScale;
        return (lScale, Math.Clamp(lCenter - lViewport / 2, 0, 1 - lViewport));
    }

    private double QSMonitorOffsetClamp(double lOffset) =>
        Math.Clamp(lOffset, 0, Math.Max(0, 1 - 1.0 / qMonitorScale));

    public void QSMonitorOffsetSet(double lOffset)
    {
        double lNormal = QSMonitorOffsetClamp(lOffset);
        if (Math.Abs(lNormal - qMonitorOffset) <= double.Epsilon)
        {
            return;
        }

        qMonitorOffset = lNormal;
        QSMonitorZoomChanged?.Invoke();
    }

    public double QSMonitorFractionResolve(double lLocal) =>
        Math.Clamp(qMonitorOffset + Math.Clamp(lLocal, 0, 1) / qMonitorScale, 0, 1);

    public double QSMonitorLocalResolve(double lFraction) => (lFraction - qMonitorOffset) * qMonitorScale;

    public double QSMonitorColumnRead(double[] lEnvelope, int lColumn, int lColumns) =>
        QSMonitorPlan.QSMonitorColumnResolve(lEnvelope, lColumn, lColumns, qMonitorScale, qMonitorOffset);
}
