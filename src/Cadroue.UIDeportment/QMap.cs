using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public enum QMapShapeKind
{
    QMapShapeBackground,
    QMapShapeRail,
    QMapShapeWaveform,
    QMapShapeCoverage,
    QMapShapeScanned,
    QMapShapeFill,
    QMapShapeShadow,
    QMapShapeRim,
    QMapShapeSide,
    QMapShapeGrip,
    QMapShapeBody,
    QMapShapeDot,
    QMapShapeShine,
    QMapShapeBorder,
}

public enum QMapHitKind
{
    QMapHitNone,
    QMapHitOrigin,
    QMapHitLimit,
    QMapHitBody,
    QMapHitCursor,
}

public sealed record QMapShape(
    QMapShapeKind QMapShapeKind,
    double QMapShapeLeft,
    double QMapShapeTop,
    double QMapShapeRight,
    double QMapShapeBottom,
    double QMapShapeRadius);

public sealed record QMapBand(
    double QMapBandLeft,
    double QMapBandTop,
    double QMapBandRight,
    double QMapBandBottom,
    int QMapBandColor,
    string QMapBandBadge,
    bool QMapBandShown,
    bool QMapBandSelected);

public sealed record QMapBadge(
    double QMapBadgeLeft,
    double QMapBadgeTop,
    double QMapBadgeRight,
    double QMapBadgeBottom,
    double QMapBadgeRadius,
    double QMapBadgeX,
    double QMapBadgeY);

// The map's wiring: hands pointer input and the surface's size to the map gate, paints its frame as shapes and
// relays its drag and range notices to the flow. It holds no map state.
public sealed class QMap
{
    internal const double QMapRailTop = CMap.CMapRailTop;
    internal const double QMapHandleWidth = CMap.CMapHandleWidth;

    private static readonly IReadOnlyDictionary<CMapHit, QMapHitKind> qMapHits = new Dictionary<CMapHit, QMapHitKind>
    {
        [CMapHit.CMapHitNone] = QMapHitKind.QMapHitNone,
        [CMapHit.CMapHitOrigin] = QMapHitKind.QMapHitOrigin,
        [CMapHit.CMapHitLimit] = QMapHitKind.QMapHitLimit,
        [CMapHit.CMapHitBody] = QMapHitKind.QMapHitBody,
        [CMapHit.CMapHitCursor] = QMapHitKind.QMapHitCursor,
    };

    private readonly QFlow qFlow;
    private readonly QChronometer qMapChronometer = new();

    public QMap(QFlow lOwner, CMap lGate)
    {
        qFlow = lOwner;
        QMapGate = lGate;
        lGate.CMapDrag.CMapDragChanged += QMapDragRaise;
        lGate.CMapDrag.CMapSpoolChanged += QMapSpoolRaise;
        QMapFlowAttach();
    }

    public event Action? QMapFrameApplied;

    public CMap QMapGate { get; }

    // One draw: the timer starts, the gate's frame is painted as shapes, and the gate records the draw with its time.
    public void QMapDraw(double lWidth, double lHeight, Action<QMapFrame> lPaint)
    {
        qMapChronometer.QChronometerStart();
        QMapGate.CMapDraw(
            lWidth,
            lHeight,
            lFrame => lPaint(QMapFrame.QMapFrameBuild(lFrame, lWidth, lHeight)),
            qMapChronometer.QChronometerRead);
    }

    public QMapHitKind QMapHoverResolve(double lX, double lY, double lWidth, double lHeight) =>
        qMapHits[QMapGate.CMapDrag.CMapHoverResolve(lX, lY, lWidth, lHeight)];

    public QMapHitKind QMapLeaveResolve() => qMapHits[QMapGate.CMapDrag.CMapLeaveResolve()];

    public bool QMapPressHandle(double lX, double lY, double lWidth, double lHeight) =>
        QMapGate.CMapDrag.CMapPressHandle(lX, lY, lWidth, lHeight);

    public bool QMapMoveHandle(double lX, double lWidth) => QMapGate.CMapDrag.CMapMoveHandle(lX, lWidth);

    public void QMapDragClear() => QMapGate.CMapDrag.CMapDragClear();

    // Each flow notice that redraws the map is its own handler, naming itself as the draw's trigger.
    private void QMapFlowAttach()
    {
        qFlow.QFlowAttachApplied += QMapAttachHandle;
        qFlow.QFlowClearApplied += QMapClearHandle;
        qFlow.QFlowCursorApplied += QMapCursorHandle;
        qFlow.QFlowSpool.QFlowSpoolApplied += QMapSpoolHandle;
        qFlow.QFlowSection.QFlowSectionChanged += (_, _) => QMapSectionsHandle();
        qFlow.QFlowKeyframe.QFlowKeyframeChanged += QMapKeyframesHandle;
        qFlow.QFlowWaveform.QFlowWaveformUpdated += QMapWaveformHandle;
    }

    private void QMapAttachHandle() => QMapTriggerApply("attach");

    private void QMapClearHandle() => QMapTriggerApply("clear");

    private void QMapCursorHandle() => QMapTriggerApply("cursor");

    private void QMapSpoolHandle() => QMapTriggerApply("spool");

    private void QMapSectionsHandle() => QMapTriggerApply("sections");

    private void QMapKeyframesHandle() => QMapTriggerApply("keyframes");

    private void QMapWaveformHandle() => QMapTriggerApply("waveform");

    private void QMapTriggerApply(string lTrigger)
    {
        QMapGate.CMapTriggerSet(lTrigger);
        QMapFrameApplied?.Invoke();
    }

    private void QMapDragRaise(bool lDragging) => qFlow.QFlowWheel.QFlowDragHandle(lDragging);

    private void QMapSpoolRaise() => qFlow.QFlowSpool.QFlowSpoolHandle();
}
