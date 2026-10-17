using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class QFlowWheel
{
    private readonly QFlow qFlow;
    private bool qFlowDragPaused;
    private Func<bool>? qFlowPlayingSource;

    public QFlowWheel(QFlow lOwner)
    {
        qFlow = lOwner;
    }

    public event Action<bool>? QFlowDragChanged;
    public event Action<double>? QFlowVolumeAdjusted;

    public bool QFlowDragPaused => qFlowDragPaused;

    public void QFlowPlayingAttach(Func<bool>? lPlayingSource) => qFlowPlayingSource = lPlayingSource;

    public bool QFlowWheelHandle(int lDelta)
    {
        if (!qFlow.QFlowCommandActive || lDelta == 0)
        {
            return false;
        }

        int lSteps = QCursor.QCursorWheelResolve(lDelta);
        switch (LPreference.LPreferenceStateCurrent.LPreferenceWheelAction)
        {
            case "Zoom":
                QFlowZoom(lSteps);
                break;
            case "Volume":
                QFlowVolumeAdjusted?.Invoke(QFlowVolumeResolve(lSteps));
                break;
            default:
                QFlowWheelSeek(lSteps);
                break;
        }

        return true;
    }

    private static double QFlowVolumeResolve(int lSteps) => lSteps * QCursor.QCursorVolumeStep;

    public void QFlowZoom(int lSteps)
    {
        if (qFlow.QFlowRange is not { } lSpool)
        {
            return;
        }

        lSpool.LSpoolZoom(qFlow.QFlowCursor, lSteps);
        qFlow.QFlowSpool.QFlowSpoolHandle();
    }

    private void QFlowWheelSeek(int lSteps)
    {
        if (qFlow.QFlowRange is not { } lSpool)
        {
            return;
        }

        qFlow.QFlowCursorSeek(qFlow.QFlowCursorClamp(qFlow.QFlowCursor + lSpool.LSpoolStepResolve(lSteps)));
    }

    public void QFlowDragHandle(bool lDragging)
    {
        QFlowDragChanged?.Invoke(lDragging);
        if (!qFlow.QFlowCommandActive || !LPreference.LPreferenceStateCurrent.LPreferenceDragPaused)
        {
            return;
        }

        if (lDragging)
        {
            if (qFlowDragPaused || qFlowPlayingSource?.Invoke() != true)
            {
                return;
            }

            QFlowPausedSet(true);
            qFlow.QFlowPauseRaise();
            return;
        }

        if (!qFlowDragPaused)
        {
            return;
        }

        QFlowPausedSet(false);
        qFlow.QFlowPlayRaise();
    }

    public void QFlowPausedSet(bool lDragPaused) => qFlowDragPaused = lDragPaused;
}
