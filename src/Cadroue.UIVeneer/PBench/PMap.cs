using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PBench;

public sealed class PMap : FrameworkElement
{
    private const byte PNavigatorAlpha = 0x33;
    private const double PNavigatorRim = 3.5;

    private static readonly Brush pMapBrushBackground = PMapBrushBuild(0xFF, 0xF3, 0xF3, 0xF3);
    private static readonly Brush pMapBrushRail = PMapBrushBuild(0xFF, 0xD1, 0xD1, 0xD1);
    private static readonly Brush pMapWaveformBrush = PMapBrushBuild(0xFF, 0xE6, 0xEA, 0xEF);
    private static readonly Brush pMapBrushWaveform = PMapBrushBuild(0xFF, 0x8C, 0x9B, 0xAD);
    private static readonly Brush pMapCoverageBrush = PMapBrushBuild(0xFF, 0x2F, 0x9E, 0x64);
    private static readonly Brush pNavigatorFrameBrush = PMapBrushBuild(PNavigatorAlpha, 0x2D, 0x7D, 0xD2);
    private static readonly Brush pNavigatorBodyBrush = PMapBrushBuild(PNavigatorAlpha, 0x3A, 0x8B, 0xE0);
    private static readonly Brush pNavigatorGripBrush = PMapBrushBuild(0xE0, 0xFF, 0xFF, 0xFF);
    private static readonly Brush pMapBadgeBrush = PMapBrushBuild(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Pen pNavigatorBorderPen = PMapPenBuild(PMapBrushBuild(0x8C, 0x0D, 0x47, 0xA1), 1.2);
    private static readonly Pen pNavigatorBodyPen = PMapPenBuild(PMapBrushBuild(0x4D, 0x0D, 0x47, 0xA1), 1.0);
    private static readonly Pen pNavigatorShinePen = PMapPenBuild(PMapBrushBuild(0x42, 0xFF, 0xFF, 0xFF), 1.0);
    private static readonly Pen pNavigatorGripPen = PMapPenBuild(pNavigatorGripBrush, 1.6);
    private static readonly Pen pNavigatorRimPen = PMapPenBuild(pNavigatorFrameBrush, PNavigatorRim);
    private static readonly Pen pNavigatorShadowPen =
        PMapPenBuild(PMapBrushBuild(0x34, 0x00, 0x00, 0x00), PNavigatorRim);
    private static readonly Pen pMapSectionPen = PMapPenBuild(PMapBrushBuild(0xFF, 0x1F, 0x27, 0x33), 1.4);
    private static readonly Typeface pMapBadgeTypeface =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    private static readonly IReadOnlyDictionary<bool, Pen?> pMapSelectedPens = new Dictionary<bool, Pen?>
    {
        [true] = pMapSectionPen,
        [false] = null,
    };

    private static readonly IReadOnlyDictionary<LMapHitKind, Cursor> pMapCursors = new Dictionary<LMapHitKind, Cursor>
    {
        [LMapHitKind.LMapHitNone] = Cursors.Arrow,
        [LMapHitKind.LMapHitOrigin] = Cursors.SizeWE,
        [LMapHitKind.LMapHitLimit] = Cursors.SizeWE,
        [LMapHitKind.LMapHitBody] = Cursors.SizeAll,
        [LMapHitKind.LMapHitCursor] = Cursors.Hand,
    };

    private static readonly IReadOnlyDictionary<LMapShapeKind, Action<DrawingContext, LMapShape>> pMapDraws =
        new Dictionary<LMapShapeKind, Action<DrawingContext, LMapShape>>
        {
            [LMapShapeKind.LMapShapeBackground] = PMapBackgroundDraw,
            [LMapShapeKind.LMapShapeRail] = PMapRailDraw,
            [LMapShapeKind.LMapShapeWaveform] = PMapWaveformDraw,
            [LMapShapeKind.LMapShapeCoverage] = PMapCoverageDraw,
            [LMapShapeKind.LMapShapeScanned] = PMapScanDraw,
            [LMapShapeKind.LMapShapeFill] = PNavigatorFillDraw,
            [LMapShapeKind.LMapShapeShadow] = PNavigatorShadowDraw,
            [LMapShapeKind.LMapShapeRim] = PNavigatorRimDraw,
            [LMapShapeKind.LMapShapeSide] = PNavigatorSideDraw,
            [LMapShapeKind.LMapShapeGrip] = PGripSideDraw,
            [LMapShapeKind.LMapShapeBody] = PGripBodyDraw,
            [LMapShapeKind.LMapShapeDot] = PGripDotDraw,
            [LMapShapeKind.LMapShapeShine] = PNavigatorShineDraw,
            [LMapShapeKind.LMapShapeBorder] = PNavigatorBorderDraw,
        };

    public LMap LMap { get; }

    public PMap(LFlow lFlow)
    {
        LMap = new LMap(lFlow);
        LMap.LMapFrameApply += InvalidateVisual;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        long pStamp = LMap.LMapDrawStart();
        LMapFrame lFrame = LMap.LMapFrameResolve(ActualWidth, ActualHeight);
        lFrame.LMapFrameUnder.ToList().ForEach(lShape => pMapDraws[lShape.LMapShapeKind](drawingContext, lShape));
        drawingContext.DrawGeometry(pMapBrushWaveform, null, PFlow.PFlowWaveformBuild(lFrame.LMapFrameWaveform));
        lFrame.LMapFrameBands.ToList().ForEach(lBand => PMapBandDraw(drawingContext, lBand));
        lFrame.LMapFrameOver.ToList().ForEach(lShape => pMapDraws[lShape.LMapShapeKind](drawingContext, lShape));
        lFrame.LMapFrameCursor.ToList().ForEach(lCursorX =>
            PCursor.PCursorDraw(drawingContext, lCursorX, PCursor.PCursorHeadHeight, ActualHeight));
        LMap.LMapDrawRecord(pStamp);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Point pPoint = e.GetPosition(this);
        e.Handled = LMap.LMapPressHandle(pPoint.X, pPoint.Y, ActualWidth, ActualHeight);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point pPoint = e.GetPosition(this);
        Cursor = pMapCursors[LMap.LMapHoverResolve(pPoint.X, pPoint.Y, ActualWidth, ActualHeight)];
        e.Handled = LMap.LMapMoveHandle(pPoint.X, ActualWidth);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        Cursor = pMapCursors[LMap.LMapLeaveResolve()];
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        PMapDragClear();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        PMapDragClear();
    }

    private void PMapDragClear()
    {
        LMap.LMapDragClear();
        ReleaseMouseCapture();
    }

    private void PMapBandDraw(DrawingContext drawingContext, LMapBand lBand)
    {
        var pRect = new Rect(
            new Point(lBand.LMapBandLeft, lBand.LMapBandTop),
            new Point(lBand.LMapBandRight, lBand.LMapBandBottom));
        FormattedText pBadgeText = PMapTextBuild(lBand.LMapBandBadge);
        drawingContext.PushOpacity(PLook.PLookOpacity[lBand.LMapBandShown]);
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBandRead(lBand.LMapBandColor),
            pMapSelectedPens[lBand.LMapBandSelected],
            pRect,
            3,
            3);
        LMap.LMapBadgeResolve(lBand, pBadgeText.Width, pBadgeText.Height)
            .ToList()
            .ForEach(lBadge => PMapBadgeDraw(drawingContext, lBand, lBadge, pBadgeText));
        drawingContext.Pop();
    }

