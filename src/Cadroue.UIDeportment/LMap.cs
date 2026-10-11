using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum LMapShapeKind
{
    LMapShapeBackground,
    LMapShapeRail,
    LMapShapeWaveform,
    LMapShapeCoverage,
    LMapShapeScanned,
    LMapShapeFill,
    LMapShapeShadow,
    LMapShapeRim,
    LMapShapeSide,
    LMapShapeGrip,
    LMapShapeBody,
    LMapShapeDot,
    LMapShapeShine,
    LMapShapeBorder,
}

public enum LMapHitKind
{
    LMapHitNone,
    LMapHitOrigin,
    LMapHitLimit,
    LMapHitBody,
    LMapHitCursor,
}

public sealed record LMapShape(
    LMapShapeKind LMapShapeKind,
    double LMapShapeLeft,
    double LMapShapeTop,
    double LMapShapeRight,
    double LMapShapeBottom,
    double LMapShapeRadius);

public sealed record LMapBand(
    double LMapBandLeft,
    double LMapBandTop,
    double LMapBandRight,
    double LMapBandBottom,
    int LMapBandColor,
    string LMapBandBadge,
    bool LMapBandShown,
    bool LMapBandSelected);

public sealed record LMapBadge(
    double LMapBadgeLeft,
    double LMapBadgeTop,
    double LMapBadgeRight,
    double LMapBadgeBottom,
    double LMapBadgeRadius,
    double LMapBadgeX,
    double LMapBadgeY);

public sealed record LMapFrame(
    IReadOnlyList<LMapShape> LMapFrameUnder,
    LFlowWaveformOutline LMapFrameWaveform,
    IReadOnlyList<LMapBand> LMapFrameBands,
    IReadOnlyList<LMapShape> LMapFrameOver,
    IReadOnlyList<double> LMapFrameCursor);

public sealed class LMap
{
    private const double LMapRenderLeast = 12;
    internal const double LMapRailTop = 3;
    private const double LMapCoverageHeight = 3;
    internal const double LMapHandleWidth = 12;
    private const double LMapSectionInset = 1;

    private static readonly LFlowWaveformOutline lMapOutlineEmpty = new(0, 0, []);

    private readonly LFlow lFlow;
    private LMapHitKind lMapDragKind;
    private LMapHitKind lMapHoverKind;
    private double lMapDragOrigin;
    private double lMapPreviousX;
    private string lMapTrigger = "attach";
    private int lMapGlyphCount;
    private readonly QChronometer lMapChronometer = new();

    public LMap(LFlow lOwner)
    {
        lFlow = lOwner;
        lFlow.LFlowAttachApplied += () => LMapTriggerApply("attach");
        lFlow.LFlowClearApplied += () => LMapTriggerApply("clear");
        lFlow.LFlowCursorApplied += () => LMapTriggerApply("cursor");
        lFlow.QFlowSpool.LFlowSpoolApplied += () => LMapTriggerApply("spool");
        lFlow.LFlowSection.LFlowSectionChanged += (_, _) => LMapTriggerApply("sections");
        lFlow.LFlowKeyframe.LFlowKeyframeChanged += (_, _) => LMapTriggerApply("keyframes");
        lFlow.LFlowWaveform.LFlowWaveformUpdated += _ => LMapTriggerApply("waveform");
    }

    public event Action? LMapFrameApplied;

    public LMapHitKind LMapDragKind => lMapDragKind;

    public string LMapTrigger => lMapTrigger;

    public int LMapGlyphCount => lMapGlyphCount;

    public void LMapDrawStart() => lMapChronometer.QChronometerStart();

    public void LMapDrawRecord() =>
        LTrace.LTraceTimelineAdd(
            "Map", lFlow.LFlowCursor, lFlow.LFlowSourcePath, lMapTrigger,
            lMapChronometer.QChronometerRead(), lMapGlyphCount);

