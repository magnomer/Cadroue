using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainArea;

internal sealed class PColumn
{
    private const double pColumnSplitterWidth = 6;

    private readonly Grid pColumnGrid;
    private readonly IReadOnlyList<ColumnDefinition> pColumnItems;
    private readonly double[] pColumnMinimumWidths;
    private readonly double[] pColumnWeights;
    private readonly double[] pColumnStoredWeights;
    private readonly bool[] pColumnHiddenFlags;
    private readonly bool[] pColumnCompactFlags;
    private readonly double[] pColumnFixedWidths;
    private readonly double[] pColumnPixelWidths;
    private readonly int pColumnFlexIndex;
    private double pColumnAppliedWidth = -1;
    private bool pColumnDefaultsPending;
    private bool pColumnPixelsReady;

    private PColumn(
        Grid pColumnGrid,
        IReadOnlyList<ColumnDefinition> pColumnItems,
        IReadOnlyList<double>? pStoredWidths,
        IReadOnlyList<bool>? pCompactPanels,
        int pFlexPanelIndex)
    {
        this.pColumnGrid = pColumnGrid;
        this.pColumnItems = pColumnItems;
        pColumnMinimumWidths = pColumnItems.Select(pColumn => pColumn.MinWidth).ToArray();
        pColumnWeights = PColumnWeightCreate(pStoredWidths, pColumnItems.Count);
        pColumnStoredWeights = pColumnWeights.ToArray();
        pColumnHiddenFlags = new bool[pColumnItems.Count];
        pColumnCompactFlags = PColumnCompactCreate(pCompactPanels, pColumnItems.Count);
        pColumnFixedWidths = new double[pColumnItems.Count];
        pColumnPixelWidths = new double[pColumnItems.Count];
        pColumnFlexIndex = pFlexPanelIndex >= 0 && pFlexPanelIndex < pColumnItems.Count ? pFlexPanelIndex : -1;
        pColumnDefaultsPending = !PColumnStoredCheck(pStoredWidths, pColumnItems.Count)
            && pColumnCompactFlags.Any(pCompact => pCompact);

        pColumnGrid.LayoutUpdated += (_, _) => PColumnMinimumApply();
        PColumnWeightsApply();
    }

    public static PColumn PColumnAttach(
        Grid pColumnGrid,
        IReadOnlyList<ColumnDefinition> pColumnItems,
        IReadOnlyList<double>? pStoredWidths,
        IReadOnlyList<bool>? pCompactPanels = null,
        int pFlexPanelIndex = -1)
    {
        return new PColumn(pColumnGrid, pColumnItems, pStoredWidths, pCompactPanels, pFlexPanelIndex);
    }

    private bool PColumnFlexCheck() =>
        pColumnFlexIndex >= 0 && !pColumnHiddenFlags[pColumnFlexIndex] && pColumnFixedWidths[pColumnFlexIndex] <= 0;

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

    public IReadOnlyList<double> PColumnWeightsRead()
    {
        if (PColumnFlexCheck() && pColumnPixelsReady)
        {
            double[] pActualWidths = pColumnItems.Select(pColumn => pColumn.ActualWidth).ToArray();
            double pActualTotal = pActualWidths.Sum();
            if (pActualTotal > 0)
            {
                var pFlexWeights = new double[pColumnItems.Count];
                for (int index = 0; index < pFlexWeights.Length; index++)
                {
                    double pFlexWeight = pColumnHiddenFlags[index] || pColumnFixedWidths[index] > 0
                        ? pColumnStoredWeights[index]
                        : pActualWidths[index] / pActualTotal;
                    pFlexWeights[index] = pFlexWeight > 0 ? pFlexWeight : 1;
                }

                return pFlexWeights;
            }
        }

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
            pMinimumTotal += pColumnFixedWidths[index] > 0
                ? pColumnFixedWidths[index]
                : pColumnMinimumWidths[index];
        }

        return pMinimumTotal + pColumnSplitterWidth * Math.Max(0, pVisibleCount - 1);
    }

    public void PColumnWidthSet(int pPanelIndex, double pPanelFixedWidth)
    {
        if (pPanelIndex < 0 || pPanelIndex >= pColumnItems.Count)
        {
            return;
        }

        if (pPanelFixedWidth > 0)
        {
            if (pColumnFixedWidths[pPanelIndex] <= 0)
            {
                pColumnStoredWeights[pPanelIndex] = pColumnWeights[pPanelIndex] > 0
                    ? pColumnWeights[pPanelIndex]
                    : pColumnStoredWeights[pPanelIndex];
            }

            pColumnFixedWidths[pPanelIndex] = pPanelFixedWidth;
            pColumnWeights[pPanelIndex] = 0;
        }
        else
        {
            if (pColumnFixedWidths[pPanelIndex] <= 0)
            {
                return;
            }

            pColumnFixedWidths[pPanelIndex] = 0;
            pColumnWeights[pPanelIndex] = pColumnStoredWeights[pPanelIndex] > 0
                ? pColumnStoredWeights[pPanelIndex]
                : 1;
        }

        pColumnAppliedWidth = -1;
        PColumnWeightsApply();
    }

    public void PColumnHide(int pPanelIndex)
    {
        if (pPanelIndex < 0 || pPanelIndex >= pColumnItems.Count || pColumnHiddenFlags[pPanelIndex])
        {
            return;
        }

        pColumnStoredWeights[pPanelIndex] = pColumnWeights[pPanelIndex] > 0
            ? pColumnWeights[pPanelIndex]
            : pColumnStoredWeights[pPanelIndex];
        pColumnHiddenFlags[pPanelIndex] = true;
        pColumnWeights[pPanelIndex] = 0;
        pColumnItems[pPanelIndex].MinWidth = 0;
        pColumnAppliedWidth = -1;
        PColumnWeightsApply();
    }

    public void PColumnShow(int pPanelIndex)
    {
        if (pPanelIndex < 0 || pPanelIndex >= pColumnItems.Count || !pColumnHiddenFlags[pPanelIndex])
        {
            return;
        }

        pColumnHiddenFlags[pPanelIndex] = false;
        pColumnWeights[pPanelIndex] = pColumnStoredWeights[pPanelIndex] > 0 ? pColumnStoredWeights[pPanelIndex] : 1;
        pColumnAppliedWidth = -1;
        PColumnWeightsApply();
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

    private static bool PColumnStoredCheck(IReadOnlyList<double>? pStoredWidths, int pCount) =>
        pStoredWidths is not null && pStoredWidths.Count == pCount && pStoredWidths.Sum(pWidth => Math.Max(0, pWidth)) > 0;

    private static bool[] PColumnCompactCreate(IReadOnlyList<bool>? pCompactPanels, int pCount)
    {
        var pCompactFlags = new bool[pCount];
        if (pCompactPanels is null)
        {
            return pCompactFlags;
        }

        for (int index = 0; index < pCount && index < pCompactPanels.Count; index++)
        {
            pCompactFlags[index] = pCompactPanels[index];
        }

        return pCompactFlags;
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
