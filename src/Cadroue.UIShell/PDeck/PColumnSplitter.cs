using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PDeck;

internal sealed partial class PColumn
{
    public Thumb PColumnSplitterBuild(int pLeftPanelIndex)
    {
        var pThumb = new Thumb
        {
            Cursor = Cursors.SizeWE,
            Background = Brushes.Transparent,
            Focusable = false,
            Template = PColumnThumbCreate()
        };
        pThumb.DragDelta += (_, pEvent) => PColumnDragApply(pLeftPanelIndex, pEvent.HorizontalChange);
        return pThumb;
    }

    private void PColumnDragApply(int pLeftPanelIndex, double pDelta)
    {
        if (pLeftPanelIndex < 0
            || pLeftPanelIndex >= pColumnItems.Count - 1
            || Math.Abs(pDelta) <= 0)
        {
            return;
        }

        double pAvailableWidth = PColumnAvailableRead();
        if (pAvailableWidth <= 0)
        {
            return;
        }

        double[] pWidths = PColumnCurrentRead(pAvailableWidth);
        double[] pMinimumWidths = PColumnMinimumRead(pAvailableWidth);
        PColumnBudgetResolve(pLeftPanelIndex, out int pReceiverIndex, out int pDonorIndex, out double pReceiverSign);
        double pReceiverDelta = pReceiverSign * pDelta;
        double pClampedDelta = Math.Clamp(
            pReceiverDelta,
            pMinimumWidths[pReceiverIndex] - pWidths[pReceiverIndex],
            pWidths[pDonorIndex] - pMinimumWidths[pDonorIndex]);
        if (Math.Abs(pClampedDelta) <= 0)
        {
            return;
        }

        pWidths[pReceiverIndex] += pClampedDelta;
        pWidths[pDonorIndex] -= pClampedDelta;
        PColumnWeightsCommit(pWidths);
    }

    private void PColumnBudgetResolve(int pLeftPanelIndex, out int pReceiverIndex, out int pDonorIndex, out double pReceiverSign)
    {
        if (!PColumnFlexCheck())
        {
            pReceiverIndex = pLeftPanelIndex;
            pDonorIndex = pLeftPanelIndex + 1;
            pReceiverSign = 1;
            return;
        }

        pDonorIndex = pColumnFlexIndex;
        if (pColumnFlexIndex > pLeftPanelIndex)
        {
            pReceiverIndex = pLeftPanelIndex;
            pReceiverSign = 1;
        }
        else
        {
            pReceiverIndex = pLeftPanelIndex + 1;
            pReceiverSign = -1;
        }
    }

    private static ControlTemplate PColumnThumbCreate()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        return new ControlTemplate(typeof(Thumb))
        {
            VisualTree = pBorder
        };
    }
}
