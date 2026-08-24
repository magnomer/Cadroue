using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Media;

using Cadroue.Core;

using Cadroue.Infrastructure;


namespace Cadroue.UIShell.PFlow;

public sealed partial class PMap : FrameworkElement
{
    private enum PMapDragMode
    {
        PMapDragNone,
        PMapResizeOrigin,
        PMapResizeLimit,
        PMapDragBody,
        PMapDragCursor,
    }

    private static readonly Brush pMapBrushBackground = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    private static readonly Brush pMapBrushRail = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1));
    private static readonly Brush pMapWaveformBrush = new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xEF));
    private static readonly Brush pMapBrushWaveform = new SolidColorBrush(Color.FromRgb(0x8C, 0x9B, 0xAD));
    private static readonly Brush pMapCoverageBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Brush pNavigatorShadowBrush = new SolidColorBrush(Color.FromArgb(0x34, 0x00, 0x00, 0x00));
    private const byte PNavigatorAlpha = 0x33;

    private static readonly Brush pNavigatorFrameBrush = new SolidColorBrush(Color.FromArgb(PNavigatorAlpha, 0x2D, 0x7D, 0xD2));
    private static readonly Brush pNavigatorFillBrush = new SolidColorBrush(Color.FromArgb(PNavigatorAlpha, 0x2D, 0x7D, 0xD2));
    private static readonly Brush pNavigatorBodyBrush = new SolidColorBrush(Color.FromArgb(PNavigatorAlpha, 0x3A, 0x8B, 0xE0));
    private static readonly Brush pNavigatorSideBrush = new SolidColorBrush(Color.FromArgb(PNavigatorAlpha, 0x2D, 0x7D, 0xD2));
    private static readonly Brush pNavigatorGripBrush = new SolidColorBrush(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF));
    private static readonly Pen pNavigatorBorderPen = new(new SolidColorBrush(Color.FromArgb(0x8C, 0x0D, 0x47, 0xA1)), 1.2);
    private static readonly Pen pNavigatorBodyPen = new(new SolidColorBrush(Color.FromArgb(0x4D, 0x0D, 0x47, 0xA1)), 1.0);
    private static readonly Pen pNavigatorShinePen = new(new SolidColorBrush(Color.FromArgb(0x42, 0xFF, 0xFF, 0xFF)), 1.0);
    private static readonly Pen pNavigatorGripPen = new(pNavigatorGripBrush, 1.6);

    private const double PNavigatorRim = 3.5;
    private static readonly Pen pNavigatorRimPen = new(pNavigatorFrameBrush, PNavigatorRim);
    private static readonly Pen pNavigatorShadowPen = new(pNavigatorShadowBrush, PNavigatorRim);
    private const double PMapHandleWidth = 12;

    private const double PGripWidth = 7;

    private const double PGripMoveRate = 0.5;

    private const double PGripMoveInset = 2.5;

    private const double PMapShadowDrop = 1.5;

    private const double PMapSectionInset = 1;
    private const double PMapBadgeHorizontal = 6;
    private const double PMapBadgeVertical = 1;
    private const double PMapBadgeMargin = 2;

    private static readonly Brush pMapBadgeBrush = new SolidColorBrush(Colors.White);
    private static readonly Typeface pMapBadgeTypeface =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Pen pMapSectionPen = new(new SolidColorBrush(Color.FromRgb(0x1F, 0x27, 0x33)), 1.4);
    private const double PMapRenderLeast = 12;

    static PMap()
    {
        pMapBrushBackground.Freeze();
        pMapBrushRail.Freeze();
        pMapWaveformBrush.Freeze();
        pMapBrushWaveform.Freeze();
        pMapCoverageBrush.Freeze();
        pNavigatorShadowBrush.Freeze();
        pNavigatorFrameBrush.Freeze();
        pNavigatorFillBrush.Freeze();
        pNavigatorBodyBrush.Freeze();
        pNavigatorRimPen.Freeze();
        pNavigatorShadowPen.Freeze();
        pNavigatorSideBrush.Freeze();
        pNavigatorGripBrush.Freeze();
        pMapSectionPen.Freeze();
        pMapBadgeBrush.Freeze();
        pNavigatorBorderPen.Freeze();
        pNavigatorBodyPen.Freeze();
        pNavigatorShinePen.Freeze();
        pNavigatorGripPen.Freeze();
    }

    private LSpool? lSpool;
    private TimeSpan lCursor;
    private string? lSourcePath;
    private IReadOnlyList<LKeyframeScanRange> lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
    private IReadOnlyList<LPiece> lSectionList = Array.Empty<LPiece>();
    private byte[] lWaveformPeaks = Array.Empty<byte>();
    private int? lSectionIndexActive;
    private PMapDragMode pMapDragMode;
    private double pMapDragX;
    private double pMapPreviousX;
    private TimeSpan lMapDragTime;
    private string pMapDrawTrigger = "attach";
    private int pMapGlyphCount;
    private readonly Dictionary<string, FormattedText> pMapBadgeCache = new(StringComparer.Ordinal);
    private double pMapBadgeDpi = -1;

    public event Action<TimeSpan>? PMapCursorChange;
    public event Action? PMapSpoolChange;
    public event Action<bool>? PMapDragChange;

    private void PMapDrawDefer(string pMapTrigger)
    {
        pMapDrawTrigger = pMapTrigger;
        InvalidateVisual();
    }

    public void PMapAttach(LSpool spool, TimeSpan cursor, string? sourcePath)
    {
        lSpool = spool;
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        lSourcePath = sourcePath;
        PMapDrawDefer("attach");
    }

    public void PMapCursorUpdate(TimeSpan cursor)
    {
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        PMapDrawDefer("cursor");
    }

    public void PMapClear()
    {
        lSpool = null;
        lCursor = TimeSpan.Zero;
        lSourcePath = null;
        lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
        lSectionList = Array.Empty<LPiece>();
        lWaveformPeaks = Array.Empty<byte>();
        lSectionIndexActive = null;
        PMapDrawDefer("clear");
    }

    public void PMapWaveformUpdate(byte[] waveformPeaks)
    {
        lWaveformPeaks = waveformPeaks;
        PMapDrawDefer("waveform");
    }

    public void PMapSectionsUpdate(IReadOnlyList<LPiece>? sections, int? selectedIndex)
    {
        lSectionList = sections?.ToArray() ?? Array.Empty<LPiece>();
        lSectionIndexActive = selectedIndex;
        PMapDrawDefer("sections");
    }

    public void PMapSpoolUpdate() => PMapDrawDefer("spool");

    public void PMapKeyframesUpdate(IReadOnlyList<LKeyframeScanRange>? scannedRanges)
    {
        lKeyframeScannedRanges = scannedRanges ?? Array.Empty<LKeyframeScanRange>();
        PMapDrawDefer("keyframes");
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (!LTrace.LTraceVerbose)
        {
            PMapContentDraw(drawingContext);
            return;
        }

        pMapGlyphCount = 0;
        long pMapStamp = System.Diagnostics.Stopwatch.GetTimestamp();
        PMapContentDraw(drawingContext);
        double pMapMilliseconds =
            (System.Diagnostics.Stopwatch.GetTimestamp() - pMapStamp) * 1000d
            / System.Diagnostics.Stopwatch.Frequency;
        LTrace.LTraceTimelineAdd(
            "Map", lCursor, lSourcePath, pMapDrawTrigger, pMapMilliseconds, pMapGlyphCount);
    }

    private void PMapContentDraw(DrawingContext drawingContext)
    {
        double actualWidth = ActualWidth;
        double actualHeight = ActualHeight;
        if (actualWidth <= 0 || actualHeight <= 0)
        {
            return;
        }

        drawingContext.DrawRectangle(pMapBrushBackground, null, new Rect(0, 0, actualWidth, actualHeight));
        if (lSpool is null || lSpool.LSpoolDuration <= TimeSpan.Zero || actualHeight < PMapRenderLeast)
        {
            return;
        }

        double coverageHeight = 3;
        double coverageBottom = Math.Max(0, actualHeight - 1);
        double coverageTop = Math.Max(0, coverageBottom - coverageHeight);
        double railTop = 3;
        double railBottom = Math.Max(railTop, coverageTop - 2);
        double railHeight = Math.Max(0, railBottom - railTop);
        if (railHeight <= 0)
        {
            return;
        }

        bool waveformActive = lWaveformPeaks.Length > 0;
        drawingContext.DrawRoundedRectangle(
            waveformActive ? pMapWaveformBrush : pMapBrushRail,
            null,
            new Rect(0, railTop, actualWidth, railHeight),
            3,
            3);
        if (waveformActive)
        {
            PMapWaveformDraw(drawingContext, actualWidth, railTop, railHeight);
        }

        PMapSectionsDraw(drawingContext, actualWidth, railTop, railHeight);
        PMapCoverageDraw(drawingContext, actualWidth, coverageTop, coverageHeight);

        double startRatio = Math.Clamp(lSpool.LSpoolRatioResolve(lSpool.LSpoolRangeOrigin), 0, 1);
        double endRatio = Math.Clamp(lSpool.LSpoolRatioResolve(lSpool.LSpoolRangeLimit), 0, 1);
        double spoolStartX = Math.Min(startRatio, endRatio) * actualWidth;
        double spoolEndX = Math.Max(startRatio, endRatio) * actualWidth;
        double spoolBodyWidth = Math.Max(0, spoolEndX - spoolStartX);
        if (spoolBodyWidth <= 0)
        {
            return;
        }

        Rect bodyRect = new(spoolStartX, railTop, spoolBodyWidth, railHeight);
        PNavigatorDraw(drawingContext, bodyRect, actualWidth);

        double cursorRatio = Math.Clamp(lSpool.LSpoolRatioResolve(lCursor), 0, 1);
        double cursorX = cursorRatio * actualWidth;
        PCursor.PCursorDraw(drawingContext, cursorX, PCursor.PCursorHeadHeight, actualHeight);
    }

}
