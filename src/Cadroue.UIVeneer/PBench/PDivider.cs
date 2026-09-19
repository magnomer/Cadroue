using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PBench;

internal sealed class PDivider : Border
{
    private readonly FrameworkElement pDividerHost;
    private readonly LDivider lDivider = new();

    public PDivider(FrameworkElement pHost)
    {
        pDividerHost = pHost;
        Height = 8;
        Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
        Cursor = Cursors.SizeNS;
        MouseLeftButtonDown += PDividerPressHandle;
        MouseLeftButtonUp += PDividerReleaseHandle;
        LostMouseCapture += PDividerCaptureHandle;
    }

    public void PDividerDetach()
    {
        MouseLeftButtonDown -= PDividerPressHandle;
        MouseMove -= PDividerMoveHandle;
        MouseLeftButtonUp -= PDividerReleaseHandle;
        LostMouseCapture -= PDividerCaptureHandle;
    }

    private void PDividerPressHandle(object sender, MouseButtonEventArgs e)
    {
        lDivider.LDividerPressHandle(e.GetPosition(Window.GetWindow(this)).Y, pDividerHost.ActualHeight);
        MouseMove -= PDividerMoveHandle;
        MouseMove += PDividerMoveHandle;
        CaptureMouse();
        e.Handled = true;
    }

    private void PDividerMoveHandle(object sender, MouseEventArgs e)
    {
        pDividerHost.Height = lDivider.LDividerMoveResolve(
            e.GetPosition(Window.GetWindow(this)).Y, pDividerHost.ActualHeight);
        e.Handled = true;
    }

    private void PDividerReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        PDividerClear();
        e.Handled = true;
    }

    private void PDividerCaptureHandle(object sender, MouseEventArgs e) => PDividerClear();

    private void PDividerClear()
    {
        MouseMove -= PDividerMoveHandle;
        lDivider.LDividerRelease();
        ReleaseMouseCapture();
    }
}
