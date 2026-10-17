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
    public QSurface QSurface { get; private set; } = null!;
    public virtual void PTabClose() => PTabList?.PListClose();
    public virtual void PTabStripAttach(QStrip lStrip, QStripTab lStripTab)
    {
    }

    public virtual void PTabTargetsResolve()
    {
    }

    public abstract LSceneTabEntry PTabLayoutRead();

    private PColumn PTabColumn { get; set; } = null!;
    private IReadOnlyList<UIElement> PTabPanels { get; set; } = Array.Empty<UIElement>();
    private UIElement? PTabExportPanel { get; set; }
    private UIElement? PTabExportSplitter { get; set; }
    private ColumnDefinition? PTabExportColumn { get; set; }

    public event Action? PTabWidthChanged;

    public void PTabExportToggle()
    {
        PTabExportApply(QSurface.QSurfaceToggleResolve());
        PTabWidthRaise();
    }

    public virtual double PTabWidthRead() => QSurface.QSurfaceWidthResolve(PTabColumn.PColumnTotalRead());

    protected void PTabWidthRaise() => PTabWidthChanged?.Invoke();

    protected LSceneTabEntry PTabLayoutCreate() => QSurface.QSurfaceLayoutRead();

    protected static void PTabLockAttach(PList pList, params UIElement[] pEditors)
    {
        Action<bool> pTabLockApply = pLocked => PTabLockApply(pEditors, pLocked);
        PTabListAttach(pList, pTabLockApply);
        pTabLockApply(pList.QList.QListLockCheck());
    }

    private static void PTabLockApply(UIElement[] pEditors, bool pLocked) =>
        pEditors.ToList().ForEach(pEditor => pEditor.IsEnabled = !pLocked);

    private static void PTabListAttach(PList pList, Action<bool> pTabLockApply) =>
        pList.QList.QListLockChanged += pTabLockApply;

    protected Grid PTabGridBuild(
        QWorkspaceMode qWorkspaceMode,
        IReadOnlyList<UIElement> pPanels,
        UIElement pCompass,
        PAction pAction,
        UIElement pFlow,
        LSceneTabEntry? lPreferenceTabLayout)
    {
        Grid pGrid = PTabFrameBuild();
        Grid pPanelGrid = PTabBodyBuild();
        List<ColumnDefinition> pColumnItems = pPanels.Select(PTabColumnBuild).ToList();
        List<ColumnDefinition> pSplitterColumns = pPanels.Select(PTabGutterBuild).ToList();
        IReadOnlyList<int> pPanelIndexes = QSurface.QSurfaceIndexesResolve(pPanels.Count);
        IReadOnlyList<int> pSplitterIndexes = QSurface.QSurfaceSplittersResolve(pPanels.Count);
        PTabPanelsAdd(pPanelGrid, pPanels, pColumnItems, pSplitterColumns, pPanelIndexes);

        List<Type> pTypes = pPanels.Select(PTabTypeRead).ToList();
        int pExportIndex = pTypes.LastIndexOf(typeof(PExport));
        PTabSurfaceSet(qWorkspaceMode.QSurfaceCreate(
            pColumnItems.Select(PColumn.PColumnMinimumRead).ToArray(),
            lPreferenceTabLayout?.LScenePanelWidths,
            pPanels.Select(PTabCompactRead).ToList(),
            pTypes.IndexOf(typeof(PViewer)),
            pExportIndex));
        PTabColumnSet(PColumn.PColumnAttach(pPanelGrid, pColumnItems, QSurface.QSurfacePlan, PTabWidthRaise));
        List<UIElement> pSplitters = pSplitterIndexes
            .Select(PTabColumn.PColumnSplitterBuild)
            .Cast<UIElement>()
            .ToList();
        PTabSplittersAdd(pPanelGrid, pSplitters, pSplitterIndexes);

        PTabBodyAdd(pGrid, pPanelGrid);
        PTabPanelsSet(pPanels, pSplitterColumns, pSplitters, pExportIndex);
        pAction.PActionAutoApply(QSurface.QSurfaceAutoResolve(lPreferenceTabLayout));
        PTabExportApply(QSurface.QSurfaceExportResolve(lPreferenceTabLayout));
        pPanelIndexes.ToList().ForEach(PTabCollapseAttach);
        QSurface.QSurfaceCollapsedResolve(lPreferenceTabLayout).ToList().ForEach(PTabCollapseSet);
        PTabActionAdd(pGrid, pCompass, pAction, pFlow);
        return pGrid;
    }

    private static Grid PTabFrameBuild()
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
        return pGrid;
    }

    private static Grid PTabBodyBuild() => new()
    {
        ClipToBounds = true,
        HorizontalAlignment = HorizontalAlignment.Stretch
    };

    private static void PTabPanelsAdd(
        Grid pPanelGrid,
        IReadOnlyList<UIElement> pPanels,
        List<ColumnDefinition> pColumnItems,
        List<ColumnDefinition> pSplitterColumns,
        IReadOnlyList<int> pPanelIndexes)
    {
        pPanelIndexes.ToList().ForEach(pIndex =>
        {
            pPanelGrid.ColumnDefinitions.Add(pColumnItems[pIndex]);
            pPanelGrid.ColumnDefinitions.Add(pSplitterColumns[pIndex]);
            Grid.SetColumn(pPanels[pIndex], QSurface.QSurfaceColumnResolve(pIndex));
            pPanelGrid.Children.Add(pPanels[pIndex]);
        });
        pPanelGrid.ColumnDefinitions.Remove(pSplitterColumns.Last());
    }

    private void PTabSurfaceSet(QSurface lSurface) => QSurface = lSurface;

    private void PTabColumnSet(PColumn pColumn) => PTabColumn = pColumn;

    private static void PTabSplittersAdd(
        Grid pPanelGrid,
        List<UIElement> pSplitters,
        IReadOnlyList<int> pSplitterIndexes) =>
        pSplitterIndexes.ToList().ForEach(pIndex =>
        {
            Grid.SetColumn(pSplitters[pIndex], QSurface.QSurfaceGutterResolve(pIndex));
            pPanelGrid.Children.Add(pSplitters[pIndex]);
        });

    private static void PTabBodyAdd(Grid pGrid, Grid pPanelGrid)
    {
        Grid.SetRow(pPanelGrid, 0);
        pGrid.Children.Add(pPanelGrid);
    }

    private void PTabPanelsSet(
        IReadOnlyList<UIElement> pPanels,
        List<ColumnDefinition> pSplitterColumns,
        List<UIElement> pSplitters,
        int pExportIndex)
    {
        PTabPanels = pPanels;
        PTabExportPanel = pPanels.ElementAtOrDefault(pExportIndex);
        PTabExportColumn = pSplitterColumns.ElementAtOrDefault(QSurface.QSurfaceLeftResolve(pExportIndex));
        PTabExportSplitter = pSplitters.ElementAtOrDefault(QSurface.QSurfaceLeftResolve(pExportIndex));
    }

    private static void PTabActionAdd(Grid pGrid, UIElement pCompass, PAction pAction, UIElement pFlow)
    {
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
    }

    private void PTabExportApply(bool pHidden)
    {
        PTabColumn.PColumnHiddenSet(QSurface.QSurfaceExportIndex, pHidden);
        PTabExportPanel?.SetValue(VisibilityProperty, PLook.PLookVisible[!pHidden]);
        PTabExportColumn?.SetValue(ColumnDefinition.WidthProperty, PLook.PLookSplitterWidth[pHidden]);
        PTabExportSplitter?.SetValue(VisibilityProperty, PLook.PLookVisible[!pHidden]);
    }

    private void PTabCollapseAttach(int pIndex) =>
        PTabCollapseRead(PTabPanels[pIndex])?.PTabListener(
            pCollapsed => PTabCollapseApply(pIndex, pCollapsed, PTabStripRead(PTabPanels[pIndex])));

    private void PTabCollapseApply(int pIndex, bool pCollapsed, double pStripWidth)
    {
        PTabColumn.PColumnWidthSet(pIndex, QSurface.QSurfaceCollapseResolve(pCollapsed, pStripWidth));
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
        MinWidth = QSurface.QSurfaceMinimumResolve(
            ((FrameworkElement)pPanel).MinWidth,
            pTabMinimums.GetValueOrDefault(pPanel.GetType(), PTabMinimumDefault))
    };

    private static ColumnDefinition PTabGutterBuild(UIElement pPanel) => new() { Width = new GridLength(6) };

    private static PTabCollapse PTabCollapseCreate(PList pPanel) =>
        new(
            pPanel.PListMinimizedCheck,
            pPanel.PListMinimizeSet,
            pHandler => pPanel.QList.QListMinimizeChanged += pHandler);

    private static PTabCollapse PTabCollapseCreate(PProcessing pPanel) =>
        new(
            pPanel.PProcessingMinimizedCheck,
            pPanel.PProcessingMinimizeSet,
            pHandler => pPanel.PProcessingMinimizeChanged += pHandler);

    private static PTabCollapse PTabCollapseCreate(PInspector pPanel) =>
        new(
            pPanel.PInspectorMinimizedCheck,
            pPanel.PInspectorMinimizeSet,
            pHandler => pPanel.QInspector.QInspectorMinimizeChanged += pHandler);

    private static PTabCollapse PTabCollapseCreate(PClinic pPanel) =>
        new(
            pPanel.PClinicMinimizedCheck,
            pPanel.PClinicMinimizeSet,
            pHandler => pPanel.PClinicMinimizeChanged += pHandler);

    private static PTabCollapse PTabCollapseCreate(PSection pPanel) =>
        new(
            pPanel.PSectionMinimizedCheck,
            pPanel.PSectionMinimizeSet,
            pHandler => pPanel.QSection.QSectionMinimizeChanged += pHandler);
}
