using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PProcessing
{
    private Point? pProcessingDragOrigin;
    private bool pProcessingDragActive;
    private Border? pProcessingRowDragging;

    private void PProcessingMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        if (!LProcessing.LProcessingOrdered)
        {
            return;
        }

        if (pProcessingRowDragging is not { } pDragRow
            || LProcessing.LProcessingDragIndex is not int pDragIndex
            || pProcessingDragOrigin is not Point pStart
            || pEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrent = pEvent.GetPosition(pProcessingRowPanel);
        if (!pProcessingDragActive
            && Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        pProcessingDragActive = true;
        pDragRow.Opacity = 0.72;
        LProcessing.LProcessingIndexMove(pDragIndex, PProcessingIndexResolve(pCurrent));
    }

    private void PProcessingUpHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        bool pReordered = pProcessingDragActive;
        PProcessingDragClear();
        if (pReordered)
        {
            PProcessingOrderChange?.Invoke();
        }
    }

    private void PProcessingLostHandle(object pSender, MouseEventArgs pEvent) => PProcessingDragClear();

    private void PProcessingDragClear()
    {
        if (pProcessingRowDragging is { } pDragRow)
        {
            pDragRow.Opacity = 1;
        }

        LProcessing.LProcessingDragSet(null);
        pProcessingDragOrigin = null;
        pProcessingDragActive = false;
        pProcessingRowDragging = null;
    }

    private int PProcessingIndexResolve(Point pPoint)
    {
        for (int pIndex = 0; pIndex < pProcessingRowPanel.Children.Count; pIndex++)
        {
            if (pProcessingRowPanel.Children[pIndex] is not Border pRow)
            {
                continue;
            }

            Point pTopLeft = pRow.TranslatePoint(new Point(0, 0), pProcessingRowPanel);
            if (pPoint.Y < pTopLeft.Y + (pRow.ActualHeight / 2))
            {
                return pIndex;
            }
        }

        return Math.Max(0, pProcessingRowPanel.Children.Count - 1);
    }
}
