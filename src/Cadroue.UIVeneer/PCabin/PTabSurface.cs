using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIVeneer.PBench;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PCabin;

public abstract class PTabSurface : UserControl
{
    private const double PTabMinimumDefault = 180;

    private sealed record PTabCollapse(
        Func<bool> PTabReader,
        Action<bool> PTabWriter,
        Action<Action<bool>> PTabListener);

    private static readonly IReadOnlyDictionary<Type, double> pTabMinimums = new Dictionary<Type, double>
    {
        [typeof(PExport)] = 300,
        [typeof(PViewer)] = 320,
        [typeof(PSection)] = 300,
        [typeof(PGroup)] = 260,
        [typeof(PList)] = 300,
        [typeof(PProcessing)] = 184,
        [typeof(PInspector)] = 360,
        [typeof(PClinic)] = 300,
    };

    private static readonly IReadOnlyDictionary<Type, bool> pTabCompacts = new Dictionary<Type, bool>
    {
        [typeof(PList)] = true,
        [typeof(PExport)] = true,
        [typeof(PProcessing)] = true,
        [typeof(PInspector)] = true,
        [typeof(PClinic)] = true,
        [typeof(PSection)] = true,
    };

    private static readonly IReadOnlyDictionary<Type, double> pTabStrips = new Dictionary<Type, double>
    {
        [typeof(PList)] = PList.PListStripWidth,
        [typeof(PProcessing)] = PProcessing.PProcessingStripWidth,
        [typeof(PInspector)] = PInspector.PInspectorStripWidth,
        [typeof(PClinic)] = PClinic.PClinicStripWidth,
        [typeof(PSection)] = PSection.PSectionStripWidth,
    };

    private static readonly IReadOnlyDictionary<Type, Func<UIElement, PTabCollapse>> pTabCollapses =
        new Dictionary<Type, Func<UIElement, PTabCollapse>>
        {
            [typeof(PList)] = pPanel => PTabCollapseCreate((PList)pPanel),
            [typeof(PProcessing)] = pPanel => PTabCollapseCreate((PProcessing)pPanel),
            [typeof(PInspector)] = pPanel => PTabCollapseCreate((PInspector)pPanel),
            [typeof(PClinic)] = pPanel => PTabCollapseCreate((PClinic)pPanel),
            [typeof(PSection)] = pPanel => PTabCollapseCreate((PSection)pPanel),
        };

    public abstract PFlow? PTabFlow { get; }
    public abstract PViewer? PTabViewer { get; }
    public virtual PList? PTabList => null;
    public virtual PGroup? PTabGroup => null;
    public virtual LStation? PTabStation => null;
    public PAction? PTabAction { get; protected set; }
    public LSurface LSurface { get; private set; } = null!;
    public virtual void PTabClose() => PTabList?.PListClose();
    public virtual void PTabStripAttach(LStrip lStrip, LStripTab lStripTab)
    {
    }

    public virtual void PTabTargetsResolve()
    {
    }

    public abstract LSceneTabRecord PTabLayoutRead();

    private PColumn PTabColumn { get; set; } = null!;
    private IReadOnlyList<UIElement> PTabPanels { get; set; } = Array.Empty<UIElement>();
    private UIElement? PTabExportPanel { get; set; }
    private UIElement? PTabExportSplitter { get; set; }
    private ColumnDefinition? PTabExportColumn { get; set; }

    public event Action? PTabWidthChange;

    public void PTabExportToggle()
    {
        PTabExportApply(LSurface.LSurfaceToggleResolve());
        PTabWidthRaise();
    }

    public virtual double PTabWidthRead() => LSurface.LSurfaceWidthResolve(PTabColumn.PColumnTotalRead());

    protected void PTabWidthRaise() => PTabWidthChange?.Invoke();

    protected LSceneTabRecord PTabLayoutCreate() => LSurface.LSurfaceLayoutRead();

