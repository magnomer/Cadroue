using System.Windows.Controls.Primitives;

namespace Cadroue.UIShell.PDeck;

internal sealed partial class PColumn
{
    public double PColumnTotalRead()
    {
        double pMinimumTotal = 0;
        int pVisibleCount = 0;
        for (int index = 0; index < pColumnItems.Count; index++)
        {
            if (pColumnHiddenFlags[index])
            {
                continue;
            }

            pVisibleCount++;
            if (pColumnFixedWidths[index] > 0)
            {
                pMinimumTotal += pColumnFixedWidths[index];
            }
            else if (PColumnFlexCheck() && pColumnPixelsReady && index != pColumnFlexIndex)
            {
                pMinimumTotal += Math.Max(pColumnMinimumWidths[index], pColumnPixelWidths[index]);
            }
            else
            {
                pMinimumTotal += pColumnMinimumWidths[index];
            }
        }

        return pMinimumTotal + pColumnSplitterWidth * Math.Max(0, pVisibleCount - 1);
    }

    private void PColumnMinimumApply()
    {
        double pAvailableWidth = PColumnAvailableRead();
        if (pAvailableWidth <= 0 || Math.Abs(pAvailableWidth - pColumnAppliedWidth) < 0.5)
        {
            return;
        }

        pColumnAppliedWidth = pAvailableWidth;
        double[] pMinimumWidths = PColumnMinimumRead(pAvailableWidth);
        for (int index = 0; index < pColumnItems.Count; index++)
        {
            pColumnItems[index].MinWidth = pMinimumWidths[index];
        }

        if (pColumnDefaultsPending)
        {
            pColumnDefaultsPending = false;
            PColumnDefaultApply(pAvailableWidth, pMinimumWidths);
        }

        if (PColumnFlexCheck() && !pColumnPixelsReady)
        {
            PColumnPixelsCreate(pAvailableWidth);
        }
    }

    private void PColumnDefaultApply(double pAvailableWidth, IReadOnlyList<double> pMinimumWidths)
    {
        double pCompactTotal = 0;
        int pFlexibleCount = 0;
        for (int index = 0; index < pColumnItems.Count; index++)
        {
            if (pColumnHiddenFlags[index])
            {
                continue;
            }

            if (pColumnCompactFlags[index])
            {
                pCompactTotal += pMinimumWidths[index];
                continue;
            }

            pFlexibleCount++;
        }

        if (pFlexibleCount == 0)
        {
            return;
        }

        double pFlexibleWidth = Math.Max(0, pAvailableWidth - pCompactTotal) / pFlexibleCount;
        var pWidths = new double[pColumnItems.Count];
        for (int index = 0; index < pColumnItems.Count; index++)
        {
            if (pColumnHiddenFlags[index])
            {
                continue;
            }

            pWidths[index] = pColumnCompactFlags[index]
                ? pMinimumWidths[index]
                : Math.Max(pMinimumWidths[index], pFlexibleWidth);
        }

        PColumnWeightsCommit(pWidths);
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

    private double[] PColumnMinimumRead(double pAvailableWidth)
    {
        double[] pMinimumWidths = pColumnMinimumWidths.ToArray();
        for (int index = 0; index < pMinimumWidths.Length; index++)
        {
            if (pColumnHiddenFlags[index])
            {
                pMinimumWidths[index] = 0;
                continue;
            }

            if (pColumnFixedWidths[index] > 0)
            {
                pMinimumWidths[index] = pColumnFixedWidths[index];
            }
        }

        double pMinimumWidthTotal = pMinimumWidths.Sum();
        if (pMinimumWidthTotal <= pAvailableWidth || pMinimumWidthTotal <= 0)
        {
            return pMinimumWidths;
        }

        double pScale = pAvailableWidth / pMinimumWidthTotal;
        for (int index = 0; index < pMinimumWidths.Length; index++)
        {
            pMinimumWidths[index] *= pScale;
        }

        return pMinimumWidths;
    }

    private double PColumnAvailableRead()
    {
        int pVisiblePanelCount = pColumnHiddenFlags.Count(pHidden => !pHidden);
        double pSlotWidth = LayoutInformation.GetLayoutSlot(pColumnGrid).Width;
        double pGridWidth = double.IsNaN(pSlotWidth) || double.IsInfinity(pSlotWidth) || pSlotWidth <= 0
            ? pColumnGrid.ActualWidth
            : pSlotWidth;

        return Math.Max(0, pGridWidth - pColumnSplitterWidth * Math.Max(0, pVisiblePanelCount - 1));
    }
}