    private static void PMapBadgeDraw(
        DrawingContext drawingContext, LMapBand lBand, LMapBadge lBadge, FormattedText pBadgeText)
    {
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBadgeRead(lBand.LMapBandColor),
            null,
            new Rect(
                new Point(lBadge.LMapBadgeLeft, lBadge.LMapBadgeTop),
                new Point(lBadge.LMapBadgeRight, lBadge.LMapBadgeBottom)),
            lBadge.LMapBadgeRadius,
            lBadge.LMapBadgeRadius);
        drawingContext.DrawText(pBadgeText, new Point(lBadge.LMapBadgeX, lBadge.LMapBadgeY));
    }

    private FormattedText PMapTextBuild(string pText) => new(
        pText,
        System.Globalization.CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        pMapBadgeTypeface,
        PSection.PSectionNameSize,
        pMapBadgeBrush,
        VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private static Rect PMapRectRead(LMapShape lShape) => new(
        new Point(lShape.LMapShapeLeft, lShape.LMapShapeTop),
        new Point(lShape.LMapShapeRight, lShape.LMapShapeBottom));

    private static void PMapBackgroundDraw(DrawingContext drawingContext, LMapShape lShape) =>
        drawingContext.DrawRectangle(pMapBrushBackground, null, PMapRectRead(lShape));

    private static void PMapRailDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pMapBrushRail, null, lShape);

    private static void PMapWaveformDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pMapWaveformBrush, null, lShape);

    private static void PMapCoverageDraw(DrawingContext drawingContext, LMapShape lShape) =>
        drawingContext.DrawRectangle(pMapBrushRail, null, PMapRectRead(lShape));

    private static void PMapScanDraw(DrawingContext drawingContext, LMapShape lShape) =>
        drawingContext.DrawRectangle(pMapCoverageBrush, null, PMapRectRead(lShape));

    private static void PNavigatorFillDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pNavigatorFrameBrush, null, lShape);

    private static void PNavigatorShadowDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapRoundedDraw(drawingContext, null, pNavigatorShadowPen, lShape);

    private static void PNavigatorRimDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapRoundedDraw(drawingContext, null, pNavigatorRimPen, lShape);

    private static void PNavigatorSideDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pNavigatorFrameBrush, null, lShape);

    private static void PGripSideDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapLineDraw(drawingContext, pNavigatorGripPen, lShape);

    private static void PGripBodyDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pNavigatorBodyBrush, pNavigatorBodyPen, lShape);

    private static void PGripDotDraw(DrawingContext drawingContext, LMapShape lShape) =>
        drawingContext.DrawEllipse(
            pNavigatorGripBrush,
            null,
            new Point(lShape.LMapShapeLeft, lShape.LMapShapeTop),
            lShape.LMapShapeRadius,
            lShape.LMapShapeRadius);

    private static void PNavigatorShineDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapLineDraw(drawingContext, pNavigatorShinePen, lShape);

    private static void PNavigatorBorderDraw(DrawingContext drawingContext, LMapShape lShape) =>
        PMapRoundedDraw(drawingContext, null, pNavigatorBorderPen, lShape);

    private static void PMapRoundedDraw(DrawingContext drawingContext, Brush? pBrush, Pen? pPen, LMapShape lShape) =>
        drawingContext.DrawRoundedRectangle(
            pBrush, pPen, PMapRectRead(lShape), lShape.LMapShapeRadius, lShape.LMapShapeRadius);

    private static void PMapLineDraw(DrawingContext drawingContext, Pen pPen, LMapShape lShape) =>
        drawingContext.DrawLine(
            pPen,
            new Point(lShape.LMapShapeLeft, lShape.LMapShapeTop),
            new Point(lShape.LMapShapeRight, lShape.LMapShapeBottom));

    private static Brush PMapBrushBuild(byte pAlpha, byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromArgb(pAlpha, pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    private static Pen PMapPenBuild(Brush pBrush, double pThickness)
    {
        var pPen = new Pen(pBrush, pThickness);
        pPen.Freeze();
        return pPen;
    }
}
