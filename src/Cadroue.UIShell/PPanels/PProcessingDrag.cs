using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PProcessing
{
    private int? pProcessingIndexDragging;
    private Point? pProcessingDragOrigin;
    private bool pProcessingDragActive;
    private Border? pProcessingRowDragging;

    private void PProcessingNumbersUpdate()
    {
        if (!pProcessingOrdered)
        {
            return;
        }

        for (int pIndex = 0; pIndex < pProcessingRowPanel.Children.Count; pIndex++)
        {
            if (pProcessingRowPanel.Children[pIndex] is Border { Child: StackPanel pRowContent }
                && pRowContent.Children.Count > 0
                && pRowContent.Children[0] is Border { Child: TextBlock pNumber })
            {
                pNumber.Text = (pIndex + 1).ToString();
            }
        }
    }

    private void PProcessingMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        if (!pProcessingOrdered)
        {
            return;
        }

        if (pProcessingRowDragging is not { } pDragRow
            || pProcessingIndexDragging is not int pDragIndex
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

        int pTargetIndex = PProcessingIndexResolve(pCurrent);
        if (pProcessingRowPanel.Children[pTargetIndex] is Border { Tag: string pTargetName }
            && pProcessingDisabledSteps.Contains(pTargetName))
        {
            return;
        }

        if (pTargetIndex != pDragIndex)
        {
            pProcessingRowPanel.Children.Remove(pDragRow);
            pTargetIndex = Math.Clamp(pTargetIndex, 0, pProcessingRowPanel.Children.Count);
            pProcessingRowPanel.Children.Insert(pTargetIndex, pDragRow);
            pProcessingIndexDragging = pTargetIndex;
            PProcessingNumbersUpdate();
        }
    }

    private void PProcessingUpHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        bool pReordered = pProcessingDragActive;
        if (pProcessingRowDragging is { } pDragRow)
        {
            pDragRow.Opacity = 1;
        }

        PProcessingDragClear();
        if (pReordered)
        {
            PProcessingOrderChange?.Invoke();
        }
    }

    private void PProcessingLostHandle(object pSender, MouseEventArgs pEvent)
    {
        if (pProcessingRowDragging is { } pDragRow)
        {
            pDragRow.Opacity = 1;
        }

        PProcessingDragClear();
    }

    private void PProcessingDragClear()
    {
        pProcessingIndexDragging = null;
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
