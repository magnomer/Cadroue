using System.Windows;

namespace Cadroue.UIVeneer.PCabin;

internal sealed partial class PColumn
{
    private void PColumnWeightsCommit(double[] pWidths)
    {
        if (!LColumn.LColumnWeightsCommit(pWidths))
        {
            return;
        }

        PColumnWeightsApply();
        pColumnWidthNotify?.Invoke();
    }

    private void PColumnWeightsApply()
    {
        LColumn.LColumnWeightsNormalize();
        bool pFlexActive = LColumn.LColumnPixelCheck();
        for (int index = 0; index < pColumnItems.Count; index++)
        {
            if (LColumn.LColumnHiddenCheck(index))
            {
                pColumnItems[index].Width = new GridLength(0, GridUnitType.Pixel);
                continue;
            }

            if (LColumn.LColumnFixedRead(index) > 0)
            {
                pColumnItems[index].Width = new GridLength(LColumn.LColumnFixedRead(index), GridUnitType.Pixel);
                continue;
            }

            if (pFlexActive)
            {
                pColumnItems[index].Width = index == LColumn.LColumnFlexIndex
                    ? new GridLength(1, GridUnitType.Star)
                    : new GridLength(Math.Max(0, LColumn.LColumnPixelRead(index)), GridUnitType.Pixel);
                continue;
            }

            pColumnItems[index].Width = new GridLength(
                Math.Max(0.0001, LColumn.LColumnWeightRead(index)), GridUnitType.Star);
        }
    }

    private void PColumnPixelsCreate(double pAvailableWidth)
    {
        if (!LColumn.LColumnPixelsCreate(pAvailableWidth))
        {
            return;
        }

        PColumnWeightsApply();
        pColumnWidthNotify?.Invoke();
    }
}
