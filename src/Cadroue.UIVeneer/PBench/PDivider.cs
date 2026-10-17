using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PBench;

internal sealed class PDivider : Border
{
    private readonly FrameworkElement pDividerHost;
    private readonly QDivider lDivider;

    public PDivider(FrameworkElement pHost, QDivider lOwner)
    {
        pDividerHost = pHost;
        lDivider = lOwner;
        PDividerLookApply();
        PDividerAttach();
    }

    private void PDividerLookApply()
    {
        Height = 8;
        Background = new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3));
        Cursor = Cursors.SizeNS;
    }

    private void PDividerAttach()
    {
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
        lDivider.QDividerPressHandle(e.GetPosition(Window.GetWindow(this)).Y, pDividerHost.ActualHeight);
        PDividerMoveAttach();
        CaptureMouse();
        e.Handled = true;
    }

    private void PDividerMoveAttach()
    {
        MouseMove -= PDividerMoveHandle;
        MouseMove += PDividerMoveHandle;
    }

    private void PDividerMoveHandle(object sender, MouseEventArgs e)
    {
        pDividerHost.Height = lDivider.QDividerMoveResolve(
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
        PDividerMoveDetach();
        lDivider.QDividerRelease();
        ReleaseMouseCapture();
    }

    private void PDividerMoveDetach() => MouseMove -= PDividerMoveHandle;
}
