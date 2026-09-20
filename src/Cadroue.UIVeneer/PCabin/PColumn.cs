using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PCabin;

internal sealed class PColumn
{
    private static readonly IReadOnlyDictionary<bool, GridUnitType> pColumnUnits = new Dictionary<bool, GridUnitType>
    {
        [true] = GridUnitType.Star,
        [false] = GridUnitType.Pixel,
    };

    private readonly Grid pColumnGrid;
    private readonly IReadOnlyList<ColumnDefinition> pColumnItems;
    private readonly LColumnPlan lColumnPlan;

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
        lColumnPlan = new LColumnPlan(
            pColumnItems.Select(PColumnMinimumRead).ToArray(),
            pStoredWidths,
            pCompactPanels,
            pFlexPanelIndex);
        lColumnPlan.LColumnPlanChange += PColumnPlanApply;
        lColumnPlan.LColumnWidthChange += pWidthNotify;
        pColumnGrid.LayoutUpdated += PColumnLayoutHandle;
        PColumnPlanApply();
    }

    public LColumn LColumn => lColumnPlan.LColumn;

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

    public void PColumnWidthSet(int pPanelIndex, double pPanelFixedWidth) =>
        lColumnPlan.LColumnWidthSet(pPanelIndex, pPanelFixedWidth);

    public void PColumnHiddenSet(int pPanelIndex, bool pPanelHidden) =>
        lColumnPlan.LColumnHiddenSet(pPanelIndex, pPanelHidden);

    public IReadOnlyList<double> PColumnWeightsRead() => lColumnPlan.LColumnWeightsRead();

    public double PColumnTotalRead() => lColumnPlan.LColumnTotalRead();

    public Thumb PColumnSplitterBuild(int pLeftPanelIndex)
    {
        var pThumb = new Thumb
        {
            Cursor = Cursors.SizeWE,
            Background = Brushes.Transparent,
            Focusable = false,
            Template = PColumnThumbCreate()
        };
        pThumb.DragDelta += (_, pEvent) => PColumnDragHandle(pLeftPanelIndex, pEvent.HorizontalChange);
        return pThumb;
    }

    private void PColumnLayoutHandle(object? pSender, EventArgs pEvent) =>
        lColumnPlan.LColumnLayoutHandle(PColumnSlotRead(), pColumnGrid.ActualWidth);

    private void PColumnDragHandle(int pLeftPanelIndex, double pDelta) =>
        lColumnPlan.LColumnDragHandle(
            pLeftPanelIndex,
            pDelta,
            PColumnSlotRead(),
            pColumnGrid.ActualWidth,
            pColumnItems.Select(PColumnActualRead).ToList());

    private void PColumnPlanApply() =>
        pColumnItems
            .Zip(lColumnPlan.LColumnSlotsResolve())
            .ToList()
            .ForEach(pPair => PColumnSlotApply(pPair.First, pPair.Second));

    private static void PColumnSlotApply(ColumnDefinition pColumn, LColumnSlot lSlot)
    {
        pColumn.MinWidth = lSlot.LColumnSlotMinimum;
        pColumn.Width = new GridLength(lSlot.LColumnSlotWidth, pColumnUnits[lSlot.LColumnSlotStar]);
    }

    private double PColumnSlotRead() => LayoutInformation.GetLayoutSlot(pColumnGrid).Width;

    private static double PColumnMinimumRead(ColumnDefinition pColumn) => pColumn.MinWidth;

    private static double PColumnActualRead(ColumnDefinition pColumn) => pColumn.ActualWidth;

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
