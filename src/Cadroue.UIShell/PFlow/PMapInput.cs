using System.Windows;
using System.Windows.Input;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PMap
{
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
