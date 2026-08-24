using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Media;

using Cadroue.Core;

using Cadroue.Infrastructure;


namespace Cadroue.UIShell.PFlow;

public sealed partial class PViewfinder : FrameworkElement
{
    private enum PViewfinderDragMode
    {
        PViewfinderDragNone,
        PViewfinderDragCursor
    }

    private const double PViewfinderRenderLeast = 28;
    private const double PTimecodeLaneHeight = 20;
    private const double PViewfinderCoverageHeight = 4;
    private const double PViewfinderRailGap = 2;
    private const double PTimecodePaddingHorizontal = 4;
    private const double PTimecodePaddingVertical = 2;
    private const double PViewfinderTickPixels = 100;
    private const double PViewfinderSectionInset = 1;
    private const double PViewfinderSectionPadding = 5;
    private const double PViewfinderSectionLeast = 18;
    private const double PViewfinderHeightLeast = 16;
    private const double PViewfinderBadgeHorizontal = 6;
    private const double PViewfinderBadgeVertical = 1;
    private const double PViewfinderBadgeGap = 6;
    private const double PViewfinderKeyframeWidth = 1;

    private static readonly Brush pViewfinderSectionBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pViewfinderBadgeBrush = new SolidColorBrush(Colors.White);
    private static readonly Typeface pViewfinderBadgeTypeface =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    private static readonly Brush pViewfinderBrushBackground = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    private static readonly Brush pViewfinderBrushRail = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1));
    private static readonly Brush pViewfinderWaveformBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xEF));
    private static readonly Brush pViewfinderBrushWaveform = new SolidColorBrush(Color.FromRgb(0x8C, 0x9B, 0xAD));
    private static readonly Brush pViewfinderBrushKeyframe = new SolidColorBrush(Color.FromRgb(0x6B, 0x74, 0x80));
    private static readonly Pen pViewfinderTickPen = new(new SolidColorBrush(Color.FromRgb(0xB0, 0xB0, 0xB0)), 1.0);
    private static readonly Brush pViewfinderTickBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
    private static readonly Brush pTimecodeBackgroundBrush = new SolidColorBrush(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF));
    private static readonly Brush pViewfinderCoverageBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Pen pTimecodeBorderPen = new(new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1)), 1.0);
    private static readonly Brush pViewfinderCursorBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
    private static readonly Typeface pViewfinderTickTypeface = new("Segoe UI");

    static PViewfinder()
    {
        pViewfinderBrushBackground.Freeze();
        pViewfinderBrushRail.Freeze();
        pViewfinderWaveformBrush.Freeze();
        pViewfinderBrushWaveform.Freeze();
        pViewfinderBrushKeyframe.Freeze();
        pViewfinderTickPen.Freeze();
        pViewfinderTickBrush.Freeze();
        pTimecodeBackgroundBrush.Freeze();
        pTimecodeBorderPen.Freeze();
        pViewfinderCursorBrush.Freeze();
        pViewfinderCoverageBrush.Freeze();
        pViewfinderSectionBrush.Freeze();
        pViewfinderBadgeBrush.Freeze();
    }

    private LSpool? lSpool;
    private TimeSpan lCursor;
    private string? lSourcePath;
    private IReadOnlyList<LKeyframeEntry> lKeyframeList = Array.Empty<LKeyframeEntry>();
    private IReadOnlyList<LKeyframeScanRange> lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
    private IReadOnlyList<LPiece> lSectionList = Array.Empty<LPiece>();
    private byte[] lWaveformPeaks = Array.Empty<byte>();
    private int? lSectionIndexActive;
    private PViewfinderDragMode pViewfinderDragMode;
    private string pViewfinderDrawTrigger = "attach";
    private int pViewfinderGlyphCount;
    private readonly Dictionary<(int Kind, string Text, double Room), FormattedText> pViewfinderTextCache = new();
    private double pViewfinderTextDpi = -1;

    public event Action<TimeSpan>? PViewfinderCursorChange;
    public event Action<int>? PViewfinderSectionSelect;
    public event Action<bool>? PViewfinderDragChange;

    private void PViewfinderDrawDefer(string pViewfinderTrigger)
    {
        pViewfinderDrawTrigger = pViewfinderTrigger;
        InvalidateVisual();
    }

    public void PViewfinderAttach(LSpool spool, TimeSpan cursor, string? sourcePath)
    {
        lSpool = spool ?? throw new ArgumentNullException(nameof(spool));
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        lSourcePath = sourcePath;
        PViewfinderDrawDefer("attach");
    }

    public void PViewfinderCursorUpdate(TimeSpan cursor)
    {
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        PViewfinderDrawDefer("cursor");
    }

    public void PViewfinderClear()
    {
        lSpool = null;
        lCursor = TimeSpan.Zero;
        lSourcePath = null;
        lKeyframeList = Array.Empty<LKeyframeEntry>();
        lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
        lSectionList = Array.Empty<LPiece>();
        lWaveformPeaks = Array.Empty<byte>();
        lSectionIndexActive = null;
        PViewfinderDrawDefer("clear");
    }

    public void PViewfinderWaveformUpdate(byte[] waveformPeaks)
    {
        lWaveformPeaks = waveformPeaks;
        PViewfinderDrawDefer("waveform");
    }

    public void PViewfinderSpoolUpdate() => PViewfinderDrawDefer("spool");

    public void PViewfinderKeyframesUpdate(
        IReadOnlyList<LKeyframeEntry>? keyframes,
        IReadOnlyList<LKeyframeScanRange>? scannedRanges)
    {
        lKeyframeList = keyframes ?? Array.Empty<LKeyframeEntry>();
        lKeyframeScannedRanges = scannedRanges ?? Array.Empty<LKeyframeScanRange>();
        PViewfinderDrawDefer("keyframes");
    }

    private static (double Top, double Bottom) PViewfinderRailRead(double actualHeight)
    {
        double pRailTop = PTimecodeLaneHeight + PViewfinderRailGap;
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
        TimeSpan pRangeStart = lSpool.LSpoolRangeOrigin;
        TimeSpan pRangeEnd = lSpool.LSpoolRangeLimit;
        double pRangeSeconds = (pRangeEnd - pRangeStart).TotalSeconds;
        if (pRailBottom <= pRailTop || pRangeSeconds <= 0)
        {
            return Rect.Empty;
        }

        LPiece pSection = lSectionList[pSectionIndex];
        TimeSpan pStart = pSection.LPieceOrigin < pRangeStart ? pRangeStart : pSection.LPieceOrigin;
        TimeSpan pEnd = pSection.LPieceEnd > pRangeEnd ? pRangeEnd : pSection.LPieceEnd;
        if (pEnd <= pStart)
        {
            return Rect.Empty;
        }

        double pLeft = Math.Clamp((pStart - pRangeStart).TotalSeconds / pRangeSeconds * pWidth, 0, pWidth);
        double pRight = Math.Clamp((pEnd - pRangeStart).TotalSeconds / pRangeSeconds * pWidth, 0, pWidth);
        return new Rect(pLeft, pRailTop, Math.Max(1, pRight - pLeft), pRailBottom - pRailTop);
    }

    public void PViewfinderSectionsUpdate(IReadOnlyList<LPiece>? sections, int? selectedIndex)
    {
        lSectionList = sections?.ToArray() ?? Array.Empty<LPiece>();
        lSectionIndexActive = selectedIndex;
        PViewfinderDrawDefer("sections");
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (!LTrace.LTraceVerbose)
        {
            PViewfinderContentDraw(drawingContext);
            return;
        }

        pViewfinderGlyphCount = 0;
        long pViewfinderStamp = System.Diagnostics.Stopwatch.GetTimestamp();
        PViewfinderContentDraw(drawingContext);
        double pViewfinderMilliseconds =
            (System.Diagnostics.Stopwatch.GetTimestamp() - pViewfinderStamp) * 1000d
            / System.Diagnostics.Stopwatch.Frequency;
        LTrace.LTraceTimelineAdd(
            "Viewfinder", lCursor, lSourcePath, pViewfinderDrawTrigger, pViewfinderMilliseconds, pViewfinderGlyphCount);
    }

    private void PViewfinderContentDraw(DrawingContext drawingContext)
    {
        double actualWidth = ActualWidth;
        double actualHeight = ActualHeight;
        drawingContext.DrawRectangle(pViewfinderBrushBackground, null, new Rect(0, 0, actualWidth, actualHeight));

        if (lSpool is null || actualWidth <= 0 || actualHeight < PViewfinderRenderLeast)
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

        bool waveformActive = lWaveformPeaks.Length > 0;
        drawingContext.DrawRoundedRectangle(
            waveformActive ? pViewfinderWaveformBrush : pViewfinderBrushRail,
            null,
            new Rect(0, railTop, actualWidth, railHeight),
            3,
            3);
        drawingContext.DrawRectangle(
            pViewfinderBrushRail,
            null,
            new Rect(0, coverageTop, actualWidth, PViewfinderCoverageHeight));

        TimeSpan rangeStart = lSpool.LSpoolRangeOrigin;
        TimeSpan rangeEnd = lSpool.LSpoolRangeLimit;
        double rangeSeconds = (rangeEnd - rangeStart).TotalSeconds;
        if (rangeSeconds <= 0)
        {
            return;
        }

        if (waveformActive)
        {
            PViewfinderWaveformDraw(drawingContext, actualWidth, railTop, railHeight, rangeStart, rangeEnd);
        }

        PViewfinderTicksDraw(drawingContext, actualWidth, rangeStart, rangeSeconds);
        PViewfinderSectionsDraw(drawingContext, actualWidth, railTop, railBottom, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderCoverageDraw(drawingContext, actualWidth, coverageTop, PViewfinderCoverageHeight, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderKeyframesDraw(drawingContext, actualWidth, railTop, railBottom, rangeStart, rangeEnd, rangeSeconds);
        PViewfinderCursorDraw(drawingContext, actualWidth, actualHeight, rangeStart, rangeEnd, rangeSeconds);
    }

}
