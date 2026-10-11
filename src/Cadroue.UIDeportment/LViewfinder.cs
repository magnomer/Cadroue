using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum LViewfinderShapeKind
{
    LViewfinderShapeBackground,
    LViewfinderShapeRail,
    LViewfinderShapeWaveform,
    LViewfinderShapeCoverage,
    LViewfinderShapeScanned,
    LViewfinderShapeKeyframe,
}

public sealed record LViewfinderShape(
    LViewfinderShapeKind LViewfinderShapeKind,
    double LViewfinderShapeLeft,
    double LViewfinderShapeTop,
    double LViewfinderShapeRight,
    double LViewfinderShapeBottom);

public sealed record LViewfinderPoint(double LViewfinderPointX, double LViewfinderPointY);

public sealed record LViewfinderGraduation(
    double LViewfinderTickX,
    double LViewfinderTickTop,
    double LViewfinderTickBottom,
    string LViewfinderTickLabel);

public sealed record LViewfinderBand(
    double LViewfinderBandLeft,
    double LViewfinderBandTop,
    double LViewfinderBandRight,
    double LViewfinderBandBottom,
    int LViewfinderBandColor,
    string LViewfinderBandBadge,
    string LViewfinderBandName,
    bool LViewfinderBandShown,
    bool LViewfinderBandSelected);

public sealed record LViewfinderCursor(double LViewfinderCursorX, string LViewfinderCursorText);

public sealed record LViewfinderFrame(
    IReadOnlyList<LViewfinderShape> LViewfinderFrameUnder,
    LFlowWaveformOutline LViewfinderFrameWaveform,
    IReadOnlyList<LViewfinderGraduation> LViewfinderFrameTicks,
    IReadOnlyList<LViewfinderBand> LViewfinderFrameBands,
    IReadOnlyList<LViewfinderShape> LViewfinderFrameOver,
    IReadOnlyList<LViewfinderShape> LViewfinderFrameKeyframes,
    IReadOnlyList<LViewfinderCursor> LViewfinderFrameCursor);

public sealed class LViewfinder
{
    public const double LViewfinderLaneHeight = 20;

    private const double LViewfinderRenderLeast = 28;
    internal const double LViewfinderCoverageHeight = 4;
    private const double LViewfinderSectionInset = 1;
    private const double LViewfinderKeyframeWidth = 1;

    private static readonly LFlowWaveformOutline lViewfinderOutlineEmpty = new(0, 0, []);

    private readonly LFlow lFlow;
    private bool lViewfinderDragActive;
    private string lViewfinderTrigger = "attach";
    private int lViewfinderGlyphCount;
    private readonly QChronometer lViewfinderChronometer = new();

    public LViewfinder(LFlow lOwner)
    {
        lFlow = lOwner;
        lFlow.LFlowAttachApplied += () => LViewfinderTriggerApply("attach");
        lFlow.LFlowClearApplied += () => LViewfinderTriggerApply("clear");
        lFlow.LFlowCursorApplied += () => LViewfinderTriggerApply("cursor");
        lFlow.QFlowSpool.LFlowSpoolApplied += () => LViewfinderTriggerApply("spool");
        lFlow.LFlowSection.LFlowSectionChanged += (_, _) => LViewfinderTriggerApply("sections");
        lFlow.LFlowKeyframe.LFlowKeyframeChanged += (_, _) => LViewfinderTriggerApply("keyframes");
        lFlow.LFlowWaveform.LFlowWaveformUpdated += _ => LViewfinderTriggerApply("waveform");
    }

    public event Action? LViewfinderFrameApplied;

    public bool LViewfinderDragActive => lViewfinderDragActive;

    public string LViewfinderTrigger => lViewfinderTrigger;

    public int LViewfinderGlyphCount => lViewfinderGlyphCount;

    public void LViewfinderDrawStart() => lViewfinderChronometer.QChronometerStart();

