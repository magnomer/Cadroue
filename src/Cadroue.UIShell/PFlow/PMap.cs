using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Media;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PMap : FrameworkElement
{
    private enum PMapDragMode
    {
        PMapDragNone,
        PMapDragResizeStart,
        PMapDragResizeEnd,
        PMapDragMove,
        PMapDragCursor,
    }

    private static readonly Brush pMapBrushBackground = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    private static readonly Brush pMapBrushRail = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1));
    private static readonly Brush pMapBrushCoverageScanned = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Brush pMapBrushNavigationShadow = new SolidColorBrush(Color.FromArgb(0x34, 0x00, 0x00, 0x00));
    private const byte PMapNavigationAlpha = 0x33;

    private static readonly Brush pMapBrushNavigationFrame = new SolidColorBrush(Color.FromArgb(PMapNavigationAlpha, 0x2D, 0x7D, 0xD2));
    private static readonly Brush pMapBrushNavigationFill = new SolidColorBrush(Color.FromArgb(PMapNavigationAlpha, 0x2D, 0x7D, 0xD2));
    private static readonly Brush pMapBrushNavigationMove = new SolidColorBrush(Color.FromArgb(PMapNavigationAlpha, 0x3A, 0x8B, 0xE0));
    private static readonly Brush pMapBrushNavigationSide = new SolidColorBrush(Color.FromArgb(PMapNavigationAlpha, 0x2D, 0x7D, 0xD2));
    private static readonly Brush pMapBrushNavigationGrip = new SolidColorBrush(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF));
    private static readonly Pen pMapPenNavigationBorder = new(new SolidColorBrush(Color.FromArgb(0x8C, 0x0D, 0x47, 0xA1)), 1.2);
    private static readonly Pen pMapPenNavigationMoveBorder = new(new SolidColorBrush(Color.FromArgb(0x4D, 0x0D, 0x47, 0xA1)), 1.0);
    private static readonly Pen pMapPenNavigationShine = new(new SolidColorBrush(Color.FromArgb(0x42, 0xFF, 0xFF, 0xFF)), 1.0);
    private static readonly Pen pMapPenNavigationGrip = new(pMapBrushNavigationGrip, 1.6);

    private const double PMapNavigationRim = 3.5;
    private static readonly Pen pMapPenNavigationRim = new(pMapBrushNavigationFrame, PMapNavigationRim);
    private static readonly Pen pMapPenNavigationRimShadow = new(pMapBrushNavigationShadow, PMapNavigationRim);
    private const double PMapHandleWidth = 12;

    private const double PGripWidth = 7;

    private const double PGripMoveRate = 0.5;

    private const double PGripMoveInset = 2.5;

    private const double PMapShadowDrop = 1.5;

    private const double PMapSectionInset = 1;
    private const double PMapBadgePaddingHorizontal = 6;
    private const double PMapBadgePaddingVertical = 1;
    private const double PMapBadgeMargin = 2;

    private static readonly Brush pMapBrushBadgeText = new SolidColorBrush(Colors.White);
    private static readonly Typeface pMapBadgeTypeface =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Pen pMapPenSectionSelect = new(new SolidColorBrush(Color.FromRgb(0x1F, 0x27, 0x33)), 1.4);
    private const double PMapMinimumRenderHeight = 12;

    static PMap()
    {
        pMapBrushBackground.Freeze();
        pMapBrushRail.Freeze();
        pMapBrushCoverageScanned.Freeze();
        pMapBrushNavigationShadow.Freeze();
        pMapBrushNavigationFrame.Freeze();
        pMapBrushNavigationFill.Freeze();
        pMapBrushNavigationMove.Freeze();
        pMapPenNavigationRim.Freeze();
        pMapPenNavigationRimShadow.Freeze();
        pMapBrushNavigationSide.Freeze();
        pMapBrushNavigationGrip.Freeze();
        pMapPenSectionSelect.Freeze();
        pMapBrushBadgeText.Freeze();
        pMapPenNavigationBorder.Freeze();
        pMapPenNavigationMoveBorder.Freeze();
        pMapPenNavigationShine.Freeze();
        pMapPenNavigationGrip.Freeze();
    }

    private LSpool? lSpool;
    private TimeSpan lCursor;
    private IReadOnlyList<LKeyframeScanRange> lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
    private IReadOnlyList<LSegment> lSectionList = Array.Empty<LSegment>();
    private int? lSectionIndexSelect;
    private PMapDragMode pMapDragMode;
    private double pMapDragStartX;
    private double pMapDragPreviousX;
    private TimeSpan lMapDragStartTime;
    private string pMapDrawTrigger = "attach";
    private int pMapGlyphCount;

    public event Action<TimeSpan>? PMapCursorChange;
    public event Action? PMapSpoolChange;
    public event Action<bool>? PMapDragChange;

    private void PMapDrawRequest(string pMapTrigger)
    {
        pMapDrawTrigger = pMapTrigger;
        InvalidateVisual();
    }

    public void PMapAttach(LSpool spool, TimeSpan cursor)
    {
        lSpool = spool;
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        PMapDrawRequest("attach");
    }

    public void PMapCursorUpdate(TimeSpan cursor)
    {
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        PMapDrawRequest("cursor");
    }

    public void PMapClear()
    {
        lSpool = null;
        lCursor = TimeSpan.Zero;
        lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
        lSectionList = Array.Empty<LSegment>();
        lSectionIndexSelect = null;
        PMapDrawRequest("clear");
    }

    public void PMapSectionsUpdate(IReadOnlyList<LSegment>? sections, int? selectedIndex)
    {
        lSectionList = sections?.ToArray() ?? Array.Empty<LSegment>();
        lSectionIndexSelect = selectedIndex;
        PMapDrawRequest("sections");
    }

    public void PMapSpoolUpdate() => PMapDrawRequest("spool");

    public void PMapKeyframesUpdate(IReadOnlyList<LKeyframeScanRange>? scannedRanges)
    {
        lKeyframeScannedRanges = scannedRanges ?? Array.Empty<LKeyframeScanRange>();
        PMapDrawRequest("keyframes");
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
        LTrace.LTraceDrawAdd("PMap", pMapDrawTrigger, pMapMilliseconds, pMapGlyphCount);
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
        if (lSpool is null || lSpool.LSpoolDuration <= TimeSpan.Zero || actualHeight < PMapMinimumRenderHeight)
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

        drawingContext.DrawRoundedRectangle(pMapBrushRail, null, new Rect(0, railTop, actualWidth, railHeight), 3, 3);

        PMapSectionsDraw(drawingContext, actualWidth, railTop, railHeight);
        PMapCoverageDraw(drawingContext, actualWidth, coverageTop, coverageHeight);

        double startRatio = Math.Clamp(lSpool.LSpoolRatioConvert(lSpool.LSpoolWorkingRangeStart), 0, 1);
        double endRatio = Math.Clamp(lSpool.LSpoolRatioConvert(lSpool.LSpoolWorkingRangeEnd), 0, 1);
        double spoolStartX = Math.Min(startRatio, endRatio) * actualWidth;
        double spoolEndX = Math.Max(startRatio, endRatio) * actualWidth;
        double spoolBodyWidth = Math.Max(0, spoolEndX - spoolStartX);
        if (spoolBodyWidth <= 0)
        {
            return;
        }

        Rect bodyRect = new(spoolStartX, railTop, spoolBodyWidth, railHeight);
        PMapNavigationDraw(drawingContext, bodyRect, actualWidth);

        double cursorRatio = Math.Clamp(lSpool.LSpoolRatioConvert(lCursor), 0, 1);
        double cursorX = cursorRatio * actualWidth;
        PCursor.PCursorDraw(drawingContext, cursorX, PCursor.PCursorHeadHeight, actualHeight);
    }

}
