using System.Windows.Media;

namespace Cadroue.UIDeportment;

// An icon drawing's tint: no brush keeps the drawing; a brush repaints every visible fill, stroke and glyph of a frozen
// copy and leaves the clear ones clear.
internal static class QIconTint
{
    private static readonly IReadOnlyDictionary<bool, Func<DrawingGroup, Brush?, DrawingGroup>> qIconTints =
        new Dictionary<bool, Func<DrawingGroup, Brush?, DrawingGroup>>
        {
            [true] = QIconDrawingRead,
            [false] = QIconTintCreate,
        };

    private static readonly IReadOnlyDictionary<Type, Action<Drawing, Brush>> qIconPainters =
        new Dictionary<Type, Action<Drawing, Brush>>
        {
            [typeof(DrawingGroup)] = QIconGroupApply,
            [typeof(GeometryDrawing)] = QIconGeometryApply,
            [typeof(GlyphRunDrawing)] = QIconGlyphApply,
        };

    public static DrawingGroup QIconTintApply(DrawingGroup qIconDrawing, Brush? qIconTintBrush) =>
        qIconTints[ReferenceEquals(qIconTintBrush, null)](qIconDrawing, qIconTintBrush);

    private static DrawingGroup QIconDrawingRead(DrawingGroup qIconDrawing, Brush? qIconTintBrush) => qIconDrawing;

    private static DrawingGroup QIconTintCreate(DrawingGroup qIconDrawing, Brush? qIconTintBrush)
    {
        var qIconGroup = qIconDrawing.Clone();
        var qIconBrush = qIconTintBrush!.Clone();
        qIconBrush.Freeze();
        QIconDrawingApply(qIconGroup, qIconBrush);
        qIconGroup.Freeze();
        return qIconGroup;
    }

    private static void QIconDrawingApply(Drawing qIconDrawing, Brush qIconTintBrush) =>
        qIconPainters.GetValueOrDefault(qIconDrawing.GetType(), QIconOtherApply)(qIconDrawing, qIconTintBrush);

    private static void QIconOtherApply(Drawing qIconDrawing, Brush qIconTintBrush)
    {
    }

    private static void QIconGroupApply(Drawing qIconDrawing, Brush qIconTintBrush)
    {
        var qIconGroup = (DrawingGroup)qIconDrawing;
        qIconGroup.Children.ToList().ForEach(qIconChild => QIconDrawingApply(qIconChild, qIconTintBrush));
    }

    private static void QIconGeometryApply(Drawing qIconDrawing, Brush qIconTintBrush)
    {
        var qIconGeometry = (GeometryDrawing)qIconDrawing;
        qIconGeometry.Brush = QIconBrushResolve(qIconGeometry.Brush, qIconTintBrush);
        qIconGeometry.Pen = QIconPenResolve(qIconGeometry.Pen, qIconTintBrush);
    }

    private static void QIconGlyphApply(Drawing qIconDrawing, Brush qIconTintBrush) =>
        ((GlyphRunDrawing)qIconDrawing).ForegroundBrush = qIconTintBrush;

    private static Brush? QIconBrushResolve(Brush? qIconBrush, Brush qIconTintBrush) =>
        QIconBrushCheck(qIconBrush) ? qIconTintBrush : qIconBrush;

    private static Pen? QIconPenResolve(Pen? qIconPen, Brush qIconTintBrush)
    {
        if (qIconPen is null || !QIconBrushCheck(qIconPen.Brush))
        {
            return qIconPen;
        }

        Pen qIconTintedPen = qIconPen.Clone();
        qIconTintedPen.Brush = qIconTintBrush;
        qIconTintedPen.Freeze();
        return qIconTintedPen;
    }

    private static bool QIconBrushCheck(Brush? qIconBrush)
    {
        if (qIconBrush is null || qIconBrush.Opacity <= 0)
        {
            return false;
        }

        return qIconBrush switch
        {
            SolidColorBrush qIconSolid => qIconSolid.Color.A > 0,
            GradientBrush qIconGradient => qIconGradient.GradientStops.Any(qIconStep => qIconStep.Color.A > 0),
            _ => true
        };
    }
}
