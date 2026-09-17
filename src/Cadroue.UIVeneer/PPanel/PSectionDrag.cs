using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PSection
{
    private Point? pSectionDragOrigin;
    private Point pSectionGrabOffset;
    private Border? pSectionRowDragging;
    private PHouse.PGhost? pSectionGhost;

    private void PSectionNumberUpdate()
    {
        for (int pIndex = 0; pIndex < pSectionRowPanel.Children.Count; pIndex++)
        {
            if (pSectionRowPanel.Children[pIndex] is Border { Tag: TextBlock pBadgeText })
            {
                pBadgeText.Text = (pIndex + 1).ToString();
            }
        }
    }

    private void PSectionDragClear()
    {
        LSection.LSectionDragSet(null, false);
        pSectionDragOrigin = null;
        pSectionRowDragging = null;
    }

    private void PSectionMoveHandle(object pSender, MouseEventArgs pEvent)
    {
        if (!LSection.LSectionDragCheck()
            || pSectionRowDragging is not { } pDragRow
            || LSection.LSectionDragIndex is not int pDragIndex
            || pSectionDragOrigin is not Point pStart
            || pEvent.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrent = pEvent.GetPosition(pSectionRowPanel);
        if (!LSection.LSectionDragActive
            && Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (!LSection.LSectionDragActive)
        {
            LSection.LSectionDragSet(pDragIndex, true);
            pDragRow.Opacity = 0.72;
            pSectionGhost = PHouse.PGhost.PGhostShow(pDragRow, pSectionGrabOffset);
        }

        pSectionGhost?.PGhostCursorSync();
        PSectionLiveMove(pDragIndex, PSectionIndexResolve(pCurrent), pDragRow);
        pEvent.Handled = true;
    }

    private void PSectionUpHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        if (pSectionRowDragging is not { } pDragRow)
        {
            return;
        }

        bool pDragMoved = LSection.LSectionDragActive;
        pDragRow.Opacity = 1;
        pSectionGhost?.PGhostClear();
        pSectionGhost = null;
        pSectionRowPanel.ReleaseMouseCapture();

        if (pDragMoved)
        {
            PSectionDragClear();
            PSectionRebuild();
            pEvent.Handled = true;
            return;
        }

        int pRowIndex = pSectionRowPanel.Children.IndexOf(pDragRow);
        PSectionDragClear();

        if (pRowIndex >= 0 && LSection.LSectionEditIndex != pRowIndex)
        {
            PSectionEditCommit();
            ModifierKeys pSectionModifiers = Keyboard.Modifiers;
            if (pSectionModifiers.HasFlag(ModifierKeys.Shift))
            {
                pFlowAttached?.PFlowRangeSelect(pRowIndex);
            }
            else if (pSectionModifiers.HasFlag(ModifierKeys.Control))
            {
                pFlowAttached?.PFlowSelectToggle(pRowIndex);
            }
            else
            {
                pFlowAttached?.PFlowSectionSelect(pRowIndex);
            }
        }

        pEvent.Handled = true;
    }

    private void PSectionLostHandle(object pSender, MouseEventArgs pEvent)
    {
        if (pSectionRowDragging is { } pDragRow)
        {
            pDragRow.Opacity = 1;
        }

        pSectionGhost?.PGhostClear();
        pSectionGhost = null;
        PSectionDragClear();
    }

    private int PSectionIndexResolve(Point pMousePoint)
    {
        int pTargetIndex = 0;
        for (int pIndex = 0; pIndex < pSectionRowPanel.Children.Count; pIndex++)
        {
            if (pSectionRowPanel.Children[pIndex] is not FrameworkElement pRow)
            {
                continue;
            }

            Point pRowPoint = pRow.TransformToAncestor(pSectionRowPanel).Transform(new Point(0, 0));
            if (pMousePoint.Y > pRowPoint.Y + pRow.ActualHeight / 2)
            {
                pTargetIndex = pIndex + 1;
            }
        }

        return Math.Clamp(pTargetIndex, 0, pSectionRowPanel.Children.Count);
    }

    private bool PSectionLiveMove(int pSectionIndex, int pTargetIndex, UIElement pSectionRow)
    {
        int pSourceIndex = pSectionRowPanel.Children.IndexOf(pSectionRow);
        if (pSourceIndex < 0 || pFlowAttached is null)
        {
            return false;
        }

        pTargetIndex = Math.Clamp(pTargetIndex, 0, pSectionRowPanel.Children.Count);
        int pInsertIndex = pSourceIndex < pTargetIndex ? pTargetIndex - 1 : pTargetIndex;
        if (pSourceIndex == pInsertIndex || !pFlowAttached.PFlowSectionMove(pSectionIndex, pTargetIndex))
        {
            return false;
        }

        pSectionRowPanel.Children.RemoveAt(pSourceIndex);
        pSectionRowPanel.Children.Insert(pInsertIndex, pSectionRow);
        LSection.LSectionDragSet(pInsertIndex, LSection.LSectionDragActive);
        PSectionNumberUpdate();
        return true;
    }
}
