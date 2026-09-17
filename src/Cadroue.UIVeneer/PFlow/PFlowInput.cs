using System.Windows.Input;
using Cadroue.Core;

using Cadroue.Application;

namespace Cadroue.UIVeneer.PFlow;

public sealed partial class PFlow
{
    private bool pFlowDragPaused;

    public event Action<bool>? PFlowDragChange;

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!pFlowCommandActive || e.Delta == 0) return;
        int pWheelSteps = e.Delta / 120;
        if (pWheelSteps == 0) pWheelSteps = e.Delta > 0 ? 1 : -1;

        switch (LPreference.LPreferenceStateCurrent.LPreferenceWheelAction)
        {
            case "Zoom":
                PFlowWheelZoom(pWheelSteps);
                break;
            case "Volume":
                PFlowVolumeRaise(pFlowVolumeCurrent + pWheelSteps * PFlowVolumeStep);
                break;
            default:
                PFlowWheelSeek(pWheelSteps);
                break;
        }

        e.Handled = true;
    }

    private void PFlowWheelSeek(int pWheelSteps)
    {
        if (lSpool is null) return;
        PFlowCursorSeek(PFlowCursorClamp(lCursor + lSpool.LSpoolStepResolve(pWheelSteps)));
    }

    private void PFlowWheelZoom(int pWheelSteps)
    {
        if (lSpool is null) return;
        lSpool.LSpoolZoom(lCursor, pWheelSteps);
        PFlowSpoolHandle();
    }

    internal void PFlowDragSet(bool pFlowDragging)
    {
        PFlowDragChange?.Invoke(pFlowDragging);
        if (!pFlowCommandActive || !LPreference.LPreferenceStateCurrent.LPreferenceDragPaused) return;

        if (pFlowDragging)
        {
            if (pFlowDragPaused || PFlowPlayingSource?.Invoke() != true) return;
            pFlowDragPaused = true;
            PFlowPauseRaise();
            return;
        }

        if (!pFlowDragPaused) return;
        pFlowDragPaused = false;
        PFlowPlayRaise();
    }
}
