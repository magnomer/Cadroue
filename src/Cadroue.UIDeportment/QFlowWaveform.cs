using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record QFlowWaveformPoint(double QFlowPointX, double QFlowPointY);

public sealed record QFlowWaveformOutline(
    double QFlowOutlineX,
    double QFlowOutlineY,
    IReadOnlyList<QFlowWaveformPoint> QFlowOutlinePoints);

public sealed class QFlowWaveform
{
    private readonly QFlow qFlow;
    private readonly LWaveformOrchestrator qWaveformOrchestrator = new();

    public QFlowWaveform(QFlow lOwner)
    {
        qFlow = lOwner;
        qWaveformOrchestrator.LWaveformReady += QFlowWaveformHandle;
    }

    public event Action? QFlowWaveformReady;
    public event Action<byte[]>? QFlowWaveformUpdated;

    public byte[] QFlowWaveformPeaks =>
        qFlow.QFlowWaveformActive ? qWaveformOrchestrator.LWaveformCurrent : Array.Empty<byte>();

    public void QFlowWaveformStart()
    {
        if (qFlow.QFlowUnloaded || !qFlow.QFlowWaveformActive)
        {
            return;
        }

        qWaveformOrchestrator.LWaveformStart(qFlow.QFlowSourcePath, qFlow.QFlowDuration, qFlow.QFlowWaveformAudio);
    }

    public void QFlowWaveformClear() => qWaveformOrchestrator.LWaveformClear();

    public void QFlowWaveformClose()
    {
        qWaveformOrchestrator.LWaveformReady -= QFlowWaveformHandle;
        qWaveformOrchestrator.Dispose();
    }

    public void QFlowWaveformApply()
    {
        if (qFlow.QFlowUnloaded)
        {
            return;
        }

        QFlowWaveformUpdated?.Invoke(QFlowWaveformPeaks);
    }

    private void QFlowWaveformHandle(LWaveformNotice lNotice)
    {
        if (qFlow.QFlowUnloaded)
        {
            return;
        }

        QFlowWaveformReady?.Invoke();
    }

    public static QFlowWaveformOutline QFlowOutlineResolve(
        byte[] lPeaks,
        double lWidth,
        double lRailTop,
        double lRailHeight,
        TimeSpan lRangeStart,
        TimeSpan lRangeEnd)
    {
        double lCenterY = lRailTop + lRailHeight / 2;
        int lColumnCount = (int)Math.Ceiling(lWidth);
        if (lColumnCount <= 1 || lRailHeight <= 2)
        {
            return new QFlowWaveformOutline(0, lCenterY, Array.Empty<QFlowWaveformPoint>());
        }

        double[] lColumns = LWaveform.LWaveformRangeRead(lPeaks, lRangeStart, lRangeEnd, lColumnCount);
        if (lColumns.Length == 0)
        {
            return new QFlowWaveformOutline(0, lCenterY, Array.Empty<QFlowWaveformPoint>());
        }

        double lColumnWidth = lWidth / lColumnCount;
        double lHalfHeight = lRailHeight / 2 - 1;
        var lPoints = new List<QFlowWaveformPoint>(lColumns.Length * 2);
        for (int lColumn = 1; lColumn < lColumns.Length; lColumn++)
        {
            lPoints.Add(new QFlowWaveformPoint(lColumn * lColumnWidth, lCenterY - lColumns[lColumn] * lHalfHeight));
        }

        for (int lColumn = lColumns.Length - 1; lColumn >= 0; lColumn--)
        {
            lPoints.Add(new QFlowWaveformPoint(lColumn * lColumnWidth, lCenterY + lColumns[lColumn] * lHalfHeight));
        }

        return new QFlowWaveformOutline(0, lCenterY - lColumns[0] * lHalfHeight, lPoints);
    }
}
