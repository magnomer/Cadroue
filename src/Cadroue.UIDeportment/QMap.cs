using Cadroue.Core;
using Cadroue.Infrastructure;

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

public sealed record QMapFrame(
    IReadOnlyList<QMapShape> QMapFrameUnder,
    QFlowWaveformOutline QMapFrameWaveform,
    IReadOnlyList<QMapBand> QMapFrameBands,
    IReadOnlyList<QMapShape> QMapFrameOver,
    IReadOnlyList<double> QMapFrameCursor);

public sealed class QMap
{
    private const double QMapRenderLeast = 12;
    internal const double QMapRailTop = 3;
    private const double QMapCoverageHeight = 3;
    internal const double QMapHandleWidth = 12;
    private const double QMapSectionInset = 1;

    private static readonly QFlowWaveformOutline qMapOutlineEmpty = new(0, 0, []);

    private readonly QFlow qFlow;
    private QMapHitKind qMapDragKind;
    private QMapHitKind qMapHoverKind;
    private double qMapDragOrigin;
    private double qMapPreviousX;
    private string qMapTrigger = "attach";
    private int qMapGlyphCount;
    private readonly QChronometer qMapChronometer = new();

    public QMap(QFlow lOwner)
    {
        qFlow = lOwner;
        QMapFlowAttach();
    }

    public event Action? QMapFrameApplied;

    public QMapHitKind QMapDragKind => qMapDragKind;

    public string QMapTrigger => qMapTrigger;

    public int QMapGlyphCount => qMapGlyphCount;

    public void QMapDrawStart() => qMapChronometer.QChronometerStart();

    public void QMapDrawRecord() =>
        LTrace.LTraceTimelineAdd(
            "Map", qFlow.QFlowCursor, qFlow.QFlowSourcePath, qMapTrigger,
            qMapChronometer.QChronometerRead(), qMapGlyphCount);

    public QMapFrame QMapFrameResolve(double lWidth, double lHeight)
    {
        QMapGlyphSet(0);
        (QMapFrame lFrame, LSpool? lSpool, double lCoverageTop, double lRailHeight) =
            QMapBaseResolve(lWidth, lHeight);
        QMapGlyphSet(lFrame.QMapFrameBands.Count);
        if (lSpool is null)
        {
            return lFrame;
        }

        return QMapOverResolve(lFrame, lSpool, lWidth, lCoverageTop, lRailHeight);
    }

    public QMapHitKind QMapHoverResolve(double lX, double lY, double lWidth, double lHeight)
    {
        QMapHitKind lHoverKind = QMapHitResolve(lX, lY, lWidth, lHeight);
        QMapHoverSet(lHoverKind);
        return lHoverKind;
    }

    public QMapHitKind QMapLeaveResolve() =>
        qMapDragKind == QMapHitKind.QMapHitNone ? QMapHitKind.QMapHitNone : qMapHoverKind;

    public bool QMapPressHandle(double lX, double lY, double lWidth, double lHeight)
    {
        if (qFlow.QFlowRange is not { } lSpool || lWidth <= 0)
        {
            return false;
        }

        QMapDragStart(lX, lY, lWidth, lHeight);
        QMapPressDispatch(lSpool, lX, lWidth);
        return true;
    }

    public bool QMapMoveHandle(double lX, double lWidth)
    {
        if (qFlow.QFlowRange is not { } lSpool || !QMapDragCheck() || lWidth <= 0)
        {
            return false;
        }

        QMapMoveDispatch(lSpool, lX, lWidth);
        return true;
    }

    public void QMapDragClear()
    {
        bool lDragging = QMapDragCheck();
        QMapDragReset();
        if (lDragging)
        {
            qFlow.QFlowWheel.QFlowDragHandle(false);
        }
    }

