using System.Windows.Controls;

namespace Cadroue.UIShell.PDeck;

internal sealed partial class PColumn
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
    private readonly Action? pColumnWidthNotify;
    private double pColumnAppliedWidth = -1;
    private bool pColumnDefaultsPending;
    private bool pColumnPixelsReady;

    private PColumn(
        Grid pColumnGrid,
        IReadOnlyList<ColumnDefinition> pColumnItems,
        IReadOnlyList<double>? pStoredWidths,
        IReadOnlyList<bool>? pCompactPanels,
        int pFlexPanelIndex,
        Action? pWidthNotify)
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
        pColumnWidthNotify = pWidthNotify;
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
        int pFlexPanelIndex = -1,
        Action? pWidthNotify = null)
    {
        return new PColumn(pColumnGrid, pColumnItems, pStoredWidths, pCompactPanels, pFlexPanelIndex, pWidthNotify);
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

    private bool PColumnFlexCheck() =>
        pColumnFlexIndex >= 0 && !pColumnHiddenFlags[pColumnFlexIndex] && pColumnFixedWidths[pColumnFlexIndex] <= 0;

    private static bool PColumnStoredCheck(IReadOnlyList<double>? pStoredWidths, int pCount) =>
        pStoredWidths is not null
        && pStoredWidths.Count == pCount
        && pStoredWidths.Sum(pWidth => Math.Max(0, pWidth)) > 0;

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
}
