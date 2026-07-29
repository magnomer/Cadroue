using System.Windows;
using System.Windows.Input;

namespace Cadroue.UIShell.PSShared;

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
        if (Cadroue.Core.LPlacement.LPlacementRead(pPlacementKey) is not { } pPlacement)
        {
            return;
        }

        double pWidth = Math.Max(pPlacement.Width, pWindow.MinWidth);
        double pHeight = Math.Max(pPlacement.Height, pWindow.MinHeight);
        double pScreenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        double pScreenBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;

        pWindow.Width = pWidth;
        pWindow.Height = pHeight;
        if (pPlacement.Left < SystemParameters.VirtualScreenLeft
            || pPlacement.Top < SystemParameters.VirtualScreenTop
            || pPlacement.Left + 100 > pScreenRight
            || pPlacement.Top + 40 > pScreenBottom)
        {
            return;
        }

        pWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        pWindow.Left = pPlacement.Left;
        pWindow.Top = pPlacement.Top;
    }

    internal static void PSGrabberPlacementSave(Window pWindow, string pPlacementKey)
    {
        Rect pBounds = pWindow.WindowState == WindowState.Normal
            ? new Rect(pWindow.Left, pWindow.Top, pWindow.Width, pWindow.Height)
            : pWindow.RestoreBounds;
        Cadroue.Core.LPlacement.LPlacementSave(pPlacementKey, pBounds.Left, pBounds.Top, pBounds.Width, pBounds.Height);
    }

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
    }

    private void PSGrabberPressHandle(object sender, MouseButtonEventArgs e)
    {
        int pDirection = PSGrabberDirectionRead(e.GetPosition(psGrabberWindow));
        if (psGrabberWindow.WindowState != WindowState.Normal || pDirection == 0)
        {
            return;
        }

        psGrabberActive = true;
        psGrabberDirection = pDirection;
        psGrabberStartPointer = PSGrabberPointerRead(e);
        psGrabberStartBounds = new Rect(
            psGrabberWindow.Left,
            psGrabberWindow.Top,
            psGrabberWindow.ActualWidth,
            psGrabberWindow.ActualHeight);
        Mouse.Capture(psGrabberWindow);
        e.Handled = true;
    }

    private void PSGrabberMoveHandle(object sender, MouseEventArgs e)
    {
        if (psGrabberActive)
        {
            PSGrabberApply(PSGrabberPointerRead(e));
            e.Handled = true;
            return;
        }

        int pDirection = psGrabberWindow.WindowState == WindowState.Normal
            ? PSGrabberDirectionRead(e.GetPosition(psGrabberWindow))
            : 0;
        psGrabberWindow.Cursor = pDirection == 0 ? null : PSGrabberCursorRead(pDirection);
    }

    private void PSGrabberReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        if (!psGrabberActive)
        {
            return;
        }

        psGrabberActive = false;
        Mouse.Capture(null);
        e.Handled = true;
    }

    private void PSGrabberCaptureHandle(object sender, MouseEventArgs e)
    {
        psGrabberActive = false;
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
        return PresentationSource.FromVisual(psGrabberWindow)?.CompositionTarget?.TransformFromDevice.Transform(pScreenPoint)
            ?? pScreenPoint;
    }

    private void PSGrabberApply(Point pPointer)
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

        psGrabberWindow.Left = pLeft;
        psGrabberWindow.Top = pTop;
        psGrabberWindow.Width = pWidth;
        psGrabberWindow.Height = pHeight;
    }
}
