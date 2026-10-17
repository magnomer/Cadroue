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

    private static readonly IReadOnlyDictionary<QMapHitKind, Cursor> pMapCursors = new Dictionary<QMapHitKind, Cursor>
    {
        [QMapHitKind.QMapHitNone] = Cursors.Arrow,
        [QMapHitKind.QMapHitOrigin] = Cursors.SizeWE,
        [QMapHitKind.QMapHitLimit] = Cursors.SizeWE,
        [QMapHitKind.QMapHitBody] = Cursors.SizeAll,
        [QMapHitKind.QMapHitCursor] = Cursors.Hand,
    };

    private static readonly IReadOnlyDictionary<QMapShapeKind, Action<DrawingContext, QMapShape>> pMapDraws =
        new Dictionary<QMapShapeKind, Action<DrawingContext, QMapShape>>
        {
            [QMapShapeKind.QMapShapeBackground] = PMapBackgroundDraw,
            [QMapShapeKind.QMapShapeRail] = PMapRailDraw,
            [QMapShapeKind.QMapShapeWaveform] = PMapWaveformDraw,
            [QMapShapeKind.QMapShapeCoverage] = PMapCoverageDraw,
            [QMapShapeKind.QMapShapeScanned] = PMapScanDraw,
            [QMapShapeKind.QMapShapeFill] = PNavigatorFillDraw,
            [QMapShapeKind.QMapShapeShadow] = PNavigatorShadowDraw,
            [QMapShapeKind.QMapShapeRim] = PNavigatorRimDraw,
            [QMapShapeKind.QMapShapeSide] = PNavigatorSideDraw,
            [QMapShapeKind.QMapShapeGrip] = PGripSideDraw,
            [QMapShapeKind.QMapShapeBody] = PGripBodyDraw,
            [QMapShapeKind.QMapShapeDot] = PGripDotDraw,
            [QMapShapeKind.QMapShapeShine] = PNavigatorShineDraw,
            [QMapShapeKind.QMapShapeBorder] = PNavigatorBorderDraw,
        };

    public QMap QMap { get; }

    public PMap(QMap lMap)
    {
        QMap = lMap;
        QMap.QMapFrameApplied += InvalidateVisual;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        QMap.QMapDrawStart();
        QMapFrame lFrame = QMap.QMapFrameResolve(ActualWidth, ActualHeight);
        PMapFrameDraw(drawingContext, lFrame);
        QMap.QMapDrawRecord();
    }

    private void PMapFrameDraw(DrawingContext drawingContext, QMapFrame lFrame)
    {
        lFrame.QMapFrameUnder.ToList().ForEach(lShape => pMapDraws[lShape.QMapShapeKind](drawingContext, lShape));
        drawingContext.DrawGeometry(pMapBrushWaveform, null, PFlow.PFlowWaveformBuild(lFrame.QMapFrameWaveform));
        lFrame.QMapFrameBands.ToList().ForEach(lBand => PMapBandDraw(drawingContext, lBand));
        lFrame.QMapFrameOver.ToList().ForEach(lShape => pMapDraws[lShape.QMapShapeKind](drawingContext, lShape));
        lFrame.QMapFrameCursor.ToList().ForEach(lCursorX =>
            PCursor.PCursorDraw(drawingContext, lCursorX, PCursor.PCursorHeadHeight, ActualHeight));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Point pPoint = e.GetPosition(this);
        e.Handled = QMap.QMapPressHandle(pPoint.X, pPoint.Y, ActualWidth, ActualHeight);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point pPoint = e.GetPosition(this);
        PMapCursorApply(QMap.QMapHoverResolve(pPoint.X, pPoint.Y, ActualWidth, ActualHeight));
        e.Handled = QMap.QMapMoveHandle(pPoint.X, ActualWidth);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        PMapCursorApply(QMap.QMapLeaveResolve());
    }

    private void PMapCursorApply(QMapHitKind lHit) => Cursor = pMapCursors[lHit];

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
        QMap.QMapDragClear();
        ReleaseMouseCapture();
    }

    private void PMapBandDraw(DrawingContext drawingContext, QMapBand lBand)
    {
        var pRect = new Rect(
            new Point(lBand.QMapBandLeft, lBand.QMapBandTop),
            new Point(lBand.QMapBandRight, lBand.QMapBandBottom));
        FormattedText pBadgeText = PMapTextBuild(lBand.QMapBandBadge);
        drawingContext.PushOpacity(PLook.PLookOpacity[lBand.QMapBandShown]);
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBandRead(lBand.QMapBandColor),
            pMapSelectedPens[lBand.QMapBandSelected],
            pRect,
            3,
            3);
        QMapText.QMapBadgeResolve(lBand, pBadgeText.Width, pBadgeText.Height)
            .ToList()
            .ForEach(lBadge => PMapBadgeDraw(drawingContext, lBand, lBadge, pBadgeText));
        drawingContext.Pop();
    }

    private static void PMapBadgeDraw(
        DrawingContext drawingContext, QMapBand lBand, QMapBadge lBadge, FormattedText pBadgeText)
    {
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBadgeRead(lBand.QMapBandColor),
            null,
            new Rect(
                new Point(lBadge.QMapBadgeLeft, lBadge.QMapBadgeTop),
                new Point(lBadge.QMapBadgeRight, lBadge.QMapBadgeBottom)),
            lBadge.QMapBadgeRadius,
            lBadge.QMapBadgeRadius);
        drawingContext.DrawText(pBadgeText, new Point(lBadge.QMapBadgeX, lBadge.QMapBadgeY));
    }

    private FormattedText PMapTextBuild(string pText) => new(
        pText,
        System.Globalization.CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        pMapBadgeTypeface,
        PSection.PSectionNameSize,
        pMapBadgeBrush,
        VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private static Rect PMapRectRead(QMapShape lShape) => new(
        new Point(lShape.QMapShapeLeft, lShape.QMapShapeTop),
        new Point(lShape.QMapShapeRight, lShape.QMapShapeBottom));

    private static void PMapBackgroundDraw(DrawingContext drawingContext, QMapShape lShape) =>
        drawingContext.DrawRectangle(pMapBrushBackground, null, PMapRectRead(lShape));

    private static void PMapRailDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pMapBrushRail, null, lShape);

    private static void PMapWaveformDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pMapWaveformBrush, null, lShape);

    private static void PMapCoverageDraw(DrawingContext drawingContext, QMapShape lShape) =>
        drawingContext.DrawRectangle(pMapBrushRail, null, PMapRectRead(lShape));

    private static void PMapScanDraw(DrawingContext drawingContext, QMapShape lShape) =>
        drawingContext.DrawRectangle(pMapCoverageBrush, null, PMapRectRead(lShape));

    private static void PNavigatorFillDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pNavigatorFrameBrush, null, lShape);

    private static void PNavigatorShadowDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapRoundedDraw(drawingContext, null, pNavigatorShadowPen, lShape);

    private static void PNavigatorRimDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapRoundedDraw(drawingContext, null, pNavigatorRimPen, lShape);

    private static void PNavigatorSideDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pNavigatorFrameBrush, null, lShape);

    private static void PGripSideDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapLineDraw(drawingContext, pNavigatorGripPen, lShape);

    private static void PGripBodyDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapRoundedDraw(drawingContext, pNavigatorBodyBrush, pNavigatorBodyPen, lShape);

    private static void PGripDotDraw(DrawingContext drawingContext, QMapShape lShape) =>
        drawingContext.DrawEllipse(
            pNavigatorGripBrush,
            null,
            new Point(lShape.QMapShapeLeft, lShape.QMapShapeTop),
            lShape.QMapShapeRadius,
            lShape.QMapShapeRadius);

    private static void PNavigatorShineDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapLineDraw(drawingContext, pNavigatorShinePen, lShape);

    private static void PNavigatorBorderDraw(DrawingContext drawingContext, QMapShape lShape) =>
        PMapRoundedDraw(drawingContext, null, pNavigatorBorderPen, lShape);

    private static void PMapRoundedDraw(DrawingContext drawingContext, Brush? pBrush, Pen? pPen, QMapShape lShape) =>
        drawingContext.DrawRoundedRectangle(
            pBrush, pPen, PMapRectRead(lShape), lShape.QMapShapeRadius, lShape.QMapShapeRadius);

    private static void PMapLineDraw(DrawingContext drawingContext, Pen pPen, QMapShape lShape) =>
        drawingContext.DrawLine(
            pPen,
            new Point(lShape.QMapShapeLeft, lShape.QMapShapeTop),
            new Point(lShape.QMapShapeRight, lShape.QMapShapeBottom));

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