    public LMapFrame LMapFrameResolve(double lWidth, double lHeight)
    {
        lMapGlyphCount = 0;
        var lUnder = new List<LMapShape>();
        var lOver = new List<LMapShape>();
        var lFrame = new LMapFrame(lUnder, lMapOutlineEmpty, [], lOver, []);
        if (lWidth <= 0 || lHeight <= 0)
        {
            return lFrame;
        }

        lUnder.Add(new LMapShape(LMapShapeKind.LMapShapeBackground, 0, 0, lWidth, lHeight, 0));
        if (lFlow.LFlowSpool is not { } lSpool || lSpool.LSpoolDuration <= TimeSpan.Zero || lHeight < LMapRenderLeast)
        {
            return lFrame;
        }

        double lCoverageBottom = Math.Max(0, lHeight - 1);
        double lCoverageTop = Math.Max(0, lCoverageBottom - LMapCoverageHeight);
        double lRailBottom = Math.Max(LMapRailTop, lCoverageTop - 2);
        double lRailHeight = Math.Max(0, lRailBottom - LMapRailTop);
        if (lRailHeight <= 0)
        {
            return lFrame;
        }

        byte[] lPeaks = lFlow.LFlowWaveform.LFlowWaveformPeaks;
        bool lWaveformActive = lPeaks.Length > 0;
        lUnder.Add(new LMapShape(
            lWaveformActive ? LMapShapeKind.LMapShapeWaveform : LMapShapeKind.LMapShapeRail,
            0, LMapRailTop, lWidth, lRailBottom, 3));
        LFlowWaveformOutline lOutline = lWaveformActive
            ? LFlowWaveform.LFlowOutlineResolve(
                lPeaks, lWidth, LMapRailTop, lRailHeight, TimeSpan.Zero, lSpool.LSpoolDuration)
            : lMapOutlineEmpty;
        IReadOnlyList<LMapBand> lBands = LMapBandsResolve(lSpool, lWidth, lRailHeight);
        lMapGlyphCount = lBands.Count;

        lOver.Add(new LMapShape(
            LMapShapeKind.LMapShapeCoverage, 0, lCoverageTop, lWidth, lCoverageTop + LMapCoverageHeight, 0));
        LMapCoverageAdd(lOver, lSpool, lWidth, lCoverageTop);

        var lCursor = new List<double>();
        double lBodyLeft = LMapSpoolResolve(lSpool, lSpool.LSpoolRangeOrigin, lWidth);
        double lBodyRight = LMapSpoolResolve(lSpool, lSpool.LSpoolRangeLimit, lWidth);
        double lBodyWidth = Math.Max(0, Math.Max(lBodyLeft, lBodyRight) - Math.Min(lBodyLeft, lBodyRight));
        if (lBodyWidth > 0)
        {
            QMapNavigator.LMapNavigatorAdd(lOver, Math.Min(lBodyLeft, lBodyRight), lBodyWidth, lRailHeight, lWidth);
            lCursor.Add(Math.Clamp(lSpool.LSpoolRatioResolve(lFlow.LFlowCursor), 0, 1) * lWidth);
        }

        return lFrame with { LMapFrameWaveform = lOutline, LMapFrameBands = lBands, LMapFrameCursor = lCursor };
    }

    public LMapHitKind LMapHoverResolve(double lX, double lY, double lWidth, double lHeight)
    {
        lMapHoverKind = LMapHitResolve(lX, lY, lWidth, lHeight);
        return lMapHoverKind;
    }

    public LMapHitKind LMapLeaveResolve() =>
        lMapDragKind == LMapHitKind.LMapHitNone ? LMapHitKind.LMapHitNone : lMapHoverKind;

