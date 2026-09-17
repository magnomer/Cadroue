using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PDeck;

internal sealed partial class PColumn
{
    private const double pColumnSplitterWidth = 6;

    private readonly Grid pColumnGrid;
    private readonly IReadOnlyList<ColumnDefinition> pColumnItems;
    private readonly Action? pColumnWidthNotify;

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
        LColumn = new LColumn(
            pColumnItems.Select(pColumn => pColumn.MinWidth).ToArray(),
            pStoredWidths,
            pCompactPanels,
            pFlexPanelIndex);
        pColumnWidthNotify = pWidthNotify;

        pColumnGrid.LayoutUpdated += (_, _) => PColumnMinimumApply();
        PColumnWeightsApply();
    }

    public LColumn LColumn { get; }

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
        if (LColumn.LColumnWidthSet(pPanelIndex, pPanelFixedWidth))
        {
            PColumnWeightsApply();
        }
    }

    public void PColumnHide(int pPanelIndex)
    {
        if (!LColumn.LColumnHiddenSet(pPanelIndex, true))
        {
            return;
        }

        pColumnItems[pPanelIndex].MinWidth = 0;
        PColumnWeightsApply();
    }

    public void PColumnShow(int pPanelIndex)
    {
        if (LColumn.LColumnHiddenSet(pPanelIndex, false))
        {
            PColumnWeightsApply();
        }
    }

    public IReadOnlyList<double> PColumnWeightsRead() => LColumn.LColumnWeightsRead();

    public double PColumnTotalRead() => LColumn.LColumnTotalRead(pColumnSplitterWidth);
}
