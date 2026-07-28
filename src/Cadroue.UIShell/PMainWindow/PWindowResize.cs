using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainWindow;

public partial class PWindow
{
    private void PResizeHandlersAdd()
    {
        PreviewMouseMove += PResizeMoveHandle;
        PreviewMouseLeftButtonDown += PResizePressHandle;
        PreviewMouseLeftButtonUp += PResizeReleaseHandle;
        LostMouseCapture += PResizeCaptureHandle;
        MouseLeave += PResizeLeaveHandle;
    }

    private void PResizeHandlersRemove()
    {
        PreviewMouseMove -= PResizeMoveHandle;
        PreviewMouseLeftButtonDown -= PResizePressHandle;
        PreviewMouseLeftButtonUp -= PResizeReleaseHandle;
        LostMouseCapture -= PResizeCaptureHandle;
        MouseLeave -= PResizeLeaveHandle;
        Mouse.OverrideCursor = null;
    }

    private void PResizePressHandle(object sender, MouseButtonEventArgs e)
    {
        int pDirection = PResizeDirectionRead(e.GetPosition(this));
        if (WindowState != WindowState.Normal || pDirection == 0)
        {
            return;
        }

        pResizeActive = true;
        pResizeDirection = pDirection;
        pResizeStartPointer = PResizePointerRead(e);
        pResizeStartBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        Mouse.Capture(this);
        e.Handled = true;
    }

    private void PResizeMoveHandle(object sender, MouseEventArgs e)
    {
        if (pResizeActive)
        {
            PResizeApply(PResizePointerRead(e));
            e.Handled = true;
            return;
        }

        int pDirection = WindowState == WindowState.Normal ? PResizeDirectionRead(e.GetPosition(this)) : 0;
        Mouse.OverrideCursor = pDirection == 0 ? null : PResizeCursorRead(pDirection);
    }

    private void PResizeLeaveHandle(object sender, MouseEventArgs e)
    {
        if (!pResizeActive)
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void PResizeReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        if (!pResizeActive)
        {
            return;
        }

        pResizeActive = false;
        RenderOptions.ProcessRenderMode = RenderMode.Default;
        Mouse.Capture(null);
        e.Handled = true;
    }

    private void PResizeCaptureHandle(object sender, MouseEventArgs e)
    {
        pResizeActive = false;
        RenderOptions.ProcessRenderMode = RenderMode.Default;
        Mouse.OverrideCursor = null;
    }

    private int PResizeDirectionRead(Point pPoint)
    {
        bool pLeft = pPoint.X >= 0 && pPoint.X < PResizeBorderPixels;
        bool pRight = pPoint.X <= ActualWidth && pPoint.X > ActualWidth - PResizeBorderPixels;
        bool pTop = pPoint.Y >= 0 && pPoint.Y < PResizeBorderPixels;
        bool pBottom = pPoint.Y <= ActualHeight && pPoint.Y > ActualHeight - PResizeBorderPixels;
        int pDirection = 0;
        if (pLeft) pDirection |= PResizeLeft;
        if (pRight) pDirection |= PResizeRight;
        if (pTop) pDirection |= PResizeTop;
        if (pBottom) pDirection |= PResizeBottom;
        return pDirection;
    }

    private static Cursor PResizeCursorRead(int pDirection)
    {
        bool pHorizontal = (pDirection & (PResizeLeft | PResizeRight)) != 0;
        bool pVertical = (pDirection & (PResizeTop | PResizeBottom)) != 0;
        if (!pHorizontal || !pVertical)
        {
            return pHorizontal ? Cursors.SizeWE : Cursors.SizeNS;
        }

        bool pLeft = (pDirection & PResizeLeft) != 0;
        bool pTop = (pDirection & PResizeTop) != 0;
        return pLeft == pTop ? Cursors.SizeNWSE : Cursors.SizeNESW;
    }

    private Point PResizePointerRead(MouseEventArgs e)
    {
        Point pScreenPoint = PointToScreen(e.GetPosition(this));
        return PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice.Transform(pScreenPoint) ?? pScreenPoint;
    }

    private void PResizeApply(Point pPointer)
    {
        double pDx = pPointer.X - pResizeStartPointer.X;
        double pDy = pPointer.Y - pResizeStartPointer.Y;
        double pLeft = pResizeStartBounds.Left;
        double pTop = pResizeStartBounds.Top;
        double pWidth = pResizeStartBounds.Width;
        double pHeight = pResizeStartBounds.Height;
        if ((pResizeDirection & PResizeLeft) != 0)
        {
            pWidth = Math.Max(MinWidth, pResizeStartBounds.Width - pDx);
            pLeft = pResizeStartBounds.Right - pWidth;
        }

        if ((pResizeDirection & PResizeRight) != 0)
        {
            pWidth = Math.Max(MinWidth, pResizeStartBounds.Width + pDx);
        }

        if ((pResizeDirection & PResizeTop) != 0)
        {
            pHeight = Math.Max(MinHeight, pResizeStartBounds.Height - pDy);
            pTop = pResizeStartBounds.Bottom - pHeight;
        }

        if ((pResizeDirection & PResizeBottom) != 0)
        {
            pHeight = Math.Max(MinHeight, pResizeStartBounds.Height + pDy);
        }

        Left = pLeft;
        Top = pTop;
        Width = pWidth;
        Height = pHeight;
    }
}
