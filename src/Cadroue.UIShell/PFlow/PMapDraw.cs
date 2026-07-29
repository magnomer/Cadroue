using System.Windows;
using System.Windows.Media;
using Cadroue.Media;
using Cadroue.UIShell.PPanels;

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

    private void PMapSectionsDraw(DrawingContext drawingContext, double actualWidth, double railTop, double railHeight)
    {
        if (lSpool is null || lSectionList.Count == 0)
        {
            return;
        }

        double durationSeconds = lSpool.LSpoolDuration.TotalSeconds;
        if (durationSeconds <= 0)
        {
            return;
        }

        double sectionTop = railTop + PMapSectionInset;
        double sectionHeight = Math.Max(4, railHeight - PMapSectionInset * 2);
        for (int index = 0; index < lSectionList.Count; index++)
        {
            LSegment section = lSectionList[index];
            if (section.LSegmentEnd <= section.LSegmentStart)
            {
                continue;
            }

            double sectionStartX = Math.Clamp(section.LSegmentStart.TotalSeconds / durationSeconds * actualWidth, 0, actualWidth);
            double sectionEndX = Math.Clamp(section.LSegmentEnd.TotalSeconds / durationSeconds * actualWidth, 0, actualWidth);
            double sectionWidth = Math.Max(1, sectionEndX - sectionStartX);
            Pen? sectionPen = index == lSectionIndexSelect ? pMapPenSectionSelect : null;
            var sectionRect = new Rect(sectionStartX, sectionTop, sectionWidth, sectionHeight);
            drawingContext.DrawRoundedRectangle(
                PSectionPalette.PSectionPaletteRead(section.LSegmentColorIndex),
                sectionPen,
                sectionRect,
                3,
                3);
            PMapSectionLabelDraw(drawingContext, sectionRect, index, section.LSegmentColorIndex);
        }
    }

    private FormattedText PMapBadgeRead(string pMapText, double pixelsPerDip)
    {
        if (pixelsPerDip != pMapBadgeDpi)
        {
            pMapBadgeCache.Clear();
            pMapBadgeDpi = pixelsPerDip;
        }

        if (pMapBadgeCache.TryGetValue(pMapText, out FormattedText? pMapCached))
        {
            return pMapCached;
        }

        pMapGlyphCount++;
        var pMapBuilt = new FormattedText(
            pMapText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            pMapBadgeTypeface,
            PSection.PSectionNameSize,
            pMapBrushBadgeText,
            pixelsPerDip);
        pMapBadgeCache[pMapText] = pMapBuilt;
        return pMapBuilt;
    }

    private void PMapSectionLabelDraw(DrawingContext drawingContext, Rect sectionRect, int sectionIndex, int sectionColorIndex)
    {
        FormattedText badgeFormatted = PMapBadgeRead(
            $"{sectionIndex + 1}", VisualTreeHelper.GetDpi(this).PixelsPerDip);

        double badgeHeight = badgeFormatted.Height + PMapBadgePaddingVertical * 2;
        double badgeWidth = Math.Max(badgeHeight, badgeFormatted.Width + PMapBadgePaddingHorizontal * 2);
        if (badgeWidth > sectionRect.Width - PMapBadgeMargin * 2 || badgeHeight > sectionRect.Height - PMapBadgeMargin * 2)
        {
            return;
        }

        var badgeRect = new Rect(
            sectionRect.Left + (sectionRect.Width - badgeWidth) / 2,
            sectionRect.Top + (sectionRect.Height - badgeHeight) / 2,
            badgeWidth,
            badgeHeight);
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBadgeRead(sectionColorIndex),
            null,
            badgeRect,
            badgeHeight / 2,
            badgeHeight / 2);
        drawingContext.DrawText(
            badgeFormatted,
            new Point(
                badgeRect.Left + (badgeWidth - badgeFormatted.Width) / 2,
                badgeRect.Top + PMapBadgePaddingVertical));
    }

    private static void PMapNavigationDraw(DrawingContext drawingContext, Rect bodyRect, double actualWidth)
    {
        Rect paintRect = bodyRect;
        paintRect.Height = Math.Max(0, bodyRect.Height - PMapShadowDrop);
        if (paintRect.Height <= 0)
        {
            return;
        }

        double radius = Math.Clamp(paintRect.Height / 2, 4, 9);

        double sideWidth = Math.Min(PGripWidth, paintRect.Width);
        double sideGrabWidth = Math.Min(PMapHandleWidth, bodyRect.Width);
        drawingContext.DrawRoundedRectangle(pMapBrushNavigationFill, null, paintRect, radius, radius);

        Rect rimRect = paintRect;
        rimRect.Inflate(-PMapNavigationRim / 2, -PMapNavigationRim / 2);
        if (rimRect.Width > 0 && rimRect.Height > 0)
        {
            double rimRadius = Math.Max(0, radius - PMapNavigationRim / 2);
            var shadowRect = new Rect(rimRect.Left, rimRect.Top + PMapShadowDrop, rimRect.Width, rimRect.Height);
            drawingContext.DrawRoundedRectangle(null, pMapPenNavigationRimShadow, shadowRect, rimRadius, rimRadius);
            drawingContext.DrawRoundedRectangle(null, pMapPenNavigationRim, rimRect, rimRadius, rimRadius);
        }

        Rect leftHandleRect = new(paintRect.Left, paintRect.Top, sideWidth, paintRect.Height);
        Rect rightHandleRect = new(paintRect.Right - sideWidth, paintRect.Top, sideWidth, paintRect.Height);
        drawingContext.DrawRoundedRectangle(pMapBrushNavigationSide, null, leftHandleRect, radius, radius);
        drawingContext.DrawRoundedRectangle(pMapBrushNavigationSide, null, rightHandleRect, radius, radius);

        PGripSideDraw(drawingContext, leftHandleRect);
        PGripSideDraw(drawingContext, rightHandleRect);

        Rect moveRect = PMapMoveResolve(bodyRect, sideGrabWidth, actualWidth);
        if (moveRect.Width > 0 && moveRect.Height > 0)
        {
            Rect movePaintRect = PGripMoveResolve(moveRect, paintRect);
            if (movePaintRect.Width > 0 && movePaintRect.Height > 0)
            {
                double moveRadius = Math.Min(8, movePaintRect.Height / 2);
                drawingContext.DrawRoundedRectangle(pMapBrushNavigationMove, pMapPenNavigationMoveBorder, movePaintRect, moveRadius, moveRadius);
                PGripMoveDraw(drawingContext, movePaintRect);
            }
        }

        drawingContext.DrawLine(
            pMapPenNavigationShine,
            new Point(paintRect.Left + 6, paintRect.Top + 1.5),
            new Point(paintRect.Right - 6, paintRect.Top + 1.5));

        Rect borderRect = paintRect;
        borderRect.Inflate(-pMapPenNavigationBorder.Thickness / 2, -pMapPenNavigationBorder.Thickness / 2);
        if (borderRect.Width > 0 && borderRect.Height > 0)
        {
            drawingContext.DrawRoundedRectangle(null, pMapPenNavigationBorder, borderRect, radius, radius);
        }
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
        double gripX = handleRect.Left + handleRect.Width / 2;
        drawingContext.DrawLine(
            pMapPenNavigationGrip,
            new Point(gripX, handleRect.Top + Math.Max(3, handleRect.Height * 0.25)),
            new Point(gripX, handleRect.Bottom - Math.Max(3, handleRect.Height * 0.25)));
    }

    private static Rect PGripMoveResolve(Rect moveRect, Rect paintRect)
    {
        double gripWidth = Math.Min(moveRect.Width, moveRect.Width * PGripMoveRate);
        double gripHeight = Math.Min(moveRect.Height, paintRect.Height) - PGripMoveInset * 2;
        if (gripWidth <= 0 || gripHeight <= 0)
        {
            return Rect.Empty;
        }

        return new Rect(
            moveRect.Left + (moveRect.Width - gripWidth) / 2,
            paintRect.Top + PGripMoveInset,
            gripWidth,
            gripHeight);
    }

    private static void PGripMoveDraw(DrawingContext drawingContext, Rect moveRect)
    {
        int columnCount = Math.Clamp((int)(moveRect.Width / 9), 2, 10);
        double columnCenterOffset = (columnCount - 1) / 2.0;
        double gripY = moveRect.Top + moveRect.Height / 2;
        for (int column = 0; column < columnCount; column++)
        {
            double gripX = moveRect.Left + moveRect.Width / 2 + (column - columnCenterOffset) * 7;
            if (gripX > moveRect.Left + 4 && gripX < moveRect.Right - 4)
            {
                drawingContext.DrawEllipse(pMapBrushNavigationGrip, null, new Point(gripX, gripY), 1.3, 1.3);
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
