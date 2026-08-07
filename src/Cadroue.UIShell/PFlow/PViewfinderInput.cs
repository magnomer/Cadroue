using System.Windows;
using System.Windows.Input;
using Cadroue.Core;
using Cadroue.Media;

using Cadroue.MigrationInterface;

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

        TimeSpan requestTime = PViewfinderPositionResolve(e.GetPosition(this).X);
        PViewfinderSelectPropagate(requestTime);
        pViewfinderDragMode = PViewfinderDragMode.PViewfinderDragCursor;
        PViewfinderDragChange?.Invoke(true);
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

        PViewfinderCursorChange?.Invoke(PViewfinderPositionResolve(e.GetPosition(this).X));
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        PViewfinderDragClear();
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        PViewfinderDragClear();
    }

    private void PViewfinderDragClear()
    {
        bool pViewfinderDragging = pViewfinderDragMode != PViewfinderDragMode.PViewfinderDragNone;
        pViewfinderDragMode = PViewfinderDragMode.PViewfinderDragNone;
        if (pViewfinderDragging)
        {
            PViewfinderDragChange?.Invoke(false);
        }
    }

    private void PViewfinderSelectPropagate(TimeSpan requestTime)
    {
        for (int index = lSectionList.Count - 1; index >= 0; index--)
        {
            LPiece section = lSectionList[index];
            if (requestTime >= section.LPieceStart && requestTime <= section.LPieceEnd)
            {
                PViewfinderSectionSelect?.Invoke(index);
                return;
            }
        }
    }

    private TimeSpan PViewfinderPositionResolve(double mouseX)
    {
        if (lSpool is null || ActualWidth <= 0)
        {
            return TimeSpan.Zero;
        }

        double clampedMouseX = Math.Clamp(mouseX, 0, ActualWidth);
        double ratio = Math.Clamp(clampedMouseX / ActualWidth, 0, 1);
        TimeSpan rangeDuration = lSpool.LSpoolRangeLimit - lSpool.LSpoolRangeOrigin;
        if (rangeDuration <= TimeSpan.Zero)
        {
            return lSpool.LSpoolRangeOrigin;
        }

        return lSpool.LSpoolRangeOrigin + TimeSpan.FromSeconds(ratio * rangeDuration.TotalSeconds);
    }
}
