using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Media;
using Cadroue.UIShell;

namespace Cadroue.UIShell.PFlow;

public sealed class PViewfinder : FrameworkElement
{
    private enum PViewfinderDragMode
    {
        PViewfinderDragNone,
        PViewfinderDragCursor
    }

    private const double PViewfinderMinimumRenderHeight = 28;
    private const double PViewfinderLabelLaneHeight = 20;
    private const double PViewfinderCoverageHeight = 4;
    private const double PViewfinderRailGap = 2;
    private const double PViewfinderLabelPaddingHorizontal = 4;
    private const double PViewfinderLabelPaddingVertical = 2;
    private const double PViewfinderTickTargetPixels = 100;

    private static readonly Brush pViewfinderBrushBackground = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    private static readonly Brush pViewfinderBrushRail = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1));
    private static readonly Pen pViewfinderPenCursor = new(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)), 1.0);
    private static readonly Pen pViewfinderPenKeyframe = new(new SolidColorBrush(Color.FromRgb(0x6B, 0x74, 0x80)), 1.1);
    private static readonly Pen pViewfinderPenTick = new(new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0)), 1.0);
    private static readonly Brush pViewfinderBrushTickText = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
    private static readonly Brush pViewfinderBrushLabelBackground = new SolidColorBrush(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF));
    private static readonly Brush pViewfinderBrushCoverageScanned = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush[] pViewfinderSectionBrushes =
    {
        new SolidColorBrush(Color.FromArgb(0x99, 0x4A, 0x90, 0xD9)),
        new SolidColorBrush(Color.FromArgb(0x99, 0x27, 0xAE, 0x60)),
        new SolidColorBrush(Color.FromArgb(0x99, 0xE6, 0x7E, 0x22)),
        new SolidColorBrush(Color.FromArgb(0x99, 0x8E, 0x44, 0xAD)),
        new SolidColorBrush(Color.FromArgb(0x99, 0xE7, 0x4C, 0x3C)),
        new SolidColorBrush(Color.FromArgb(0x99, 0x16, 0xA0, 0x85)),
    };
    private static readonly Pen pViewfinderPenLabelBorder = new(new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1)), 1.0);
    private static readonly Brush pViewfinderBrushCursorText = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
    private static readonly Typeface pViewfinderTickTypeface = new("Consolas");

    static PViewfinder()
    {
        pViewfinderBrushBackground.Freeze();
        pViewfinderBrushRail.Freeze();
        pViewfinderPenCursor.Freeze();
        pViewfinderPenKeyframe.Freeze();
        pViewfinderPenTick.Freeze();
        pViewfinderBrushTickText.Freeze();
        pViewfinderBrushLabelBackground.Freeze();
        pViewfinderPenLabelBorder.Freeze();
        pViewfinderBrushCursorText.Freeze();
        pViewfinderBrushCoverageScanned.Freeze();
    }

    private LSpool? lSpool;
    private TimeSpan lCursor;
    private IReadOnlyList<LKeyframeEntry> lKeyframes = Array.Empty<LKeyframeEntry>();
    private IReadOnlyList<LKeyframeScanRange> lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
    private IReadOnlyList<LSectionEntry> lSectionList = Array.Empty<LSectionEntry>();
    private int? lSectionIndexSelect;
    private PViewfinderDragMode pViewfinderDragMode;

    public event Action<TimeSpan>? PViewfinderCursorChangeRequest;
    public event Action<int>? PViewfinderSectionSelectRequest;

    public void PViewfinderAttach(LSpool spool, TimeSpan cursor)
    {
        lSpool = spool ?? throw new ArgumentNullException(nameof(spool));
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        InvalidateVisual();
    }

    public void PViewfinderCursorUpdate(TimeSpan cursor)
    {
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        InvalidateVisual();
    }

    public void PViewfinderClear()
    {
        lSpool = null;
        lCursor = TimeSpan.Zero;
        lKeyframes = Array.Empty<LKeyframeEntry>();
        lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
        lSectionList = Array.Empty<LSectionEntry>();
        lSectionIndexSelect = null;
        InvalidateVisual();
    }

    public void PViewfinderSpoolUpdate() => InvalidateVisual();

    public void PViewfinderKeyframesUpdate(
        IReadOnlyList<LKeyframeEntry>? keyframes,
        IReadOnlyList<LKeyframeScanRange>? scannedRanges)
    {
        lKeyframes = keyframes ?? Array.Empty<LKeyframeEntry>();
        lKeyframeScannedRanges = scannedRanges ?? Array.Empty<LKeyframeScanRange>();
        InvalidateVisual();
    }

    public void PViewfinderSectionsUpdate(IReadOnlyList<LSectionEntry>? sections, int? selectedIndex)
    {
        lSectionList = sections?.ToArray() ?? Array.Empty<LSectionEntry>();
        lSectionIndexSelect = selectedIndex;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        double actualWidth = ActualWidth;
        double actualHeight = ActualHeight;
        drawingContext.DrawRectangle(pViewfinderBrushBackground, null, new Rect(0, 0, actualWidth, actualHeight));

        if (lSpool is null || actualWidth <= 0 || actualHeight < PViewfinderMinimumRenderHeight)
        {
            return;
        }

        double coverageBottom = Math.Max(0, actualHeight - 1);
        double coverageTop = Math.Max(0, coverageBottom - PViewfinderCoverageHeight);
        double railTop = PViewfinderLabelLaneHeight + PViewfinderRailGap;
        double railBottom = Math.Max(railTop, coverageTop - PViewfinderRailGap);
        double railHeight = railBottom - railTop;

        if (railHeight <= 0)
        {
            return;
        }

        drawingContext.DrawRoundedRectangle(
            pViewfinderBrushRail,
            null,
            new Rect(0, railTop, actualWidth, railHeight),
            3,
            3);
        drawingContext.DrawRectangle(
            pViewfinderBrushRail,
            null,
            new Rect(0, coverageTop, actualWidth, PViewfinderCoverageHeight));

        TimeSpan rangeStart = lSpool.LSpoolWorkingRangeStart;
        TimeSpan rangeEnd = lSpool.LSpoolWorkingRangeEnd;
        double rangeSeconds = (rangeEnd - rangeStart).TotalSeconds;
        if (rangeSeconds <= 0)
        {
            return;
        }

        PViewfinderTicksDraw(drawingContext, actualWidth, rangeStart, rangeSeconds);
        PViewfinderSectionsDraw(drawingContext, actualWidth, railTop, railBottom, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderKeyframeCoverageDraw(drawingContext, actualWidth, coverageTop, PViewfinderCoverageHeight, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderKeyframesDraw(drawingContext, actualWidth, railTop, railBottom, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderCursorDraw(drawingContext, actualWidth, actualHeight, rangeStart, rangeEnd, rangeSeconds);
    }


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
            LSectionEntry section = lSectionList[index];
            TimeSpan sectionStart = section.LSectionStart < rangeStart ? rangeStart : section.LSectionStart;
            TimeSpan sectionEnd = section.LSectionEnd > rangeEnd ? rangeEnd : section.LSectionEnd;
            if (sectionEnd <= sectionStart)
            {
                continue;
            }

            double sectionStartX = Math.Clamp((sectionStart - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double sectionEndX = Math.Clamp((sectionEnd - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double sectionWidth = Math.Max(1, sectionEndX - sectionStartX);
            Brush sectionBrush = pViewfinderSectionBrushes[Math.Abs(section.LSectionColorIndex) % pViewfinderSectionBrushes.Length];
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

    private void PViewfinderKeyframeCoverageDraw(
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
        TimeSpan visibleSearchStart = PViewfinderMaximumTime(rangeStart, lCursor - LKeyframeOrchestrator.LKeyframeRangeBefore);
        TimeSpan visibleSearchEnd = PViewfinderMinimumTime(rangeEnd, lCursor + LKeyframeOrchestrator.LKeyframeRangeAfter);
        if (visibleSearchEnd <= visibleSearchStart)
        {
            return;
        }

        TimeSpan[] visibleSearchKeyframes = lKeyframes
            .Where(entry => entry.LKeyframePresentationTime >= visibleSearchStart && entry.LKeyframePresentationTime <= visibleSearchEnd)
            .Select(entry => entry.LKeyframePresentationTime)
            .ToArray();
        if (!PViewfinderKeyframeVisibilityAllowed(actualWidth, rangeSeconds, visibleSearchKeyframes.Length))
        {
            return;
        }

        foreach (TimeSpan keyframeTime in visibleSearchKeyframes)
        {
            double keyframeX = Math.Clamp((keyframeTime - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            drawingContext.DrawLine(pViewfinderPenKeyframe, new Point(keyframeX, railTop), new Point(keyframeX, railBottom));
        }
    }

    private static bool PViewfinderKeyframeVisibilityAllowed(
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

    private static TimeSpan PViewfinderMinimumTime(TimeSpan first, TimeSpan second)
        => first <= second ? first : second;

    private static TimeSpan PViewfinderMaximumTime(TimeSpan first, TimeSpan second)
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

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (lSpool is null || ActualWidth <= 0)
        {
            return;
        }

        TimeSpan requestTime = PViewfinderPositionTimeConvert(e.GetPosition(this).X);
        PViewfinderSectionSelectPropagate(requestTime);
        pViewfinderDragMode = PViewfinderDragMode.PViewfinderDragCursor;
        PViewfinderCursorChangeRequest?.Invoke(requestTime);
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (pViewfinderDragMode == PViewfinderDragMode.PViewfinderDragNone || lSpool is null || ActualWidth <= 0)
        {
            return;
        }

        PViewfinderCursorChangeRequest?.Invoke(PViewfinderPositionTimeConvert(e.GetPosition(this).X));
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        pViewfinderDragMode = PViewfinderDragMode.PViewfinderDragNone;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        pViewfinderDragMode = PViewfinderDragMode.PViewfinderDragNone;
    }

    private void PViewfinderSectionSelectPropagate(TimeSpan requestTime)
    {
        for (int index = 0; index < lSectionList.Count; index++)
        {
            LSectionEntry section = lSectionList[index];
            if (requestTime >= section.LSectionStart && requestTime <= section.LSectionEnd)
            {
                PViewfinderSectionSelectRequest?.Invoke(index);
                return;
            }
        }
    }

    private TimeSpan PViewfinderPositionTimeConvert(double mouseX)
    {
        if (lSpool is null || ActualWidth <= 0)
        {
            return TimeSpan.Zero;
        }

        double clampedMouseX = Math.Clamp(mouseX, 0, ActualWidth);
        double ratio = Math.Clamp(clampedMouseX / ActualWidth, 0, 1);
        TimeSpan rangeDuration = lSpool.LSpoolWorkingRangeEnd - lSpool.LSpoolWorkingRangeStart;
        if (rangeDuration <= TimeSpan.Zero)
        {
            return lSpool.LSpoolWorkingRangeStart;
        }

        return lSpool.LSpoolWorkingRangeStart + TimeSpan.FromSeconds(ratio * rangeDuration.TotalSeconds);
    }

    private static string PViewfinderTimeFormat(TimeSpan time) =>
        time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
}
