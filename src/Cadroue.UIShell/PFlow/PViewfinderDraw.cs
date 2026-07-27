using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Cadroue.Media;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PViewfinder
{
    private void PViewfinderSectionsDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double railTop,
        double railBottom,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        double rangeSeconds)
    {
        if (lSectionList.Count == 0)
        {
            return;
        }

        double sectionTop = railTop + 3;
        double sectionHeight = Math.Max(4, (railBottom - railTop) * 0.28);
        for (int index = 0; index < lSectionList.Count; index++)
        {
            LSegment section = lSectionList[index];
            TimeSpan sectionStart = section.LSegmentStart < rangeStart ? rangeStart : section.LSegmentStart;
            TimeSpan sectionEnd = section.LSegmentEnd > rangeEnd ? rangeEnd : section.LSegmentEnd;
            if (sectionEnd <= sectionStart)
            {
                continue;
            }

            double sectionStartX = Math.Clamp((sectionStart - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double sectionEndX = Math.Clamp((sectionEnd - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double sectionWidth = Math.Max(1, sectionEndX - sectionStartX);
            Brush sectionBrush = pViewfinderSectionBrushes[Math.Abs(section.LSegmentColorIndex) % pViewfinderSectionBrushes.Length];
            Pen? sectionPen = index == lSectionIndexSelect ? new Pen(Brushes.Black, 1.5) : null;
            drawingContext.DrawRoundedRectangle(sectionBrush, sectionPen, new Rect(sectionStartX, sectionTop, sectionWidth, sectionHeight), 3, 3);
        }
    }

    private void PViewfinderCursorDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double actualHeight,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        double rangeSeconds)
    {
        if (lCursor < rangeStart || lCursor > rangeEnd)
        {
            return;
        }

        double cursorRatio = Math.Clamp((lCursor - rangeStart).TotalSeconds / rangeSeconds, 0, 1);
        double cursorX = cursorRatio * actualWidth;
        drawingContext.DrawLine(
            pViewfinderPenCursor,
            new Point(cursorX, PViewfinderLabelLaneHeight),
            new Point(cursorX, actualHeight));

        string timeText = PViewfinderTimeFormat(lCursor);
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var formattedText = new FormattedText(
            timeText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            pViewfinderTickTypeface,
            10,
            pViewfinderBrushCursorText,
            pixelsPerDip);
        double labelWidth = formattedText.Width + PViewfinderLabelPaddingHorizontal * 2;
        double labelHeight = formattedText.Height + PViewfinderLabelPaddingVertical * 2;
        double labelX = actualWidth <= labelWidth
            ? 0
            : Math.Clamp(cursorX - labelWidth / 2, 0, actualWidth - labelWidth);
        double safeLabelWidth = Math.Min(labelWidth, actualWidth);
        var labelRect = new Rect(labelX, 0, safeLabelWidth, labelHeight);
        drawingContext.DrawRoundedRectangle(pViewfinderBrushLabelBackground, pViewfinderPenLabelBorder, labelRect, 3, 3);
        drawingContext.DrawText(formattedText, new Point(labelX + PViewfinderLabelPaddingHorizontal, PViewfinderLabelPaddingVertical));
    }

    private void PViewfinderCoverageDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double coverageTop,
        double coverageHeight,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        double rangeSeconds)
    {
        foreach (LKeyframeScanRange range in lKeyframeScannedRanges)
        {
            TimeSpan scanStart = range.LKeyframeScanRangeStartTime < rangeStart
                ? rangeStart
                : range.LKeyframeScanRangeStartTime;
            TimeSpan scanEnd = range.LKeyframeScanRangeEndTime > rangeEnd
                ? rangeEnd
                : range.LKeyframeScanRangeEndTime;
            if (scanEnd <= scanStart)
            {
                continue;
            }

            double scanStartX = Math.Clamp((scanStart - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double scanEndX = Math.Clamp((scanEnd - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double scanWidth = Math.Max(1, scanEndX - scanStartX);
            if (scanStartX + scanWidth > actualWidth)
            {
                scanWidth = Math.Max(0, actualWidth - scanStartX);
            }

            if (scanWidth <= 0)
            {
                continue;
            }

            drawingContext.DrawRectangle(
                pViewfinderBrushCoverageScanned,
                null,
                new Rect(scanStartX, coverageTop, scanWidth, coverageHeight));
        }
    }

    private void PViewfinderKeyframesDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double railTop,
        double railBottom,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        double rangeSeconds)
    {
        TimeSpan visibleSearchStart = PViewfinderMaxResolve(rangeStart, lCursor - LKeyframeOrchestrator.LKeyframeRangeBefore);
        TimeSpan visibleSearchEnd = PViewfinderMinResolve(rangeEnd, lCursor + LKeyframeOrchestrator.LKeyframeRangeAfter);
        if (visibleSearchEnd <= visibleSearchStart)
        {
            return;
        }

        TimeSpan[] visibleSearchKeyframes = lKeyframes
            .Where(entry => entry.LKeyframePresentationTime >= visibleSearchStart && entry.LKeyframePresentationTime <= visibleSearchEnd)
            .Select(entry => entry.LKeyframePresentationTime)
            .ToArray();
        if (!PViewfinderVisibilityCheck(actualWidth, rangeSeconds, visibleSearchKeyframes.Length))
        {
            return;
        }

        foreach (TimeSpan keyframeTime in visibleSearchKeyframes)
        {
            double keyframeX = Math.Clamp((keyframeTime - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            drawingContext.DrawLine(pViewfinderPenKeyframe, new Point(keyframeX, railTop), new Point(keyframeX, railBottom));
        }
    }

    private static bool PViewfinderVisibilityCheck(
        double actualWidth,
        double rangeSeconds,
        int visibleSearchKeyframeCount)
    {
        if (visibleSearchKeyframeCount <= 0 || actualWidth <= 0 || rangeSeconds <= 0)
        {
            return false;
        }

        double searchDurationSeconds = LKeyframeOrchestrator.LKeyframeSearchDuration.TotalSeconds;
        if (searchDurationSeconds <= 0)
        {
            return false;
        }

        double searchAreaWidth = actualWidth * (searchDurationSeconds / rangeSeconds);
        double pixelsPerKeyframe = searchAreaWidth / visibleSearchKeyframeCount;
        return pixelsPerKeyframe > App.LPreferenceStateCurrent.LPreferenceKeyframeMinimumPixels;
    }

    private static TimeSpan PViewfinderMinResolve(TimeSpan first, TimeSpan second)
        => first <= second ? first : second;

    private static TimeSpan PViewfinderMaxResolve(TimeSpan first, TimeSpan second)
        => first >= second ? first : second;

    private void PViewfinderTicksDraw(
        DrawingContext drawingContext,
        double actualWidth,
        TimeSpan rangeStart,
        double rangeSeconds)
    {
        double[] tickStepOptionsSeconds = { 0.1, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800, 3600 };
        double tickIntervalSeconds = rangeSeconds / (actualWidth / PViewfinderTickTargetPixels);
        double tickStepSeconds = tickStepOptionsSeconds[^1];
        foreach (double candidateStepSeconds in tickStepOptionsSeconds)
        {
            if (candidateStepSeconds >= tickIntervalSeconds)
            {
                tickStepSeconds = candidateStepSeconds;
                break;
            }
        }

        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        double tickStartSeconds = Math.Ceiling(rangeStart.TotalSeconds / tickStepSeconds) * tickStepSeconds;
        for (double tickSeconds = tickStartSeconds; tickSeconds <= rangeStart.TotalSeconds + rangeSeconds + 1e-9; tickSeconds += tickStepSeconds)
        {
            double tickX = (tickSeconds - rangeStart.TotalSeconds) / rangeSeconds * actualWidth;
            drawingContext.DrawLine(
                pViewfinderPenTick,
                new Point(tickX, PViewfinderLabelLaneHeight * 0.5),
                new Point(tickX, PViewfinderLabelLaneHeight));
            string tickLabel = PViewfinderTimeFormat(TimeSpan.FromSeconds(tickSeconds));
            var formattedText = new FormattedText(
                tickLabel,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                pViewfinderTickTypeface,
                9,
                pViewfinderBrushTickText,
                pixelsPerDip);
            drawingContext.DrawText(
                formattedText,
                new Point(tickX + 2, PViewfinderLabelLaneHeight * 0.5 - formattedText.Height / 2));
        }
    }

    private static string PViewfinderTimeFormat(TimeSpan time) =>
        time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
}
