using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainArea;

internal sealed class PResizableColumnLayout
{
    private const double pSplitterWidth = 6;

    private readonly Grid pGrid;
    private readonly IReadOnlyList<ColumnDefinition> pPanelColumns;
    private readonly double[] pPanelMinimumWidths;
    private readonly double[] pPanelWeights;
    private readonly double[] pPanelStoredWeights;
    private readonly bool[] pPanelHiddenFlags;
    private readonly bool[] pPanelCompactFlags;
    private double pAppliedAvailableWidth = -1;
    private bool pPanelDefaultsPending;

    private PResizableColumnLayout(
        Grid pGrid,
        IReadOnlyList<ColumnDefinition> pPanelColumns,
        IReadOnlyList<double>? pStoredWidths,
        IReadOnlyList<bool>? pCompactPanels)
    {
        this.pGrid = pGrid;
        this.pPanelColumns = pPanelColumns;
        pPanelMinimumWidths = pPanelColumns.Select(pColumn => pColumn.MinWidth).ToArray();
        pPanelWeights = PWeightCreate(pStoredWidths, pPanelColumns.Count);
        pPanelStoredWeights = pPanelWeights.ToArray();
        pPanelHiddenFlags = new bool[pPanelColumns.Count];
        pPanelCompactFlags = PCompactCreate(pCompactPanels, pPanelColumns.Count);
        pPanelDefaultsPending = !PStoredCheck(pStoredWidths, pPanelColumns.Count)
            && pPanelCompactFlags.Any(pCompact => pCompact);

        pGrid.LayoutUpdated += (_, _) => PMinimumWidthsApply();
        PWeightsApply();
    }

    public static PResizableColumnLayout PAttach(
        Grid pGrid,
        IReadOnlyList<ColumnDefinition> pPanelColumns,
        IReadOnlyList<double>? pStoredWidths,
        IReadOnlyList<bool>? pCompactPanels = null)
    {
        return new PResizableColumnLayout(pGrid, pPanelColumns, pStoredWidths, pCompactPanels);
    }

    public Thumb PSplitterBuild(int pLeftPanelIndex)
    {
        var pThumb = new Thumb
        {
            Cursor = Cursors.SizeWE,
            Background = Brushes.Transparent,
            Focusable = false,
            Template = PThumbTemplateCreate()
        };
        pThumb.DragDelta += (_, pEvent) => PDragApply(pLeftPanelIndex, pEvent.HorizontalChange);
        return pThumb;
    }

    public IReadOnlyList<double> PWeightsRead()
    {
        var pWeights = new double[pPanelColumns.Count];
        for (int index = 0; index < pWeights.Length; index++)
        {
            double pWeight = pPanelHiddenFlags[index]
                ? pPanelStoredWeights[index]
                : pPanelWeights[index];
            pWeights[index] = pWeight > 0 ? pWeight : 1;
        }

        return pWeights;
    }

    public void PPanelHide(int pPanelIndex)
    {
        if (pPanelIndex < 0 || pPanelIndex >= pPanelColumns.Count || pPanelHiddenFlags[pPanelIndex])
        {
            return;
        }

        pPanelStoredWeights[pPanelIndex] = pPanelWeights[pPanelIndex] > 0
            ? pPanelWeights[pPanelIndex]
            : pPanelStoredWeights[pPanelIndex];
        pPanelHiddenFlags[pPanelIndex] = true;
        pPanelWeights[pPanelIndex] = 0;
        pPanelColumns[pPanelIndex].MinWidth = 0;
        pAppliedAvailableWidth = -1;
        PWeightsApply();
    }

    public void PPanelShow(int pPanelIndex)
    {
        if (pPanelIndex < 0 || pPanelIndex >= pPanelColumns.Count || !pPanelHiddenFlags[pPanelIndex])
        {
            return;
        }

        pPanelHiddenFlags[pPanelIndex] = false;
        pPanelWeights[pPanelIndex] = pPanelStoredWeights[pPanelIndex] > 0 ? pPanelStoredWeights[pPanelIndex] : 1;
        pAppliedAvailableWidth = -1;
        PWeightsApply();
    }

    private void PDragApply(int pLeftPanelIndex, double pDelta)
    {
        if (pLeftPanelIndex < 0
            || pLeftPanelIndex >= pPanelColumns.Count - 1
            || Math.Abs(pDelta) <= 0)
        {
            return;
        }

        double pAvailableWidth = PAvailableWidthRead();
        if (pAvailableWidth <= 0)
        {
            return;
        }

        double[] pWidths = PCurrentWidthsRead(pAvailableWidth);
        double[] pMinimumWidths = PMinimumWidthsRead(pAvailableWidth);
        double pLeftWidth = pWidths[pLeftPanelIndex];
        double pRightWidth = pWidths[pLeftPanelIndex + 1];
        double pMinimumDelta = pMinimumWidths[pLeftPanelIndex] - pLeftWidth;
        double pMaximumDelta = pRightWidth - pMinimumWidths[pLeftPanelIndex + 1];
        double pClampedDelta = Math.Clamp(pDelta, pMinimumDelta, pMaximumDelta);
        if (Math.Abs(pClampedDelta) <= 0)
        {
            return;
        }

        pWidths[pLeftPanelIndex] += pClampedDelta;
        pWidths[pLeftPanelIndex + 1] -= pClampedDelta;
        PWeightsCommit(pWidths);
    }

