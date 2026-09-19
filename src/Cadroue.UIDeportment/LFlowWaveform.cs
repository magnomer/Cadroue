using Cadroue.Infrastructure;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record LFlowWaveformPoint(double LFlowPointX, double LFlowPointY);

public sealed record LFlowWaveformOutline(
    double LFlowOutlineX,
    double LFlowOutlineY,
    IReadOnlyList<LFlowWaveformPoint> LFlowOutlinePoints);

public sealed class LFlowWaveform
{
    private readonly LFlow lFlow;
    private readonly LWaveformOrchestrator lWaveformOrchestrator = new();

    public LFlowWaveform(LFlow lOwner)
    {
        lFlow = lOwner;
        lWaveformOrchestrator.LWaveformReady += LFlowWaveformHandle;
    }

    public event Action? LFlowWaveformReady;
    public event Action<byte[]>? LFlowWaveformUpdate;

    public byte[] LFlowWaveformPeaks =>
        lFlow.LFlowWaveformActive ? lWaveformOrchestrator.LWaveformCurrent : Array.Empty<byte>();

    public void LFlowWaveformStart()
    {
        if (lFlow.LFlowUnloaded || !lFlow.LFlowWaveformActive)
        {
            return;
        }

        lWaveformOrchestrator.LWaveformStart(lFlow.LFlowSourcePath, lFlow.LFlowDuration, lFlow.LFlowWaveformAudio);
    }

    public void LFlowWaveformClear() => lWaveformOrchestrator.LWaveformClear();

    public void LFlowWaveformClose()
    {
        lWaveformOrchestrator.LWaveformReady -= LFlowWaveformHandle;
        lWaveformOrchestrator.Dispose();
    }

    public void LFlowWaveformApply()
    {
        if (lFlow.LFlowUnloaded)
        {
            return;
        }

        LFlowWaveformUpdate?.Invoke(LFlowWaveformPeaks);
    }

    private void LFlowWaveformHandle(LWaveformNotice lNotice)
    {
        if (lFlow.LFlowUnloaded)
        {
            return;
        }

        LFlowWaveformReady?.Invoke();
    }

    public static LFlowWaveformOutline LFlowOutlineResolve(
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
            return new LFlowWaveformOutline(0, lCenterY, Array.Empty<LFlowWaveformPoint>());
        }

        double[] lColumns = LWaveform.LWaveformRangeRead(lPeaks, lRangeStart, lRangeEnd, lColumnCount);
        if (lColumns.Length == 0)
        {
            return new LFlowWaveformOutline(0, lCenterY, Array.Empty<LFlowWaveformPoint>());
        }

        double lColumnWidth = lWidth / lColumnCount;
        double lHalfHeight = lRailHeight / 2 - 1;
        var lPoints = new List<LFlowWaveformPoint>(lColumns.Length * 2);
        for (int lColumn = 1; lColumn < lColumns.Length; lColumn++)
        {
            lPoints.Add(new LFlowWaveformPoint(lColumn * lColumnWidth, lCenterY - lColumns[lColumn] * lHalfHeight));
        }

        for (int lColumn = lColumns.Length - 1; lColumn >= 0; lColumn--)
        {
            lPoints.Add(new LFlowWaveformPoint(lColumn * lColumnWidth, lCenterY + lColumns[lColumn] * lHalfHeight));
        }

        return new LFlowWaveformOutline(0, lCenterY - lColumns[0] * lHalfHeight, lPoints);
    }
}
