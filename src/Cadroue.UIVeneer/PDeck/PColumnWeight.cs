using System.Windows;

namespace Cadroue.UIVeneer.PDeck;

internal sealed partial class PColumn
{
    public IReadOnlyList<double> PColumnWeightsRead()
    {
        var pWeights = new double[pColumnItems.Count];
        for (int index = 0; index < pWeights.Length; index++)
        {
            double pWeight = pColumnHiddenFlags[index] || pColumnFixedWidths[index] > 0
                ? pColumnStoredWeights[index]
                : pColumnWeights[index];
            pWeights[index] = pWeight > 0 ? pWeight : 1;
        }

        return pWeights;
    }

    private void PColumnWeightsCommit(double[] pWidths)
    {
        double pWidthTotal = pWidths.Sum();
        if (pWidthTotal <= 0)
        {
            return;
        }

        for (int index = 0; index < pWidths.Length; index++)
        {
            pColumnWeights[index] = pColumnHiddenFlags[index] ? 0 : pWidths[index] / pWidthTotal;
            pColumnPixelWidths[index] = pColumnHiddenFlags[index] ? 0 : Math.Max(0, pWidths[index]);
        }

        if (PColumnFlexCheck())
        {
            pColumnPixelsReady = true;
        }

        PColumnWeightsApply();
        pColumnWidthNotify?.Invoke();
    }

    private void PColumnWeightsApply()
    {
        double pWeightTotal = pColumnWeights.Sum(pWeight => Math.Max(0, pWeight));
        if (pWeightTotal <= 0)
        {
            for (int index = 0; index < pColumnWeights.Length; index++)
            {
                pColumnWeights[index] = pColumnHiddenFlags[index] ? 0 : 1;
            }
        }

        bool pFlexActive = PColumnFlexCheck() && pColumnPixelsReady;
        for (int index = 0; index < pColumnItems.Count; index++)
        {
            if (pColumnHiddenFlags[index])
            {
                pColumnItems[index].Width = new GridLength(0, GridUnitType.Pixel);
                continue;
            }

            if (pColumnFixedWidths[index] > 0)
            {
                pColumnItems[index].Width = new GridLength(pColumnFixedWidths[index], GridUnitType.Pixel);
                continue;
            }

            if (pFlexActive)
            {
                pColumnItems[index].Width = index == pColumnFlexIndex
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(Math.Max(0, pColumnPixelWidths[index]), GridUnitType.Pixel);
                continue;
            }

            pColumnItems[index].Width = new GridLength(Math.Max(0.0001, pColumnWeights[index]), GridUnitType.Star);
        }
    }

    private void PColumnPixelsCreate(double pAvailableWidth)
    {
        double pWeightTotal = pColumnWeights.Sum(pWeight => Math.Max(0, pWeight));
        if (pWeightTotal <= 0 || pAvailableWidth <= 0)
        {
            return;
        }

        for (int index = 0; index < pColumnItems.Count; index++)
        {
            pColumnPixelWidths[index] = Math.Max(0, pColumnWeights[index]) / pWeightTotal * pAvailableWidth;
        }

        pColumnPixelsReady = true;
        PColumnWeightsApply();
        pColumnWidthNotify?.Invoke();
    }

    private static double[] PColumnWeightCreate(IReadOnlyList<double>? pStoredWidths, int pCount)
    {
        if (pStoredWidths is null || pStoredWidths.Count != pCount)
        {
            return Enumerable.Repeat(1d, pCount).ToArray();
        }

        double pWidthTotal = pStoredWidths.Sum(pWidth => Math.Max(0, pWidth));
        if (pWidthTotal <= 0)
        {
            return Enumerable.Repeat(1d, pCount).ToArray();
        }

        return pStoredWidths
            .Select(pWidth => Math.Max(0, pWidth) / pWidthTotal)
            .ToArray();
    }
}
