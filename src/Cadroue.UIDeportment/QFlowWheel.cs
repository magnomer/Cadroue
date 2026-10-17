using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The wheel and drag wiring: hands each wheel turn and drag to the flow gate and relays its answers to the spool and
// the viewer. It keeps no wheel or drag state, only the viewer's playing seam.
public sealed class QFlowWheel
{
    private readonly QFlow qFlow;
    private readonly CFlow qFlowWheelGate;
    private Func<bool>? qFlowPlayingSource;

    public QFlowWheel(QFlow lOwner, CFlow lGate)
    {
        qFlow = lOwner;
        qFlowWheelGate = lGate;
        lGate.CFlowZoomed += QFlowZoomRaise;
        lGate.CFlowDragChanged += QFlowDragRaise;
        lGate.CFlowVolumeAdjusted += QFlowVolumeRaise;
    }

    public event Action<bool>? QFlowDragChanged;
    public event Action<double>? QFlowVolumeAdjusted;

    // The viewer's playing seam, handed on unread for the gate to ask.
    public void QFlowPlayingAttach(Func<bool>? lPlayingSource) => qFlowPlayingSource = lPlayingSource;

    // The raw turn and its count of wheel notches; the gate decides what the turn does.
    public bool QFlowWheelHandle(int lDelta) =>
        qFlowWheelGate.CFlowWheelRun(lDelta, QCursor.QCursorWheelResolve(lDelta));

    public void QFlowZoom(int lSteps) => qFlowWheelGate.CFlowZoom(lSteps);

    public void QFlowDragHandle(bool lDragging) => qFlowWheelGate.CFlowDragSet(lDragging, qFlowPlayingSource);

    private void QFlowZoomRaise() => qFlow.QFlowSpool.QFlowSpoolHandle();

    private void QFlowDragRaise(bool lDragging) => QFlowDragChanged?.Invoke(lDragging);

    private void QFlowVolumeRaise(double lVolume) => QFlowVolumeAdjusted?.Invoke(lVolume);
}
