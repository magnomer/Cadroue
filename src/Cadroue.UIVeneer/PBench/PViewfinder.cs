using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PBench;

public sealed class PViewfinder : FrameworkElement
{
    private static readonly Brush pViewfinderSectionBrush = PViewfinderBrushBuild(0xFF, 0x11, 0x18, 0x27);
    private static readonly Brush pViewfinderBadgeBrush = PViewfinderBrushBuild(0xFF, 0xFF, 0xFF, 0xFF);
    private static readonly Brush pViewfinderBrushBackground = PViewfinderBrushBuild(0xFF, 0xF3, 0xF3, 0xF3);
    private static readonly Brush pViewfinderBrushRail = PViewfinderBrushBuild(0xFF, 0xD1, 0xD1, 0xD1);
    private static readonly Brush pViewfinderWaveformBrush = PViewfinderBrushBuild(0xFF, 0xE6, 0xEA, 0xEF);
    private static readonly Brush pViewfinderBrushWaveform = PViewfinderBrushBuild(0xFF, 0x8C, 0x9B, 0xAD);
    private static readonly Brush pViewfinderBrushKeyframe = PViewfinderBrushBuild(0xFF, 0x6B, 0x74, 0x80);
    private static readonly Brush pViewfinderTickBrush = PViewfinderBrushBuild(0xFF, 0x88, 0x88, 0x88);
    private static readonly Brush pViewfinderCoverageBrush = PViewfinderBrushBuild(0xFF, 0x2F, 0x9E, 0x64);
    private static readonly Brush pViewfinderCursorBrush = PViewfinderBrushBuild(0xFF, 0x1A, 0x1A, 0x1A);
    private static readonly Brush pTimecodeBackgroundBrush = PViewfinderBrushBuild(0xE0, 0xFF, 0xFF, 0xFF);
    private static readonly Pen pViewfinderTickPen =
        PViewfinderPenBuild(PViewfinderBrushBuild(0xFF, 0xB0, 0xB0, 0xB0), 1.0);
    private static readonly Pen pTimecodeBorderPen = PViewfinderPenBuild(pViewfinderBrushRail, 1.0);
    private static readonly Pen pViewfinderSectionPen = PViewfinderPenBuild(Brushes.Black, 1.5);
    private static readonly Typeface pViewfinderBadgeTypeface =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface pViewfinderTickTypeface = new("Segoe UI");

    private static readonly IReadOnlyDictionary<bool, Pen?> pViewfinderSelectedPens = new Dictionary<bool, Pen?>
    {
        [true] = pViewfinderSectionPen,
        [false] = null,
    };

    private static readonly IReadOnlyDictionary<LViewfinderShapeKind, Brush> pViewfinderBrushes =
        new Dictionary<LViewfinderShapeKind, Brush>
        {
            [LViewfinderShapeKind.LViewfinderShapeBackground] = pViewfinderBrushBackground,
            [LViewfinderShapeKind.LViewfinderShapeRail] = pViewfinderBrushRail,
            [LViewfinderShapeKind.LViewfinderShapeWaveform] = pViewfinderWaveformBrush,
            [LViewfinderShapeKind.LViewfinderShapeCoverage] = pViewfinderBrushRail,
            [LViewfinderShapeKind.LViewfinderShapeScanned] = pViewfinderCoverageBrush,
            [LViewfinderShapeKind.LViewfinderShapeKeyframe] = pViewfinderBrushKeyframe,
        };

    private static readonly IReadOnlyDictionary<LViewfinderShapeKind, double> pViewfinderRadii =
        new Dictionary<LViewfinderShapeKind, double>
        {
            [LViewfinderShapeKind.LViewfinderShapeBackground] = 0,
            [LViewfinderShapeKind.LViewfinderShapeRail] = 3,
            [LViewfinderShapeKind.LViewfinderShapeWaveform] = 3,
            [LViewfinderShapeKind.LViewfinderShapeCoverage] = 0,
            [LViewfinderShapeKind.LViewfinderShapeScanned] = 0,
            [LViewfinderShapeKind.LViewfinderShapeKeyframe] = 0,
        };

    public LViewfinder LViewfinder { get; }

