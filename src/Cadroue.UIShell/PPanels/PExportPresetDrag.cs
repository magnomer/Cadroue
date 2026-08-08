using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private void PExportDragClear()
    {
        if (pPresetRowDragging is not null)
        {
            pPresetRowDragging.Opacity = pPresetRowOpacity;
        }

        pPresetDragGhost?.PGhostClear();
        pPresetDragGhost = null;
        pPresetRowDragging = null;
        pPresetNameDragging = null;
        pExportDragOrigin = null;
        pPresetDragActive = false;
    }

    private void PExportMoveHandle(object pSender, System.Windows.Input.MouseEventArgs pEvent)
    {
        if (pPresetNameDragging is not { } lPresetName
            || pPresetRowDragging is not { } pPresetRow
            || pPresetNameEditing is not null
            || pExportDragOrigin is not Point pStart
            || pEvent.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return;
        }

        Point pCurrent = pEvent.GetPosition(pPresetRowPanel);
        if (!pPresetDragActive
            && Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        if (!pPresetDragActive)
        {
            pPresetDragActive = true;
            pPresetRow.Opacity = 0.42;
            pPresetDragGhost = PMainWindow.PGhost.PGhostShow(pPresetRow, pPresetDragOffset);
        }

        pPresetDragGhost?.PGhostCursorSync();
        PExportLiveMove(lPresetName, PExportIndexResolve(pCurrent), pPresetRow);
        pEvent.Handled = true;
    }

    private void PExportUpHandle(object pSender, System.Windows.Input.MouseButtonEventArgs pEvent)
    {
        if (pPresetRowDragging is null || pPresetNameDragging is not { } lPresetName)
        {
            return;
        }

        bool pDragMoved = pPresetDragActive;
        PExportDragClear();
        pPresetRowPanel.ReleaseMouseCapture();

        if (!pDragMoved && !string.Equals(pPresetNameEditing, lPresetName, StringComparison.OrdinalIgnoreCase))
        {
            PExportEditCommit();
            PExportPresetSelect(lPresetName);
        }

        pEvent.Handled = true;
    }

    private void PExportLostHandle(object pSender, System.Windows.Input.MouseEventArgs pEvent)
    {
        PExportDragClear();
    }

    private int PExportIndexResolve(Point pMousePoint)
    {
        int lTargetIndex = 0;
        for (int lIndex = 0; lIndex < pPresetRowPanel.Children.Count; lIndex++)
        {
            if (pPresetRowPanel.Children[lIndex] is not FrameworkElement pRow)
            {
                continue;
            }

            Point pRowPoint = pRow.TransformToAncestor(pPresetRowPanel).Transform(new Point(0, 0));
            double pRowCenterY = pRowPoint.Y + pRow.ActualHeight / 2;
            if (pMousePoint.Y > pRowCenterY)
            {
                lTargetIndex = lIndex + 1;
            }
        }

        return Math.Clamp(lTargetIndex, 0, pPresetRowPanel.Children.Count);
    }

    private bool PExportLiveMove(string lPresetName, int lTargetIndex, UIElement pPresetRow)
    {
        int lSourceIndex = pPresetRowPanel.Children.IndexOf(pPresetRow);
        if (lSourceIndex < 0)
        {
            return false;
        }

        lTargetIndex = Math.Clamp(lTargetIndex, 0, pPresetRowPanel.Children.Count);
        int lInsertIndex = lSourceIndex < lTargetIndex ? lTargetIndex - 1 : lTargetIndex;
        if (lSourceIndex == lInsertIndex)
        {
            return false;
        }

        int lDataTargetIndex = lTargetIndex - PExportDividerRead(lTargetIndex);
        if (!LPreset.LPresetMove(lPresetName, lDataTargetIndex))
        {
            return false;
        }

        pPresetRowPanel.Children.RemoveAt(lSourceIndex);
        pPresetRowPanel.Children.Insert(lInsertIndex, pPresetRow);
        return true;
    }

    private int PExportDividerRead(int lChildIndex)
    {
        int lDividerCount = 0;
        int lLimit = Math.Min(lChildIndex, pPresetRowPanel.Children.Count);
        for (int lIndex = 0; lIndex < lLimit; lIndex++)
        {
            if (pPresetRowPanel.Children[lIndex] is Border { Tag: "Divider" })
            {
                lDividerCount++;
            }
        }

        return lDividerCount;
    }

    private static bool PExportInsideCheck(DependencyObject? pSource, DependencyObject pTarget)
    {
        while (pSource is not null)
        {
            if (ReferenceEquals(pSource, pTarget))
            {
                return true;
            }

            pSource = pSource is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(pSource)
                : LogicalTreeHelper.GetParent(pSource);
        }

        return false;
    }

    private static bool PExportSourceCheck(object pSource)
    {
        DependencyObject? pObject = pSource as DependencyObject;
        while (pObject is not null)
        {
            if (pObject is Button)
            {
                return true;
            }

            pObject = VisualTreeHelper.GetParent(pObject);
        }

        return false;
    }
}
