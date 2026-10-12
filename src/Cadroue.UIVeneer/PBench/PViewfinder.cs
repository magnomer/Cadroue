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

    private static readonly IReadOnlyDictionary<QViewfinderShapeKind, Brush> pViewfinderBrushes =
        new Dictionary<QViewfinderShapeKind, Brush>
        {
            [QViewfinderShapeKind.QViewfinderShapeBackground] = pViewfinderBrushBackground,
            [QViewfinderShapeKind.QViewfinderShapeRail] = pViewfinderBrushRail,
            [QViewfinderShapeKind.QViewfinderShapeWaveform] = pViewfinderWaveformBrush,
            [QViewfinderShapeKind.QViewfinderShapeCoverage] = pViewfinderBrushRail,
            [QViewfinderShapeKind.QViewfinderShapeScanned] = pViewfinderCoverageBrush,
            [QViewfinderShapeKind.QViewfinderShapeKeyframe] = pViewfinderBrushKeyframe,
        };

    private static readonly IReadOnlyDictionary<QViewfinderShapeKind, double> pViewfinderRadii =
        new Dictionary<QViewfinderShapeKind, double>
        {
            [QViewfinderShapeKind.QViewfinderShapeBackground] = 0,
            [QViewfinderShapeKind.QViewfinderShapeRail] = 3,
            [QViewfinderShapeKind.QViewfinderShapeWaveform] = 3,
            [QViewfinderShapeKind.QViewfinderShapeCoverage] = 0,
            [QViewfinderShapeKind.QViewfinderShapeScanned] = 0,
            [QViewfinderShapeKind.QViewfinderShapeKeyframe] = 0,
        };

    public QViewfinder QViewfinder { get; }

    public PViewfinder(QViewfinder lViewfinder)
    {
        QViewfinder = lViewfinder;
        QViewfinder.QViewfinderFrameApplied += InvalidateVisual;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        QViewfinder.QViewfinderDrawStart();
        QViewfinderFrame lFrame = QViewfinder.QViewfinderFrameResolve(ActualWidth, ActualHeight);
        lFrame.QViewfinderFrameUnder.ToList().ForEach(lShape => PViewfinderShapeDraw(drawingContext, lShape));
        drawingContext.DrawGeometry(
            pViewfinderBrushWaveform, null, PFlow.PFlowWaveformBuild(lFrame.QViewfinderFrameWaveform));
        lFrame.QViewfinderFrameTicks.ToList().ForEach(lTick => PViewfinderTickDraw(drawingContext, lTick));
        lFrame.QViewfinderFrameBands.ToList().ForEach(lBand => PViewfinderBandDraw(drawingContext, lBand));
        lFrame.QViewfinderFrameOver.ToList().ForEach(lShape => PViewfinderShapeDraw(drawingContext, lShape));
        PViewfinderKeyframesDraw(drawingContext, lFrame.QViewfinderFrameKeyframes);
        lFrame.QViewfinderFrameCursor.ToList().ForEach(lCursor => PViewfinderCursorDraw(drawingContext, lCursor));
        QViewfinder.QViewfinderDrawRecord();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        e.Handled = QViewfinder.QViewfinderPressHandle(e.GetPosition(this).X, ActualWidth);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        e.Handled = QViewfinder.QViewfinderMoveHandle(e.GetPosition(this).X, ActualWidth);
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        QViewfinder.QViewfinderDragClear();
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        QViewfinder.QViewfinderDragClear();
    }

    private static void PViewfinderShapeDraw(DrawingContext drawingContext, QViewfinderShape lShape) =>
        drawingContext.DrawRoundedRectangle(
            pViewfinderBrushes[lShape.QViewfinderShapeKind],
            null,
            PViewfinderRectRead(lShape),
            pViewfinderRadii[lShape.QViewfinderShapeKind],
            pViewfinderRadii[lShape.QViewfinderShapeKind]);

    private static void PViewfinderKeyframesDraw(DrawingContext drawingContext, IReadOnlyList<QViewfinderShape> lMarks)
    {
        var pGuidelines = new GuidelineSet();
        lMarks.ToList().ForEach(lMark => pGuidelines.GuidelinesX.Add(lMark.QViewfinderShapeLeft));
        lMarks.ToList().ForEach(lMark => pGuidelines.GuidelinesX.Add(lMark.QViewfinderShapeRight));
        pGuidelines.Freeze();
        drawingContext.PushGuidelineSet(pGuidelines);
        lMarks.ToList().ForEach(lMark => PViewfinderShapeDraw(drawingContext, lMark));
        drawingContext.Pop();
    }

    private void PViewfinderTickDraw(DrawingContext drawingContext, QViewfinderGraduation lTick)
    {
        drawingContext.DrawLine(
            pViewfinderTickPen,
            new Point(lTick.QViewfinderTickX, lTick.QViewfinderTickTop),
            new Point(lTick.QViewfinderTickX, lTick.QViewfinderTickBottom));
        FormattedText pLabel = PViewfinderTextBuild(
            lTick.QViewfinderTickLabel, pViewfinderTickTypeface, 9, pViewfinderTickBrush);
        QViewfinderPoint lPoint = QViewfinderText.QViewfinderTickResolve(lTick, pLabel.Height);
        drawingContext.DrawText(pLabel, new Point(lPoint.QViewfinderPointX, lPoint.QViewfinderPointY));
    }

    private void PViewfinderBandDraw(DrawingContext drawingContext, QViewfinderBand lBand)
    {
        FormattedText pBadgeText = PViewfinderTextBuild(
            lBand.QViewfinderBandBadge, pViewfinderBadgeTypeface, PSection.PSectionNameSize, pViewfinderBadgeBrush);
        drawingContext.PushOpacity(PLook.PLookOpacity[lBand.QViewfinderBandShown]);
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBandRead(lBand.QViewfinderBandColor),
            pViewfinderSelectedPens[lBand.QViewfinderBandSelected],
            new Rect(
                new Point(lBand.QViewfinderBandLeft, lBand.QViewfinderBandTop),
                new Point(lBand.QViewfinderBandRight, lBand.QViewfinderBandBottom)),
            3,
            3);
        QViewfinderText.QViewfinderLabelResolve(lBand, pBadgeText.Width, pBadgeText.Height)
            .ToList()
            .ForEach(lLabel => PViewfinderLabelDraw(drawingContext, lBand, lLabel, pBadgeText));
        drawingContext.Pop();
    }

    private void PViewfinderLabelDraw(
        DrawingContext drawingContext, QViewfinderBand lBand, QViewfinderLabel lLabel, FormattedText pBadgeText)
    {
        FormattedText pNameText = PViewfinderNameBuild(lLabel.QViewfinderLabelName, lLabel.QViewfinderLabelRoom);
        QViewfinderBadge lBadge = QViewfinderText.QViewfinderBadgeResolve(
            lBand, lLabel, pBadgeText.Width, pBadgeText.Height, pNameText.Width, pNameText.Height);
        drawingContext.DrawRoundedRectangle(
            PSectionPalette.PSectionBadgeRead(lBand.QViewfinderBandColor),
            null,
            new Rect(
                new Point(lBadge.QViewfinderBadgeLeft, lBadge.QViewfinderBadgeTop),
                new Point(lBadge.QViewfinderBadgeRight, lBadge.QViewfinderBadgeBottom)),
            lBadge.QViewfinderBadgeRadius,
            lBadge.QViewfinderBadgeRadius);
        drawingContext.DrawText(pBadgeText, PViewfinderPointRead(lBadge.QViewfinderBadgeText));
        drawingContext.DrawText(pNameText, PViewfinderPointRead(lBadge.QViewfinderBadgeName));
    }

    private void PViewfinderCursorDraw(DrawingContext drawingContext, QViewfinderCursor lCursor)
    {
        FormattedText pTimeText = PViewfinderTextBuild(
            lCursor.QViewfinderCursorText, pViewfinderTickTypeface, 10, pViewfinderCursorBrush);
        QViewfinderChip lChip = QViewfinderText.QViewfinderChipResolve(
            lCursor, pTimeText.Width, pTimeText.Height, ActualWidth, ActualHeight);
        var pChipRect = new Rect(
            new Point(lChip.QViewfinderChipLeft, lChip.QViewfinderChipTop),
            new Point(lChip.QViewfinderChipRight, lChip.QViewfinderChipBottom));
        PCursor.PCursorDraw(
            drawingContext, lCursor.QViewfinderCursorX, QViewfinder.QViewfinderLaneHeight, ActualHeight, pChipRect);
        drawingContext.DrawRoundedRectangle(pTimecodeBackgroundBrush, pTimecodeBorderPen, pChipRect, 3, 3);
        drawingContext.DrawText(pTimeText, PViewfinderPointRead(lChip.QViewfinderChipText));
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

    private static Rect PViewfinderRectRead(QViewfinderShape lShape) => new(
        new Point(lShape.QViewfinderShapeLeft, lShape.QViewfinderShapeTop),
        new Point(lShape.QViewfinderShapeRight, lShape.QViewfinderShapeBottom));

    private static Point PViewfinderPointRead(QViewfinderPoint lPoint) =>
        new(lPoint.QViewfinderPointX, lPoint.QViewfinderPointY);

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
