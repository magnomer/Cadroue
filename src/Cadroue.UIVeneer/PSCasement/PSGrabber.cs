using System.Windows;
using Cadroue.UIDeportment;
using System.Windows.Input;

namespace Cadroue.UIVeneer;

internal sealed class PSGrabber
{
    private const int PSGrabberBorderPixels = 14;
    private const int PSGrabberLeft = 1;
    private const int PSGrabberRight = 2;
    private const int PSGrabberTop = 4;
    private const int PSGrabberBottom = 8;

    private readonly Window psGrabberWindow;
    private bool psGrabberActive;
    private int psGrabberDirection;
    private Point psGrabberStartPointer;
    private Rect psGrabberStartBounds;

    internal PSGrabber(Window pWindow)
    {
        psGrabberWindow = pWindow;
    }

    internal static void PSGrabberPlacementRestore(Window pWindow, string pPlacementKey)
    {
        bool pStored = Cadroue.UIDeportment.QPlacement.QPlacementRead(
            pPlacementKey, out double pLeftStored, out double pTopStored,
            out double pWidthStored, out double pHeightStored);
        (double? pLeft, double? pTop, double pWidth, double pHeight) = PSGrabberStoredResolve(
            pWindow, pStored, pLeftStored, pTopStored, pWidthStored, pHeightStored);
        QSashMaximize.QSashMaximizeAttach(pWindow);
        QSashPlacement.QSashPlacementRestore(pWindow, pLeft, pTop, pWidth, pHeight);
    }

    private static (double?, double?, double, double) PSGrabberStoredResolve(
        Window pWindow,
        bool pStored,
        double pLeft,
        double pTop,
        double pWidthStored,
        double pHeightStored)
    {
        double pWidth = Math.Max(pStored ? pWidthStored : pWindow.Width, pWindow.MinWidth);
        double pHeight = Math.Max(pStored ? pHeightStored : pWindow.Height, pWindow.MinHeight);
        return (pStored ? pLeft : null, pStored ? pTop : null, pWidth, pHeight);
    }

    internal static void PSGrabberPlacementSave(Window pWindow, string pPlacementKey)
    {
        Rect pBounds = PSGrabberRectResolve(pWindow);
        Cadroue.UIDeportment.QPlacement.QPlacementSave(
            pPlacementKey,
            pBounds.Left,
            pBounds.Top,
            pBounds.Width,
            pBounds.Height);
    }

    private static Rect PSGrabberRectResolve(Window pWindow) =>
        pWindow.WindowState == WindowState.Normal
            ? new Rect(pWindow.Left, pWindow.Top, pWindow.Width, pWindow.Height)
            : pWindow.RestoreBounds;

    internal void PSGrabberAttach()
    {
        psGrabberWindow.PreviewMouseMove += PSGrabberMoveHandle;
        psGrabberWindow.PreviewMouseLeftButtonDown += PSGrabberPressHandle;
        psGrabberWindow.PreviewMouseLeftButtonUp += PSGrabberReleaseHandle;
        psGrabberWindow.LostMouseCapture += PSGrabberCaptureHandle;
    }

    internal void PSGrabberDetach()
    {
        psGrabberWindow.PreviewMouseMove -= PSGrabberMoveHandle;
        psGrabberWindow.PreviewMouseLeftButtonDown -= PSGrabberPressHandle;
        psGrabberWindow.PreviewMouseLeftButtonUp -= PSGrabberReleaseHandle;
        psGrabberWindow.LostMouseCapture -= PSGrabberCaptureHandle;
        PSGrabberCursorApply(0);
    }

    private void PSGrabberPressHandle(object sender, MouseButtonEventArgs e)
    {
        int pDirection = PSGrabberDirectionRead(e.GetPosition(psGrabberWindow));
        if (psGrabberWindow.WindowState != WindowState.Normal
            || pDirection == 0
            || QSashGuard.QSashGuardCheck(e.OriginalSource as DependencyObject))
        {
            return;
        }

        PSGrabberStartSet(pDirection, e);
        PSGrabberCursorApply(pDirection);
        PSGrabberPressAccept(e);
    }

    private void PSGrabberStartSet(int pDirection, MouseButtonEventArgs e)
    {
        psGrabberActive = true;
        psGrabberDirection = pDirection;
        psGrabberStartPointer = PSGrabberPointerRead(e);
        psGrabberStartBounds = new Rect(
            psGrabberWindow.Left,
            psGrabberWindow.Top,
            psGrabberWindow.ActualWidth,
            psGrabberWindow.ActualHeight);
    }

    private void PSGrabberPressAccept(MouseButtonEventArgs e)
    {
        Mouse.Capture(psGrabberWindow);
        e.Handled = true;
    }

    private void PSGrabberMoveHandle(object sender, MouseEventArgs e)
    {
        if (psGrabberActive)
        {
            PSGrabberDragAccept(e);
            return;
        }

        PSGrabberCursorApply(PSGrabberHoverRead(e));
    }

    private void PSGrabberDragAccept(MouseEventArgs e)
    {
        PSGrabberApply(PSGrabberPointerRead(e));
        e.Handled = true;
    }