    public PViewfinder(LFlow lFlow)
    {
        LViewfinder = new LViewfinder(lFlow);
        LViewfinder.LViewfinderFrameApply += InvalidateVisual;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        long pStamp = LViewfinder.LViewfinderDrawStart();
        LViewfinderFrame lFrame = LViewfinder.LViewfinderFrameResolve(ActualWidth, ActualHeight);
        lFrame.LViewfinderFrameUnder.ToList().ForEach(lShape => PViewfinderShapeDraw(drawingContext, lShape));
        drawingContext.DrawGeometry(
            pViewfinderBrushWaveform, null, PFlow.PFlowWaveformBuild(lFrame.LViewfinderFrameWaveform));
        lFrame.LViewfinderFrameTicks.ToList().ForEach(lTick => PViewfinderTickDraw(drawingContext, lTick));
        lFrame.LViewfinderFrameBands.ToList().ForEach(lBand => PViewfinderBandDraw(drawingContext, lBand));
        lFrame.LViewfinderFrameOver.ToList().ForEach(lShape => PViewfinderShapeDraw(drawingContext, lShape));
        PViewfinderKeyframesDraw(drawingContext, lFrame.LViewfinderFrameKeyframes);
        lFrame.LViewfinderFrameCursor.ToList().ForEach(lCursor => PViewfinderCursorDraw(drawingContext, lCursor));
        LViewfinder.LViewfinderDrawRecord(pStamp);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        e.Handled = LViewfinder.LViewfinderPressHandle(e.GetPosition(this).X, ActualWidth);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        e.Handled = LViewfinder.LViewfinderMoveHandle(e.GetPosition(this).X, ActualWidth);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        LViewfinder.LViewfinderDragClear();
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        LViewfinder.LViewfinderDragClear();
    }

    private static void PViewfinderShapeDraw(DrawingContext drawingContext, LViewfinderShape lShape) =>
        drawingContext.DrawRoundedRectangle(
            pViewfinderBrushes[lShape.LViewfinderShapeKind],
            null,
            PViewfinderRectRead(lShape),
            pViewfinderRadii[lShape.LViewfinderShapeKind],
            pViewfinderRadii[lShape.LViewfinderShapeKind]);

    private static void PViewfinderKeyframesDraw(DrawingContext drawingContext, IReadOnlyList<LViewfinderShape> lMarks)
    {
        var pGuidelines = new GuidelineSet();
        lMarks.ToList().ForEach(lMark => pGuidelines.GuidelinesX.Add(lMark.LViewfinderShapeLeft));
        lMarks.ToList().ForEach(lMark => pGuidelines.GuidelinesX.Add(lMark.LViewfinderShapeRight));
        pGuidelines.Freeze();
        drawingContext.PushGuidelineSet(pGuidelines);
        lMarks.ToList().ForEach(lMark => PViewfinderShapeDraw(drawingContext, lMark));
        drawingContext.Pop();
    }

    private void PViewfinderTickDraw(DrawingContext drawingContext, LViewfinderTick lTick)
    {
        drawingContext.DrawLine(
            pViewfinderTickPen,
            new Point(lTick.LViewfinderTickX, lTick.LViewfinderTickTop),
            new Point(lTick.LViewfinderTickX, lTick.LViewfinderTickBottom));
        FormattedText pLabel = PViewfinderTextBuild(
            lTick.LViewfinderTickLabel, pViewfinderTickTypeface, 9, pViewfinderTickBrush);
        LViewfinderPoint lPoint = LViewfinderText.LViewfinderTickResolve(lTick, pLabel.Height);
        drawingContext.DrawText(pLabel, new Point(lPoint.LViewfinderPointX, lPoint.LViewfinderPointY));
    }