    public void LViewfinderDrawRecord()
    {
        LTrace.LTraceTimelineAdd(
            "Viewfinder",
            lFlow.LFlowCursor,
            lFlow.LFlowSourcePath,
            lViewfinderTrigger,
            lViewfinderChronometer.QChronometerRead(),
            lViewfinderGlyphCount);
    }

    public LViewfinderFrame LViewfinderFrameResolve(double lWidth, double lHeight)
    {
        lViewfinderGlyphCount = 0;
        var lUnder = new List<LViewfinderShape>
        {
            new(LViewfinderShapeKind.LViewfinderShapeBackground, 0, 0, lWidth, lHeight)
        };
        var lFrame = new LViewfinderFrame(lUnder, lViewfinderOutlineEmpty, [], [], [], [], []);
        if (lFlow.LFlowSpool is not { } lSpool || lWidth <= 0 || lHeight < LViewfinderRenderLeast)
        {
            return lFrame;
        }

        double lCoverageTop = Math.Max(0, Math.Max(0, lHeight - 1) - LViewfinderCoverageHeight);
        (double lRailTop, double lRailBottom) = QViewfinderRail.LViewfinderRailResolve(lHeight);
        double lRailHeight = lRailBottom - lRailTop;
        if (lRailHeight <= 0)
        {
            return lFrame;
        }

        byte[] lPeaks = lFlow.LFlowWaveform.LFlowWaveformPeaks;
        bool lWaveformActive = lPeaks.Length > 0;
        lUnder.Add(new LViewfinderShape(
            lWaveformActive ? LViewfinderShapeKind.LViewfinderShapeWaveform : LViewfinderShapeKind.LViewfinderShapeRail,
            0, lRailTop, lWidth, lRailBottom));
        lUnder.Add(new LViewfinderShape(
            LViewfinderShapeKind.LViewfinderShapeCoverage,
            0,
            lCoverageTop,
            lWidth,
            lCoverageTop + LViewfinderCoverageHeight));

        TimeSpan lRangeStart = lSpool.LSpoolRangeOrigin;
        TimeSpan lRangeEnd = lSpool.LSpoolRangeLimit;
        double lRangeSeconds = (lRangeEnd - lRangeStart).TotalSeconds;
        if (lRangeSeconds <= 0)
        {
            return lFrame;
        }

        LFlowWaveformOutline lOutline = lWaveformActive
            ? LFlowWaveform.LFlowOutlineResolve(lPeaks, lWidth, lRailTop, lRailHeight, lRangeStart, lRangeEnd)
            : lViewfinderOutlineEmpty;
        IReadOnlyList<LViewfinderGraduation> lTicks =
            QViewfinderRuler.LViewfinderTicksResolve(lWidth, lRangeStart, lRangeSeconds);
        IReadOnlyList<LViewfinderBand> lBands =
            LViewfinderBandsResolve(lWidth, lRailTop, lRailBottom, lRangeStart, lRangeEnd, lRangeSeconds);
        IReadOnlyList<LViewfinderShape> lOver =
            LViewfinderCoverageResolve(lSpool, lWidth, lCoverageTop, lRangeStart, lRangeSeconds);
        IReadOnlyList<LViewfinderShape> lKeyframes =
            LViewfinderKeyframesResolve(lSpool, lWidth, lRailTop, lRailBottom, lRangeStart, lRangeSeconds);
        var lCursor = new List<LViewfinderCursor>();
        TimeSpan lTime = lFlow.LFlowCursor;
        if (lTime >= lRangeStart && lTime <= lRangeEnd)
        {
            double lCursorX = Math.Clamp((lTime - lRangeStart).TotalSeconds / lRangeSeconds, 0, 1) * lWidth;
            lCursor.Add(new LViewfinderCursor(lCursorX, LCursor.LCursorTimeFormat(lTime)));
        }

        lViewfinderGlyphCount = lTicks.Count + lBands.Count * 2 + lCursor.Count;
        return lFrame with
        {
            LViewfinderFrameWaveform = lOutline,
            LViewfinderFrameTicks = lTicks,
            LViewfinderFrameBands = lBands,
            LViewfinderFrameOver = lOver,
            LViewfinderFrameKeyframes = lKeyframes,
            LViewfinderFrameCursor = lCursor
        };
    }

