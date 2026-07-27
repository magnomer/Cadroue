using System.Windows;
using System.Windows.Media;
using Cadroue.Media;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PMap
{
    private void PMapCoverageDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double coverageTop,
        double coverageHeight)
    {
        drawingContext.DrawRectangle(
            pMapBrushRail,
            null,
            new Rect(0, coverageTop, actualWidth, coverageHeight));

        if (lSpool is null || lSpool.LSpoolDuration <= TimeSpan.Zero || lKeyframeScannedRanges.Count == 0)
        {
            return;
        }

        double durationSeconds = lSpool.LSpoolDuration.TotalSeconds;
        foreach (LKeyframeScanRange range in lKeyframeScannedRanges)
        {
            TimeSpan scanStart = range.LKeyframeScanRangeStartTime < TimeSpan.Zero
                ? TimeSpan.Zero
                : range.LKeyframeScanRangeStartTime;
            TimeSpan scanEnd = range.LKeyframeScanRangeEndTime > lSpool.LSpoolDuration
                ? lSpool.LSpoolDuration
                : range.LKeyframeScanRangeEndTime;
            if (scanEnd <= scanStart)
            {
                continue;
            }

            double scanStartX = Math.Clamp(scanStart.TotalSeconds / durationSeconds * actualWidth, 0, actualWidth);
            double scanEndX = Math.Clamp(scanEnd.TotalSeconds / durationSeconds * actualWidth, 0, actualWidth);
            double scanWidth = Math.Max(1, scanEndX - scanStartX);
            if (scanStartX + scanWidth > actualWidth)
            {
                scanWidth = Math.Max(0, actualWidth - scanStartX);
            }

            if (scanWidth <= 0)
            {
                continue;
            }

            drawingContext.DrawRectangle(
                pMapBrushCoverageScanned,
                null,
                new Rect(scanStartX, coverageTop, scanWidth, coverageHeight));
        }
    }

    private static void PMapNavigationDraw(DrawingContext drawingContext, Rect bodyRect, double actualWidth)
    {
        double radius = Math.Clamp(bodyRect.Height / 2, 4, 9);
        double sideWidth = Math.Min(PMapHandleWidth, bodyRect.Width);
        Rect shadowRect = new(bodyRect.Left, bodyRect.Top + 1.5, bodyRect.Width, bodyRect.Height);
        drawingContext.DrawRoundedRectangle(pMapBrushNavigationShadow, null, shadowRect, radius, radius);
        drawingContext.DrawRoundedRectangle(pMapBrushNavigationFrame, null, bodyRect, radius, radius);

        Rect fillRect = bodyRect;
        fillRect.Inflate(-3.5, -3.5);
        if (fillRect.Width > 0 && fillRect.Height > 0)
        {
            drawingContext.DrawRoundedRectangle(pMapBrushNavigationFill, null, fillRect, Math.Max(0, radius - 3), Math.Max(0, radius - 3));
        }

        Rect innerRect = bodyRect;
        innerRect.Inflate(-2.2, -2.2);
        if (innerRect.Width > 0 && innerRect.Height > 0)
        {
            drawingContext.DrawRoundedRectangle(pMapBrushNavigationInner, null, innerRect, Math.Max(0, radius - 2), Math.Max(0, radius - 2));
        }

        Rect leftHandleRect = new(bodyRect.Left, bodyRect.Top, sideWidth, bodyRect.Height);
        Rect rightHandleRect = new(bodyRect.Right - sideWidth, bodyRect.Top, sideWidth, bodyRect.Height);
        drawingContext.DrawRoundedRectangle(pMapBrushNavigationSide, null, leftHandleRect, radius, radius);
        drawingContext.DrawRoundedRectangle(pMapBrushNavigationSide, null, rightHandleRect, radius, radius);

        PGripSideDraw(drawingContext, leftHandleRect);
        PGripSideDraw(drawingContext, rightHandleRect);

        Rect moveRect = PMapMoveResolve(bodyRect, sideWidth, actualWidth);
        if (moveRect.Width > 0 && moveRect.Height > 0)
        {
            double moveRadius = Math.Min(8, moveRect.Height / 2);
            drawingContext.DrawRoundedRectangle(pMapBrushNavigationMove, pMapPenNavigationMoveBorder, moveRect, moveRadius, moveRadius);
            PGripMoveDraw(drawingContext, moveRect);
        }

        drawingContext.DrawLine(
            pMapPenNavigationShine,
            new Point(bodyRect.Left + 6, bodyRect.Top + 1.5),
            new Point(bodyRect.Right - 6, bodyRect.Top + 1.5));
        drawingContext.DrawRoundedRectangle(null, pMapPenNavigationBorder, bodyRect, radius, radius);
    }

    private static Rect PMapMoveResolve(Rect bodyRect, double sideWidth, double actualWidth)
    {
        double moveHeight = Math.Clamp(bodyRect.Height, 1, 16);
        double moveSpaceWidth = Math.Max(0, bodyRect.Width - sideWidth * 2);
        if (moveSpaceWidth <= 0)
        {
            return Rect.Empty;
        }

        double desiredWidth = bodyRect.Width * 0.42;
        double minimumWidth = Math.Min(72, moveSpaceWidth);
        double maximumWidth = Math.Min(320, moveSpaceWidth);
        double moveWidth = Math.Clamp(desiredWidth, minimumWidth, maximumWidth);

        double moveLeftMinimum = bodyRect.Left + sideWidth;
        double moveLeftMaximum = bodyRect.Right - sideWidth - moveWidth;
        double moveLeft = moveLeftMaximum < moveLeftMinimum
            ? bodyRect.Left + (bodyRect.Width - moveWidth) / 2
            : Math.Clamp(bodyRect.Left + (bodyRect.Width - moveWidth) / 2, moveLeftMinimum, moveLeftMaximum);

        moveLeft = Math.Clamp(moveLeft, 0, Math.Max(0, actualWidth - moveWidth));
        return new Rect(moveLeft, bodyRect.Top, moveWidth, moveHeight);
    }

    private static void PGripSideDraw(DrawingContext drawingContext, Rect handleRect)
    {
        Rect innerRect = handleRect;
        innerRect.Inflate(-4.5, -4.5);
        if (innerRect.Width > 0 && innerRect.Height > 0)
        {
            drawingContext.DrawRectangle(pMapBrushNavigationSideInner, null, innerRect);
        }

        for (int i = 0; i < 2; i++)
        {
            double gripX = handleRect.Left + handleRect.Width / 2 + (i - 0.5) * 4;
            drawingContext.DrawLine(
                pMapPenNavigationGrip,
                new Point(gripX, handleRect.Top + Math.Max(3, handleRect.Height * 0.25)),
                new Point(gripX, handleRect.Bottom - Math.Max(3, handleRect.Height * 0.25)));
        }
    }

    private static void PGripMoveDraw(DrawingContext drawingContext, Rect moveRect)
    {
        int columnCount = Math.Clamp((int)(moveRect.Width / 9), 2, 10);
        double columnCenterOffset = (columnCount - 1) / 2.0;
        for (int row = 0; row < 2; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                double gripX = moveRect.Left + moveRect.Width / 2 + (column - columnCenterOffset) * 7;
                double gripY = moveRect.Top + moveRect.Height / 2 + (row - 0.5) * 5;
                if (gripX > moveRect.Left + 4 && gripX < moveRect.Right - 4)
                {
                    drawingContext.DrawEllipse(pMapBrushNavigationGrip, null, new Point(gripX, gripY), 1.3, 1.3);
                }
            }
        }
    }

    private Rect PMapBodyResolve(double spoolStartX, double spoolEndX)
    {
        double actualHeight = ActualHeight;
        double coverageTop = Math.Max(0, Math.Max(0, actualHeight - 1) - 3);
        double railTop = 3;
        double railBottom = Math.Max(railTop, coverageTop - 2);
        return new Rect(spoolStartX, railTop, Math.Max(0, spoolEndX - spoolStartX), Math.Max(0, railBottom - railTop));
    }

    private static Rect PMapLeftResolve(Rect bodyRect)
        => new(bodyRect.Left, bodyRect.Top, Math.Min(PMapHandleWidth, bodyRect.Width), bodyRect.Height);

    private static Rect PMapRightResolve(Rect bodyRect)
    {
        double handleWidth = Math.Min(PMapHandleWidth, bodyRect.Width);
        return new Rect(bodyRect.Right - handleWidth, bodyRect.Top, handleWidth, bodyRect.Height);
    }
}
