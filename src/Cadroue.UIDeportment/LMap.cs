using System.Diagnostics;
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
    private const double LMapRailTop = 3;
    private const double LMapCoverageHeight = 3;
    private const double LMapHandleWidth = 12;
    private const double LMapGripWidth = 7;
    private const double LMapGripRate = 0.5;
    private const double LMapGripInset = 2.5;
    private const double LMapShadowDrop = 1.5;
    private const double LMapNavigatorRim = 3.5;
    private const double LMapBorderThickness = 1.2;
    private const double LMapSectionInset = 1;
    private const double LMapBadgeHorizontal = 6;
    private const double LMapBadgeVertical = 1;
    private const double LMapBadgeMargin = 2;

    private static readonly LFlowWaveformOutline lMapOutlineEmpty = new(0, 0, []);

    private readonly LFlow lFlow;
    private LMapHitKind lMapDragKind;
    private LMapHitKind lMapHoverKind;
    private double lMapDragOrigin;
    private double lMapPreviousX;
    private string lMapTrigger = "attach";
    private int lMapGlyphCount;

    public LMap(LFlow lOwner)
    {
        lFlow = lOwner;
        lFlow.LFlowAttachApply += () => LMapTriggerApply("attach");
        lFlow.LFlowClearApply += () => LMapTriggerApply("clear");
        lFlow.LFlowCursorApply += () => LMapTriggerApply("cursor");
        lFlow.LFlowSpoolApply += () => LMapTriggerApply("spool");
        lFlow.LFlowSection.LFlowSectionChange += (_, _) => LMapTriggerApply("sections");
        lFlow.LFlowKeyframe.LFlowKeyframeChange += (_, _) => LMapTriggerApply("keyframes");
        lFlow.LFlowWaveform.LFlowWaveformUpdate += _ => LMapTriggerApply("waveform");
    }

    public event Action? LMapFrameApply;

    public LMapHitKind LMapDragKind => lMapDragKind;

    public string LMapTrigger => lMapTrigger;

    public int LMapGlyphCount => lMapGlyphCount;

    public long LMapDrawStart() => Stopwatch.GetTimestamp();

    public void LMapDrawRecord(long lStamp)
    {
        double lMilliseconds = (Stopwatch.GetTimestamp() - lStamp) * 1000d / Stopwatch.Frequency;
        LTrace.LTraceTimelineAdd(
            "Map", lFlow.LFlowCursor, lFlow.LFlowSourcePath, lMapTrigger, lMilliseconds, lMapGlyphCount);
    }

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
            LMapNavigatorAdd(lOver, Math.Min(lBodyLeft, lBodyRight), lBodyWidth, lRailHeight, lWidth);
            lCursor.Add(Math.Clamp(lSpool.LSpoolRatioResolve(lFlow.LFlowCursor), 0, 1) * lWidth);
        }

        return lFrame with { LMapFrameWaveform = lOutline, LMapFrameBands = lBands, LMapFrameCursor = lCursor };
    }

    public IReadOnlyList<LMapBadge> LMapBadgeResolve(LMapBand lBand, double lTextWidth, double lTextHeight)
    {
        double lBandWidth = lBand.LMapBandRight - lBand.LMapBandLeft;
        double lBandHeight = lBand.LMapBandBottom - lBand.LMapBandTop;
        double lBadgeHeight = lTextHeight + LMapBadgeVertical * 2;
        double lBadgeWidth = Math.Max(lBadgeHeight, lTextWidth + LMapBadgeHorizontal * 2);
        if (lBadgeWidth > lBandWidth - LMapBadgeMargin * 2 || lBadgeHeight > lBandHeight - LMapBadgeMargin * 2)
        {
            return [];
        }

        double lLeft = lBand.LMapBandLeft + (lBandWidth - lBadgeWidth) / 2;
        double lTop = lBand.LMapBandTop + (lBandHeight - lBadgeHeight) / 2;
        return
        [
            new LMapBadge(
                lLeft,
                lTop,
                lLeft + lBadgeWidth,
                lTop + lBadgeHeight,
                lBadgeHeight / 2,
                lLeft + (lBadgeWidth - lTextWidth) / 2,
                lTop + LMapBadgeVertical)
        ];
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
                lFlow.LFlowDragSet(lSpool.LSpoolRangeOrigin);
                break;
            case LMapHitKind.LMapHitLimit:
                lFlow.LFlowDragSet(lSpool.LSpoolRangeLimit);
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
                lSpool.LSpoolStartSet(lFlow.LFlowDragTime + lDragDelta);
                lFlow.LFlowSpoolHandle();
                break;
            case LMapHitKind.LMapHitLimit:
                lSpool.LSpoolEndSet(lFlow.LFlowDragTime + lDragDelta);
                lFlow.LFlowSpoolHandle();
                break;
            case LMapHitKind.LMapHitBody:
                lSpool.LSpoolMove(lSpool.LSpoolTimeResolve((lX - lMapPreviousX) / lWidth));
                lMapPreviousX = lX;
                lFlow.LFlowSpoolHandle();
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
        LMapFrameApply?.Invoke();
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
            LMapMoveResolve(lBodyLeft, lBodyWidth, lRailBottom - LMapRailTop, lHandleWidth, lWidth);
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
                lIndex == lFlow.LFlowSectionIndex));
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

    private static void LMapNavigatorAdd(
        List<LMapShape> lOver, double lBodyLeft, double lBodyWidth, double lBodyHeight, double lWidth)
    {
        double lPaintHeight = Math.Max(0, lBodyHeight - LMapShadowDrop);
        if (lPaintHeight <= 0)
        {
            return;
        }

        double lTop = LMapRailTop;
        double lBottom = lTop + lPaintHeight;
        double lRight = lBodyLeft + lBodyWidth;
        double lRadius = Math.Clamp(lPaintHeight / 2, 4, 9);
        double lSideWidth = Math.Min(LMapGripWidth, lBodyWidth);
        double lGrabWidth = Math.Min(LMapHandleWidth, lBodyWidth);
        lOver.Add(new LMapShape(LMapShapeKind.LMapShapeFill, lBodyLeft, lTop, lRight, lBottom, lRadius));

        double lRimInset = LMapNavigatorRim / 2;
        if (lBodyWidth - LMapNavigatorRim > 0 && lPaintHeight - LMapNavigatorRim > 0)
        {
            double lRimRadius = Math.Max(0, lRadius - lRimInset);
            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeShadow,
                lBodyLeft + lRimInset,
                lTop + lRimInset + LMapShadowDrop,
                lRight - lRimInset,
                lBottom - lRimInset + LMapShadowDrop,
                lRimRadius));
            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeRim,
                lBodyLeft + lRimInset,
                lTop + lRimInset,
                lRight - lRimInset,
                lBottom - lRimInset,
                lRimRadius));
        }

        lOver.Add(new LMapShape(
            LMapShapeKind.LMapShapeSide, lBodyLeft, lTop, lBodyLeft + lSideWidth, lBottom, lRadius));
        lOver.Add(new LMapShape(LMapShapeKind.LMapShapeSide, lRight - lSideWidth, lTop, lRight, lBottom, lRadius));
        double lGripInset = Math.Max(3, lPaintHeight * 0.25);
        lOver.Add(new LMapShape(
            LMapShapeKind.LMapShapeGrip,
            lBodyLeft + lSideWidth / 2, lTop + lGripInset, lBodyLeft + lSideWidth / 2, lBottom - lGripInset, 0));
        lOver.Add(new LMapShape(
            LMapShapeKind.LMapShapeGrip,
            lRight - lSideWidth / 2, lTop + lGripInset, lRight - lSideWidth / 2, lBottom - lGripInset, 0));

        (double lMoveLeft, double lMoveWidth, double lMoveHeight) =
            LMapMoveResolve(lBodyLeft, lBodyWidth, lBodyHeight, lGrabWidth, lWidth);
        double lGripWidth = Math.Min(lMoveWidth, lMoveWidth * LMapGripRate);
        double lGripHeight = Math.Min(lMoveHeight, lPaintHeight) - LMapGripInset * 2;
        if (lMoveWidth > 0 && lMoveHeight > 0 && lGripWidth > 0 && lGripHeight > 0)
        {
            double lGripLeft = lMoveLeft + (lMoveWidth - lGripWidth) / 2;
            double lGripTop = lTop + LMapGripInset;
            double lMoveRadius = Math.Min(8, lGripHeight / 2);
            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeBody,
                lGripLeft,
                lGripTop,
                lGripLeft + lGripWidth,
                lGripTop + lGripHeight,
                lMoveRadius));
            LMapDotsAdd(lOver, lGripLeft, lGripTop, lGripWidth, lGripHeight);
        }

        lOver.Add(new LMapShape(LMapShapeKind.LMapShapeShine, lBodyLeft + 6, lTop + 1.5, lRight - 6, lTop + 1.5, 0));
        double lBorderInset = LMapBorderThickness / 2;
        if (lBodyWidth - LMapBorderThickness > 0 && lPaintHeight - LMapBorderThickness > 0)
        {
            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeBorder,
                lBodyLeft + lBorderInset,
                lTop + lBorderInset,
                lRight - lBorderInset,
                lBottom - lBorderInset,
                lRadius));
        }
    }

    private static void LMapDotsAdd(List<LMapShape> lOver, double lLeft, double lTop, double lWidth, double lHeight)
    {
        int lColumnCount = Math.Clamp((int)(lWidth / 9), 2, 10);
        double lCenterOffset = (lColumnCount - 1) / 2.0;
        double lY = lTop + lHeight / 2;
        for (int lColumn = 0; lColumn < lColumnCount; lColumn++)
        {
            double lX = lLeft + lWidth / 2 + (lColumn - lCenterOffset) * 7;
            if (lX > lLeft + 4 && lX < lLeft + lWidth - 4)
            {
                lOver.Add(new LMapShape(LMapShapeKind.LMapShapeDot, lX, lY, lX, lY, 1.3));
            }
        }
    }

    private static (double, double, double) LMapMoveResolve(
        double lBodyLeft, double lBodyWidth, double lBodyHeight, double lSideWidth, double lWidth)
    {
        double lMoveHeight = Math.Clamp(lBodyHeight, 1, 16);
        double lSpaceWidth = Math.Max(0, lBodyWidth - lSideWidth * 2);
        if (lSpaceWidth <= 0)
        {
            return (0, 0, 0);
        }

        double lMoveWidth = Math.Clamp(lBodyWidth * 0.42, Math.Min(72, lSpaceWidth), Math.Min(320, lSpaceWidth));
        double lCentered = lBodyLeft + (lBodyWidth - lMoveWidth) / 2;
        double lLeftMinimum = lBodyLeft + lSideWidth;
        double lLeftMaximum = lBodyLeft + lBodyWidth - lSideWidth - lMoveWidth;
        double lMoveLeft = lLeftMaximum < lLeftMinimum ? lCentered : Math.Clamp(lCentered, lLeftMinimum, lLeftMaximum);
        return (Math.Clamp(lMoveLeft, 0, Math.Max(0, lWidth - lMoveWidth)), lMoveWidth, lMoveHeight);
    }
}
