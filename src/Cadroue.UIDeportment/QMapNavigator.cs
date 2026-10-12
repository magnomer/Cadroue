namespace Cadroue.UIDeportment;

public static class QMapNavigator
{
    private const double QMapGripWidth = 7;
    private const double QMapGripRate = 0.5;
    private const double QMapGripInset = 2.5;
    private const double QMapShadowDrop = 1.5;
    private const double QMapNavigatorRim = 3.5;
    private const double QMapBorderThickness = 1.2;

    internal static void QMapNavigatorAdd(
        List<QMapShape> lOver, double lBodyLeft, double lBodyWidth, double lBodyHeight, double lWidth)
    {
        double lPaintHeight = Math.Max(0, lBodyHeight - QMapShadowDrop);
        if (lPaintHeight <= 0)
        {
            return;
        }

        double lTop = QMap.QMapRailTop;
        double lBottom = lTop + lPaintHeight;
        double lRight = lBodyLeft + lBodyWidth;
        double lRadius = Math.Clamp(lPaintHeight / 2, 4, 9);
        double lSideWidth = Math.Min(QMapGripWidth, lBodyWidth);
        double lGrabWidth = Math.Min(QMap.QMapHandleWidth, lBodyWidth);
        lOver.Add(new QMapShape(QMapShapeKind.QMapShapeFill, lBodyLeft, lTop, lRight, lBottom, lRadius));

        double lRimInset = QMapNavigatorRim / 2;
        if (lBodyWidth - QMapNavigatorRim > 0 && lPaintHeight - QMapNavigatorRim > 0)
        {
            double lRimRadius = Math.Max(0, lRadius - lRimInset);
            lOver.Add(new QMapShape(
                QMapShapeKind.QMapShapeShadow,
                lBodyLeft + lRimInset,
                lTop + lRimInset + QMapShadowDrop,
                lRight - lRimInset,
                lBottom - lRimInset + QMapShadowDrop,
                lRimRadius));
            lOver.Add(new QMapShape(
                QMapShapeKind.QMapShapeRim,
                lBodyLeft + lRimInset,
                lTop + lRimInset,
                lRight - lRimInset,
                lBottom - lRimInset,
                lRimRadius));
        }

        lOver.Add(new QMapShape(
            QMapShapeKind.QMapShapeSide, lBodyLeft, lTop, lBodyLeft + lSideWidth, lBottom, lRadius));
        lOver.Add(new QMapShape(QMapShapeKind.QMapShapeSide, lRight - lSideWidth, lTop, lRight, lBottom, lRadius));
        double lGripInset = Math.Max(3, lPaintHeight * 0.25);
        lOver.Add(new QMapShape(
            QMapShapeKind.QMapShapeGrip,
            lBodyLeft + lSideWidth / 2, lTop + lGripInset, lBodyLeft + lSideWidth / 2, lBottom - lGripInset, 0));
        lOver.Add(new QMapShape(
            QMapShapeKind.QMapShapeGrip,
            lRight - lSideWidth / 2, lTop + lGripInset, lRight - lSideWidth / 2, lBottom - lGripInset, 0));

        (double lMoveLeft, double lMoveWidth, double lMoveHeight) =
            LMapMoveResolve(lBodyLeft, lBodyWidth, lBodyHeight, lGrabWidth, lWidth);
        double lGripWidth = Math.Min(lMoveWidth, lMoveWidth * QMapGripRate);
        double lGripHeight = Math.Min(lMoveHeight, lPaintHeight) - QMapGripInset * 2;
        if (lMoveWidth > 0 && lMoveHeight > 0 && lGripWidth > 0 && lGripHeight > 0)
        {
            double lGripLeft = lMoveLeft + (lMoveWidth - lGripWidth) / 2;
            double lGripTop = lTop + QMapGripInset;
            double lMoveRadius = Math.Min(8, lGripHeight / 2);
            lOver.Add(new QMapShape(
                QMapShapeKind.QMapShapeBody,
                lGripLeft,
                lGripTop,
                lGripLeft + lGripWidth,
                lGripTop + lGripHeight,
                lMoveRadius));
            QMapDotsAdd(lOver, lGripLeft, lGripTop, lGripWidth, lGripHeight);
        }

        lOver.Add(new QMapShape(QMapShapeKind.QMapShapeShine, lBodyLeft + 6, lTop + 1.5, lRight - 6, lTop + 1.5, 0));
        double lBorderInset = QMapBorderThickness / 2;
        if (lBodyWidth - QMapBorderThickness > 0 && lPaintHeight - QMapBorderThickness > 0)
        {
            lOver.Add(new QMapShape(
                QMapShapeKind.QMapShapeBorder,
                lBodyLeft + lBorderInset,
                lTop + lBorderInset,
                lRight - lBorderInset,
                lBottom - lBorderInset,
                lRadius));
        }
    }

    private static void QMapDotsAdd(List<QMapShape> lOver, double lLeft, double lTop, double lWidth, double lHeight)
    {
        int lColumnCount = Math.Clamp((int)(lWidth / 9), 2, 10);
        double lCenterOffset = (lColumnCount - 1) / 2.0;
        double lY = lTop + lHeight / 2;
        for (int lColumn = 0; lColumn < lColumnCount; lColumn++)
        {
            double lX = lLeft + lWidth / 2 + (lColumn - lCenterOffset) * 7;
            if (lX > lLeft + 4 && lX < lLeft + lWidth - 4)
            {
                lOver.Add(new QMapShape(QMapShapeKind.QMapShapeDot, lX, lY, lX, lY, 1.3));
            }
        }
    }

    internal static (double, double, double) LMapMoveResolve(
        double lBodyLeft, double lBodyWidth, double lBodyHeight, double lSideWidth, double lWidth)
    {
        double lMoveHeight = Math.Clamp(lBodyHeight, 1, 16);
        double lSpaceWidth = Math.Max(0, lBodyWidth - lSideWidth * 2);
        if (lSpaceWidth <= 0)
        {
            return (0, 0, 0);
        }

        double lMoveWidth = Math.Clamp(lBodyWidth * 0.42, Math.Min(72, lSpaceWidth), Math.Min(320, lSpaceWidth));
        double lCentered = lBodyLeft + (lBodyWidth - lMoveWidth) / 2;
        double lLeftMinimum = lBodyLeft + lSideWidth;
        double lLeftMaximum = lBodyLeft + lBodyWidth - lSideWidth - lMoveWidth;
        double lMoveLeft = lLeftMaximum < lLeftMinimum ? lCentered : Math.Clamp(lCentered, lLeftMinimum, lLeftMaximum);
        return (Math.Clamp(lMoveLeft, 0, Math.Max(0, lWidth - lMoveWidth)), lMoveWidth, lMoveHeight);
    }
}