    public LViewfinderPoint LViewfinderPopupResolve(int lIndex, double lWidth, double lHeight)
    {
        LViewfinderBand? lBand = LViewfinderSectionResolve(lIndex, lWidth, lHeight);
        double lBandWidth = lBand is null ? 0 : lBand.LViewfinderBandRight - lBand.LViewfinderBandLeft;
        double lBandHeight = lBand is null ? 0 : lBand.LViewfinderBandBottom - lBand.LViewfinderBandTop;
        return new LViewfinderPoint(
            LFlowName.LFlowOffsetResolve(lBand is null, lBand?.LViewfinderBandLeft ?? 0, lBandWidth, lWidth),
            LFlowName.LFlowOffsetResolve(lBand is null, lBand?.LViewfinderBandTop ?? 0, lBandHeight, lHeight));
    }

    public LViewfinderBand? LViewfinderSectionResolve(int lIndex, double lWidth, double lHeight)
    {
        IReadOnlyList<LPiece> lSections = lFlow.LFlowSection.LFlowSectionsRead();
        if (lFlow.LFlowSpool is not { } lSpool || lIndex < 0 || lIndex >= lSections.Count
            || lWidth <= 0 || lHeight <= 0)
        {
            return null;
        }

        (double lRailTop, double lRailBottom) = QViewfinderRail.LViewfinderRailResolve(lHeight);
        if (lRailBottom <= lRailTop)
        {
            return null;
        }

        return LViewfinderBandResolve(
            lSections[lIndex],
            lIndex,
            lWidth,
            lRailTop,
            lRailBottom,
            lSpool.LSpoolRangeOrigin,
            lSpool.LSpoolRangeLimit);
    }

    public bool LViewfinderPressHandle(double lX, double lWidth)
    {
        if (lFlow.LFlowSpool is not { } lSpool || lWidth <= 0)
        {
            return false;
        }

        TimeSpan lTime = LViewfinderTimeResolve(lSpool, lX, lWidth);
        IReadOnlyList<LPiece> lSections = lFlow.LFlowSection.LFlowSectionsRead();
        for (int lIndex = lSections.Count - 1; lIndex >= 0; lIndex--)
        {
            if (lTime >= lSections[lIndex].LPieceOrigin && lTime <= lSections[lIndex].LPieceEnd)
            {
                lFlow.LFlowSection.LFlowSectionSelect(lIndex);
                break;
            }
        }

        lViewfinderDragActive = true;
        lFlow.LFlowDragHandle(true);
        lFlow.LFlowCursorSeek(lTime);
        return true;
    }

    public bool LViewfinderMoveHandle(double lX, double lWidth)
    {
        if (!lViewfinderDragActive || lFlow.LFlowSpool is not { } lSpool || lWidth <= 0)
        {
            return false;
        }

        lFlow.LFlowCursorSeek(LViewfinderTimeResolve(lSpool, lX, lWidth));
        return true;
    }

    public void LViewfinderDragClear()
    {
        bool lDragging = lViewfinderDragActive;
        lViewfinderDragActive = false;
        if (lDragging)
        {
            lFlow.LFlowDragHandle(false);
        }
    }

    private void LViewfinderTriggerApply(string lTrigger)
    {
        lViewfinderTrigger = lTrigger;
        LViewfinderFrameApplied?.Invoke();
    }

    private static TimeSpan LViewfinderTimeResolve(LSpool lSpool, double lX, double lWidth)
    {
        double lRatio = Math.Clamp(Math.Clamp(lX, 0, lWidth) / lWidth, 0, 1);
        TimeSpan lRange = lSpool.LSpoolRangeLimit - lSpool.LSpoolRangeOrigin;
        if (lRange <= TimeSpan.Zero)
        {
            return lSpool.LSpoolRangeOrigin;
        }

        return lSpool.LSpoolRangeOrigin + TimeSpan.FromSeconds(lRatio * lRange.TotalSeconds);
    }

