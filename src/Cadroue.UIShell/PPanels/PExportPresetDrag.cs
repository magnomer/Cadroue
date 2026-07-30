using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PExport
{
    private void PExportPresetDragClear()
    {
        pPresetNameDragging = null;
        pPresetDragStart = null;
        pPresetDragActive = false;
    }

    private int PExportPresetIndexResolve(Point pMousePoint)
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

    private bool PExportPresetMoveLive(string lPresetName, int lTargetIndex, UIElement pPresetRow)
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

        int lDataTargetIndex = lTargetIndex - PExportPresetDividerCountBefore(lTargetIndex);
        if (!LExportSpecificState.LPresetMoveToIndex(lPresetName, lDataTargetIndex))
        {
            return false;
        }

        pPresetRowPanel.Children.RemoveAt(lSourceIndex);
        pPresetRowPanel.Children.Insert(lInsertIndex, pPresetRow);
        return true;
    }

    private int PExportPresetDividerCountBefore(int lChildIndex)
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

    private static bool PExportButtonSourceCheck(object pSource)
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