    private void PWeightsCommit(double[] pWidths)
    {
        double pWidthTotal = pWidths.Sum();
        if (pWidthTotal <= 0)
        {
            return;
        }

        for (int index = 0; index < pWidths.Length; index++)
        {
            pPanelWeights[index] = pPanelHiddenFlags[index] ? 0 : pWidths[index] / pWidthTotal;
        }

        PWeightsApply();
    }

    private void PWeightsApply()
    {
        double pWeightTotal = pPanelWeights.Sum(pWeight => Math.Max(0, pWeight));
        if (pWeightTotal <= 0)
        {
            for (int index = 0; index < pPanelWeights.Length; index++)
            {
                pPanelWeights[index] = pPanelHiddenFlags[index] ? 0 : 1;
            }
        }

        for (int index = 0; index < pPanelColumns.Count; index++)
        {
            pPanelColumns[index].Width = pPanelHiddenFlags[index]
                ? new GridLength(0, GridUnitType.Pixel)
                : new GridLength(Math.Max(0.0001, pPanelWeights[index]), GridUnitType.Star);
        }
    }

    private void PMinimumWidthsApply()
    {
        double pAvailableWidth = PAvailableWidthRead();
        if (pAvailableWidth <= 0 || Math.Abs(pAvailableWidth - pAppliedAvailableWidth) < 0.5)
        {
            return;
        }

        pAppliedAvailableWidth = pAvailableWidth;
        double[] pMinimumWidths = PMinimumWidthsRead(pAvailableWidth);
        for (int index = 0; index < pPanelColumns.Count; index++)
        {
            pPanelColumns[index].MinWidth = pMinimumWidths[index];
        }

        if (!pPanelDefaultsPending)
        {
            return;
        }

        pPanelDefaultsPending = false;
        PDefaultWidthsApply(pAvailableWidth, pMinimumWidths);
    }

    private void PDefaultWidthsApply(double pAvailableWidth, IReadOnlyList<double> pMinimumWidths)
    {
        double pCompactTotal = 0;
        int pFlexibleCount = 0;
        for (int index = 0; index < pPanelColumns.Count; index++)
        {
            if (pPanelHiddenFlags[index])
            {
                continue;
            }

            if (pPanelCompactFlags[index])
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
        var pWidths = new double[pPanelColumns.Count];
        for (int index = 0; index < pPanelColumns.Count; index++)
        {
            if (pPanelHiddenFlags[index])
            {
                continue;
            }

            pWidths[index] = pPanelCompactFlags[index]
                ? pMinimumWidths[index]
                : Math.Max(pMinimumWidths[index], pFlexibleWidth);
        }

        PWeightsCommit(pWidths);
    }

    private double[] PCurrentWidthsRead(double pAvailableWidth)
    {
        double[] pWidths = pPanelColumns.Select(pColumn => pColumn.ActualWidth).ToArray();
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

    private double[] PMinimumWidthsRead(double pAvailableWidth)
    {
        double[] pMinimumWidths = pPanelMinimumWidths.ToArray();
        for (int index = 0; index < pMinimumWidths.Length; index++)
        {
            if (pPanelHiddenFlags[index])
            {
                pMinimumWidths[index] = 0;
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

    private double PAvailableWidthRead()
    {
        int pVisiblePanelCount = pPanelHiddenFlags.Count(pHidden => !pHidden);
        double pSlotWidth = LayoutInformation.GetLayoutSlot(pGrid).Width;
        double pGridWidth = double.IsNaN(pSlotWidth) || double.IsInfinity(pSlotWidth) || pSlotWidth <= 0
            ? pGrid.ActualWidth
            : pSlotWidth;

        return Math.Max(0, pGridWidth - pSplitterWidth * Math.Max(0, pVisiblePanelCount - 1));
    }

    private static bool PStoredCheck(IReadOnlyList<double>? pStoredWidths, int pCount) =>
        pStoredWidths is not null && pStoredWidths.Count == pCount && pStoredWidths.Sum(pWidth => Math.Max(0, pWidth)) > 0;

    private static bool[] PCompactCreate(IReadOnlyList<bool>? pCompactPanels, int pCount)
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

    private static double[] PWeightCreate(IReadOnlyList<double>? pStoredWidths, int pCount)
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

    private static ControlTemplate PThumbTemplateCreate()
    {
        var pBorder = new FrameworkElementFactory(typeof(Border));
        pBorder.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        return new ControlTemplate(typeof(Thumb))
        {
            VisualTree = pBorder
        };
    }
}
