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
        PViewfinderCoverageDraw(drawingContext, actualWidth, coverageTop, PViewfinderCoverageHeight, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderKeyframesDraw(drawingContext, actualWidth, railTop, railBottom, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderCursorDraw(drawingContext, actualWidth, actualHeight, rangeStart, rangeEnd, rangeSeconds);
    }


}
