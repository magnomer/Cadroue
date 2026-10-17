using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PExport
{
    private void PExportDragClear()
    {
        PExportOpacityRestore();
        QGhostLayer.QGhostLayerClear(pPresetRowDragging);
        PExportDragReset();
        QExport.QExportDragClear();
    }

    private void PExportOpacityRestore()
    {
        if (pPresetRowDragging is not null)
        {
            pPresetRowDragging.Opacity = pPresetOpacityOrigin;
        }
    }

    private void PExportDragSet(Border pRowBorder)
    {
        pPresetRowDragging = pRowBorder;
        pPresetOpacityOrigin = pRowBorder.Opacity;
    }

    private void PExportOriginSet(Point? pOrigin) => pExportDragOrigin = pOrigin;

    private void PExportDragReset()
    {
        pPresetRowDragging = null;
        pExportDragOrigin = null;
    }

    private void PExportMoveHandle(object pSender, System.Windows.Input.MouseEventArgs pEvent) =>
        pEvent.Handled = PExportDragHandle(pEvent) || pEvent.Handled;

    private bool PExportDragHandle(System.Windows.Input.MouseEventArgs pEvent)
    {
        if (QExport.QExportDragging is not { } lPresetName
            || pPresetRowDragging is not { } pPresetRow
            || QExport.QExportEditing is not null
            || pExportDragOrigin is not Point pStart
            || pEvent.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
        {
            return false;
        }

        Point pCurrent = pEvent.GetPosition(pPresetRowPanel);
        if (!QExport.QExportDragActive && PExportThresholdCheck(pCurrent, pStart))
        {
            return false;
        }

        if (QExport.QExportDragMove())
        {
            PExportGhostApply(pPresetRow);
            QGhostLayer.QGhostLayerShow(pPresetRow, QExport.QExportGhost);
        }

        QGhostLayer.QGhostLayerSync(pPresetRow);
        PExportLiveMove(lPresetName, PExportIndexResolve(pCurrent), pPresetRow);
        return true;
    }

    private static bool PExportThresholdCheck(Point pCurrent, Point pStart) =>
        Math.Abs(pCurrent.X - pStart.X) < SystemParameters.MinimumHorizontalDragDistance
        && Math.Abs(pCurrent.Y - pStart.Y) < SystemParameters.MinimumVerticalDragDistance;

    private static void PExportGhostApply(Border pPresetRow) => pPresetRow.Opacity = 0.42;

    private void PExportUpHandle(object pSender, System.Windows.Input.MouseButtonEventArgs pEvent) =>
        pEvent.Handled = PExportDropHandle() || pEvent.Handled;

    private bool PExportDropHandle()
    {
        if (pPresetRowDragging is null || QExport.QExportDragging is not { } lPresetName)
        {
            return false;
        }

        bool pDragMoved = QExport.QExportDragActive;
        PExportDragClear();
        pPresetRowPanel.ReleaseMouseCapture();

        if (!pDragMoved && !QExport.QExportEditingCheck(lPresetName))
        {
            PExportEditCommit();
            PExportPresetSelect(lPresetName);
        }

        return true;
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

        (int lInsertIndex, int lDataTargetIndex) = PExportTargetResolve(lSourceIndex, lTargetIndex);
        if (lSourceIndex == lInsertIndex)
        {
            return false;
        }

        if (!LPreset.LPresetMove(lPresetName, lDataTargetIndex))
        {
            return false;
        }

        PExportRowMove(lSourceIndex, lInsertIndex, pPresetRow);
        return true;
    }

    private (int lPresetInsertIndex, int lPresetTargetIndex) PExportTargetResolve(int lSourceIndex, int lTargetIndex)
    {
        lTargetIndex = Math.Clamp(lTargetIndex, 0, pPresetRowPanel.Children.Count);
        int lInsertIndex = lSourceIndex < lTargetIndex ? lTargetIndex - 1 : lTargetIndex;
        return (lInsertIndex, lTargetIndex - PExportHeaderRead(lTargetIndex));
    }

    private void PExportRowMove(int lSourceIndex, int lInsertIndex, UIElement pPresetRow)
    {
        pPresetRowPanel.Children.RemoveAt(lSourceIndex);
        pPresetRowPanel.Children.Insert(lInsertIndex, pPresetRow);
    }

    private int PExportHeaderRead(int lChildIndex)
    {
        int lHeaderCount = 0;
        int lLimit = Math.Min(lChildIndex, pPresetRowPanel.Children.Count);
        for (int lIndex = 0; lIndex < lLimit; lIndex++)
        {
            if (pPresetRowPanel.Children[lIndex] is Border { Tag: "Header" })
            {
                lHeaderCount++;
            }
        }

        return lHeaderCount;
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
