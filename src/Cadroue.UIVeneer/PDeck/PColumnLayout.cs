using System.Windows.Controls.Primitives;

namespace Cadroue.UIVeneer.PDeck;

internal sealed partial class PColumn
{
    private void PColumnMinimumApply()
    {
        double pAvailableWidth = PColumnAvailableRead();
        if (!LColumn.LColumnAppliedSet(pAvailableWidth))
        {
            return;
        }

        double[] pMinimumWidths = LColumn.LColumnMinimumRead(pAvailableWidth);
        for (int index = 0; index < pColumnItems.Count; index++)
        {
            pColumnItems[index].MinWidth = pMinimumWidths[index];
        }

        if (LColumn.LColumnDefaultsRead()
            && LColumn.LColumnDefaultResolve(pAvailableWidth, pMinimumWidths) is { } pDefaultWidths)
        {
            PColumnWeightsCommit(pDefaultWidths);
        }

        if (LColumn.LColumnFlexCheck() && !LColumn.LColumnPixelsReady)
        {
            PColumnPixelsCreate(pAvailableWidth);
        }
    }

    private double[] PColumnCurrentRead(double pAvailableWidth)
    {
        double[] pWidths = pColumnItems.Select(pColumn => pColumn.ActualWidth).ToArray();
        double pWidthTotal = pWidths.Sum();
        if (pWidthTotal <= 0)
        {
            double pEqualWidth = pAvailableWidth / pWidths.Length;
            for (int index = 0; index < pWidths.Length; index++)
            {
                pWidths[index] = pEqualWidth;
            }
        }

        return pWidths;
    }

    private double PColumnAvailableRead()
    {
        double pSlotWidth = LayoutInformation.GetLayoutSlot(pColumnGrid).Width;
        double pGridWidth = double.IsNaN(pSlotWidth) || double.IsInfinity(pSlotWidth) || pSlotWidth <= 0
            ? pColumnGrid.ActualWidth
            : pSlotWidth;

        return LColumn.LColumnAvailableResolve(pGridWidth, pColumnSplitterWidth);
    }
}
