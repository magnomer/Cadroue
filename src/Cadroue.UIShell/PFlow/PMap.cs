using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Media;

namespace Cadroue.UIShell.PFlow;

public sealed class PMap : FrameworkElement
{
    private enum PMapDragMode
    {
        PMapDragNone,
        PMapDragResizeStart,
        PMapDragResizeEnd,
        PMapDragMove,
        PMapDragCursor,
    }

    private static readonly Brush pMapBrushBackground = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
    private static readonly Brush pMapBrushRail = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1));
    private static readonly Brush pMapBrushCoverageScanned = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pMapBrushNavigationShadow = new SolidColorBrush(Color.FromArgb(0x34, 0x00, 0x00, 0x00));
    private static readonly Brush pMapBrushNavigationFrame = new SolidColorBrush(Color.FromRgb(0x2D, 0x7D, 0xD2));
    private static readonly Brush pMapBrushNavigationFill = new SolidColorBrush(Color.FromArgb(0x9E, 0xE5, 0xEE, 0xF8));
    private static readonly Brush pMapBrushNavigationInner = new SolidColorBrush(Color.FromArgb(0x29, 0x0D, 0x47, 0xA1));
    private static readonly Brush pMapBrushNavigationMove = new SolidColorBrush(Color.FromRgb(0x3A, 0x8B, 0xE0));
    private static readonly Brush pMapBrushNavigationSide = new SolidColorBrush(Color.FromRgb(0x2D, 0x7D, 0xD2));
    private static readonly Brush pMapBrushNavigationSideInner = new SolidColorBrush(Color.FromArgb(0xCC, 0x17, 0x6B, 0xBE));
    private static readonly Brush pMapBrushNavigationGrip = new SolidColorBrush(Color.FromArgb(0xE0, 0xFF, 0xFF, 0xFF));
    private static readonly Pen pMapPenNavigationBorder = new(new SolidColorBrush(Color.FromArgb(0x8C, 0x0D, 0x47, 0xA1)), 1.2);
    private static readonly Pen pMapPenNavigationMoveBorder = new(new SolidColorBrush(Color.FromArgb(0x4D, 0x0D, 0x47, 0xA1)), 1.0);
    private static readonly Pen pMapPenNavigationShine = new(new SolidColorBrush(Color.FromArgb(0x42, 0xFF, 0xFF, 0xFF)), 1.0);
    private static readonly Pen pMapPenNavigationGrip = new(pMapBrushNavigationGrip, 1.6);
    private static readonly Pen pMapPenCursor = new(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)), 1.0);
    private const double PMapHandleWidth = 12;
    private const double PMapMinimumRenderHeight = 12;

    static PMap()
    {
        pMapBrushBackground.Freeze();
        pMapBrushRail.Freeze();
        pMapBrushCoverageScanned.Freeze();
        pMapBrushNavigationShadow.Freeze();
        pMapBrushNavigationFrame.Freeze();
        pMapBrushNavigationFill.Freeze();
        pMapBrushNavigationInner.Freeze();
        pMapBrushNavigationMove.Freeze();
        pMapBrushNavigationSide.Freeze();
        pMapBrushNavigationSideInner.Freeze();
        pMapBrushNavigationGrip.Freeze();
        pMapPenNavigationBorder.Freeze();
        pMapPenNavigationMoveBorder.Freeze();
        pMapPenNavigationShine.Freeze();
        pMapPenNavigationGrip.Freeze();
        pMapPenCursor.Freeze();
    }

    private LSpool? lSpool;
    private TimeSpan lCursor;
    private IReadOnlyList<LKeyframeScanRange> lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
    private PMapDragMode pMapDragMode;
    private double pMapDragStartX;
    private double pMapDragPreviousX;
    private TimeSpan lMapDragStartTime;

    public event Action<TimeSpan>? PMapCursorChange;
    public event Action? PMapSpoolChange;

    public void PMapAttach(LSpool spool, TimeSpan cursor)
    {
        lSpool = spool;
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        InvalidateVisual();
    }

    public void PMapCursorUpdate(TimeSpan cursor)
    {
        lCursor = cursor < TimeSpan.Zero ? TimeSpan.Zero : cursor;
        InvalidateVisual();
    }

    public void PMapClear()
    {
        lSpool = null;
        lCursor = TimeSpan.Zero;
        lKeyframeScannedRanges = Array.Empty<LKeyframeScanRange>();
        InvalidateVisual();
    }

    public void PMapSpoolUpdate() => InvalidateVisual();

    public void PMapKeyframesUpdate(IReadOnlyList<LKeyframeScanRange>? scannedRanges)
    {
        lKeyframeScannedRanges = scannedRanges ?? Array.Empty<LKeyframeScanRange>();
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        double actualWidth = ActualWidth;
        double actualHeight = ActualHeight;
        if (actualWidth <= 0 || actualHeight <= 0)
        {
            return;
        }

        drawingContext.DrawRectangle(pMapBrushBackground, null, new Rect(0, 0, actualWidth, actualHeight));
        if (lSpool is null || lSpool.LSpoolDuration <= TimeSpan.Zero || actualHeight < PMapMinimumRenderHeight)
        {
            return;
        }

        double coverageHeight = 3;
        double coverageBottom = Math.Max(0, actualHeight - 1);
        double coverageTop = Math.Max(0, coverageBottom - coverageHeight);
        double railTop = 3;
        double railBottom = Math.Max(railTop, coverageTop - 2);
        double railHeight = Math.Max(0, railBottom - railTop);
        if (railHeight <= 0)
        {
            return;
        }

        drawingContext.DrawRoundedRectangle(pMapBrushRail, null, new Rect(0, railTop, actualWidth, railHeight), 3, 3);
        PMapCoverageDraw(drawingContext, actualWidth, coverageTop, coverageHeight);

        double startRatio = Math.Clamp(lSpool.LSpoolRatioConvert(lSpool.LSpoolWorkingRangeStart), 0, 1);
        double endRatio = Math.Clamp(lSpool.LSpoolRatioConvert(lSpool.LSpoolWorkingRangeEnd), 0, 1);
        double spoolStartX = Math.Min(startRatio, endRatio) * actualWidth;
        double spoolEndX = Math.Max(startRatio, endRatio) * actualWidth;
        double spoolBodyWidth = Math.Max(0, spoolEndX - spoolStartX);
        if (spoolBodyWidth <= 0)
        {
            return;
        }

        Rect bodyRect = new(spoolStartX, railTop, spoolBodyWidth, railHeight);
        PMapNavigationDraw(drawingContext, bodyRect, actualWidth);

        double cursorRatio = Math.Clamp(lSpool.LSpoolRatioConvert(lCursor), 0, 1);
        double cursorX = cursorRatio * actualWidth;
        drawingContext.DrawLine(pMapPenCursor, new Point(cursorX, 0), new Point(cursorX, actualHeight));
    }

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

    private Cursor PMapCursorResolve(Point mousePoint)
    {
        if (lSpool is null || ActualWidth <= 0)
        {
            return Cursors.Arrow;
        }

        double actualWidth = ActualWidth;
        double spoolStartX = Math.Clamp(lSpool.LSpoolRatioConvert(lSpool.LSpoolWorkingRangeStart), 0, 1) * actualWidth;
        double spoolEndX = Math.Clamp(lSpool.LSpoolRatioConvert(lSpool.LSpoolWorkingRangeEnd), 0, 1) * actualWidth;
        if (spoolStartX > spoolEndX)
        {
            (spoolStartX, spoolEndX) = (spoolEndX, spoolStartX);
        }

        Rect bodyRect = PMapBodyResolve(spoolStartX, spoolEndX);
        Rect leftHandleRect = PMapLeftResolve(bodyRect);
        Rect rightHandleRect = PMapRightResolve(bodyRect);
        Rect moveHandleRect = PMapMoveResolve(bodyRect, Math.Min(PMapHandleWidth, bodyRect.Width), actualWidth);

        if (leftHandleRect.Contains(mousePoint) || rightHandleRect.Contains(mousePoint))
        {
            return Cursors.SizeWE;
        }

        return moveHandleRect.Contains(mousePoint) ? Cursors.SizeAll : Cursors.Hand;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (lSpool is null || ActualWidth <= 0)
        {
            return;
        }

        double mouseX = e.GetPosition(this).X;
        double actualWidth = ActualWidth;
        double spoolStartX = Math.Clamp(lSpool.LSpoolRatioConvert(lSpool.LSpoolWorkingRangeStart), 0, 1) * actualWidth;
        double spoolEndX = Math.Clamp(lSpool.LSpoolRatioConvert(lSpool.LSpoolWorkingRangeEnd), 0, 1) * actualWidth;
        if (spoolStartX > spoolEndX)
        {
            (spoolStartX, spoolEndX) = (spoolEndX, spoolStartX);
        }

        Rect bodyRect = PMapBodyResolve(spoolStartX, spoolEndX);
        Rect leftHandleRect = PMapLeftResolve(bodyRect);
        Rect rightHandleRect = PMapRightResolve(bodyRect);
        Rect moveHandleRect = PMapMoveResolve(bodyRect, Math.Min(PMapHandleWidth, bodyRect.Width), actualWidth);
        Point mousePoint = e.GetPosition(this);

        pMapDragStartX = mouseX;
        pMapDragPreviousX = mouseX;
        if (leftHandleRect.Contains(mousePoint))
        {
            pMapDragMode = PMapDragMode.PMapDragResizeStart;
            lMapDragStartTime = lSpool.LSpoolWorkingRangeStart;
        }
        else if (rightHandleRect.Contains(mousePoint))
        {
            pMapDragMode = PMapDragMode.PMapDragResizeEnd;
            lMapDragStartTime = lSpool.LSpoolWorkingRangeEnd;
        }
        else if (moveHandleRect.Contains(mousePoint))
        {
            pMapDragMode = PMapDragMode.PMapDragMove;
        }
        else
        {
            pMapDragMode = PMapDragMode.PMapDragCursor;
            PMapCursorChange?.Invoke(PMapRatioConvert(Math.Clamp(mouseX / actualWidth, 0, 1)));
        }

        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Point mousePoint = e.GetPosition(this);
        Cursor = PMapCursorResolve(mousePoint);
        if (lSpool is null || pMapDragMode == PMapDragMode.PMapDragNone || ActualWidth <= 0)
        {
            return;
        }

        double mouseX = mousePoint.X;
        double actualWidth = ActualWidth;
        double dragDeltaRatio = (mouseX - pMapDragStartX) / actualWidth;
        TimeSpan dragDeltaTime = lSpool.LSpoolTimeConvert(dragDeltaRatio);

        switch (pMapDragMode)
        {
            case PMapDragMode.PMapDragResizeStart:
                lSpool.LSpoolStartResize(lMapDragStartTime + dragDeltaTime);
                PMapSpoolChange?.Invoke();
                InvalidateVisual();
                break;
            case PMapDragMode.PMapDragResizeEnd:
                lSpool.LSpoolEndResize(lMapDragStartTime + dragDeltaTime);
                PMapSpoolChange?.Invoke();
                InvalidateVisual();
                break;
            case PMapDragMode.PMapDragMove:
                double moveDeltaRatio = (mouseX - pMapDragPreviousX) / actualWidth;
                lSpool.LSpoolMove(lSpool.LSpoolTimeConvert(moveDeltaRatio));
                pMapDragPreviousX = mouseX;
                PMapSpoolChange?.Invoke();
                InvalidateVisual();
                break;
            case PMapDragMode.PMapDragCursor:
                PMapCursorChange?.Invoke(PMapRatioConvert(Math.Clamp(mouseX / actualWidth, 0, 1)));
                break;
        }

        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (pMapDragMode == PMapDragMode.PMapDragNone)
        {
            Cursor = Cursors.Arrow;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        PMapDragEnd();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        PMapDragEnd();
    }

    private void PMapDragEnd()
    {
        pMapDragMode = PMapDragMode.PMapDragNone;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }
    }

    private TimeSpan PMapRatioConvert(double ratio)
        => lSpool?.LSpoolTimeConvert(ratio) ?? TimeSpan.Zero;
}