    private void QMapFlowAttach()
    {
        qFlow.QFlowAttachApplied += () => QMapTriggerApply("attach");
        qFlow.QFlowClearApplied += () => QMapTriggerApply("clear");
        qFlow.QFlowCursorApplied += () => QMapTriggerApply("cursor");
        qFlow.QFlowSpool.QFlowSpoolApplied += () => QMapTriggerApply("spool");
        qFlow.QFlowSection.QFlowSectionChanged += (_, _) => QMapTriggerApply("sections");
        qFlow.QFlowKeyframe.QFlowKeyframeChanged += () => QMapTriggerApply("keyframes");
        qFlow.QFlowWaveform.QFlowWaveformUpdated += () => QMapTriggerApply("waveform");
    }

    private (QMapFrame, LSpool?, double, double) QMapBaseResolve(double lWidth, double lHeight)
    {
        var lUnder = new List<QMapShape>();
        var lFrame = new QMapFrame(lUnder, qMapOutlineEmpty, [], new List<QMapShape>(), []);
        if (lWidth <= 0 || lHeight <= 0)
        {
            return (lFrame, null, 0, 0);
        }

        lUnder.Add(new QMapShape(QMapShapeKind.QMapShapeBackground, 0, 0, lWidth, lHeight, 0));
        if (qFlow.QFlowRange is not { } lSpool || lSpool.LSpoolDuration <= TimeSpan.Zero || lHeight < QMapRenderLeast)
        {
            return (lFrame, null, 0, 0);
        }

        double lCoverageBottom = Math.Max(0, lHeight - 1);
        double lCoverageTop = Math.Max(0, lCoverageBottom - QMapCoverageHeight);
        double lRailBottom = Math.Max(QMapRailTop, lCoverageTop - 2);
        double lRailHeight = Math.Max(0, lRailBottom - QMapRailTop);
        if (lRailHeight <= 0)
        {
            return (lFrame, null, 0, 0);
        }

        bool lWaveformActive = qFlow.QFlowWaveform.QFlowWaveformGate.CWaveformShown;
        lUnder.Add(new QMapShape(
            lWaveformActive ? QMapShapeKind.QMapShapeWaveform : QMapShapeKind.QMapShapeRail,
            0, QMapRailTop, lWidth, lRailBottom, 3));
        QFlowWaveformOutline lOutline = qFlow.QFlowWaveform.QFlowOutlineResolve(
            lWidth, QMapRailTop, lRailHeight, TimeSpan.Zero, lSpool.LSpoolDuration);
        IReadOnlyList<QMapBand> lBands = QMapBandsResolve(lSpool, lWidth, lRailHeight);
        QMapFrame lBase = lFrame with { QMapFrameWaveform = lOutline, QMapFrameBands = lBands };
        return (lBase, lSpool, lCoverageTop, lRailHeight);
    }

    private QMapFrame QMapOverResolve(
        QMapFrame lFrame, LSpool lSpool, double lWidth, double lCoverageTop, double lRailHeight)
    {
        var lOver = new List<QMapShape>();
        lOver.Add(new QMapShape(
            QMapShapeKind.QMapShapeCoverage, 0, lCoverageTop, lWidth, lCoverageTop + QMapCoverageHeight, 0));
        QMapCoverageAdd(lOver, lSpool, lWidth, lCoverageTop);

        var lCursor = new List<double>();
        double lBodyLeft = QMapSpoolResolve(lSpool, lSpool.LSpoolRangeOrigin, lWidth);
        double lBodyRight = QMapSpoolResolve(lSpool, lSpool.LSpoolRangeLimit, lWidth);
        double lBodyWidth = Math.Max(0, Math.Max(lBodyLeft, lBodyRight) - Math.Min(lBodyLeft, lBodyRight));
        if (lBodyWidth > 0)
        {
            QMapNavigator.QMapNavigatorAdd(lOver, Math.Min(lBodyLeft, lBodyRight), lBodyWidth, lRailHeight, lWidth);
            lCursor.Add(Math.Clamp(lSpool.LSpoolRatioResolve(qFlow.QFlowCursor), 0, 1) * lWidth);
        }

        return lFrame with { QMapFrameOver = lOver, QMapFrameCursor = lCursor };
    }

    private void QMapGlyphSet(int lGlyphCount) => qMapGlyphCount = lGlyphCount;

    private void QMapHoverSet(QMapHitKind lHoverKind) => qMapHoverKind = lHoverKind;