    private int PSGrabberHoverRead(MouseEventArgs e) =>
        psGrabberWindow.WindowState == WindowState.Normal
            && !QSashGuard.QSashGuardCheck(e.OriginalSource as DependencyObject)
            ? PSGrabberDirectionRead(e.GetPosition(psGrabberWindow))
            : 0;

    private void PSGrabberCursorApply(int pDirection)
    {
        psGrabberWindow.Cursor = pDirection == 0 ? null : PSGrabberCursorRead(pDirection);
        psGrabberWindow.ForceCursor = pDirection != 0;
    }

    private void PSGrabberReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        if (!psGrabberActive)
        {
            return;
        }

        PSGrabberActiveSet(false);
        PSGrabberReleaseAccept(e);
    }

    private void PSGrabberActiveSet(bool pActive) => psGrabberActive = pActive;

    private static void PSGrabberReleaseAccept(MouseButtonEventArgs e)
    {
        Mouse.Capture(null);
        e.Handled = true;
    }

    private void PSGrabberCaptureHandle(object sender, MouseEventArgs e)
    {
        PSGrabberActiveSet(false);
        PSGrabberCursorApply(0);
    }

    private int PSGrabberDirectionRead(Point pPoint)
    {
        double pWidth = psGrabberWindow.ActualWidth;
        double pHeight = psGrabberWindow.ActualHeight;
        bool pLeft = pPoint.X >= 0 && pPoint.X < PSGrabberBorderPixels;
        bool pRight = pPoint.X <= pWidth && pPoint.X > pWidth - PSGrabberBorderPixels;
        bool pTop = pPoint.Y >= 0 && pPoint.Y < PSGrabberBorderPixels;
        bool pBottom = pPoint.Y <= pHeight && pPoint.Y > pHeight - PSGrabberBorderPixels;
        int pDirection = 0;
        if (pLeft) pDirection |= PSGrabberLeft;
        if (pRight) pDirection |= PSGrabberRight;
        if (pTop) pDirection |= PSGrabberTop;
        if (pBottom) pDirection |= PSGrabberBottom;
        return pDirection;
    }

    private static Cursor PSGrabberCursorRead(int pDirection)
    {
        bool pHorizontal = (pDirection & (PSGrabberLeft | PSGrabberRight)) != 0;
        bool pVertical = (pDirection & (PSGrabberTop | PSGrabberBottom)) != 0;
        if (!pHorizontal || !pVertical)
        {
            return pHorizontal ? Cursors.SizeWE : Cursors.SizeNS;
        }

        bool pLeft = (pDirection & PSGrabberLeft) != 0;
        bool pTop = (pDirection & PSGrabberTop) != 0;
        return pLeft == pTop ? Cursors.SizeNWSE : Cursors.SizeNESW;
    }

    private Point PSGrabberPointerRead(MouseEventArgs e)
    {
        Point pScreenPoint = psGrabberWindow.PointToScreen(e.GetPosition(psGrabberWindow));
        return PresentationSource.FromVisual(psGrabberWindow)?.CompositionTarget?.TransformFromDevice
            .Transform(pScreenPoint)
            ?? pScreenPoint;
    }

    private void PSGrabberApply(Point pPointer)
    {
        (double pLeft, double pTop, double pWidth, double pHeight) = PSGrabberDragResolve(pPointer);
        PSGrabberBoundsSet(pLeft, pTop, pWidth, pHeight);
    }

    private (double, double, double, double) PSGrabberDragResolve(Point pPointer)
    {
        double pDx = pPointer.X - psGrabberStartPointer.X;
        double pDy = pPointer.Y - psGrabberStartPointer.Y;
        double pLeft = psGrabberStartBounds.Left;
        double pTop = psGrabberStartBounds.Top;
        double pWidth = psGrabberStartBounds.Width;
        double pHeight = psGrabberStartBounds.Height;

        if ((psGrabberDirection & PSGrabberLeft) != 0)
        {
            pWidth = Math.Max(psGrabberWindow.MinWidth, psGrabberStartBounds.Width - pDx);
            pLeft = psGrabberStartBounds.Right - pWidth;
        }

        if ((psGrabberDirection & PSGrabberRight) != 0)
        {
            pWidth = Math.Max(psGrabberWindow.MinWidth, psGrabberStartBounds.Width + pDx);
        }

        if ((psGrabberDirection & PSGrabberTop) != 0)
        {
            pHeight = Math.Max(psGrabberWindow.MinHeight, psGrabberStartBounds.Height - pDy);
            pTop = psGrabberStartBounds.Bottom - pHeight;
        }

        if ((psGrabberDirection & PSGrabberBottom) != 0)
        {
            pHeight = Math.Max(psGrabberWindow.MinHeight, psGrabberStartBounds.Height + pDy);
        }

        return (pLeft, pTop, pWidth, pHeight);
    }

    private void PSGrabberBoundsSet(double pLeft, double pTop, double pWidth, double pHeight)
    {
        psGrabberWindow.Left = pLeft;
        psGrabberWindow.Top = pTop;
        psGrabberWindow.Width = pWidth;
        psGrabberWindow.Height = pHeight;
    }
}
