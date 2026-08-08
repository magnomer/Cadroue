using System.Windows;
using System.Windows.Input;

using Cadroue.Core;

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
        double spoolStartX = Math.Clamp(lSpool.LSpoolRatioResolve(lSpool.LSpoolRangeOrigin), 0, 1) * actualWidth;
        double spoolEndX = Math.Clamp(lSpool.LSpoolRatioResolve(lSpool.LSpoolRangeLimit), 0, 1) * actualWidth;
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
        double spoolStartX = Math.Clamp(lSpool.LSpoolRatioResolve(lSpool.LSpoolRangeOrigin), 0, 1) * actualWidth;
        double spoolEndX = Math.Clamp(lSpool.LSpoolRatioResolve(lSpool.LSpoolRangeLimit), 0, 1) * actualWidth;
        if (spoolStartX > spoolEndX)
        {
            (spoolStartX, spoolEndX) = (spoolEndX, spoolStartX);
        }

        Rect bodyRect = PMapBodyResolve(spoolStartX, spoolEndX);
        Rect leftHandleRect = PMapLeftResolve(bodyRect);
        Rect rightHandleRect = PMapRightResolve(bodyRect);
        Rect moveHandleRect = PMapMoveResolve(bodyRect, Math.Min(PMapHandleWidth, bodyRect.Width), actualWidth);
        Point mousePoint = e.GetPosition(this);

        pMapDragX = mouseX;
        pMapPreviousX = mouseX;
        if (leftHandleRect.Contains(mousePoint))
        {
            pMapDragMode = PMapDragMode.PMapResizeStart;
            lMapDragTime = lSpool.LSpoolRangeOrigin;
        }
        else if (rightHandleRect.Contains(mousePoint))
        {
            pMapDragMode = PMapDragMode.PMapResizeEnd;
            lMapDragTime = lSpool.LSpoolRangeLimit;
        }
        else if (moveHandleRect.Contains(mousePoint))
        {
            pMapDragMode = PMapDragMode.PMapDragMove;
        }
        else
        {
            pMapDragMode = PMapDragMode.PMapDragCursor;
            PMapCursorChange?.Invoke(PMapRatioResolve(Math.Clamp(mouseX / actualWidth, 0, 1)));
        }

        PMapDragChange?.Invoke(true);
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
        double dragDeltaRatio = (mouseX - pMapDragX) / actualWidth;
        TimeSpan dragDeltaTime = lSpool.LSpoolTimeResolve(dragDeltaRatio);

        switch (pMapDragMode)
        {
            case PMapDragMode.PMapResizeStart:
                lSpool.LSpoolStartSet(lMapDragTime + dragDeltaTime);
                PMapSpoolChange?.Invoke();
                InvalidateVisual();
                break;
            case PMapDragMode.PMapResizeEnd:
                lSpool.LSpoolEndSet(lMapDragTime + dragDeltaTime);
                PMapSpoolChange?.Invoke();
                InvalidateVisual();
                break;
            case PMapDragMode.PMapDragMove:
                double moveDeltaRatio = (mouseX - pMapPreviousX) / actualWidth;
                lSpool.LSpoolMove(lSpool.LSpoolTimeResolve(moveDeltaRatio));
                pMapPreviousX = mouseX;
                PMapSpoolChange?.Invoke();
                InvalidateVisual();
                break;
            case PMapDragMode.PMapDragCursor:
                PMapCursorChange?.Invoke(PMapRatioResolve(Math.Clamp(mouseX / actualWidth, 0, 1)));
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
        PMapDragClear();
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        PMapDragClear();
    }

    private void PMapDragClear()
    {
        bool pMapDragging = pMapDragMode != PMapDragMode.PMapDragNone;
        pMapDragMode = PMapDragMode.PMapDragNone;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        if (pMapDragging)
        {
            PMapDragChange?.Invoke(false);
        }
    }

    private TimeSpan PMapRatioResolve(double ratio)
        => lSpool?.LSpoolTimeResolve(ratio) ?? TimeSpan.Zero;
}