    protected static void PTabLockAttach(PList pList, params UIElement[] pEditors)
    {
        Action<bool> pTabLockApply = pLocked => pEditors.ToList().ForEach(pEditor => pEditor.IsEnabled = !pLocked);
        pList.PListLockChange += pTabLockApply;
        pTabLockApply(pList.PListLockCheck());
    }

    protected Grid PTabGridBuild(
        IReadOnlyList<UIElement> pPanels,
        UIElement pCompass,
        PAction pAction,
        UIElement pFlow,
        LSceneTabRecord? lPreferenceTabLayout)
    {
        var pGrid = new Grid
        {
            Margin = new Thickness(8, 0, 8, 0),
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var pPanelGrid = new Grid
        {
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        List<ColumnDefinition> pColumnItems = pPanels.Select(PTabColumnBuild).ToList();
        List<ColumnDefinition> pSplitterColumns = pPanels.Select(PTabGutterBuild).ToList();
        IReadOnlyList<int> pPanelIndexes = LSurface.LSurfaceIndexesResolve(pPanels.Count);
        IReadOnlyList<int> pSplitterIndexes = LSurface.LSurfaceSplittersResolve(pPanels.Count);
        pPanelIndexes.ToList().ForEach(pIndex =>
        {
            pPanelGrid.ColumnDefinitions.Add(pColumnItems[pIndex]);
            pPanelGrid.ColumnDefinitions.Add(pSplitterColumns[pIndex]);
            Grid.SetColumn(pPanels[pIndex], LSurface.LSurfaceColumnResolve(pIndex));
            pPanelGrid.Children.Add(pPanels[pIndex]);
        });
        pPanelGrid.ColumnDefinitions.Remove(pSplitterColumns.Last());

        List<Type> pTypes = pPanels.Select(PTabTypeRead).ToList();
        int pExportIndex = pTypes.LastIndexOf(typeof(PExport));
        PTabColumn = PColumn.PColumnAttach(
            pPanelGrid,
            pColumnItems,
            lPreferenceTabLayout?.LScenePanelWidths,
            pPanels.Select(PTabCompactRead).ToList(),
            pTypes.IndexOf(typeof(PViewer)),
            PTabWidthRaise);
        List<UIElement> pSplitters = pSplitterIndexes
            .Select(PTabColumn.PColumnSplitterBuild)
            .Cast<UIElement>()
            .ToList();
        pSplitterIndexes.ToList().ForEach(pIndex =>
        {
            Grid.SetColumn(pSplitters[pIndex], LSurface.LSurfaceGutterResolve(pIndex));
            pPanelGrid.Children.Add(pSplitters[pIndex]);
        });

        Grid.SetRow(pPanelGrid, 0);
        pGrid.Children.Add(pPanelGrid);
        LSurface = new LSurface(PTabColumn.LColumn, pPanels.Count, pExportIndex);
        LSurface.LSurfaceActionAttach(pAction.LAction);
        PTabPanels = pPanels;
        PTabAction = pAction;
        PTabExportPanel = pPanels.ElementAtOrDefault(pExportIndex);
        PTabExportColumn = pSplitterColumns.ElementAtOrDefault(LSurface.LSurfaceLeftResolve(pExportIndex));
        PTabExportSplitter = pSplitters.ElementAtOrDefault(LSurface.LSurfaceLeftResolve(pExportIndex));
        pAction.PActionAutoApply(LSurface.LSurfaceAutoResolve(lPreferenceTabLayout));
        PTabExportApply(LSurface.LSurfaceExportResolve(lPreferenceTabLayout));
        pPanelIndexes.ToList().ForEach(PTabCollapseAttach);
        LSurface.LSurfaceCollapsedResolve(lPreferenceTabLayout).ToList().ForEach(PTabCollapseSet);

        var pActionRowContent = new Grid { MinHeight = 72 };
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionRowContent.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pAction, 1);
        pActionRowContent.Children.Add(pCompass);
        pActionRowContent.Children.Add(pAction);

        var pActionRowBox = new Border
        {
            Margin = new Thickness(8, 8, 8, 8),
            MinHeight = 74,
            Padding = new Thickness(10, 0, 10, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = System.Windows.Media.Brushes.White,
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xD7, 0xDF, 0xEA)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Child = pActionRowContent,
            SnapsToDevicePixels = true
        };
        Grid.SetRow(pActionRowBox, 1);
        Grid.SetRow(pFlow, 2);
        pGrid.Children.Add(pActionRowBox);
        pGrid.Children.Add(pFlow);
        return pGrid;
    }

    private void PTabExportApply(bool pHidden)
    {
        PTabColumn.PColumnHiddenSet(LSurface.LSurfaceExportIndex, pHidden);
        PTabExportPanel?.SetValue(VisibilityProperty, PLook.PLookVisible[!pHidden]);
        PTabExportColumn?.SetValue(ColumnDefinition.WidthProperty, PLook.PLookSplitterWidth[pHidden]);
        PTabExportSplitter?.SetValue(VisibilityProperty, PLook.PLookVisible[!pHidden]);
    }

    private void PTabCollapseAttach(int pIndex) =>
        PTabCollapseRead(PTabPanels[pIndex])?.PTabListener(
            pCollapsed => PTabCollapseApply(pIndex, pCollapsed, PTabStripRead(PTabPanels[pIndex])));

    private void PTabCollapseApply(int pIndex, bool pCollapsed, double pStripWidth)
    {
        PTabColumn.PColumnWidthSet(pIndex, LSurface.LSurfaceCollapseResolve(pCollapsed, pStripWidth));
        PTabWidthRaise();
    }

    private void PTabCollapseSet(int pIndex) => PTabCollapseRead(PTabPanels[pIndex])?.PTabWriter(true);

    private static PTabCollapse? PTabCollapseRead(UIElement pPanel) =>
        pTabCollapses.GetValueOrDefault(pPanel.GetType())?.Invoke(pPanel);

    private static double PTabStripRead(UIElement pPanel) => pTabStrips.GetValueOrDefault(pPanel.GetType(), 0);

    private static bool PTabCompactRead(UIElement pPanel) => pTabCompacts.GetValueOrDefault(pPanel.GetType(), false);

    private static Type PTabTypeRead(UIElement pPanel) => pPanel.GetType();

    private static ColumnDefinition PTabColumnBuild(UIElement pPanel) => new()
    {
        Width = new GridLength(1, GridUnitType.Star),
        MinWidth = LSurface.LSurfaceMinimumResolve(
            ((FrameworkElement)pPanel).MinWidth,
            pTabMinimums.GetValueOrDefault(pPanel.GetType(), PTabMinimumDefault))
    };

    private static ColumnDefinition PTabGutterBuild(UIElement pPanel) => new() { Width = new GridLength(6) };

    private static PTabCollapse PTabCollapseCreate(PList pPanel) =>
        new(pPanel.PListMinimizedCheck, pPanel.PListMinimizeSet, pHandler => pPanel.PListMinimizeChange += pHandler);

    private static PTabCollapse PTabCollapseCreate(PProcessing pPanel) =>
        new(
            pPanel.PProcessingMinimizedCheck,
            pPanel.PProcessingMinimizeSet,
            pHandler => pPanel.PProcessingMinimizeChange += pHandler);

    private static PTabCollapse PTabCollapseCreate(PInspector pPanel) =>
        new(
            pPanel.PInspectorMinimizedCheck,
            pPanel.PInspectorMinimizeSet,
            pHandler => pPanel.PInspectorMinimizeChange += pHandler);

    private static PTabCollapse PTabCollapseCreate(PClinic pPanel) =>
        new(
            pPanel.PClinicMinimizedCheck,
            pPanel.PClinicMinimizeSet,
            pHandler => pPanel.PClinicMinimizeChange += pHandler);

    private static PTabCollapse PTabCollapseCreate(PSection pPanel) =>
        new(
            pPanel.PSectionMinimizedCheck,
            pPanel.PSectionMinimizeSet,
            pHandler => pPanel.LSection.LSectionMinimizeChange += pHandler);
}
