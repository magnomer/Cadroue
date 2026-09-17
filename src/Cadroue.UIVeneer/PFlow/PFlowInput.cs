using System.Windows.Input;
using Cadroue.Core;

using Cadroue.Application;

namespace Cadroue.UIVeneer.PFlow;

public sealed partial class PFlow
{
    public event Action<bool>? PFlowDragChange;

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!LFlow.LFlowCommandActive || e.Delta == 0) return;
        int pWheelSteps = e.Delta / 120;
        if (pWheelSteps == 0) pWheelSteps = e.Delta > 0 ? 1 : -1;

        switch (LPreference.LPreferenceStateCurrent.LPreferenceWheelAction)
        {
            case "Zoom":
                PFlowWheelZoom(pWheelSteps);
                break;
            case "Volume":
                PFlowVolumeAdjust?.Invoke(pWheelSteps * PFlowVolumeStep);
                break;
            default:
                PFlowWheelSeek(pWheelSteps);
                break;
        }

        e.Handled = true;
    }

    private void PFlowWheelSeek(int pWheelSteps)
    {
        if (LFlow.LFlowSpool is not { } lSpool) return;
        PFlowCursorSeek(LFlow.LFlowCursorClamp(LFlow.LFlowCursor + lSpool.LSpoolStepResolve(pWheelSteps)));
    }

    private void PFlowWheelZoom(int pWheelSteps)
    {
        if (LFlow.LFlowSpool is not { } lSpool) return;
        lSpool.LSpoolZoom(LFlow.LFlowCursor, pWheelSteps);
        PFlowSpoolHandle();
    }

    internal void PFlowDragSet(bool pFlowDragging)
    {
        PFlowDragChange?.Invoke(pFlowDragging);
        if (!LFlow.LFlowCommandActive || !LPreference.LPreferenceStateCurrent.LPreferenceDragPaused) return;

        if (pFlowDragging)
        {
            if (LFlow.LFlowDragPaused || PFlowPlayingSource?.Invoke() != true) return;
            LFlow.LFlowPausedSet(true);
            PFlowPauseRaise();
            return;
        }

        if (!LFlow.LFlowDragPaused) return;
        LFlow.LFlowPausedSet(false);
        PFlowPlayRaise();
    }
}
