using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PColumn
{
    private static readonly IReadOnlyDictionary<bool, GridUnitType> pColumnUnits = new Dictionary<bool, GridUnitType>
    {
        [true] = GridUnitType.Star,
        [false] = GridUnitType.Pixel,
    };

    private readonly Grid pColumnGrid;
    private readonly IReadOnlyList<ColumnDefinition> pColumnItems;
    private readonly QColumnPlan lColumnPlan;

    private PColumn(
        Grid pColumnGrid,
        IReadOnlyList<ColumnDefinition> pColumnItems,
        QColumnPlan lColumnPlan,
        Action? pWidthNotify)
    {
        this.pColumnGrid = pColumnGrid;
        this.pColumnItems = pColumnItems;
        this.lColumnPlan = lColumnPlan;
        PColumnEventsAttach(pWidthNotify);
        PColumnPlanApply();
    }

    public static PColumn PColumnAttach(
        Grid pColumnGrid,
        IReadOnlyList<ColumnDefinition> pColumnItems,
        QColumnPlan lColumnPlan,
        Action? pWidthNotify = null)
    {
        return new PColumn(pColumnGrid, pColumnItems, lColumnPlan, pWidthNotify);
    }

    public static double PColumnMinimumRead(ColumnDefinition pColumn) => pColumn.MinWidth;

    public void PColumnWidthSet(int pPanelIndex, double pPanelFixedWidth) =>
        lColumnPlan.QColumnWidthSet(pPanelIndex, pPanelFixedWidth);

    public void PColumnHiddenSet(int pPanelIndex, bool pPanelHidden) =>
        lColumnPlan.QColumnHiddenSet(pPanelIndex, pPanelHidden);

    public double PColumnTotalRead() => lColumnPlan.QColumnTotalRead();

    public Thumb PColumnSplitterBuild(int pLeftPanelIndex)
    {
        Thumb pThumb = PColumnGripBuild();
        PColumnDragAttach(pThumb, pLeftPanelIndex);
        return pThumb;
    }

    private void PColumnEventsAttach(Action? pWidthNotify)
    {
        lColumnPlan.QColumnPlanChanged += PColumnPlanApply;
        lColumnPlan.QColumnWidthChanged += pWidthNotify;
        pColumnGrid.LayoutUpdated += PColumnLayoutHandle;
    }

    private static Thumb PColumnGripBuild() => new()
    {
        Cursor = Cursors.SizeWE,
        Background = Brushes.Transparent,
        Focusable = false,
        Template = PColumnThumbCreate()
    };

    private void PColumnDragAttach(Thumb pThumb, int pLeftPanelIndex) =>
        pThumb.DragDelta += (_, pEvent) => PColumnDragHandle(pLeftPanelIndex, pEvent.HorizontalChange);

    private void PColumnLayoutHandle(object? pSender, EventArgs pEvent) =>
        lColumnPlan.QColumnLayoutHandle(PColumnSlotRead(), pColumnGrid.ActualWidth);

    private void PColumnDragHandle(int pLeftPanelIndex, double pDelta) =>
        lColumnPlan.QColumnDragHandle(
            pLeftPanelIndex,
            pDelta,
            PColumnSlotRead(),
            pColumnGrid.ActualWidth,
            pColumnItems.Select(PColumnActualRead).ToList());

    private void PColumnPlanApply() =>
        pColumnItems
            .Zip(lColumnPlan.QColumnSlotsResolve())
            .ToList()
            .ForEach(pPair => PColumnSlotApply(pPair.First, pPair.Second));

    private static void PColumnSlotApply(ColumnDefinition pColumn, QColumnSlot lSlot)
    {
        pColumn.MinWidth = lSlot.QColumnSlotMinimum;
        pColumn.Width = PColumnLengthResolve(lSlot);
    }

    private static GridLength PColumnLengthResolve(QColumnSlot lSlot) =>
        new(lSlot.QColumnSlotWidth, pColumnUnits[lSlot.QColumnSlotStar]);

    private double PColumnSlotRead() => LayoutInformation.GetLayoutSlot(pColumnGrid).Width;

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