    private void QMapDragStart(double lX, double lY, double lWidth, double lHeight)
    {
        qMapDragOrigin = lX;
        qMapPreviousX = lX;
        qMapDragKind = QMapHitResolve(lX, lY, lWidth, lHeight);
        qMapHoverKind = qMapDragKind;
    }

    private void QMapPressDispatch(LSpool lSpool, double lX, double lWidth)
    {
        switch (qMapDragKind)
        {
            case QMapHitKind.QMapHitOrigin:
                qFlow.QFlowSpool.QFlowDragSet(lSpool.LSpoolRangeOrigin);
                break;
            case QMapHitKind.QMapHitLimit:
                qFlow.QFlowSpool.QFlowDragSet(lSpool.LSpoolRangeLimit);
                break;
        }

        qFlow.QFlowWheel.QFlowDragHandle(true);
        if (qMapDragKind == QMapHitKind.QMapHitCursor)
        {
            qFlow.QFlowCursorSeek(QMapTimeResolve(lSpool, lX, lWidth));
        }
    }

    private void QMapMoveDispatch(LSpool lSpool, double lX, double lWidth)
    {
        TimeSpan lDragDelta = QMapDeltaResolve(lSpool, lX, lWidth);
        switch (qMapDragKind)
        {
            case QMapHitKind.QMapHitOrigin:
                lSpool.LSpoolStartSet(qFlow.QFlowSpool.QFlowDragTime + lDragDelta);
                qFlow.QFlowSpool.QFlowSpoolHandle();
                break;
            case QMapHitKind.QMapHitLimit:
                lSpool.LSpoolEndSet(qFlow.QFlowSpool.QFlowDragTime + lDragDelta);
                qFlow.QFlowSpool.QFlowSpoolHandle();
                break;
            case QMapHitKind.QMapHitBody:
                lSpool.LSpoolMove(QMapStepResolve(lSpool, lX, lWidth));
                QMapPreviousSet(lX);
                qFlow.QFlowSpool.QFlowSpoolHandle();
                break;
            case QMapHitKind.QMapHitCursor:
                qFlow.QFlowCursorSeek(QMapTimeResolve(lSpool, lX, lWidth));
                break;
        }
    }

    private TimeSpan QMapDeltaResolve(LSpool lSpool, double lX, double lWidth) =>
        lSpool.LSpoolTimeResolve((lX - qMapDragOrigin) / lWidth);

    private TimeSpan QMapStepResolve(LSpool lSpool, double lX, double lWidth) =>
        lSpool.LSpoolTimeResolve((lX - qMapPreviousX) / lWidth);

    private void QMapPreviousSet(double lX) => qMapPreviousX = lX;

    private bool QMapDragCheck() => qMapDragKind != QMapHitKind.QMapHitNone;

    private void QMapDragReset() => qMapDragKind = QMapHitKind.QMapHitNone;

    private void QMapTriggerApply(string lTrigger)
    {
        qMapTrigger = lTrigger;
        QMapFrameApplied?.Invoke();
    }

    private static TimeSpan QMapTimeResolve(LSpool lSpool, double lX, double lWidth) =>
        lSpool.LSpoolTimeResolve(Math.Clamp(lX / lWidth, 0, 1));

    private static double QMapSpoolResolve(LSpool lSpool, TimeSpan lTime, double lWidth) =>
        Math.Clamp(lSpool.LSpoolRatioResolve(lTime), 0, 1) * lWidth;

