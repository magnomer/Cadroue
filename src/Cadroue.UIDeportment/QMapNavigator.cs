namespace Cadroue.UIDeportment;

public static class QMapNavigator
{
    private const double LMapGripWidth = 7;
    private const double LMapGripRate = 0.5;
    private const double LMapGripInset = 2.5;
    private const double LMapShadowDrop = 1.5;
    private const double LMapNavigatorRim = 3.5;
    private const double LMapBorderThickness = 1.2;

    internal static void LMapNavigatorAdd(
        List<LMapShape> lOver, double lBodyLeft, double lBodyWidth, double lBodyHeight, double lWidth)
    {
        double lPaintHeight = Math.Max(0, lBodyHeight - LMapShadowDrop);
        if (lPaintHeight <= 0)
        {
            return;
        }

        double lTop = LMap.LMapRailTop;
        double lBottom = lTop + lPaintHeight;
        double lRight = lBodyLeft + lBodyWidth;
        double lRadius = Math.Clamp(lPaintHeight / 2, 4, 9);
        double lSideWidth = Math.Min(LMapGripWidth, lBodyWidth);
        double lGrabWidth = Math.Min(LMap.LMapHandleWidth, lBodyWidth);
        lOver.Add(new LMapShape(LMapShapeKind.LMapShapeFill, lBodyLeft, lTop, lRight, lBottom, lRadius));

        double lRimInset = LMapNavigatorRim / 2;
        if (lBodyWidth - LMapNavigatorRim > 0 && lPaintHeight - LMapNavigatorRim > 0)
        {
            double lRimRadius = Math.Max(0, lRadius - lRimInset);
            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeShadow,
                lBodyLeft + lRimInset,
                lTop + lRimInset + LMapShadowDrop,
                lRight - lRimInset,
                lBottom - lRimInset + LMapShadowDrop,
                lRimRadius));
            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeRim,
                lBodyLeft + lRimInset,
                lTop + lRimInset,
                lRight - lRimInset,
                lBottom - lRimInset,
                lRimRadius));
        }

        lOver.Add(new LMapShape(
            LMapShapeKind.LMapShapeSide, lBodyLeft, lTop, lBodyLeft + lSideWidth, lBottom, lRadius));
        lOver.Add(new LMapShape(LMapShapeKind.LMapShapeSide, lRight - lSideWidth, lTop, lRight, lBottom, lRadius));
        double lGripInset = Math.Max(3, lPaintHeight * 0.25);
        lOver.Add(new LMapShape(
            LMapShapeKind.LMapShapeGrip,
            lBodyLeft + lSideWidth / 2, lTop + lGripInset, lBodyLeft + lSideWidth / 2, lBottom - lGripInset, 0));
        lOver.Add(new LMapShape(
            LMapShapeKind.LMapShapeGrip,
            lRight - lSideWidth / 2, lTop + lGripInset, lRight - lSideWidth / 2, lBottom - lGripInset, 0));

        (double lMoveLeft, double lMoveWidth, double lMoveHeight) =
            LMapMoveResolve(lBodyLeft, lBodyWidth, lBodyHeight, lGrabWidth, lWidth);
        double lGripWidth = Math.Min(lMoveWidth, lMoveWidth * LMapGripRate);
        double lGripHeight = Math.Min(lMoveHeight, lPaintHeight) - LMapGripInset * 2;
        if (lMoveWidth > 0 && lMoveHeight > 0 && lGripWidth > 0 && lGripHeight > 0)
        {
            double lGripLeft = lMoveLeft + (lMoveWidth - lGripWidth) / 2;
            double lGripTop = lTop + LMapGripInset;
            double lMoveRadius = Math.Min(8, lGripHeight / 2);
            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeBody,
                lGripLeft,
                lGripTop,
                lGripLeft + lGripWidth,
                lGripTop + lGripHeight,
                lMoveRadius));
            LMapDotsAdd(lOver, lGripLeft, lGripTop, lGripWidth, lGripHeight);
        }

        lOver.Add(new LMapShape(LMapShapeKind.LMapShapeShine, lBodyLeft + 6, lTop + 1.5, lRight - 6, lTop + 1.5, 0));
        double lBorderInset = LMapBorderThickness / 2;
        if (lBodyWidth - LMapBorderThickness > 0 && lPaintHeight - LMapBorderThickness > 0)
        {
            lOver.Add(new LMapShape(
                LMapShapeKind.LMapShapeBorder,
                lBodyLeft + lBorderInset,
                lTop + lBorderInset,
                lRight - lBorderInset,
                lBottom - lBorderInset,
                lRadius));
        }
    }

    private static void LMapDotsAdd(List<LMapShape> lOver, double lLeft, double lTop, double lWidth, double lHeight)
    {
        int lColumnCount = Math.Clamp((int)(lWidth / 9), 2, 10);
        double lCenterOffset = (lColumnCount - 1) / 2.0;
        double lY = lTop + lHeight / 2;
        for (int lColumn = 0; lColumn < lColumnCount; lColumn++)
        {
            double lX = lLeft + lWidth / 2 + (lColumn - lCenterOffset) * 7;
            if (lX > lLeft + 4 && lX < lLeft + lWidth - 4)
            {
                lOver.Add(new LMapShape(LMapShapeKind.LMapShapeDot, lX, lY, lX, lY, 1.3));
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