    private void PViewfinderBandDraw(DrawingContext drawingContext, LViewfinderBand lBand)
    {
        FormattedText pBadgeText = PViewfinderTextBuild(
            lBand.LViewfinderBandBadge, pViewfinderBadgeTypeface, PSection.PSectionNameSize, pViewfinderBadgeBrush);
        drawingContext.PushOpacity(PLook.PLookOpacity[lBand.LViewfinderBandShown]);
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBandRead(lBand.LViewfinderBandColor),
            pViewfinderSelectedPens[lBand.LViewfinderBandSelected],
            new Rect(
                new Point(lBand.LViewfinderBandLeft, lBand.LViewfinderBandTop),
                new Point(lBand.LViewfinderBandRight, lBand.LViewfinderBandBottom)),
            3,
            3);
        LViewfinderText.LViewfinderLabelResolve(lBand, pBadgeText.Width, pBadgeText.Height)
            .ToList()
            .ForEach(lLabel => PViewfinderLabelDraw(drawingContext, lBand, lLabel, pBadgeText));
        drawingContext.Pop();
    }

    private void PViewfinderLabelDraw(
        DrawingContext drawingContext, LViewfinderBand lBand, LViewfinderLabel lLabel, FormattedText pBadgeText)
    {
        FormattedText pNameText = PViewfinderNameBuild(lLabel.LViewfinderLabelName, lLabel.LViewfinderLabelRoom);
        LViewfinderBadge lBadge = LViewfinderText.LViewfinderBadgeResolve(
            lBand, lLabel, pBadgeText.Width, pBadgeText.Height, pNameText.Width, pNameText.Height);
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBadgeRead(lBand.LViewfinderBandColor),
            null,
            new Rect(
                new Point(lBadge.LViewfinderBadgeLeft, lBadge.LViewfinderBadgeTop),
                new Point(lBadge.LViewfinderBadgeRight, lBadge.LViewfinderBadgeBottom)),
            lBadge.LViewfinderBadgeRadius,
            lBadge.LViewfinderBadgeRadius);
        drawingContext.DrawText(pBadgeText, PViewfinderPointRead(lBadge.LViewfinderBadgeText));
        drawingContext.DrawText(pNameText, PViewfinderPointRead(lBadge.LViewfinderBadgeName));
    }

    private void PViewfinderCursorDraw(DrawingContext drawingContext, LViewfinderCursor lCursor)
    {
        FormattedText pTimeText = PViewfinderTextBuild(
            lCursor.LViewfinderCursorText, pViewfinderTickTypeface, 10, pViewfinderCursorBrush);
        LViewfinderChip lChip = LViewfinderText.LViewfinderChipResolve(
            lCursor, pTimeText.Width, pTimeText.Height, ActualWidth, ActualHeight);
        var pChipRect = new Rect(
            new Point(lChip.LViewfinderChipLeft, lChip.LViewfinderChipTop),
            new Point(lChip.LViewfinderChipRight, lChip.LViewfinderChipBottom));
        PCursor.PCursorDraw(
            drawingContext, lCursor.LViewfinderCursorX, LViewfinder.LViewfinderLaneHeight, ActualHeight, pChipRect);
        drawingContext.DrawRoundedRectangle(pTimecodeBackgroundBrush, pTimecodeBorderPen, pChipRect, 3, 3);
        drawingContext.DrawText(pTimeText, PViewfinderPointRead(lChip.LViewfinderChipText));
    }

    private FormattedText PViewfinderTextBuild(string pText, Typeface pTypeface, double pSize, Brush pBrush) => new(
        pText,
        CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        pTypeface,
        pSize,
        pBrush,
        VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private FormattedText PViewfinderNameBuild(string pName, double pRoom) => new(
        pName,
        CultureInfo.CurrentCulture,
        FlowDirection.LeftToRight,
        pViewfinderTickTypeface,
        PSection.PSectionNameSize,
        pViewfinderSectionBrush,
        VisualTreeHelper.GetDpi(this).PixelsPerDip)
    {
        MaxTextWidth = pRoom,
        MaxLineCount = 1,
        Trimming = TextTrimming.CharacterEllipsis
    };

    private static Rect PViewfinderRectRead(LViewfinderShape lShape) => new(
        new Point(lShape.LViewfinderShapeLeft, lShape.LViewfinderShapeTop),
        new Point(lShape.LViewfinderShapeRight, lShape.LViewfinderShapeBottom));

    private static Point PViewfinderPointRead(LViewfinderPoint lPoint) =>
        new(lPoint.LViewfinderPointX, lPoint.LViewfinderPointY);

    private static Brush PViewfinderBrushBuild(byte pAlpha, byte pRed, byte pGreen, byte pBlue)
    {
        var pBrush = new SolidColorBrush(Color.FromArgb(pAlpha, pRed, pGreen, pBlue));
        pBrush.Freeze();
        return pBrush;
    }

    private static Pen PViewfinderPenBuild(Brush pBrush, double pThickness)
    {
        var pPen = new Pen(pBrush, pThickness);
        pPen.Freeze();
        return pPen;
    }
}