    private QMapHitKind QMapHitResolve(double lX, double lY, double lWidth, double lHeight)
    {
        if (qFlow.QFlowRange is not { } lSpool || lWidth <= 0)
        {
            return QMapHitKind.QMapHitNone;
        }

        double lOriginX = QMapSpoolResolve(lSpool, lSpool.LSpoolRangeOrigin, lWidth);
        double lLimitX = QMapSpoolResolve(lSpool, lSpool.LSpoolRangeLimit, lWidth);
        double lBodyLeft = Math.Min(lOriginX, lLimitX);
        double lBodyWidth = Math.Max(0, Math.Max(lOriginX, lLimitX) - lBodyLeft);
        double lCoverageTop = Math.Max(0, Math.Max(0, lHeight - 1) - QMapCoverageHeight);
        double lRailBottom = Math.Max(QMapRailTop, lCoverageTop - 2);
        double lHandleWidth = Math.Min(QMapHandleWidth, lBodyWidth);
        bool lInsideY = lY >= QMapRailTop && lY <= lRailBottom;
        if (lInsideY && lX >= lBodyLeft && lX <= lBodyLeft + lHandleWidth)
        {
            return QMapHitKind.QMapHitOrigin;
        }

        if (lInsideY && lX >= lBodyLeft + lBodyWidth - lHandleWidth && lX <= lBodyLeft + lBodyWidth)
        {
            return QMapHitKind.QMapHitLimit;
        }

        (double lMoveLeft, double lMoveWidth, double lMoveHeight) =
            QMapNavigator.LMapMoveResolve(lBodyLeft, lBodyWidth, lRailBottom - QMapRailTop, lHandleWidth, lWidth);
        bool lInsideMove = lMoveWidth > 0
            && lX >= lMoveLeft && lX <= lMoveLeft + lMoveWidth
            && lY >= QMapRailTop && lY <= QMapRailTop + lMoveHeight;
        return lInsideMove ? QMapHitKind.QMapHitBody : QMapHitKind.QMapHitCursor;
    }

    private IReadOnlyList<QMapBand> QMapBandsResolve(LSpool lSpool, double lWidth, double lRailHeight)
    {
        IReadOnlyList<LPiece> lSections = qFlow.QFlowSection.QFlowSectionsRead();
        double lDurationSeconds = lSpool.LSpoolDuration.TotalSeconds;
        var lBands = new List<QMapBand>(lSections.Count);
        double lTop = QMapRailTop + QMapSectionInset;
        double lHeight = Math.Max(4, lRailHeight - QMapSectionInset * 2);
        for (int lIndex = 0; lIndex < lSections.Count; lIndex++)
        {
            LPiece lSection = lSections[lIndex];
            if (lSection.LPieceEnd <= lSection.LPieceOrigin)
            {
                continue;
            }

            double lLeft = Math.Clamp(lSection.LPieceOrigin.TotalSeconds / lDurationSeconds * lWidth, 0, lWidth);
            double lRight = Math.Clamp(lSection.LPieceEnd.TotalSeconds / lDurationSeconds * lWidth, 0, lWidth);
            lBands.Add(new QMapBand(
                lLeft,
                lTop,
                lLeft + Math.Max(1, lRight - lLeft),
                lTop + lHeight,
                lSection.LPieceColorIndex,
                $"{lIndex + 1}",
                !lSection.LPieceHidden,
                lIndex == qFlow.QFlowEdit.QFlowSectionIndex));
        }

        return lBands;
    }

    private void QMapCoverageAdd(List<QMapShape> lOver, LSpool lSpool, double lWidth, double lCoverageTop)
    {
        double lDurationSeconds = lSpool.LSpoolDuration.TotalSeconds;
        IReadOnlyList<LKeyframeScanRange> lRanges = qFlow.QFlowKeyframe.QFlowKeyframeRanges;
        foreach (LKeyframeScanRange lRange in LKeyframeView.LKeyframeCoverageResolve(lRanges, lSpool, true))
        {
            double lLeft = Math.Clamp(lRange.LKeyframeRangeOrigin.TotalSeconds / lDurationSeconds * lWidth, 0, lWidth);
            double lRight = Math.Clamp(lRange.LKeyframeRangeLimit.TotalSeconds / lDurationSeconds * lWidth, 0, lWidth);
            double lScanWidth = Math.Min(Math.Max(1, lRight - lLeft), Math.Max(0, lWidth - lLeft));
            if (lScanWidth <= 0)
            {
                continue;
            }

            lOver.Add(new QMapShape(
                QMapShapeKind.QMapShapeScanned,
                lLeft,
                lCoverageTop,
                lLeft + lScanWidth,
                lCoverageTop + QMapCoverageHeight,
                0));
        }
    }
}
