using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using Cadroue.Application;

namespace Cadroue.UIVeneer.PBench;

public sealed partial class PFlow
{
    private readonly LWaveformOrchestrator lWaveformOrchestrator = new();

    public static Geometry? PFlowWaveformBuild(
        byte[] pFlowWaveformPeaks,
        double pFlowWaveformWidth,
        double pFlowWaveformRailTop,
        double pFlowWaveformRailHeight,
        TimeSpan pFlowWaveformRangeStart,
        TimeSpan pFlowWaveformRangeEnd)
    {
        int pFlowWaveformColumnCount = (int)Math.Ceiling(pFlowWaveformWidth);
        if (pFlowWaveformColumnCount <= 1 || pFlowWaveformRailHeight <= 2)
        {
            return null;
        }

        double[] pFlowWaveformColumns = LWaveform.LWaveformRangeRead(
            pFlowWaveformPeaks, pFlowWaveformRangeStart, pFlowWaveformRangeEnd, pFlowWaveformColumnCount);
        if (pFlowWaveformColumns.Length == 0)
        {
            return null;
        }

        double pFlowWaveformColumnWidth = pFlowWaveformWidth / pFlowWaveformColumnCount;
        double pFlowWaveformCenterY = pFlowWaveformRailTop + pFlowWaveformRailHeight / 2;
        double pFlowWaveformHalfHeight = pFlowWaveformRailHeight / 2 - 1;
        var pFlowWaveformGeometry = new StreamGeometry();
        using (StreamGeometryContext pFlowWaveformContext = pFlowWaveformGeometry.Open())
        {
            pFlowWaveformContext.BeginFigure(
                new Point(0, pFlowWaveformCenterY - pFlowWaveformColumns[0] * pFlowWaveformHalfHeight), true, true);
            for (int pFlowWaveformColumn = 1; pFlowWaveformColumn < pFlowWaveformColumns.Length; pFlowWaveformColumn++)
            {
                pFlowWaveformContext.LineTo(
                    new Point(
                        pFlowWaveformColumn * pFlowWaveformColumnWidth,
                        pFlowWaveformCenterY - pFlowWaveformColumns[pFlowWaveformColumn] * pFlowWaveformHalfHeight),
                    false,
                    false);
            }

            for (int pFlowWaveformColumn = pFlowWaveformColumns.Length - 1;
                pFlowWaveformColumn >= 0;
                pFlowWaveformColumn--)
            {
                pFlowWaveformContext.LineTo(
                    new Point(
                        pFlowWaveformColumn * pFlowWaveformColumnWidth,
                        pFlowWaveformCenterY + pFlowWaveformColumns[pFlowWaveformColumn] * pFlowWaveformHalfHeight),
                    false,
                    false);
            }
        }

        pFlowWaveformGeometry.Freeze();
        return pFlowWaveformGeometry;
    }
    public void PFlowWaveformSet(bool pFlowWaveformRequest)
    {
        if (!LFlow.LFlowWaveformSet(pFlowWaveformRequest))
        {
            return;
        }

        PFlowWaveformApply();
        PFlowWaveformStart();
    }

    private void PFlowWaveformStart()
    {
        if (LFlow.LFlowUnloaded || !LFlow.LFlowWaveformActive)
        {
            return;
        }

        lWaveformOrchestrator.LWaveformStart(
            LFlow.LFlowSourcePath,
            LFlow.LFlowDuration,
            LFlow.LFlowWaveformAudio);
    }

    private void PFlowWaveformClear()
    {
        lWaveformOrchestrator.LWaveformClear();
    }

    private void PFlowWaveformClose()
    {
        lWaveformOrchestrator.LWaveformReady -= PFlowWaveformHandle;
        lWaveformOrchestrator.Dispose();
    }

    private void PFlowWaveformHandle(LWaveformNotice pFlowWaveformNotice)
    {
        if (LFlow.LFlowUnloaded || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
        {
            return;
        }

        Dispatcher.InvokeAsync(PFlowWaveformApply, DispatcherPriority.Background);
    }

    private void PFlowWaveformApply()
    {
        if (LFlow.LFlowUnloaded)
        {
            return;
        }

        byte[] pFlowWaveformPeaks = LFlow.LFlowWaveformActive
            ? lWaveformOrchestrator.LWaveformCurrent
            : Array.Empty<byte>();
        pViewfinder.PViewfinderWaveformUpdate(pFlowWaveformPeaks);
        pMap.PMapWaveformUpdate(pFlowWaveformPeaks);
    }
}