    public bool LMapPressHandle(double lX, double lY, double lWidth, double lHeight)
    {
        if (lFlow.LFlowSpool is not { } lSpool || lWidth <= 0)
        {
            return false;
        }

        lMapDragOrigin = lX;
        lMapPreviousX = lX;
        lMapDragKind = LMapHitResolve(lX, lY, lWidth, lHeight);
        lMapHoverKind = lMapDragKind;
        switch (lMapDragKind)
        {
            case LMapHitKind.LMapHitOrigin:
                lFlow.QFlowSpool.LFlowDragSet(lSpool.LSpoolRangeOrigin);
                break;
            case LMapHitKind.LMapHitLimit:
                lFlow.QFlowSpool.LFlowDragSet(lSpool.LSpoolRangeLimit);
                break;
        }

        lFlow.LFlowDragHandle(true);
        if (lMapDragKind == LMapHitKind.LMapHitCursor)
        {
            lFlow.LFlowCursorSeek(LMapTimeResolve(lSpool, lX, lWidth));
        }

        return true;
    }

    public bool LMapMoveHandle(double lX, double lWidth)
    {
        if (lFlow.LFlowSpool is not { } lSpool || lMapDragKind == LMapHitKind.LMapHitNone || lWidth <= 0)
        {
            return false;
        }

        TimeSpan lDragDelta = lSpool.LSpoolTimeResolve((lX - lMapDragOrigin) / lWidth);
        switch (lMapDragKind)
        {
            case LMapHitKind.LMapHitOrigin:
                lSpool.LSpoolStartSet(lFlow.QFlowSpool.LFlowDragTime + lDragDelta);
                lFlow.QFlowSpool.LFlowSpoolHandle();
                break;
            case LMapHitKind.LMapHitLimit:
                lSpool.LSpoolEndSet(lFlow.QFlowSpool.LFlowDragTime + lDragDelta);
                lFlow.QFlowSpool.LFlowSpoolHandle();
                break;
            case LMapHitKind.LMapHitBody:
                lSpool.LSpoolMove(lSpool.LSpoolTimeResolve((lX - lMapPreviousX) / lWidth));
                lMapPreviousX = lX;
                lFlow.QFlowSpool.LFlowSpoolHandle();
                break;
            case LMapHitKind.LMapHitCursor:
                lFlow.LFlowCursorSeek(LMapTimeResolve(lSpool, lX, lWidth));
                break;
        }

        return true;
    }

    public void LMapDragClear()
    {
        bool lDragging = lMapDragKind != LMapHitKind.LMapHitNone;
        lMapDragKind = LMapHitKind.LMapHitNone;
        if (lDragging)
        {
            lFlow.LFlowDragHandle(false);
        }
    }

    private void LMapTriggerApply(string lTrigger)
    {
        lMapTrigger = lTrigger;
        LMapFrameApplied?.Invoke();
    }

    private static TimeSpan LMapTimeResolve(LSpool lSpool, double lX, double lWidth) =>
        lSpool.LSpoolTimeResolve(Math.Clamp(lX / lWidth, 0, 1));

    private static double LMapSpoolResolve(LSpool lSpool, TimeSpan lTime, double lWidth) =>
        Math.Clamp(lSpool.LSpoolRatioResolve(lTime), 0, 1) * lWidth;

