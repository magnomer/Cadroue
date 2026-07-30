using System.Windows.Threading;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PFlow
{
    private readonly LWaveformOrchestrator lWaveformOrchestrator = new();
    private bool pFlowWaveformShow = App.LPreferenceStateCurrent.LPreferenceWaveformShow;

    public event Action<bool>? PFlowWaveformChange;

    public bool PFlowWaveformCheck() => pFlowWaveformShow;

    public void PFlowWaveformSet(bool pFlowWaveformRequest)
    {
        if (pFlowWaveformShow == pFlowWaveformRequest)
        {
            return;
        }

        pFlowWaveformShow = pFlowWaveformRequest;
        PFlowWaveformApply();
        PFlowWaveformStart();
        PFlowWaveformChange?.Invoke(pFlowWaveformShow);
    }

    private void PFlowWaveformStart()
    {
        if (pFlowUnloaded || !pFlowWaveformShow)
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

        byte[] pFlowWaveformPeaks = pFlowWaveformShow
            ? lWaveformOrchestrator.LWaveformCurrent
            : Array.Empty<byte>();
        pViewfinder.PViewfinderWaveformUpdate(pFlowWaveformPeaks);
        pMap.PMapWaveformUpdate(pFlowWaveformPeaks);
    }
}
