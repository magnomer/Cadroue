using System.Windows;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIVeneer.PPanel;

namespace Cadroue.UIVeneer.PFlow;

public sealed partial class PViewfinder
{
    private const double PViewfinderHiddenOpacity = 0.4;

    private void PViewfinderSectionsDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double railTop,
        double railBottom,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        double rangeSeconds)
    {
        if (lSectionList.Count == 0)
        {
            return;
        }

        double sectionTop = railTop + PViewfinderSectionInset;
        double sectionHeight = Math.Max(4, railBottom - railTop - PViewfinderSectionInset * 2);
        for (int index = 0; index < lSectionList.Count; index++)
        {
            LPiece section = lSectionList[index];
            TimeSpan sectionStart = section.LPieceOrigin < rangeStart ? rangeStart : section.LPieceOrigin;
            TimeSpan sectionEnd = section.LPieceEnd > rangeEnd ? rangeEnd : section.LPieceEnd;
            if (sectionEnd <= sectionStart)
            {
                continue;
            }

            double sectionStartX = Math.Clamp(
                (sectionStart - rangeStart).TotalSeconds / rangeSeconds * actualWidth,
                0,
                actualWidth);
            double sectionEndX = Math.Clamp(
                (sectionEnd - rangeStart).TotalSeconds / rangeSeconds * actualWidth,
                0,
                actualWidth);
            double sectionWidth = Math.Max(1, sectionEndX - sectionStartX);
            Brush sectionBrush = PSectionPalette.PSectionPaletteRead(section.LPieceColorIndex);
            Pen? sectionPen = index == lFlow.LFlowSectionIndex ? new Pen(Brushes.Black, 1.5) : null;
            var sectionRect = new Rect(sectionStartX, sectionTop, sectionWidth, sectionHeight);

            if (section.LPieceHidden)
            {
                drawingContext.PushOpacity(PViewfinderHiddenOpacity);
            }

            drawingContext.DrawRoundedRectangle(sectionBrush, sectionPen, sectionRect, 3, 3);
            PViewfinderSectionDraw(drawingContext, sectionRect, index, section.LPieceColorIndex, section.LPieceName);

            if (section.LPieceHidden)
            {
                drawingContext.Pop();
            }
        }
    }

    private void PViewfinderSectionDraw(
        DrawingContext drawingContext,
        Rect sectionRect,
        int sectionIndex,
        int sectionColorIndex,
        string sectionName)
    {
        double labelRoom = sectionRect.Width - PViewfinderSectionPadding * 2;
        if (labelRoom <= 0 || sectionRect.Height < PViewfinderHeightLeast)
        {
            return;
        }

        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        FormattedText badgeFormatted = PViewfinderLabelRead(
            PViewfinderKindBadge, $"{sectionIndex + 1}", 0, pixelsPerDip);

        double badgeHeight = badgeFormatted.Height + PViewfinderBadgeVertical * 2;
        double badgeWidth = Math.Max(badgeHeight, badgeFormatted.Width + PViewfinderBadgeHorizontal * 2);
        if (badgeWidth > labelRoom || badgeHeight > sectionRect.Height - 2)
        {
            return;
        }

        double nameRoom = labelRoom - badgeWidth - PViewfinderBadgeGap;
        FormattedText? nameFormatted = null;
        if (!string.IsNullOrEmpty(sectionName) && nameRoom >= PViewfinderSectionLeast)
        {
            nameFormatted = PViewfinderLabelRead(
                PViewfinderKindName, sectionName, Math.Round(nameRoom), pixelsPerDip);
        }

        double labelWidth = nameFormatted is null
            ? badgeWidth
            : badgeWidth + PViewfinderBadgeGap + nameFormatted.Width;
        double labelLeft = sectionRect.Left + (sectionRect.Width - labelWidth) / 2;

        var badgeRect = new Rect(
            labelLeft,
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
                badgeRect.Top + PViewfinderBadgeVertical));

        if (nameFormatted is null)
        {
            return;
        }

        drawingContext.DrawText(
            nameFormatted,
            new Point(
                badgeRect.Right + PViewfinderBadgeGap,
                sectionRect.Top + (sectionRect.Height - nameFormatted.Height) / 2));
    }
}