    private LMapHitKind LMapHitResolve(double lX, double lY, double lWidth, double lHeight)
    {
        if (lFlow.LFlowSpool is not { } lSpool || lWidth <= 0)
        {
            return LMapHitKind.LMapHitNone;
        }

        double lOriginX = LMapSpoolResolve(lSpool, lSpool.LSpoolRangeOrigin, lWidth);
        double lLimitX = LMapSpoolResolve(lSpool, lSpool.LSpoolRangeLimit, lWidth);
        double lBodyLeft = Math.Min(lOriginX, lLimitX);
        double lBodyWidth = Math.Max(0, Math.Max(lOriginX, lLimitX) - lBodyLeft);
        double lCoverageTop = Math.Max(0, Math.Max(0, lHeight - 1) - LMapCoverageHeight);
        double lRailBottom = Math.Max(LMapRailTop, lCoverageTop - 2);
        double lHandleWidth = Math.Min(LMapHandleWidth, lBodyWidth);
        bool lInsideY = lY >= LMapRailTop && lY <= lRailBottom;
        if (lInsideY && lX >= lBodyLeft && lX <= lBodyLeft + lHandleWidth)
        {
            return LMapHitKind.LMapHitOrigin;
        }

        if (lInsideY && lX >= lBodyLeft + lBodyWidth - lHandleWidth && lX <= lBodyLeft + lBodyWidth)
        {
            return LMapHitKind.LMapHitLimit;
        }

        (double lMoveLeft, double lMoveWidth, double lMoveHeight) =
            QMapNavigator.LMapMoveResolve(lBodyLeft, lBodyWidth, lRailBottom - LMapRailTop, lHandleWidth, lWidth);
        bool lInsideMove = lMoveWidth > 0
            && lX >= lMoveLeft && lX <= lMoveLeft + lMoveWidth
            && lY >= LMapRailTop && lY <= LMapRailTop + lMoveHeight;
        return lInsideMove ? LMapHitKind.LMapHitBody : LMapHitKind.LMapHitCursor;
    }

    private IReadOnlyList<LMapBand> LMapBandsResolve(LSpool lSpool, double lWidth, double lRailHeight)
    {
        IReadOnlyList<LPiece> lSections = lFlow.LFlowSection.LFlowSectionsRead();
        double lDurationSeconds = lSpool.LSpoolDuration.TotalSeconds;
        var lBands = new List<LMapBand>(lSections.Count);
        double lTop = LMapRailTop + LMapSectionInset;
        double lHeight = Math.Max(4, lRailHeight - LMapSectionInset * 2);
        for (int lIndex = 0; lIndex < lSections.Count; lIndex++)
        {
            LPiece lSection = lSections[lIndex];
            if (lSection.LPieceEnd <= lSection.LPieceOrigin)
            {
                continue;
            }

            double lLeft = Math.Clamp(lSection.LPieceOrigin.TotalSeconds / lDurationSeconds * lWidth, 0, lWidth);
            double lRight = Math.Clamp(lSection.LPieceEnd.TotalSeconds / lDurationSeconds * lWidth, 0, lWidth);
            lBands.Add(new LMapBand(
                lLeft,
                lTop,
                lLeft + Math.Max(1, lRight - lLeft),
                lTop + lHeight,
                lSection.LPieceColorIndex,
                $"{lIndex + 1}",
                !lSection.LPieceHidden,
                lIndex == lFlow.QFlowEdit.LFlowSectionIndex));
        }

        return lBands;
    }

    private void LMapCoverageAdd(List<LMapShape> lOver, LSpool lSpool, double lWidth, double lCoverageTop)
    {
        double lDurationSeconds = lSpool.LSpoolDuration.TotalSeconds;
        IReadOnlyList<LKeyframeScanRange> lRanges = lFlow.LFlowKeyframe.LFlowKeyframeRanges;
        foreach (LKeyframeScanRange lRange in LKeyframeView.LKeyframeCoverageResolve(lRanges, lSpool, true))
        {
            double lLeft = Math.Clamp(lRange.LKeyframeRangeOrigin.TotalSeconds / lDurationSeconds * lWidth, 0, lWidth);
            double lRight = Math.Clamp(lRange.LKeyframeRangeLimit.TotalSeconds / lDurationSeconds * lWidth, 0, lWidth);
            double lScanWidth = Math.Min(Math.Max(1, lRight - lLeft), Math.Max(0, lWidth - lLeft));
            if (lScanWidth <= 0)
            {
                continue;
            }

            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeScanned,
                lLeft,
                lCoverageTop,
                lLeft + lScanWidth,
                lCoverageTop + LMapCoverageHeight,
                0));
        }
    }
}
