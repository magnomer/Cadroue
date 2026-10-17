using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QViewfinder
{
    public const double QViewfinderLaneHeight = 20;

    private const double QViewfinderRenderLeast = 28;
    internal const double QViewfinderCoverageHeight = 4;
    private const double QViewfinderSectionInset = 1;
    private const double QViewfinderKeyframeWidth = 1;

    private static readonly QFlowWaveformOutline qViewfinderOutlineEmpty = new(0, 0, []);

    private readonly QFlow qFlow;
    private bool qViewfinderDragActive;
    private string qViewfinderTrigger = "attach";
    private int qViewfinderGlyphCount;
    private readonly QChronometer qViewfinderChronometer = new();

    public QViewfinder(QFlow lOwner)
    {
        qFlow = lOwner;
        QViewfinderTriggersAttach();
    }

    // Every flow change that alters the drawing asks a new frame, naming its trigger for the draw record.
    private void QViewfinderTriggersAttach()
    {
        qFlow.QFlowAttachApplied += () => QViewfinderTriggerApply("attach");
        qFlow.QFlowClearApplied += () => QViewfinderTriggerApply("clear");
        qFlow.QFlowCursorApplied += () => QViewfinderTriggerApply("cursor");
        qFlow.QFlowSpool.QFlowSpoolApplied += () => QViewfinderTriggerApply("spool");
        qFlow.QFlowSection.QFlowSectionChanged += (_, _) => QViewfinderTriggerApply("sections");
        qFlow.QFlowKeyframe.QFlowKeyframeChanged += (_, _) => QViewfinderTriggerApply("keyframes");
        qFlow.QFlowWaveform.QFlowWaveformUpdated += _ => QViewfinderTriggerApply("waveform");
    }

    public event Action? QViewfinderFrameApplied;

    public bool QViewfinderDragActive => qViewfinderDragActive;

    public string QViewfinderTrigger => qViewfinderTrigger;

    public int QViewfinderGlyphCount => qViewfinderGlyphCount;

    public void QViewfinderDrawStart() => qViewfinderChronometer.QChronometerStart();

    public void QViewfinderDrawRecord()
    {
        LTrace.LTraceTimelineAdd(
            "Viewfinder",
            qFlow.QFlowCursor,
            qFlow.QFlowSourcePath,
            qViewfinderTrigger,
            qViewfinderChronometer.QChronometerRead(),
            qViewfinderGlyphCount);
    }

    public QViewfinderFrame QViewfinderFrameResolve(double lWidth, double lHeight)
    {
        QViewfinderGlyphSet(0);
        (QViewfinderFrame lFrame, int lGlyphCount) = QViewfinderLayoutResolve(lWidth, lHeight);
        QViewfinderGlyphSet(lGlyphCount);
        return lFrame;
    }

    private void QViewfinderGlyphSet(int lGlyphCount) => qViewfinderGlyphCount = lGlyphCount;

    // The frame's shapes for this size, with the count of glyphs it draws; an empty frame counts none.
    private (QViewfinderFrame, int) QViewfinderLayoutResolve(double lWidth, double lHeight)
    {
        var lUnder = new List<QViewfinderShape>
        {
            new(QViewfinderShapeKind.QViewfinderShapeBackground, 0, 0, lWidth, lHeight)
        };
        var lFrame = new QViewfinderFrame(lUnder, qViewfinderOutlineEmpty, [], [], [], [], []);
        if (qFlow.QFlowRange is not { } lSpool || lWidth <= 0 || lHeight < QViewfinderRenderLeast)
        {
            return (lFrame, 0);
        }

        double lCoverageTop = Math.Max(0, Math.Max(0, lHeight - 1) - QViewfinderCoverageHeight);
        (double lRailTop, double lRailBottom) = QViewfinderRail.LViewfinderRailResolve(lHeight);
        double lRailHeight = lRailBottom - lRailTop;
        if (lRailHeight <= 0)
        {
            return (lFrame, 0);
        }

        byte[] lPeaks = qFlow.QFlowWaveform.QFlowWaveformPeaks;
        bool lWaveformActive = lPeaks.Length > 0;
        lUnder.Add(new QViewfinderShape(
            lWaveformActive ? QViewfinderShapeKind.QViewfinderShapeWaveform : QViewfinderShapeKind.QViewfinderShapeRail,
            0, lRailTop, lWidth, lRailBottom));
        lUnder.Add(new QViewfinderShape(
            QViewfinderShapeKind.QViewfinderShapeCoverage,
            0,
            lCoverageTop,
            lWidth,
            lCoverageTop + QViewfinderCoverageHeight));

        TimeSpan lRangeStart = lSpool.LSpoolRangeOrigin;
        TimeSpan lRangeEnd = lSpool.LSpoolRangeLimit;
        double lRangeSeconds = (lRangeEnd - lRangeStart).TotalSeconds;
        if (lRangeSeconds <= 0)
        {
            return (lFrame, 0);
        }

        QFlowWaveformOutline lOutline = lWaveformActive
            ? QFlowWaveform.QFlowOutlineResolve(lPeaks, lWidth, lRailTop, lRailHeight, lRangeStart, lRangeEnd)
            : qViewfinderOutlineEmpty;
        IReadOnlyList<QViewfinderGraduation> lTicks =
            QViewfinderRuler.QViewfinderTicksResolve(lWidth, lRangeStart, lRangeSeconds);
        IReadOnlyList<QViewfinderBand> lBands =
            QViewfinderBandsResolve(lWidth, lRailTop, lRailBottom, lRangeStart, lRangeEnd, lRangeSeconds);
        IReadOnlyList<QViewfinderShape> lOver =
            QViewfinderCoverageResolve(lSpool, lWidth, lCoverageTop, lRangeStart, lRangeSeconds);
        IReadOnlyList<QViewfinderShape> lKeyframes =
            QViewfinderKeyframesResolve(lSpool, lWidth, lRailTop, lRailBottom, lRangeStart, lRangeSeconds);
        var lCursor = new List<QViewfinderCursor>();
        TimeSpan lTime = qFlow.QFlowCursor;
        if (lTime >= lRangeStart && lTime <= lRangeEnd)
        {
            double lCursorX = Math.Clamp((lTime - lRangeStart).TotalSeconds / lRangeSeconds, 0, 1) * lWidth;
            lCursor.Add(new QViewfinderCursor(lCursorX, QCursor.QCursorTimeFormat(lTime)));
        }

        int lGlyphCount = lTicks.Count + lBands.Count * 2 + lCursor.Count;
        return (lFrame with
        {
            QViewfinderFrameWaveform = lOutline,
            QViewfinderFrameTicks = lTicks,
            QViewfinderFrameBands = lBands,
            QViewfinderFrameOver = lOver,
            QViewfinderFrameKeyframes = lKeyframes,
            QViewfinderFrameCursor = lCursor
        }, lGlyphCount);
    }

    public QViewfinderPoint QViewfinderPopupResolve(int lIndex, double lWidth, double lHeight)
    {
        QViewfinderBand? lBand = QViewfinderSectionResolve(lIndex, lWidth, lHeight);
        double lBandWidth = lBand is null ? 0 : lBand.QViewfinderBandRight - lBand.QViewfinderBandLeft;
        double lBandHeight = lBand is null ? 0 : lBand.QViewfinderBandBottom - lBand.QViewfinderBandTop;
        return new QViewfinderPoint(
            QFlowName.QFlowOffsetResolve(lBand is null, lBand?.QViewfinderBandLeft ?? 0, lBandWidth, lWidth),
            QFlowName.QFlowOffsetResolve(lBand is null, lBand?.QViewfinderBandTop ?? 0, lBandHeight, lHeight));
    }

    public QViewfinderBand? QViewfinderSectionResolve(int lIndex, double lWidth, double lHeight)
    {
        IReadOnlyList<LPiece> lSections = qFlow.QFlowSection.QFlowSectionsRead();
        if (qFlow.QFlowRange is not { } lSpool || lIndex < 0 || lIndex >= lSections.Count
            || lWidth <= 0 || lHeight <= 0)
        {
            return null;
        }

        (double lRailTop, double lRailBottom) = QViewfinderRail.LViewfinderRailResolve(lHeight);
        if (lRailBottom <= lRailTop)
        {
            return null;
        }

        return QViewfinderBandResolve(
            lSections[lIndex],
            lIndex,
            lWidth,
            lRailTop,
            lRailBottom,
            lSpool.LSpoolRangeOrigin,
            lSpool.LSpoolRangeLimit);
    }

    public bool QViewfinderPressHandle(double lX, double lWidth)
    {
        if (qFlow.QFlowRange is not { } lSpool || lWidth <= 0)
        {
            return false;
        }

        TimeSpan lTime = QViewfinderTimeResolve(lSpool, lX, lWidth);
        int lHit = QViewfinderHitFind(qFlow.QFlowSection.QFlowSectionsRead(), lTime);
        if (lHit >= 0)
        {
            qFlow.QFlowSection.QFlowSectionSelect(lHit);
        }

        QViewfinderDragSet(true);
        qFlow.QFlowWheel.QFlowDragHandle(true);
        qFlow.QFlowCursorSeek(lTime);
        return true;
    }

    // The topmost section holding the time, or -1.
    private static int QViewfinderHitFind(IReadOnlyList<LPiece> lSections, TimeSpan lTime)
    {
        for (int lIndex = lSections.Count - 1; lIndex >= 0; lIndex--)
        {
            if (lTime >= lSections[lIndex].LPieceOrigin && lTime <= lSections[lIndex].LPieceEnd)
            {
                return lIndex;
            }
        }

        return -1;
    }

    private void QViewfinderDragSet(bool lDragActive) => qViewfinderDragActive = lDragActive;

    public bool QViewfinderMoveHandle(double lX, double lWidth)
    {
        if (!qViewfinderDragActive || qFlow.QFlowRange is not { } lSpool || lWidth <= 0)
        {
            return false;
        }

        qFlow.QFlowCursorSeek(QViewfinderTimeResolve(lSpool, lX, lWidth));
        return true;
    }

    public void QViewfinderDragClear()
    {
        bool lDragging = qViewfinderDragActive;
        QViewfinderDragSet(false);
        if (lDragging)
        {
            qFlow.QFlowWheel.QFlowDragHandle(false);
        }
    }

    private void QViewfinderTriggerApply(string lTrigger)
    {
        QViewfinderTriggerSet(lTrigger);
        QViewfinderFrameApplied?.Invoke();
    }

    private void QViewfinderTriggerSet(string lTrigger) => qViewfinderTrigger = lTrigger;

    private static TimeSpan QViewfinderTimeResolve(LSpool lSpool, double lX, double lWidth)
    {
        double lRatio = Math.Clamp(Math.Clamp(lX, 0, lWidth) / lWidth, 0, 1);
        TimeSpan lRange = lSpool.LSpoolRangeLimit - lSpool.LSpoolRangeOrigin;
        if (lRange <= TimeSpan.Zero)
        {
            return lSpool.LSpoolRangeOrigin;
        }

        return lSpool.LSpoolRangeOrigin + TimeSpan.FromSeconds(lRatio * lRange.TotalSeconds);
    }

    private IReadOnlyList<QViewfinderBand> QViewfinderBandsResolve(
        double lWidth,
        double lRailTop,
        double lRailBottom,
        TimeSpan lRangeStart,
        TimeSpan lRangeEnd,
        double lRangeSeconds)
    {
        IReadOnlyList<LPiece> lSections = qFlow.QFlowSection.QFlowSectionsRead();
        var lBands = new List<QViewfinderBand>(lSections.Count);
        for (int lIndex = 0; lIndex < lSections.Count; lIndex++)
        {
            if (QViewfinderBandResolve(lSections[lIndex], lIndex, lWidth, lRailTop, lRailBottom, lRangeStart, lRangeEnd)
                is { } lBand)
            {
                lBands.Add(lBand);
            }
        }

        return lBands;
    }

    private QViewfinderBand? QViewfinderBandResolve(
        LPiece lSection,
        int lIndex,
        double lWidth,
        double lRailTop,
        double lRailBottom,
        TimeSpan lRangeStart,
        TimeSpan lRangeEnd)
    {
        double lRangeSeconds = (lRangeEnd - lRangeStart).TotalSeconds;
        TimeSpan lStart = lSection.LPieceOrigin < lRangeStart ? lRangeStart : lSection.LPieceOrigin;
        TimeSpan lEnd = lSection.LPieceEnd > lRangeEnd ? lRangeEnd : lSection.LPieceEnd;
        if (lEnd <= lStart || lRangeSeconds <= 0)
        {
            return null;
        }

        double lLeft = Math.Clamp((lStart - lRangeStart).TotalSeconds / lRangeSeconds * lWidth, 0, lWidth);
        double lRight = Math.Clamp((lEnd - lRangeStart).TotalSeconds / lRangeSeconds * lWidth, 0, lWidth);
        double lTop = lRailTop + QViewfinderSectionInset;
        double lHeight = Math.Max(4, lRailBottom - lRailTop - QViewfinderSectionInset * 2);
        return new QViewfinderBand(
            lLeft,
            lTop,
            lLeft + Math.Max(1, lRight - lLeft),
            lTop + lHeight,
            lSection.LPieceColorIndex,
            $"{lIndex + 1}",
            lSection.LPieceName,
            !lSection.LPieceHidden,
            lIndex == qFlow.QFlowEdit.QFlowSectionIndex);
    }

    private IReadOnlyList<QViewfinderShape> QViewfinderCoverageResolve(
        LSpool lSpool, double lWidth, double lCoverageTop, TimeSpan lRangeStart, double lRangeSeconds)
    {
        var lShapes = new List<QViewfinderShape>();
        IReadOnlyList<LKeyframeScanRange> lRanges = qFlow.QFlowKeyframe.QFlowKeyframeRanges;
        foreach (LKeyframeScanRange lRange in LKeyframeView.LKeyframeCoverageResolve(lRanges, lSpool, false))
        {
            double lLeft = Math.Clamp(
                (lRange.LKeyframeRangeOrigin - lRangeStart).TotalSeconds / lRangeSeconds * lWidth, 0, lWidth);
            double lRight = Math.Clamp(
                (lRange.LKeyframeRangeLimit - lRangeStart).TotalSeconds / lRangeSeconds * lWidth, 0, lWidth);
            double lScanWidth = Math.Min(Math.Max(1, lRight - lLeft), Math.Max(0, lWidth - lLeft));
            if (lScanWidth <= 0)
            {
                continue;
            }

            lShapes.Add(new QViewfinderShape(
                QViewfinderShapeKind.QViewfinderShapeScanned,
                lLeft,
                lCoverageTop,
                lLeft + lScanWidth,
                lCoverageTop + QViewfinderCoverageHeight));
        }

        return lShapes;
    }

    private IReadOnlyList<QViewfinderShape> QViewfinderKeyframesResolve(
        LSpool lSpool, double lWidth, double lRailTop, double lRailBottom, TimeSpan lRangeStart, double lRangeSeconds)
    {
        IReadOnlyList<LKeyframeEntry> lVisible = LKeyframeView.LKeyframeVisibleResolve(
            qFlow.QFlowKeyframe.QFlowKeyframeEntries, qFlow.QFlowCursor, lSpool);
        double[] lOffsets = lVisible
            .Select(lEntry => (lEntry.LKeyframePresentationTime - lRangeStart).TotalSeconds / lRangeSeconds * lWidth)
            .ToArray();
        if (lOffsets.Length == 0 || !QViewfinderGapCheck(lOffsets))
        {
            return [];
        }

        double lKeyframeRight = Math.Max(0, lWidth - QViewfinderKeyframeWidth);
        return lOffsets
            .Select(lOffset => Math.Clamp(lOffset, 0, lKeyframeRight))
            .Select(lX => new QViewfinderShape(
                QViewfinderShapeKind.QViewfinderShapeKeyframe,
                lX,
                lRailTop,
                lX + QViewfinderKeyframeWidth,
                lRailBottom))
            .ToArray();
    }

    private static bool QViewfinderGapCheck(double[] lOffsets)
    {
        double lMinimumGap = Math.Max(
            QViewfinderKeyframeWidth, LPreference.LPreferenceStateCurrent.LPreferenceKeyframePixels);
        for (int lIndex = 1; lIndex < lOffsets.Length; lIndex++)
        {
            if (lOffsets[lIndex] - lOffsets[lIndex - 1] < lMinimumGap)
            {
                return false;
            }
        }

        return true;
    }
}
