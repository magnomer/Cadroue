using System.Windows;
using System.Windows.Media;

namespace Cadroue.UIShell.PFlow;

internal static class PCursor
{
    internal const double PCursorHeadWidth = 11;
    internal const double PCursorHeadHeight = 14;

    private const double PCursorHeadTip = 5;

    private const double PCursorChipGap = 2;

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
        const double pHalfWidth = PCursorHeadWidth / 2;
        var pGeometry = new StreamGeometry();
        using (StreamGeometryContext pContext = pGeometry.Open())
        {
            pContext.BeginFigure(new Point(0, 0), true, true);
            pContext.LineTo(new Point(pHalfWidth, -PCursorHeadTip), true, false);
            pContext.LineTo(new Point(pHalfWidth, -PCursorHeadHeight), true, false);
            pContext.LineTo(new Point(-pHalfWidth, -PCursorHeadHeight), true, false);
            pContext.LineTo(new Point(-pHalfWidth, -PCursorHeadTip), true, false);
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
        var pGuidelines = new GuidelineSet();
        pGuidelines.GuidelinesX.Add(cursorX - pCursorPen.Thickness / 2);
        pGuidelines.GuidelinesX.Add(cursorX + pCursorPen.Thickness / 2);
        drawingContext.PushGuidelineSet(pGuidelines);

        if (chipRect.IsEmpty || chipRect.Height <= 0)
        {
            PCursorLineDraw(drawingContext, cursorX, lineTop, lineBottom);
        }
        else
        {
            PCursorLineDraw(drawingContext, cursorX, lineTop, chipRect.Top - PCursorChipGap);
            PCursorLineDraw(drawingContext, cursorX, chipRect.Bottom + PCursorChipGap, lineBottom);
        }

        drawingContext.PushTransform(new TranslateTransform(cursorX, lineTop));
        drawingContext.DrawGeometry(pCursorBrush, null, pCursorHeadGeometry);
        drawingContext.Pop();

        drawingContext.Pop();
    }

    private static void PCursorLineDraw(DrawingContext drawingContext, double cursorX, double top, double bottom)
    {
        if (bottom <= top)
        {
            return;
        }

        drawingContext.DrawLine(pCursorPen, new Point(cursorX, top), new Point(cursorX, bottom));
    }

    internal static Rect PCursorChipResolve(
        double cursorX,
        double chipWidth,
        double chipHeight,
        double lineTop,
        double lineBottom,
        double actualWidth)
    {
        double pChipLeft = Math.Clamp(cursorX - chipWidth / 2, 0, Math.Max(0, actualWidth - chipWidth));
        double pChipTop = (lineTop + lineBottom) / 2 - chipHeight / 2;
        return new Rect(pChipLeft, pChipTop, chipWidth, chipHeight);
    }
}
