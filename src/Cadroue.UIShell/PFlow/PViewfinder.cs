using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Media;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PViewfinder : FrameworkElement
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
    private const double PViewfinderSectionInset = 1;
    private const double PViewfinderSectionLabelPadding = 5;
    private const double PViewfinderSectionLabelLeast = 18;
    private const double PViewfinderSectionLabelHeightLeast = 16;
    private const double PViewfinderBadgePaddingHorizontal = 6;
    private const double PViewfinderBadgePaddingVertical = 1;
    private const double PViewfinderBadgeGap = 6;

    private static readonly Brush pViewfinderBrushSectionText = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pViewfinderBrushBadgeText = new SolidColorBrush(Colors.White);
    private static readonly Typeface pViewfinderBadgeTypeface =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    private static readonly Brush pViewfinderBrushBackground = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    private static readonly Brush pViewfinderBrushRail = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1));
    private static readonly Pen pViewfinderPenKeyframe = new(new SolidColorBrush(Color.FromRgb(0x6B, 0x74, 0x80)), 1.1);
    private static readonly Pen pViewfinderPenTick = new(new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0)), 1.0);
    private static readonly Brush pViewfinderBrushTickText = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
    private static readonly Brush pViewfinderBrushLabelBackground = new SolidColorBrush(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF));
    private static readonly Brush pViewfinderBrushCoverageScanned = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Pen pViewfinderPenLabelBorder = new(new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1)), 1.0);
    private static readonly Brush pViewfinderBrushCursorText = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
    private static readonly Typeface pViewfinderTickTypeface = new("Segoe UI");

    static PViewfinder()
    {
        pViewfinderBrushBackground.Freeze();
        pViewfinderBrushRail.Freeze();
        pViewfinderPenKeyframe.Freeze();
        pViewfinderPenTick.Freeze();
        pViewfinderBrushTickText.Freeze();
        pViewfinderBrushLabelBackground.Freeze();
        pViewfinderPenLabelBorder.Freeze();
        pViewfinderBrushCursorText.Freeze();
        pViewfinderBrushCoverageScanned.Freeze();
        pViewfinderBrushSectionText.Freeze();
        pViewfinderBrushBadgeText.Freeze();
    }

    private LSpool? lSpool;
    private TimeSpan lCursor;
    private IReadOnlyList<LKeyframeEntry> lKeyframes = Array.Empty<LKeyframeEntry>();
    private IReadOnlyList<LKeyframeScanRange> lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
    private IReadOnlyList<LSegment> lSectionList = Array.Empty<LSegment>();
    private int? lSectionIndexSelect;
    private PViewfinderDragMode pViewfinderDragMode;

    public event Action<TimeSpan>? PViewfinderCursorChange;
    public event Action<int>? PViewfinderSectionSelect;

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
        lSectionList = Array.Empty<LSegment>();
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

    private static (double Top, double Bottom) PViewfinderRailRead(double actualHeight)
    {
        double pRailTop = PViewfinderLabelLaneHeight + PViewfinderRailGap;
        double pCoverageTop = Math.Max(0, Math.Max(0, actualHeight - 1) - PViewfinderCoverageHeight);
        return (pRailTop, Math.Max(pRailTop, pCoverageTop - PViewfinderRailGap));
    }

    internal Rect PViewfinderSectionRead(int pSectionIndex)
    {
        if (lSpool is null || pSectionIndex < 0 || pSectionIndex >= lSectionList.Count)
        {
            return Rect.Empty;
        }

        double pWidth = ActualWidth;
        if (pWidth <= 0 || ActualHeight <= 0)
        {
            return Rect.Empty;
        }

        (double pRailTop, double pRailBottom) = PViewfinderRailRead(ActualHeight);
        TimeSpan pRangeStart = lSpool.LSpoolWorkingRangeStart;
        TimeSpan pRangeEnd = lSpool.LSpoolWorkingRangeEnd;
        double pRangeSeconds = (pRangeEnd - pRangeStart).TotalSeconds;
        if (pRailBottom <= pRailTop || pRangeSeconds <= 0)
        {
            return Rect.Empty;
        }

        LSegment pSection = lSectionList[pSectionIndex];
        TimeSpan pStart = pSection.LSegmentStart < pRangeStart ? pRangeStart : pSection.LSegmentStart;
        TimeSpan pEnd = pSection.LSegmentEnd > pRangeEnd ? pRangeEnd : pSection.LSegmentEnd;
        if (pEnd <= pStart)
        {
            return Rect.Empty;
        }

        double pLeft = Math.Clamp((pStart - pRangeStart).TotalSeconds / pRangeSeconds * pWidth, 0, pWidth);
        double pRight = Math.Clamp((pEnd - pRangeStart).TotalSeconds / pRangeSeconds * pWidth, 0, pWidth);
        return new Rect(pLeft, pRailTop, Math.Max(1, pRight - pLeft), pRailBottom - pRailTop);
    }

    public void PViewfinderSectionsUpdate(IReadOnlyList<LSegment>? sections, int? selectedIndex)
    {
        lSectionList = sections?.ToArray() ?? Array.Empty<LSegment>();
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
        (double railTop, double railBottom) = PViewfinderRailRead(actualHeight);
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
        PViewfinderCoverageDraw(drawingContext, actualWidth, coverageTop, PViewfinderCoverageHeight, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderKeyframesDraw(drawingContext, actualWidth, railTop, railBottom, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderCursorDraw(drawingContext, actualWidth, actualHeight, rangeStart, rangeEnd, rangeSeconds);
    }

}
