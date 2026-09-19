using System.Windows;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PBench;

internal static class PCursor
{
    internal const double PCursorHeadWidth = 11;
    internal const double PCursorHeadHeight = 14;

    private const double PCursorHeadTip = 5;
    private const double PCursorHeadHalf = PCursorHeadWidth / 2;

    private static readonly Brush pCursorBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x27, 0x33));
    private static readonly Pen pCursorPen = new(pCursorBrush, 1.0);

    private static readonly StreamGeometry pCursorHeadGeometry = PCursorHeadCreate();

    static PCursor()
    {
        pCursorBrush.Freeze();
        pCursorPen.Freeze();
    }

    private static StreamGeometry PCursorHeadCreate()
    {
        var pGeometry = new StreamGeometry();
        using (StreamGeometryContext pContext = pGeometry.Open())
        {
            pContext.BeginFigure(new Point(0, 0), true, true);
            pContext.LineTo(new Point(PCursorHeadHalf, -PCursorHeadTip), true, false);
            pContext.LineTo(new Point(PCursorHeadHalf, -PCursorHeadHeight), true, false);
            pContext.LineTo(new Point(-PCursorHeadHalf, -PCursorHeadHeight), true, false);
            pContext.LineTo(new Point(-PCursorHeadHalf, -PCursorHeadTip), true, false);
        }

        pGeometry.Freeze();
        return pGeometry;
    }

    internal static void PCursorDraw(DrawingContext drawingContext, double cursorX, double lineTop, double lineBottom)
        => PCursorDraw(drawingContext, cursorX, lineTop, lineBottom, Rect.Empty);

    internal static void PCursorDraw(
        DrawingContext drawingContext,
        double cursorX,
        double lineTop,
        double lineBottom,
        Rect chipRect)
    {
        (double pGuideLeft, double pGuideRight) = LCursor.LCursorGuideResolve(cursorX, pCursorPen.Thickness);
        var pGuidelines = new GuidelineSet();
        pGuidelines.GuidelinesX.Add(pGuideLeft);
        pGuidelines.GuidelinesX.Add(pGuideRight);
        drawingContext.PushGuidelineSet(pGuidelines);

        LCursor
            .LCursorLinesResolve(lineTop, lineBottom, chipRect.IsEmpty, chipRect.Top, chipRect.Bottom, chipRect.Height)
            .ToList()
            .ForEach(pLine => PCursorLineDraw(drawingContext, cursorX, pLine));

        drawingContext.PushTransform(new TranslateTransform(cursorX, lineTop));
        drawingContext.DrawGeometry(pCursorBrush, null, pCursorHeadGeometry);
        drawingContext.Pop();

        drawingContext.Pop();
    }

    private static void PCursorLineDraw(DrawingContext drawingContext, double cursorX, LCursorLine pLine) =>
        drawingContext.DrawLine(
            pCursorPen,
            new Point(cursorX, pLine.LCursorLineTop),
            new Point(cursorX, pLine.LCursorLineBottom));

    internal static Rect PCursorChipResolve(
        double cursorX,
        double chipWidth,
        double chipHeight,
        double lineTop,
        double lineBottom,
        double actualWidth)
    {
        (double pChipLeft, double pChipTop) = LCursor.LCursorChipResolve(
            cursorX, chipWidth, chipHeight, lineTop, lineBottom, actualWidth);
        return new Rect(pChipLeft, pChipTop, chipWidth, chipHeight);
    }
}
