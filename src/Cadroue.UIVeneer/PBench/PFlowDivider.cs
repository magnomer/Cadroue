using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIVeneer.PBench;

public sealed partial class PFlow
{
    private const double PFlowHeightMinimum = 200;
    private const double PFlowHeightMaximum = 520;

    private Border? pDividerThumb;
    private double pDividerYOrigin;
    private double pDividerHeightOrigin;
    private bool pDividerState;

    private void PDividerAttach()
    {
        pDividerThumb = PDividerBuild();
        pDividerThumb.MouseLeftButtonDown += PDividerPressHandle;
        pDividerThumb.MouseMove += PDividerMoveHandle;
        pDividerThumb.MouseLeftButtonUp += PDividerReleaseHandle;
        pDividerThumb.LostMouseCapture += PDividerCaptureHandle;
    }

    private void PDividerDetach()
    {
        if (pDividerThumb is null) return;
        pDividerThumb.MouseLeftButtonDown -= PDividerPressHandle;
        pDividerThumb.MouseMove -= PDividerMoveHandle;
        pDividerThumb.MouseLeftButtonUp -= PDividerReleaseHandle;
        pDividerThumb.LostMouseCapture -= PDividerCaptureHandle;
    }

    private void PDividerPressHandle(object sender, MouseButtonEventArgs e)
    {
        Window? ownerWindow = Window.GetWindow(this);
        if (pDividerThumb is null || ownerWindow is null) return;
        pDividerState = true;
        pDividerYOrigin = e.GetPosition(ownerWindow).Y;
        pDividerHeightOrigin = ActualHeight;
        pDividerThumb.CaptureMouse();
        e.Handled = true;
    }

    private void PDividerMoveHandle(object sender, MouseEventArgs e)
    {
        if (!pDividerState) return;
        Window? ownerWindow = Window.GetWindow(this);
        if (ownerWindow is null) { PDividerClear(); return; }
        Height = Math.Clamp(
            pDividerHeightOrigin + pDividerYOrigin - e.GetPosition(ownerWindow).Y,
            PFlowHeightMinimum,
            PFlowHeightMaximum);
        e.Handled = true;
    }

    private void PDividerReleaseHandle(object sender, MouseButtonEventArgs e) { PDividerClear(); e.Handled = true; }

    private void PDividerCaptureHandle(object sender, MouseEventArgs e) => PDividerClear();

    private void PDividerClear()
    {
        pDividerState = false;
        if (pDividerThumb?.IsMouseCaptured == true) pDividerThumb.ReleaseMouseCapture();
    }
}