    private IReadOnlyList<LViewfinderBand> LViewfinderBandsResolve(
        double lWidth,
        double lRailTop,
        double lRailBottom,
        TimeSpan lRangeStart,
        TimeSpan lRangeEnd,
        double lRangeSeconds)
    {
        IReadOnlyList<LPiece> lSections = lFlow.LFlowSection.LFlowSectionsRead();
        var lBands = new List<LViewfinderBand>(lSections.Count);
        for (int lIndex = 0; lIndex < lSections.Count; lIndex++)
        {
            if (LViewfinderBandResolve(lSections[lIndex], lIndex, lWidth, lRailTop, lRailBottom, lRangeStart, lRangeEnd)
                is { } lBand)
            {
                lBands.Add(lBand);
            }
        }

        return lBands;
    }

    private LViewfinderBand? LViewfinderBandResolve(
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
        double lTop = lRailTop + LViewfinderSectionInset;
        double lHeight = Math.Max(4, lRailBottom - lRailTop - LViewfinderSectionInset * 2);
        return new LViewfinderBand(
            lLeft,
            lTop,
            lLeft + Math.Max(1, lRight - lLeft),
            lTop + lHeight,
            lSection.LPieceColorIndex,
            $"{lIndex + 1}",
            lSection.LPieceName,
            !lSection.LPieceHidden,
            lIndex == lFlow.QFlowEdit.LFlowSectionIndex);
    }

    private IReadOnlyList<LViewfinderShape> LViewfinderCoverageResolve(
        LSpool lSpool, double lWidth, double lCoverageTop, TimeSpan lRangeStart, double lRangeSeconds)
    {
        var lShapes = new List<LViewfinderShape>();
        IReadOnlyList<LKeyframeScanRange> lRanges = lFlow.LFlowKeyframe.LFlowKeyframeRanges;
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

            lShapes.Add(new LViewfinderShape(
                LViewfinderShapeKind.LViewfinderShapeScanned,
                lLeft,
                lCoverageTop,
                lLeft + lScanWidth,
                lCoverageTop + LViewfinderCoverageHeight));
        }

        return lShapes;
    }

    private IReadOnlyList<LViewfinderShape> LViewfinderKeyframesResolve(
        LSpool lSpool, double lWidth, double lRailTop, double lRailBottom, TimeSpan lRangeStart, double lRangeSeconds)
    {
        IReadOnlyList<LKeyframeEntry> lVisible = LKeyframeView.LKeyframeVisibleResolve(
            lFlow.LFlowKeyframe.LFlowKeyframeEntries, lFlow.LFlowCursor, lSpool);
        double[] lOffsets = lVisible
            .Select(lEntry => (lEntry.LKeyframePresentationTime - lRangeStart).TotalSeconds / lRangeSeconds * lWidth)
            .ToArray();
        if (lOffsets.Length == 0 || !LViewfinderGapCheck(lOffsets))
        {
            return [];
        }

        double lKeyframeRight = Math.Max(0, lWidth - LViewfinderKeyframeWidth);
        return lOffsets
            .Select(lOffset => Math.Clamp(lOffset, 0, lKeyframeRight))
            .Select(lX => new LViewfinderShape(
                LViewfinderShapeKind.LViewfinderShapeKeyframe,
                lX,
                lRailTop,
                lX + LViewfinderKeyframeWidth,
                lRailBottom))
            .ToArray();
    }

    private static bool LViewfinderGapCheck(double[] lOffsets)
    {
        double lMinimumGap = Math.Max(
            LViewfinderKeyframeWidth, LPreference.LPreferenceStateCurrent.LPreferenceKeyframePixels);
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
