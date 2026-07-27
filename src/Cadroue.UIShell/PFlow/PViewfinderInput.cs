using System.Windows;
using System.Windows.Input;
using Cadroue.Media;

namespace Cadroue.UIShell.PFlow;

public sealed partial class PViewfinder
{
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (lSpool is null || ActualWidth <= 0)
        {
            return;
        }

        TimeSpan requestTime = PViewfinderPositionConvert(e.GetPosition(this).X);
        PViewfinderSelectPropagate(requestTime);
        pViewfinderDragMode = PViewfinderDragMode.PViewfinderDragCursor;
        PViewfinderCursorChange?.Invoke(requestTime);
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (pViewfinderDragMode == PViewfinderDragMode.PViewfinderDragNone || lSpool is null || ActualWidth <= 0)
        {
            return;
        }

        PViewfinderCursorChange?.Invoke(PViewfinderPositionConvert(e.GetPosition(this).X));
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        pViewfinderDragMode = PViewfinderDragMode.PViewfinderDragNone;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        pViewfinderDragMode = PViewfinderDragMode.PViewfinderDragNone;
    }

    private void PViewfinderSelectPropagate(TimeSpan requestTime)
    {
        for (int index = 0; index < lSectionList.Count; index++)
        {
            LSegment section = lSectionList[index];
            if (requestTime >= section.LSegmentStart && requestTime <= section.LSegmentEnd)
            {
                PViewfinderSectionSelect?.Invoke(index);
                return;
            }
        }
    }

    private TimeSpan PViewfinderPositionConvert(double mouseX)
    {
        if (lSpool is null || ActualWidth <= 0)
        {
            return TimeSpan.Zero;
        }

        double clampedMouseX = Math.Clamp(mouseX, 0, ActualWidth);
        double ratio = Math.Clamp(clampedMouseX / ActualWidth, 0, 1);
        TimeSpan rangeDuration = lSpool.LSpoolWorkingRangeEnd - lSpool.LSpoolWorkingRangeStart;
        if (rangeDuration <= TimeSpan.Zero)
        {
            return lSpool.LSpoolWorkingRangeStart;
        }

        return lSpool.LSpoolWorkingRangeStart + TimeSpan.FromSeconds(ratio * rangeDuration.TotalSeconds);
    }
}
