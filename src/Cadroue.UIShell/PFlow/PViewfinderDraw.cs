using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.UIShell;
using Cadroue.UIShell.PPanels;

using Cadroue.Core;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PViewfinder
{
    private const int PViewfinderCacheLimit = 512;
    private const double PViewfinderHiddenOpacity = 0.4;
    private const int PViewfinderTickKind = 0;
    private const int PViewfinderKindBadge = 1;
    private const int PViewfinderKindName = 2;

    private FormattedText PViewfinderLabelRead(int pViewfinderKind, string pViewfinderText, double pViewfinderRoom, double pixelsPerDip)
    {
        if (pixelsPerDip != pViewfinderTextDpi || pViewfinderTextCache.Count > PViewfinderCacheLimit)
        {
            pViewfinderTextCache.Clear();
            pViewfinderTextDpi = pixelsPerDip;
        }

        var pViewfinderKey = (pViewfinderKind, pViewfinderText, pViewfinderRoom);
        if (pViewfinderTextCache.TryGetValue(pViewfinderKey, out FormattedText? pViewfinderCached))
        {
            return pViewfinderCached;
        }

        pViewfinderGlyphCount++;
        FormattedText pViewfinderBuilt = pViewfinderKind switch
        {
            PViewfinderTickKind => new FormattedText(
                pViewfinderText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                pViewfinderTickTypeface,
                9,
                pViewfinderTickBrush,
                pixelsPerDip),
            PViewfinderKindBadge => new FormattedText(
                pViewfinderText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                pViewfinderBadgeTypeface,
                PSection.PSectionNameSize,
                pViewfinderBadgeBrush,
                pixelsPerDip),
            _ => new FormattedText(
                pViewfinderText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                pViewfinderTickTypeface,
                PSection.PSectionNameSize,
                pViewfinderSectionBrush,
                pixelsPerDip)
            {
                MaxTextWidth = pViewfinderRoom,
                MaxLineCount = 1,
                Trimming = TextTrimming.CharacterEllipsis
            }
        };

        pViewfinderTextCache[pViewfinderKey] = pViewfinderBuilt;
        return pViewfinderBuilt;
    }

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
            TimeSpan sectionStart = section.LPieceStart < rangeStart ? rangeStart : section.LPieceStart;
            TimeSpan sectionEnd = section.LPieceEnd > rangeEnd ? rangeEnd : section.LPieceEnd;
            if (sectionEnd <= sectionStart)
            {
                continue;
            }

            double sectionStartX = Math.Clamp((sectionStart - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double sectionEndX = Math.Clamp((sectionEnd - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double sectionWidth = Math.Max(1, sectionEndX - sectionStartX);
            Brush sectionBrush = PSectionPalette.PSectionPaletteRead(section.LPieceColorIndex);
            Pen? sectionPen = index == lSectionIndexActive ? new Pen(Brushes.Black, 1.5) : null;
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

    private void PViewfinderWaveformDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double railTop,
        double railHeight,
        TimeSpan rangeStart,
        TimeSpan rangeEnd)
    {
        Geometry? waveformGeometry = PFlow.PFlowWaveformBuild(
            lWaveformPeaks, actualWidth, railTop, railHeight, rangeStart, rangeEnd);
        if (waveformGeometry is not null)
        {
            drawingContext.DrawGeometry(pViewfinderBrushWaveform, null, waveformGeometry);
        }
    }

    private void PViewfinderCursorDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double actualHeight,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        double rangeSeconds)
    {
        if (lCursor < rangeStart || lCursor > rangeEnd)
        {
            return;
        }

        double cursorRatio = Math.Clamp((lCursor - rangeStart).TotalSeconds / rangeSeconds, 0, 1);
        double cursorX = cursorRatio * actualWidth;

        string timeText = PViewfinderTimeFormat(lCursor);
        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        pViewfinderGlyphCount++;
        var formattedText = new FormattedText(
            timeText,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            pViewfinderTickTypeface,
            10,
            pViewfinderCursorBrush,
            pixelsPerDip);
        double labelWidth = formattedText.Width + PTimecodePaddingHorizontal * 2;
        double labelHeight = formattedText.Height + PTimecodePaddingVertical * 2;
        double safeLabelWidth = Math.Min(labelWidth, actualWidth);
        Rect labelRect = PCursor.PCursorChipResolve(
            cursorX,
            safeLabelWidth,
            labelHeight,
            PTimecodeLaneHeight,
            actualHeight,
            actualWidth);

        PCursor.PCursorDraw(drawingContext, cursorX, PTimecodeLaneHeight, actualHeight, labelRect);
        drawingContext.DrawRoundedRectangle(pTimecodeBackgroundBrush, pTimecodeBorderPen, labelRect, 3, 3);
        drawingContext.DrawText(
            formattedText,
            new Point(labelRect.Left + PTimecodePaddingHorizontal, labelRect.Top + PTimecodePaddingVertical));
    }

    private void PViewfinderCoverageDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double coverageTop,
        double coverageHeight,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        double rangeSeconds)
    {
        foreach (LKeyframeScanRange range in LKeyframeView.LKeyframeCoverageResolve(lKeyframeScannedRanges, lSpool!, false))
        {
            double scanStartX = Math.Clamp((range.LKeyframeRangeOrigin - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
            double scanEndX = Math.Clamp((range.LKeyframeRangeLimit - rangeStart).TotalSeconds / rangeSeconds * actualWidth, 0, actualWidth);
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
                pViewfinderCoverageBrush,
                null,
                new Rect(scanStartX, coverageTop, scanWidth, coverageHeight));
        }
    }

    private void PViewfinderKeyframesDraw(
        DrawingContext drawingContext,
        double actualWidth,
        double railTop,
        double railBottom,
        TimeSpan rangeStart,
        TimeSpan rangeEnd,
        double rangeSeconds)
    {
        IReadOnlyList<LKeyframeEntry> visible = LKeyframeView.LKeyframeVisibleResolve(lKeyframeList, lCursor, lSpool!);
        if (visible.Count == 0)
        {
            return;
        }

        double[] visibleSearchOffsets = visible
            .Select(entry => (entry.LKeyframePresentationTime - rangeStart).TotalSeconds / rangeSeconds * actualWidth)
            .ToArray();
        if (!PViewfinderVisibilityCheck(actualWidth, rangeSeconds, visibleSearchOffsets))
        {
            return;
        }

        double keyframeRight = Math.Max(0, actualWidth - PViewfinderKeyframeWidth);
        var keyframeGuidelines = new GuidelineSet();
        foreach (double keyframeOffset in visibleSearchOffsets)
        {
            double keyframeX = Math.Clamp(keyframeOffset, 0, keyframeRight);
            keyframeGuidelines.GuidelinesX.Add(keyframeX);
            keyframeGuidelines.GuidelinesX.Add(keyframeX + PViewfinderKeyframeWidth);
        }

        keyframeGuidelines.Freeze();
        drawingContext.PushGuidelineSet(keyframeGuidelines);
        foreach (double keyframeOffset in visibleSearchOffsets)
        {
            double keyframeX = Math.Clamp(keyframeOffset, 0, keyframeRight);
            drawingContext.DrawRectangle(
                pViewfinderBrushKeyframe,
                null,
                new Rect(keyframeX, railTop, PViewfinderKeyframeWidth, railBottom - railTop));
        }

        drawingContext.Pop();
    }

    private static bool PViewfinderVisibilityCheck(
        double actualWidth,
        double rangeSeconds,
        double[] visibleSearchOffsets)
    {
        if (visibleSearchOffsets.Length == 0 || actualWidth <= 0 || rangeSeconds <= 0)
        {
            return false;
        }

        double keyframeMinimumGap = Math.Max(
            PViewfinderKeyframeWidth,
            LPreference.LPreferenceStateCurrent.LPreferenceKeyframePixels);
        for (int keyframeIndex = 1; keyframeIndex < visibleSearchOffsets.Length; keyframeIndex++)
        {
            if (visibleSearchOffsets[keyframeIndex] - visibleSearchOffsets[keyframeIndex - 1] < keyframeMinimumGap)
            {
                return false;
            }
        }

        return true;
    }

    private void PViewfinderTicksDraw(
        DrawingContext drawingContext,
        double actualWidth,
        TimeSpan rangeStart,
        double rangeSeconds)
    {
        double[] tickStepOptionsSeconds = { 0.1, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800, 3600 };
        double tickIntervalSeconds = rangeSeconds / (actualWidth / PViewfinderTickPixels);
        double tickStepSeconds = tickStepOptionsSeconds[^1];
        foreach (double candidateStepSeconds in tickStepOptionsSeconds)
        {
            if (candidateStepSeconds >= tickIntervalSeconds)
            {
                tickStepSeconds = candidateStepSeconds;
                break;
            }
        }

        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        double tickStartSeconds = Math.Ceiling(rangeStart.TotalSeconds / tickStepSeconds) * tickStepSeconds;
        for (double tickSeconds = tickStartSeconds; tickSeconds <= rangeStart.TotalSeconds + rangeSeconds + 1e-9; tickSeconds += tickStepSeconds)
        {
            double tickX = (tickSeconds - rangeStart.TotalSeconds) / rangeSeconds * actualWidth;
            drawingContext.DrawLine(
                pViewfinderTickPen,
                new Point(tickX, PTimecodeLaneHeight * 0.5),
                new Point(tickX, PTimecodeLaneHeight));
            string tickLabel = PViewfinderTimeFormat(TimeSpan.FromSeconds(tickSeconds));
            FormattedText formattedText = PViewfinderLabelRead(
                PViewfinderTickKind, tickLabel, 0, pixelsPerDip);
            drawingContext.DrawText(
                formattedText,
                new Point(tickX + 2, PTimecodeLaneHeight * 0.5 - formattedText.Height / 2));
        }
    }

    private static string PViewfinderTimeFormat(TimeSpan time) =>
        time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes}:{time.Seconds:D2}";
}
