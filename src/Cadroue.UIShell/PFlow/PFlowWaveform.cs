using Cadroue.Core;
using System.Windows.Threading;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private readonly LWaveformOrchestrator lWaveformOrchestrator = new();
    private bool pFlowWaveformActive = PProgram.LPreferenceStateCurrent.LPreferenceWaveform;

    public event Action<bool>? PFlowWaveformChange;

    public bool PFlowWaveformCheck() => pFlowWaveformActive;

    public void PFlowWaveformSet(bool pFlowWaveformRequest)
    {
        if (pFlowWaveformActive == pFlowWaveformRequest)
        {
            return;
        }

        pFlowWaveformActive = pFlowWaveformRequest;
        PFlowWaveformApply();
        PFlowWaveformStart();
        PFlowWaveformChange?.Invoke(pFlowWaveformActive);
    }

    private void PFlowWaveformStart()
    {
        if (pFlowUnloaded || !pFlowWaveformActive)
        {
            return;
        }

        lWaveformOrchestrator.LWaveformStart(lSourcePath, lSpool?.LSpoolDuration ?? TimeSpan.Zero);
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

    private void PFlowWaveformHandle(byte[] pFlowWaveformPeaks)
    {
        if (pFlowUnloaded || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
        {
            return;
        }

        Dispatcher.InvokeAsync(PFlowWaveformApply, DispatcherPriority.Background);
    }

    private void PFlowWaveformApply()
    {
        if (pFlowUnloaded)
        {
            return;
        }

        byte[] pFlowWaveformPeaks = pFlowWaveformActive
            ? lWaveformOrchestrator.LWaveformCurrent
            : Array.Empty<byte>();
        pViewfinder.PViewfinderWaveformUpdate(pFlowWaveformPeaks);
        pMap.PMapWaveformUpdate(pFlowWaveformPeaks);
    }
}
