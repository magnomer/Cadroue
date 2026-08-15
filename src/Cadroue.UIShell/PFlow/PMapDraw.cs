using System.Windows;
using System.Windows.Media;
using Cadroue.Media;
using Cadroue.UIShell.PPanels;

using Cadroue.Core;


namespace Cadroue.UIShell.PFlow;

public sealed partial class PMap
{
    private const double PMapHiddenOpacity = 0.4;

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
        foreach (LKeyframeScanRange range in LKeyframeView.LKeyframeCoverageResolve(lKeyframeScannedRanges, lSpool, true))
        {
            double scanStartX = Math.Clamp(range.LKeyframeRangeOrigin.TotalSeconds / durationSeconds * actualWidth, 0, actualWidth);
            double scanEndX = Math.Clamp(range.LKeyframeRangeLimit.TotalSeconds / durationSeconds * actualWidth, 0, actualWidth);
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
                pMapCoverageBrush,
                null,
                new Rect(scanStartX, coverageTop, scanWidth, coverageHeight));
        }
    }

    private void PMapWaveformDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double railTop,
        double railHeight)
    {
        if (lSpool is null || lSpool.LSpoolDuration <= TimeSpan.Zero)
        {
            return;
        }

        Geometry? waveformGeometry = PFlow.PFlowWaveformBuild(
            lWaveformPeaks, actualWidth, railTop, railHeight, TimeSpan.Zero, lSpool.LSpoolDuration);
        if (waveformGeometry is not null)
        {
            drawingContext.DrawGeometry(pMapBrushWaveform, null, waveformGeometry);
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
            LPiece section = lSectionList[index];
            if (section.LPieceEnd <= section.LPieceOrigin)
            {
                continue;
            }

            double sectionStartX = Math.Clamp(section.LPieceOrigin.TotalSeconds / durationSeconds * actualWidth, 0, actualWidth);
            double sectionEndX = Math.Clamp(section.LPieceEnd.TotalSeconds / durationSeconds * actualWidth, 0, actualWidth);
            double sectionWidth = Math.Max(1, sectionEndX - sectionStartX);
            Pen? sectionPen = index == lSectionIndexActive ? pMapSectionPen : null;
            var sectionRect = new Rect(sectionStartX, sectionTop, sectionWidth, sectionHeight);

            if (section.LPieceHidden)
            {
                drawingContext.PushOpacity(PMapHiddenOpacity);
            }

            drawingContext.DrawRoundedRectangle(
                PSectionPalette.PSectionPaletteRead(section.LPieceColorIndex),
                sectionPen,
                sectionRect,
                3,
                3);
            PMapSectionDraw(drawingContext, sectionRect, index, section.LPieceColorIndex);

            if (section.LPieceHidden)
            {
                drawingContext.Pop();
            }
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
            pMapBadgeBrush,
            pixelsPerDip);
        pMapBadgeCache[pMapText] = pMapBuilt;
        return pMapBuilt;
    }

    private void PMapSectionDraw(DrawingContext drawingContext, Rect sectionRect, int sectionIndex, int sectionColorIndex)
    {
        FormattedText badgeFormatted = PMapBadgeRead(
            $"{sectionIndex + 1}", VisualTreeHelper.GetDpi(this).PixelsPerDip);

        double badgeHeight = badgeFormatted.Height + PMapBadgeVertical * 2;
        double badgeWidth = Math.Max(badgeHeight, badgeFormatted.Width + PMapBadgeHorizontal * 2);
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
                badgeRect.Top + PMapBadgeVertical));
    }

    private static void PNavigatorDraw(DrawingContext drawingContext, Rect bodyRect, double actualWidth)
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
        drawingContext.DrawRoundedRectangle(pNavigatorFillBrush, null, paintRect, radius, radius);

        Rect rimRect = paintRect;
        rimRect.Inflate(-PNavigatorRim / 2, -PNavigatorRim / 2);
        if (rimRect.Width > 0 && rimRect.Height > 0)
        {
            double rimRadius = Math.Max(0, radius - PNavigatorRim / 2);
            var shadowRect = new Rect(rimRect.Left, rimRect.Top + PMapShadowDrop, rimRect.Width, rimRect.Height);
            drawingContext.DrawRoundedRectangle(null, pNavigatorShadowPen, shadowRect, rimRadius, rimRadius);
            drawingContext.DrawRoundedRectangle(null, pNavigatorRimPen, rimRect, rimRadius, rimRadius);
        }

        Rect leftHandleRect = new(paintRect.Left, paintRect.Top, sideWidth, paintRect.Height);
        Rect rightHandleRect = new(paintRect.Right - sideWidth, paintRect.Top, sideWidth, paintRect.Height);
        drawingContext.DrawRoundedRectangle(pNavigatorSideBrush, null, leftHandleRect, radius, radius);
        drawingContext.DrawRoundedRectangle(pNavigatorSideBrush, null, rightHandleRect, radius, radius);

        PGripSideDraw(drawingContext, leftHandleRect);
        PGripSideDraw(drawingContext, rightHandleRect);

        Rect moveRect = PMapMoveResolve(bodyRect, sideGrabWidth, actualWidth);
        if (moveRect.Width > 0 && moveRect.Height > 0)
        {
            Rect movePaintRect = PGripMoveResolve(moveRect, paintRect);
            if (movePaintRect.Width > 0 && movePaintRect.Height > 0)
            {
                double moveRadius = Math.Min(8, movePaintRect.Height / 2);
                drawingContext.DrawRoundedRectangle(pNavigatorBodyBrush, pNavigatorBodyPen, movePaintRect, moveRadius, moveRadius);
                PGripMoveDraw(drawingContext, movePaintRect);
            }
        }

        drawingContext.DrawLine(
            pNavigatorShinePen,
            new Point(paintRect.Left + 6, paintRect.Top + 1.5),
            new Point(paintRect.Right - 6, paintRect.Top + 1.5));

        Rect borderRect = paintRect;
        borderRect.Inflate(-pNavigatorBorderPen.Thickness / 2, -pNavigatorBorderPen.Thickness / 2);
        if (borderRect.Width > 0 && borderRect.Height > 0)
        {
            drawingContext.DrawRoundedRectangle(null, pNavigatorBorderPen, borderRect, radius, radius);
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
            pNavigatorGripPen,
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
                drawingContext.DrawEllipse(pNavigatorGripBrush, null, new Point(gripX, gripY), 1.3, 1.3);
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
